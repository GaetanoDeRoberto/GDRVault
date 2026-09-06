using GDRVault.Core;
using System.Diagnostics;
using System.Windows;


namespace GDRVault;

public partial class EntryDetailsWindow : Window
{
    private readonly VaultEntry _entry;

    public bool EntryModified { get; private set; }

    public bool EntryDeleted { get; private set; }

    public EntryDetailsWindow(VaultEntry entry)
    {
        InitializeComponent();

        _entry = entry;

        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        TitleText.Text = _entry.Title;
        UsernameBox.Text = _entry.Username;
        PasswordBox.Password = _entry.Password;
        VisiblePasswordBox.Text = _entry.Password;
        UrlBox.Text = _entry.Url;
        CategoryBox.Text = _entry.Category;
        NotesBox.Text = _entry.Notes;

        FavoriteButton.Content =
            _entry.IsFavorite ? "★" : "☆";
    }

    private void ShowPasswordButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (VisiblePasswordBox.Visibility == Visibility.Visible)
        {
            VisiblePasswordBox.Visibility =
                Visibility.Collapsed;

            PasswordBox.Visibility =
                Visibility.Visible;

            ShowPasswordButton.Content = "Mostra";
        }
        else
        {
            VisiblePasswordBox.Visibility =
                Visibility.Visible;

            PasswordBox.Visibility =
                Visibility.Collapsed;

            ShowPasswordButton.Content = "Nascondi";
        }
    }

    private void CopyUsernameButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_entry.Username))
            return;

        Clipboard.SetText(_entry.Username);

        MessageBox.Show(
            "Username copiato negli appunti.",
            "GDRVault",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void CopyPasswordButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        string password = PasswordBox.Password;

        if (string.IsNullOrEmpty(password))
            return;

        try
        {
            Clipboard.SetText(password);

            MessageBox.Show(
                "Password copiata negli appunti.\n\nVerrà cancellata automaticamente tra 15 secondi.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            await Task.Delay(15000);

            // Cancella solo se nel frattempo l'utente
            // non ha copiato qualcos'altro.
            if (Clipboard.ContainsText() &&
                Clipboard.GetText() == password)
            {
                Clipboard.Clear();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile copiare la password.\n\n{ex.Message}",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void FavoriteButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _entry.IsFavorite = !_entry.IsFavorite;

        FavoriteButton.Content =
            _entry.IsFavorite ? "★" : "☆";

        EntryModified = true;
    }

    private void EditButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var window = new EntryWindow(_entry)
        {
            Owner = this
        };

        bool? result = window.ShowDialog();

        if (result == true)
        {
            RefreshDisplay();

            EntryModified = true;
        }
    }

    private void DeleteButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            $"Vuoi eliminare la credenziale \"{_entry.Title}\"?",
            "Elimina credenziale",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        EntryDeleted = true;
        DialogResult = true;
        Close();
    }


    private void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OpenUrlButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_entry.Url))
        {
            MessageBox.Show(
                "Questa credenziale non contiene un URL.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        try
        {
            string url = _entry.Url.Trim();

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile aprire l'URL.\n\n{ex.Message}",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}