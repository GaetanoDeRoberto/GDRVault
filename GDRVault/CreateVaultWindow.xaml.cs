using GDRVault.Core;
using GDRVault.Storage;
using System.Windows;

namespace GDRVault;

public partial class CreateVaultWindow : Window
{
    private readonly VaultService _vaultService;

    public CreateVaultWindow(VaultService vaultService)
    {
        InitializeComponent();

        _vaultService = vaultService;
    }

    private async void CreateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string password = MasterPasswordBox.Password;
        string confirmation = ConfirmPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show(
                "Inserisci una Master Password.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (password.Length < 12)
        {
            MessageBox.Show(
                "La Master Password deve contenere almeno 12 caratteri.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (password != confirmation)
        {
            MessageBox.Show(
                "Le due Master Password non coincidono.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            var vault = new VaultDocument();

            await _vaultService.CreateVaultAsync(
                vault,
                password);

            MessageBox.Show(
                "Vault creato con successo.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(
                ex.Message,
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Errore durante la creazione del Vault:\n\n{ex.Message}",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}