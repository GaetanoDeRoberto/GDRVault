using System.Security.Cryptography;

namespace GDRVault.Security.Tests;

public class VaultCryptoTests
{
    [Fact]
    public async Task EncryptAndDecrypt_ShouldReturnOriginalText()
    {
        const string originalText =
            "Testo segreto di GDRVault";

        const string masterPassword =
            "PasswordMaster-123!";

        byte[] encrypted =
            await VaultCrypto.EncryptAsync(
                originalText,
                masterPassword);

        string decrypted =
            await VaultCrypto.DecryptAsync(
                encrypted,
                masterPassword);

        Assert.Equal(originalText, decrypted);
    }

    [Fact]
    public async Task WrongPassword_ShouldFail()
    {
        const string originalText =
            "Informazioni riservate";

        const string correctPassword =
            "PasswordCorretta-123!";

        const string wrongPassword =
            "PasswordSbagliata-456!";

        byte[] encrypted =
            await VaultCrypto.EncryptAsync(
                originalText,
                correctPassword);

        await Assert.ThrowsAsync<CryptographicException>(
            async () =>
                await VaultCrypto.DecryptAsync(
                    encrypted,
                    wrongPassword));
    }

    [Fact]
    public async Task ModifiedCiphertext_ShouldFail()
    {
        const string originalText =
            "Dati che non devono essere modificati";

        const string masterPassword =
            "PasswordMaster-123!";

        byte[] encrypted =
            await VaultCrypto.EncryptAsync(
                originalText,
                masterPassword);

        // Modifichiamo un byte del ciphertext.
        encrypted[^1] ^= 0x01;

        await Assert.ThrowsAsync<CryptographicException>(
            async () =>
                await VaultCrypto.DecryptAsync(
                    encrypted,
                    masterPassword));
    }

    [Fact]
    public async Task TwoEncryptions_ShouldProduceDifferentResults()
    {
        const string originalText =
            "Stesso testo";

        const string masterPassword =
            "PasswordMaster-123!";

        byte[] encrypted1 =
            await VaultCrypto.EncryptAsync(
                originalText,
                masterPassword);

        byte[] encrypted2 =
            await VaultCrypto.EncryptAsync(
                originalText,
                masterPassword);

        Assert.False(
            encrypted1.SequenceEqual(encrypted2));
    }

    [Fact]
    public async Task UnicodeText_ShouldWork()
    {
        const string originalText =
            "Password 🔐 — àèìòù — 日本語 — Привет";

        const string masterPassword =
            "MasterPassword-123!";

        byte[] encrypted =
            await VaultCrypto.EncryptAsync(
                originalText,
                masterPassword);

        string decrypted =
            await VaultCrypto.DecryptAsync(
                encrypted,
                masterPassword);

        Assert.Equal(originalText, decrypted);
    }

    [Fact]
    public async Task EmptyPlaintext_ShouldFail()
    {
        const string masterPassword =
            "PasswordMaster-123!";

        await Assert.ThrowsAsync<ArgumentException>(
            async () =>
                await VaultCrypto.EncryptAsync(
                    string.Empty,
                    masterPassword));
    }

    [Fact]
    public async Task EmptyMasterPassword_ShouldFail()
    {
        const string originalText =
            "Testo segreto";

        await Assert.ThrowsAsync<ArgumentException>(
            async () =>
                await VaultCrypto.EncryptAsync(
                    originalText,
                    string.Empty));
    }
}