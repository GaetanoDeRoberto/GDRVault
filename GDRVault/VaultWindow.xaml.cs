using GDRVault.Core;
using GDRVault.Storage;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GDRVault;

public partial class VaultWindow : Window, INotifyPropertyChanged
{
    private readonly VaultService _vaultService;
    private string _masterPassword;
    private readonly VaultDocument _vault;

    private bool _isInitialized = false;
    private bool _isRefreshingEntries = false;
    private bool _onlyFavorites = false;

    private readonly DispatcherTimer _autoLockTimer;
    private readonly AutofillPipeServer _autofillPipeServer;

    private const int AutoLockMinutes = 5;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string EntryCountText =>
        _vault.Entries.Count switch
        {
            0 => "Nessuna credenziale",
            1 => "1 credenziale",
            _ => $"{_vault.Entries.Count} credenziali"
        };

    public VaultWindow(
        VaultService vaultService,
        VaultDocument vault,
        string masterPassword)
    {
        InitializeComponent();

        _vaultService = vaultService;
        _vault = vault;
        _masterPassword = masterPassword;

        _autofillPipeServer = new AutofillPipeServer(_vault);
        _autofillPipeServer.Start();

        DataContext = this;

        _autoLockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(AutoLockMinutes)
        };

        _autoLockTimer.Tick += AutoLockTimer_Tick;

        PreviewMouseMove += UserActivity;
        PreviewMouseDown += UserActivity;
        PreviewKeyDown += UserActivity;
        PreviewTouchDown += UserActivity;

        Loaded += VaultWindow_Loaded;
        Closed += VaultWindow_Closed;

        _isInitialized = true;

