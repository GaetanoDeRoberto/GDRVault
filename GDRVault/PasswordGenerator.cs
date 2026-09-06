using System.Security.Cryptography;

namespace GDRVault;

public static class PasswordGenerator
{
    private const string Lowercase =
        "abcdefghijklmnopqrstuvwxyz";

    private const string Uppercase =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private const string Numbers =
        "0123456789";

    private const string Symbols =
        "!@#$%^&*()-_=+";

    public static string Generate(
        int length,
        bool useLowercase = true,
        bool useUppercase = true,
        bool useNumbers = true,
        bool useSymbols = true)
    {
        if (length < 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                "La password deve contenere almeno 12 caratteri.");
        }

        string characters = "";

        if (useLowercase)
            characters += Lowercase;

        if (useUppercase)
            characters += Uppercase;

        if (useNumbers)
            characters += Numbers;

        if (useSymbols)
            characters += Symbols;

        if (characters.Length == 0)
        {
            throw new ArgumentException(
                "Seleziona almeno un tipo di carattere.");
        }

        char[] result = new char[length];

        for (int i = 0; i < length; i++)
        {
            int index =
                RandomNumberGenerator.GetInt32(
                    characters.Length);

            result[i] = characters[index];
        }

        return new string(result);
    }
}