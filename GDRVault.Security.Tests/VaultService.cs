using GDRVault.Core;
using GDRVault.Security;
using System.Text.Json;

namespace GDRVault.Storage;

public class VaultService
{
    private readonly VaultDatabase _database;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public VaultService(VaultDatabase database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
    }

    public async Task<bool> VaultExistsAsync()
    {
        return await _database.ExistsAsync();
    }

    public async Task CreateVaultAsync(
        VaultDocument vault,
        string masterPassword)
    {
        ArgumentNullException.ThrowIfNull(vault);
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);

        if (await _database.ExistsAsync())
        {
            throw new InvalidOperationException(
                "Un vault esiste già.");
        }

        vault.Version = 1;
        vault.CreatedAt = DateTime.UtcNow;
        vault.ModifiedAt = DateTime.UtcNow;

        await SaveInternalAsync(
            vault,
            masterPassword);
    }

    public async Task SaveVaultAsync(
        VaultDocument vault,
        string masterPassword)
    {
        ArgumentNullException.ThrowIfNull(vault);
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);

        if (!await _database.ExistsAsync())
        {
            throw new InvalidOperationException(
                "Il vault non esiste ancora.");
        }

        vault.ModifiedAt = DateTime.UtcNow;

        await SaveInternalAsync(
            vault,
            masterPassword);
    }

    public async Task<VaultDocument> OpenVaultAsync(
        string masterPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);

        byte[]? encryptedData =
            await _database.LoadAsync();

        if (encryptedData is null)
        {
            throw new InvalidOperationException(
                "Il vault non esiste.");
        }

        string json =
            await VaultCrypto.DecryptAsync(
                encryptedData,
                masterPassword);

        VaultDocument? vault =
            JsonSerializer.Deserialize<VaultDocument>(
                json,
                JsonOptions);

        if (vault is null)
        {
            throw new InvalidDataException(
                "Il contenuto del vault non è valido.");
        }

        return vault;
    }

    private async Task SaveInternalAsync(
        VaultDocument vault,
        string masterPassword)
    {
        string json =
            JsonSerializer.Serialize(
                vault,
                JsonOptions);

        byte[] encryptedData =
            await VaultCrypto.EncryptAsync(
                json,
                masterPassword);

        await _database.SaveAsync(
            encryptedData,
            vault.Version,
            vault.CreatedAt,
            vault.ModifiedAt);
    }
}