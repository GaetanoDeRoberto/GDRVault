using GDRVault.Core;
using GDRVault.Storage;
using System.IO;
using System.Security.Cryptography;
using System.Windows;

namespace GDRVault;

public partial class MainWindow : Window
{
    private readonly VaultService _vaultService;

    public MainWindow()
    {
        InitializeComponent();

        string appDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "GDRVault");

        string databasePath =
            Path.Combine(
                appDirectory,
                "vault.db");

        var database =
            new VaultDatabase(databasePath);

        _vaultService =
            new VaultService(database);
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(
    object sender,
    RoutedEventArgs e)
    {
        try
        {
            await _vaultService.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile inizializzare GDRVault:\n\n{ex.Message}",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Close();
        }
    }
    private async void UnlockButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        string password =
            MasterPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show(
                "Inserisci la Master Password.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            MasterPasswordBox.Focus();
            return;
        }

        try
        {
            VaultDocument vault =
                await _vaultService.OpenVaultAsync(password);

            MasterPasswordBox.Clear();

            var window =
                new VaultWindow(
                    _vaultService,
                    vault,
                    password)
                {
                    Owner = this
                };

            Hide();

            window.ShowDialog();

            Show();

            MasterPasswordBox.Clear();
            MasterPasswordBox.Focus();
        }
        catch (CryptographicException)
        {
            MessageBox.Show(
                "Master Password errata.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            MasterPasswordBox.SelectAll();
            MasterPasswordBox.Focus();
        }
        catch (InvalidDataException)
        {
            MessageBox.Show(
                "Il contenuto del Vault non è valido.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
                $"Errore durante l'apertura del Vault:\n\n{ex.Message}",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void CreateVaultButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _vaultService.InitializeAsync();

            if (await _vaultService.VaultExistsAsync())
            {
                MessageBox.Show(
                    "Esiste già un Vault.",
                    "GDRVault",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var window =
                new CreateVaultWindow(_vaultService)
                {
                    Owner = this
                };

            bool? result =
                window.ShowDialog();

            if (result == true)
            {
                MasterPasswordBox.Clear();

                MessageBox.Show(
                    "Ora puoi sbloccare il Vault con la Master Password appena creata.",
                    "GDRVault",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                MasterPasswordBox.Focus();
            }
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
}