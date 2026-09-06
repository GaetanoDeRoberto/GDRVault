using GDRVault.Core;
using System.Windows;

namespace GDRVault;

public partial class EntryWindow : Window
{
    public VaultEntry? Entry { get; private set; }

    private readonly VaultEntry? _existingEntry;

    public EntryWindow(VaultEntry? entry = null)
    {
        InitializeComponent();

        _existingEntry = entry;

        if (entry is not null)
        {
            Title = "Modifica credenziale";

            TitleBox.Text = entry.Title;
            UsernameBox.Text = entry.Username;
            PasswordBox.Password = entry.Password;
            UrlBox.Text = entry.Url;
            NotesBox.Text = entry.Notes;

            for (int i = 0; i < CategoryBox.Items.Count; i++)
            {
                if (CategoryBox.Items[i]
                    is System.Windows.Controls.ComboBoxItem item &&
                    item.Content?.ToString() == entry.Category)
                {
                    CategoryBox.SelectedIndex = i;
                    break;
                }
            }
        }

        TitleBox.Focus();
    }

    private void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string title = TitleBox.Text.Trim();
        string username = UsernameBox.Text.Trim();
        string password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show(
                "Inserisci un nome per la credenziale.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            TitleBox.Focus();
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            MessageBox.Show(
                "Inserisci una password.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            PasswordBox.Focus();
            return;
        }

        DateTime now = DateTime.UtcNow;

        if (_existingEntry is not null)
        {
            _existingEntry.Title = title;
            _existingEntry.Username = username;
            _existingEntry.Password = password;
            _existingEntry.Url = UrlBox.Text.Trim();
            _existingEntry.Notes = NotesBox.Text.Trim();
            _existingEntry.Category =
                (CategoryBox.SelectedItem
                    as System.Windows.Controls.ComboBoxItem)
                ?.Content?.ToString()
                ?? "Altro";

            _existingEntry.ModifiedAt = now;

            Entry = _existingEntry;
        }
        else
        {
            Entry = new VaultEntry
            {
                Id = Guid.NewGuid(),
                Title = title,
                Username = username,
                Password = password,
                Url = UrlBox.Text.Trim(),
                Notes = NotesBox.Text.Trim(),
                Category =
                    (CategoryBox.SelectedItem
                        as System.Windows.Controls.ComboBoxItem)
                    ?.Content?.ToString()
                    ?? "Altro",
                IsFavorite = false,
                CreatedAt = now,
                ModifiedAt = now
            };
        }

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

    private void GeneratePasswordButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var window = new PasswordGeneratorWindow
        {
            Owner = this
        };

        bool? result = window.ShowDialog();

        if (result == true &&
            !string.IsNullOrEmpty(window.GeneratedPassword))
        {
            PasswordBox.Password =
                window.GeneratedPassword;
        }
    }
}