        PopulateCategories();
        RefreshEntries();
    }

    private void VaultWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        ResetAutoLockTimer();
    }

    private void UserActivity(
        object sender,
        InputEventArgs e)
    {
        ResetAutoLockTimer();
    }

    private void ResetAutoLockTimer()
    {
        _autoLockTimer.Stop();
        _autoLockTimer.Start();
    }

    private void AutoLockTimer_Tick(
        object? sender,
        EventArgs e)
    {
        _autoLockTimer.Stop();

        MessageBox.Show(
            "Il Vault è stato bloccato automaticamente dopo 5 minuti di inattività.",
            "GDRVault",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        Close();
    }

    private void VaultWindow_Closed(
        object? sender,
        EventArgs e)
    {
        _autoLockTimer.Stop();
        _autofillPipeServer.Dispose();

        PreviewMouseMove -= UserActivity;
        PreviewMouseDown -= UserActivity;
        PreviewKeyDown -= UserActivity;
        PreviewTouchDown -= UserActivity;
    }

    private async void NewEntryButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var window = new EntryWindow
        {
            Owner = this
        };

        bool? result = window.ShowDialog();

        if (result == true &&
            window.Entry is not null)
        {
            _vault.Entries.Add(window.Entry);

            PopulateCategories();
            RefreshEntries();

            OnPropertyChanged(nameof(EntryCountText));

            await AutoSaveAsync();
        }
    }

    private async void EntriesList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (EntriesList.SelectedItem is not VaultEntry entry)
            return;

        var window =
            new EntryDetailsWindow(entry)
            {
                Owner = this
            };

        window.ShowDialog();

        if (window.EntryDeleted)
        {
            _vault.Entries.Remove(entry);

            PopulateCategories();
            RefreshEntries();

            OnPropertyChanged(nameof(EntryCountText));
        }
        else if (window.EntryModified)
        {
            RefreshEntries();

            OnPropertyChanged(nameof(EntryCountText));
        }

        if (window.EntryDeleted || window.EntryModified)
        {
            await AutoSaveAsync();
        }

        EntriesList.SelectedItem = null;
    }

    private async Task AutoSaveAsync()
    {
        try
        {
            await _vaultService.SaveVaultAsync(
                _vault,
                _masterPassword);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile salvare automaticamente il Vault.\n\n{ex.Message}",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void BackupButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Esporta backup GDRVault",
            Filter = "Backup GDRVault (*.gdrvault)|*.gdrvault",
            DefaultExt = ".gdrvault",
            AddExtension = true,
            FileName = "GDRVault-Backup"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await _vaultService.ExportBackupAsync(
                dialog.FileName);

            MessageBox.Show(
                "Backup creato correttamente.\n\nIl file contiene esclusivamente i dati cifrati del Vault.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile creare il backup.\n\n{ex.Message}",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _vaultService.SaveVaultAsync(
                _vault,
                _masterPassword);

            MessageBox.Show(
                "Vault salvato correttamente.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            ResetAutoLockTimer();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile salvare il Vault.\n\n{ex.Message}",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        RefreshEntries();
    }

    private void CategoryFilterBox_PreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        // Il template personalizzato non usa un ToggleButton standard.
        // Gestiamo quindi manualmente l'apertura/chiusura della ComboBox.
        CategoryFilterBox.IsDropDownOpen =
            !CategoryFilterBox.IsDropDownOpen;

        e.Handled = true;
    }

    private void CategoryFilterBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!_isInitialized || _isRefreshingEntries)
            return;

        RefreshEntries();
    }

    private void FavoritesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _onlyFavorites = !_onlyFavorites;

        FavoritesButton.Content =
            _onlyFavorites
                ? "★ Preferiti"
                : "☆ Preferiti";

        RefreshEntries();
    }

    private void PopulateCategories()
    {
        if (CategoryFilterBox == null)
            return;

        string previousCategory =
            CategoryFilterBox.SelectedItem?.ToString()
            ?? "Tutte le categorie";

        _isRefreshingEntries = true;

        try
        {
            CategoryFilterBox.Items.Clear();

            // Prima voce sempre presente
            CategoryFilterBox.Items.Add(
                "Tutte le categorie");

            var categories = _vault.Entries
                .Where(entry =>
                    !string.IsNullOrWhiteSpace(entry.Category))
                .Select(entry =>
                    entry.Category.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    category => category,
                    StringComparer.OrdinalIgnoreCase);

            foreach (string category in categories)
            {
                CategoryFilterBox.Items.Add(category);
            }

            int selectedIndex =
                CategoryFilterBox.Items.IndexOf(previousCategory);

            CategoryFilterBox.SelectedIndex =
                selectedIndex >= 0
                    ? selectedIndex
                    : 0;
        }
        finally
        {
            _isRefreshingEntries = false;
        }
    }

    private void RefreshEntries()
    {
        if (!_isInitialized || _isRefreshingEntries)
            return;

        try
        {
            _isRefreshingEntries = true;

            string searchText =
                SearchBox?.Text?.Trim()
                ?? string.Empty;

            string selectedCategory =
                CategoryFilterBox?.SelectedItem?.ToString()
                ?? "Tutte le categorie";

            var entries = _vault.Entries
                .Where(entry =>
                    string.IsNullOrWhiteSpace(searchText) ||

                    (entry.Title?.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase)
                        ?? false) ||

                    (entry.Username?.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase)
                        ?? false))
                .Where(entry =>
                    selectedCategory == "Tutte le categorie" ||

                    string.Equals(
                        entry.Category,
                        selectedCategory,
                        StringComparison.OrdinalIgnoreCase))
                .Where(entry =>
                    !_onlyFavorites ||
                    entry.IsFavorite)
                .ToList();

            /*
             * Non costruiamo più manualmente le card.
             * Il DataTemplate già presente nel VaultWindow.xaml
             * si occupa della grafica.
             */
            EntriesList.ItemsSource = entries;

            OnPropertyChanged(
                nameof(EntryCountText));
        }
        finally
        {
            _isRefreshingEntries = false;
        }
    }

    private void LockButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _autoLockTimer.Stop();
        Close();
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    private async void RestoreButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Ripristina backup GDRVault",
            Filter = "Backup GDRVault (*.gdrvault)|*.gdrvault",
            DefaultExt = ".gdrvault",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() != true)
            return;

        var result = MessageBox.Show(
            "Il ripristino sostituirà il Vault attualmente aperto.\n\n" +
            "Sei sicuro di voler continuare?",
            "GDRVault - Ripristino",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            await _vaultService.ImportBackupAsync(
                dialog.FileName,
                _masterPassword);

            MessageBox.Show(
                "Backup ripristinato correttamente.\n\n" +
                "GDRVault verrà chiuso. Riaprilo per caricare il Vault ripristinato.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }
        catch (CryptographicException)
        {
            MessageBox.Show(
                "Il backup non può essere aperto con la Master Password attuale.\n\n" +
                "Il Vault corrente non è stato modificato.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (InvalidDataException ex)
        {
            MessageBox.Show(
                $"Il backup non è valido.\n\n" +
                $"{ex.Message}\n\n" +
                "Il Vault corrente non è stato modificato.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile ripristinare il backup.\n\n" +
                $"{ex.Message}\n\n" +
                "Il Vault corrente non è stato modificato.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void ChangeMasterPasswordButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var window = new ChangeMasterPasswordWindow
        {
            Owner = this
        };

        if (window.ShowDialog() != true ||
            string.IsNullOrEmpty(window.NewPassword))
        {
            return;
        }

        try
        {
            await _vaultService.ChangeMasterPasswordAsync(
                _vault,
                _masterPassword,
                window.NewPassword);

            _masterPassword =
                window.NewPassword;

            MessageBox.Show(
                "Master Password cambiata correttamente.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile cambiare la Master Password.\n\n{ex.Message}",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}