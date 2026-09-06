using GDRVault.Core;
using GDRVault.Storage;
using System.Security.Cryptography;

namespace GDRVault.Security.Tests;

public class VaultServiceTests
{
    [Fact]
    public async Task CreateSaveAndOpenVault_ShouldPreserveData()
    {
        string databasePath =
            Path.Combine(
                Path.GetTempPath(),
                $"gdrvault-test-{Guid.NewGuid():N}.db");

        const string masterPassword =
            "MasterPassword-123!";

        try
        {
            // 1. Creiamo il database
            var database =
                new VaultDatabase(databasePath);

            var service =
                new VaultService(database);

            await service.InitializeAsync();

            // 2. Creiamo il vault
            var vault = new VaultDocument();

            vault.Entries.Add(
                new VaultEntry
                {
                    Title = "GitHub",
                    Username = "utente@example.com",
                    Password = "PasswordSegreta-123!",
                    Url = "https://github.com",
                    Notes = "Account di test",
                    Category = "Lavoro",
                    IsFavorite = true
                });

            vault.Entries.Add(
                new VaultEntry
                {
                    Title = "Amazon",
                    Username = "utente@example.com",
                    Password = "AmazonPassword-456!",
                    Url = "https://www.amazon.it",
                    Category = "Shopping"
                });

            await service.CreateVaultAsync(
                vault,
                masterPassword);

            // 3. Verifichiamo che il vault esista
            Assert.True(
                await service.VaultExistsAsync());

            // 4. Simuliamo la chiusura dell'applicazione
            //    creando una nuova istanza del servizio.
            var database2 =
                new VaultDatabase(databasePath);

            var service2 =
                new VaultService(database2);

            await service2.InitializeAsync();

            // 5. Riapriamo il vault
            VaultDocument openedVault =
                await service2.OpenVaultAsync(
                    masterPassword);

            // 6. Verifichiamo i dati
            Assert.Equal(
                2,
                openedVault.Entries.Count);

            VaultEntry github =
                openedVault.Entries[0];

            Assert.Equal(
                "GitHub",
                github.Title);

            Assert.Equal(
                "utente@example.com",
                github.Username);

            Assert.Equal(
                "PasswordSegreta-123!",
                github.Password);

            Assert.Equal(
                "https://github.com",
                github.Url);

            Assert.Equal(
                "Lavoro",
                github.Category);

            Assert.True(
                github.IsFavorite);

            VaultEntry amazon =
                openedVault.Entries[1];

            Assert.Equal(
                "Amazon",
                amazon.Title);

            Assert.Equal(
                "AmazonPassword-456!",
                amazon.Password);
        }
        finally
        {
            // Eliminiamo il database temporaneo
            // utilizzato dal test.
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task OpenVaultWithWrongPassword_ShouldFail()
    {
        string databasePath =
            Path.Combine(
                Path.GetTempPath(),
                $"gdrvault-test-{Guid.NewGuid():N}.db");

        const string correctPassword =
            "PasswordCorretta-123!";

        const string wrongPassword =
            "PasswordSbagliata-456!";

        try
        {
            var database =
                new VaultDatabase(databasePath);

            var service =
                new VaultService(database);

            await service.InitializeAsync();

            var vault =
                new VaultDocument();

            vault.Entries.Add(
                new VaultEntry
                {
                    Title = "Account di test",
                    Username = "utente",
                    Password = "PasswordSuperSegreta!"
                });

            await service.CreateVaultAsync(
                vault,
                correctPassword);

            await Assert.ThrowsAsync<CryptographicException>(
                async () =>
                    await service.OpenVaultAsync(
                        wrongPassword));
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task ExistingVault_ShouldPreventSecondCreation()
    {
        string databasePath =
            Path.Combine(
                Path.GetTempPath(),
                $"gdrvault-test-{Guid.NewGuid():N}.db");

        const string masterPassword =
            "MasterPassword-123!";

        try
        {
            var database =
                new VaultDatabase(databasePath);

            var service =
                new VaultService(database);

            await service.InitializeAsync();

            await service.CreateVaultAsync(
                new VaultDocument(),
                masterPassword);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                    await service.CreateVaultAsync(
                        new VaultDocument(),
                        masterPassword));
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}