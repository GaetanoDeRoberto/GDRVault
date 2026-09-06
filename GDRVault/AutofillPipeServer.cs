using GDRVault.Core;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace GDRVault;

/// <summary>
/// Server locale utilizzato da GDRVault.Bridge per fornire le credenziali
/// all'estensione del browser quando il Vault è sbloccato.
/// </summary>
internal sealed class AutofillPipeServer : IDisposable
{
    private const string PipeName = "GDRVault.Autofill";
    private const int MaxMessageSize = 1024 * 1024;

    private readonly VaultDocument _vault;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private Task? _serverTask;
    private bool _disposed;

    public AutofillPipeServer(VaultDocument vault)
    {
        _vault = vault ?? throw new ArgumentNullException(nameof(vault));
    }

    public void Start()
    {
        if (_serverTask is not null)
            return;

        _serverTask = Task.Run(RunAsync);
    }

    private async Task RunAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                await pipe.WaitForConnectionAsync(_cancellation.Token);

                string requestJson = await ReadLengthPrefixedJsonAsync(
                    pipe,
                    _cancellation.Token);

                PipeRequest? request;

                try
                {
                    request = JsonSerializer.Deserialize<PipeRequest>(
                        requestJson,
                        _jsonOptions);
                }
                catch (JsonException)
                {
                    await WriteLengthPrefixedJsonAsync(
                        pipe,
                        new PipeResponse
                        {
                            Success = false,
                            Error = "Richiesta JSON non valida."
                        },
                        _cancellation.Token);

                    continue;
                }

                PipeResponse response = HandleRequest(request);

                await WriteLengthPrefixedJsonAsync(
                    pipe,
                    response,
                    _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException)
            {
                // La pipe può essere chiusa dal client. Ripartiamo in ascolto.
            }
            catch (Exception)
            {
                // Un errore del singolo client non deve terminare il server.
            }
        }
    }

    private PipeResponse HandleRequest(PipeRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Action))
        {
            return new PipeResponse
            {
                Success = false,
                Error = "Azione non specificata."
            };
        }

        switch (request.Action.Trim().ToLowerInvariant())
        {
            case "ping":
                return new PipeResponse
                {
                    Success = true,
                    Action = "pong"
                };

            case "get_credentials":
                return GetCredentials(request.Domain);

            default:
                return new PipeResponse
                {
                    Success = false,
                    Error = $"Azione non supportata: {request.Action}"
                };
        }
    }

    private PipeResponse GetCredentials(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return new PipeResponse
            {
                Success = false,
                Error = "Dominio non specificato."
            };
        }

        string? requestedHost = NormalizeHost(domain);

        if (requestedHost is null)
        {
            return new PipeResponse
            {
                Success = false,
                Error = "Dominio non valido."
            };
        }

        var credentials = _vault.Entries
            .Where(entry => HostsMatch(entry.Url, requestedHost))
            .Select(entry => new CredentialDto
            {
                Id = entry.Id,
                Title = entry.Title,
                Username = entry.Username,
                Password = entry.Password,
                Url = entry.Url
            })
            .ToList();

        return new PipeResponse
        {
            Success = true,
            Credentials = credentials
        };
    }

    private static bool HostsMatch(string? entryUrl, string requestedHost)
    {
        string? entryHost = NormalizeHost(entryUrl);

        if (entryHost is null)
            return false;

        return string.Equals(
            entryHost,
            requestedHost,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string candidate = value.Trim();

        if (!candidate.Contains("://", StringComparison.Ordinal))
            candidate = "https://" + candidate;

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri))
            return null;

        if (string.IsNullOrWhiteSpace(uri.Host))
            return null;

        return uri.Host.TrimEnd('.').ToLowerInvariant();
    }

    private static async Task<string> ReadLengthPrefixedJsonAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        byte[] header = new byte[4];

        await ReadExactlyAsync(
            stream,
            header,
            cancellationToken);

        uint length = BinaryPrimitives.ReadUInt32LittleEndian(header);

        if (length == 0 || length > MaxMessageSize)
            throw new InvalidDataException("Messaggio Bridge non valido.");

        byte[] payload = new byte[length];

        await ReadExactlyAsync(
            stream,
            payload,
            cancellationToken);

        return Encoding.UTF8.GetString(payload);
    }

    private static async Task WriteLengthPrefixedJsonAsync(
        Stream stream,
        object value,
        CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(value);
        byte[] payload = Encoding.UTF8.GetBytes(json);

        if (payload.Length > MaxMessageSize)
            throw new InvalidDataException("Risposta troppo grande.");

        byte[] header = new byte[4];

        BinaryPrimitives.WriteUInt32LittleEndian(
            header,
            (uint)payload.Length);

        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task ReadExactlyAsync(
        Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        int offset = 0;

        while (offset < buffer.Length)
        {
            int read = await stream.ReadAsync(
                buffer.AsMemory(offset),
                cancellationToken);

            if (read == 0)
                throw new EndOfStreamException();

            offset += read;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cancellation.Cancel();

        _cancellation.Dispose();
    }

    private sealed class PipeRequest
    {
        public string Action { get; set; } = string.Empty;

        public string Domain { get; set; } = string.Empty;
    }

    private sealed class PipeResponse
    {
        public bool Success { get; set; }

        public string? Action { get; set; }

        public string? Error { get; set; }

        public List<CredentialDto>? Credentials { get; set; }
    }

    private sealed class CredentialDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;
    }
}
