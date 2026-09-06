using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace GDRVault.Security;

public static class VaultCrypto
{
    private const int KeySize = 32;       // 256 bit
    private const int SaltSize = 16;      // 128 bit
    private const int NonceSize = 12;     // 96 bit
    private const int TagSize = 16;       // 128 bit

    // Argon2id parameters
    private const int Argon2MemorySize = 65536; // 64 MB
    private const int Argon2Iterations = 3;
    private const int Argon2Parallelism = 2;

    public static async Task<byte[]> EncryptAsync(
        string plaintext,
        string masterPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintext);
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);

        byte[] key = await DeriveKeyAsync(masterPassword, salt);

        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[TagSize];

        try
        {
            using var aes = new AesGcm(key, TagSize);

            aes.Encrypt(
                nonce,
                plaintextBytes,
                ciphertext,
                tag);

            // Formato:
            // [version][salt][nonce][tag][ciphertext]
            byte[] result = new byte[
                1 +
                SaltSize +
                NonceSize +
                TagSize +
                ciphertext.Length];

            int offset = 0;

            result[offset++] = 1; // formato versione 1

            Buffer.BlockCopy(
                salt, 0,
                result, offset,
                SaltSize);

            offset += SaltSize;

            Buffer.BlockCopy(
                nonce, 0,
                result, offset,
                NonceSize);

            offset += NonceSize;

            Buffer.BlockCopy(
                tag, 0,
                result, offset,
                TagSize);

            offset += TagSize;

            Buffer.BlockCopy(
                ciphertext, 0,
                result, offset,
                ciphertext.Length);

            return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    public static async Task<string> DecryptAsync(
        byte[] encryptedData,
        string masterPassword)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);

        int minimumSize =
            1 +
            SaltSize +
            NonceSize +
            TagSize;

        if (encryptedData.Length < minimumSize)
        {
            throw new CryptographicException(
                "Il vault cifrato non è valido.");
        }

        int offset = 0;

        byte version = encryptedData[offset++];

        if (version != 1)
        {
            throw new CryptographicException(
                $"Versione del vault non supportata: {version}.");
        }

        byte[] salt = new byte[SaltSize];
        Buffer.BlockCopy(
            encryptedData,
            offset,
            salt,
            0,
            SaltSize);

        offset += SaltSize;

        byte[] nonce = new byte[NonceSize];
        Buffer.BlockCopy(
            encryptedData,
            offset,
            nonce,
            0,
            NonceSize);

        offset += NonceSize;

        byte[] tag = new byte[TagSize];
        Buffer.BlockCopy(
            encryptedData,
            offset,
            tag,
            0,
            TagSize);

        offset += TagSize;

        int ciphertextLength =
            encryptedData.Length - offset;

        if (ciphertextLength < 0)
        {
            throw new CryptographicException(
                "Il vault cifrato non è valido.");
        }

        byte[] ciphertext = new byte[ciphertextLength];

        Buffer.BlockCopy(
            encryptedData,
            offset,
            ciphertext,
            0,
            ciphertextLength);

        byte[] plaintextBytes =
            new byte[ciphertextLength];

        byte[] key = await DeriveKeyAsync(
            masterPassword,
            salt);

        try
        {
            using var aes = new AesGcm(key, TagSize);

            aes.Decrypt(
                nonce,
                ciphertext,
                tag,
                plaintextBytes);

            return Encoding.UTF8.GetString(
                plaintextBytes);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException(
                "Impossibile sbloccare il vault. " +
                "La Master Password potrebbe essere errata " +
                "oppure i dati potrebbero essere stati modificati.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    private static async Task<byte[]> DeriveKeyAsync(
        string masterPassword,
        byte[] salt)
    {
        byte[] passwordBytes =
            Encoding.UTF8.GetBytes(masterPassword);

        try
        {
            var argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt,
                MemorySize = Argon2MemorySize,
                Iterations = Argon2Iterations,
                DegreeOfParallelism = Argon2Parallelism
            };

            return await argon2.GetBytesAsync(KeySize);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                passwordBytes);
        }
    }
}