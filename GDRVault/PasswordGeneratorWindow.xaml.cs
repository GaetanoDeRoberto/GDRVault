using System.Windows;

namespace GDRVault;

public partial class PasswordGeneratorWindow : Window
{
    public string? GeneratedPassword { get; private set; }

    public PasswordGeneratorWindow()
    {
        InitializeComponent();

        GeneratePassword();
    }

    private void GeneratePassword()
    {
        if (!int.TryParse(
                LengthBox.Text,
                out int length))
        {
            MessageBox.Show(
                "Inserisci una lunghezza valida.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            LengthBox.Focus();
            return;
        }

        if (length < 12 || length > 64)
        {
            MessageBox.Show(
                "La lunghezza deve essere compresa tra 12 e 64 caratteri.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            LengthBox.Focus();
            return;
        }

        try
        {
            GeneratedPassword =
                PasswordGenerator.Generate(
                    length,
                    LowercaseCheckBox.IsChecked == true,
                    UppercaseCheckBox.IsChecked == true,
                    NumbersCheckBox.IsChecked == true,
                    SymbolsCheckBox.IsChecked == true);

            GeneratedPasswordBox.Text =
                GeneratedPassword;
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(
                ex.Message,
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void RegenerateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        GeneratePassword();
    }

    private void UsePasswordButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(GeneratedPassword))
            return;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}