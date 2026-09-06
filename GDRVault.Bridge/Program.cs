using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace GDRVault.Bridge;

internal static class Program
{
    private const int MaxMessageSize = 1024 * 1024;
    private const string PipeName = "GDRVault.Autofill";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static async Task Main()
    {
        try
        {
            while (true)
            {
                string? message = await ReadNativeMessageAsync(Console.OpenStandardInput());

                if (message is null)
                    break;

                BridgeRequest? request;

                try
                {
                    request = JsonSerializer.Deserialize<BridgeRequest>(
                        message,
                        JsonOptions);
                }
                catch (JsonException)
                {
                    await WriteNativeMessageAsync(new BridgeResponse
                    {
                        Success = false,
                        Error = "Richiesta JSON non valida."
                    });

                    continue;
                }

                if (request is null)
                {
                    await WriteNativeMessageAsync(new BridgeResponse
                    {
                        Success = false,
                        Error = "Richiesta vuota."
                    });

                    continue;
                }

                BridgeResponse response = await HandleRequestAsync(request);

                await WriteNativeMessageAsync(response);
            }
        }
        catch
        {
            // Il processo Bridge non deve scrivere eccezioni su stdout:
            // stdout è riservato esclusivamente al protocollo Native Messaging.
        }
    }

    private static async Task<BridgeResponse> HandleRequestAsync(
        BridgeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Action))
        {
            return new BridgeResponse
            {
                Success = false,
                Error = "Azione non specificata."
            };
        }

        switch (request.Action.Trim().ToLowerInvariant())
        {
            case "ping":
                return new BridgeResponse
                {
                    Success = true,
                    Action = "pong"
                };

            case "get_credentials":
                return await GetCredentialsAsync(request);

            default:
                return new BridgeResponse
                {
                    Success = false,
                    Error = $"Azione non supportata: {request.Action}"
                };
        }
    }

    private static async Task<BridgeResponse> GetCredentialsAsync(
        BridgeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Domain))
        {
            return new BridgeResponse
            {
                Success = false,
                Error = "Dominio non specificato."
            };
        }

        var pipeRequest = new PipeRequest
        {
            Action = "get_credentials",
            Domain = request.Domain.Trim()
        };

        string pipeResponse;

        try
        {
            using var pipe = new NamedPipeClientStream(
                ".",
                PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            await pipe.ConnectAsync(1500);

            await WriteLengthPrefixedJsonAsync(pipe, pipeRequest);

            pipeResponse = await ReadLengthPrefixedJsonAsync(pipe);
        }
        catch (TimeoutException)
        {
            return new BridgeResponse
            {
                Success = false,
                Error = "GDRVault non è disponibile o non è sbloccato."
            };
        }
        catch (IOException)
        {
            return new BridgeResponse
            {
                Success = false,
                Error = "Impossibile comunicare con GDRVault."
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new BridgeResponse
            {
                Success = false,
                Error = "Accesso al canale GDRVault non autorizzato."
            };
        }

        try
        {
            return JsonSerializer.Deserialize<BridgeResponse>(
                       pipeResponse,
                       JsonOptions)
                   ?? new BridgeResponse
                   {
                       Success = false,
                       Error = "Risposta di GDRVault non valida."
                   };
        }
        catch (JsonException)
        {
            return new BridgeResponse
            {
                Success = false,
                Error = "Risposta di GDRVault non valida."
            };
        }
    }

    private static async Task<string?> ReadNativeMessageAsync(Stream input)
    {
        byte[] header = new byte[4];

        int firstByte = await input.ReadAsync(header.AsMemory(0, 1));

        if (firstByte == 0)
            return null;

        await ReadExactlyAsync(input, header.AsMemory(1, 3));

        uint length = BinaryPrimitives.ReadUInt32LittleEndian(header);

        if (length == 0)
            return string.Empty;

        if (length > MaxMessageSize)
            throw new InvalidDataException("Messaggio Native Messaging troppo grande.");

        byte[] payload = new byte[length];

        await ReadExactlyAsync(input, payload);

        return Encoding.UTF8.GetString(payload);
    }

    private static async Task WriteNativeMessageAsync(
        BridgeResponse response)
    {
        string json = JsonSerializer.Serialize(response, JsonOptions);
        byte[] payload = Encoding.UTF8.GetBytes(json);

        if (payload.Length > MaxMessageSize)
            throw new InvalidDataException("Risposta troppo grande.");

        byte[] header = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(
            header,
            (uint)payload.Length);

        Stream output = Console.OpenStandardOutput();

        await output.WriteAsync(header);
        await output.WriteAsync(payload);
        await output.FlushAsync();
    }

    private static async Task WriteLengthPrefixedJsonAsync(
        Stream stream,
        object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        byte[] payload = Encoding.UTF8.GetBytes(json);

        if (payload.Length > MaxMessageSize)
            throw new InvalidDataException("Messaggio troppo grande.");

        byte[] header = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(
            header,
            (uint)payload.Length);

        await stream.WriteAsync(header);
        await stream.WriteAsync(payload);
        await stream.FlushAsync();
    }

    private static async Task<string> ReadLengthPrefixedJsonAsync(
        Stream stream)
    {
        byte[] header = new byte[4];

        await ReadExactlyAsync(stream, header);

        uint length = BinaryPrimitives.ReadUInt32LittleEndian(header);

        if (length == 0 || length > MaxMessageSize)
            throw new InvalidDataException("Messaggio Bridge non valido.");

        byte[] payload = new byte[length];

        await ReadExactlyAsync(stream, payload);

        return Encoding.UTF8.GetString(payload);
    }

    private static async Task ReadExactlyAsync(
        Stream stream,
        Memory<byte> buffer)
    {
        while (!buffer.IsEmpty)
        {
            int read = await stream.ReadAsync(buffer);

            if (read == 0)
                throw new EndOfStreamException();

            buffer = buffer[read..];
        }
    }
}

internal sealed class BridgeRequest
{
    public string Action { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;
}

internal sealed class PipeRequest
{
    public string Action { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;
}

internal sealed class BridgeResponse
{
    public bool Success { get; set; }

    public string? Action { get; set; }

    public string? Error { get; set; }

    public List<CredentialDto>? Credentials { get; set; }
}

internal sealed class CredentialDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
}
