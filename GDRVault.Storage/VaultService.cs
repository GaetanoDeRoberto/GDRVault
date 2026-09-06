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

    public async Task ChangeMasterPasswordAsync(
    VaultDocument vault,
    string currentPassword,
    string newPassword)
    {
        ArgumentNullException.ThrowIfNull(vault);
        ArgumentException.ThrowIfNullOrEmpty(currentPassword);
        ArgumentException.ThrowIfNullOrEmpty(newPassword);

        // Verifica che la password attuale sia realmente quella del Vault.
        byte[]? encryptedData =
            await _database.LoadAsync();

        if (encryptedData is null)
        {
            throw new InvalidOperationException(
                "Il Vault non esiste.");
        }

        await DecryptVaultAsync(
            encryptedData,
            currentPassword);

        // Ricifra l'intero Vault con la nuova password.
        vault.ModifiedAt = DateTime.UtcNow;

        await SaveInternalAsync(
            vault,
            newPassword);
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

        return await DecryptVaultAsync(
            encryptedData,
            masterPassword);
    }

    public async Task ExportBackupAsync(
        string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        byte[]? encryptedData =
            await _database.LoadAsync();

        if (encryptedData is null)
        {
            throw new InvalidOperationException(
                "Il vault non esiste.");
        }

        await File.WriteAllBytesAsync(
            filePath,
            encryptedData);
    }

    public async Task ImportBackupAsync(
        string filePath,
        string masterPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Il file di backup non esiste.",
                filePath);
        }

        byte[] encryptedData =
            await File.ReadAllBytesAsync(filePath);

        if (encryptedData.Length == 0)
        {
            throw new InvalidDataException(
                "Il file di backup è vuoto.");
        }

        // Prima verifichiamo che il backup sia valido
        // e che la Master Password sia corretta.
        VaultDocument vault =
            await DecryptVaultAsync(
                encryptedData,
                masterPassword);

        // Solo dopo la verifica sostituiamo il Vault.
        vault.ModifiedAt = DateTime.UtcNow;

        await _database.SaveAsync(
            encryptedData,
            vault.Version,
            vault.CreatedAt,
            vault.ModifiedAt);
    }

    private async Task<VaultDocument> DecryptVaultAsync(
        byte[] encryptedData,
        string masterPassword)
    {
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
                "Il contenuto del Vault non è valido.");
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