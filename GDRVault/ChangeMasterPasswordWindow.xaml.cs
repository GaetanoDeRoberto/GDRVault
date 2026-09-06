using System.Windows;

namespace GDRVault;

public partial class ChangeMasterPasswordWindow : Window
{
    public string? NewPassword { get; private set; }

    public ChangeMasterPasswordWindow()
    {
        InitializeComponent();

        CurrentPasswordBox.Focus();
    }

    private void ChangeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string currentPassword =
            CurrentPasswordBox.Password;

        string newPassword =
            NewPasswordBox.Password;

        string confirmPassword =
            ConfirmPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            MessageBox.Show(
                "Inserisci la Master Password attuale.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            CurrentPasswordBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            MessageBox.Show(
                "Inserisci la nuova Master Password.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            NewPasswordBox.Focus();
            return;
        }

        if (newPassword.Length < 12)
        {
            MessageBox.Show(
                "La nuova Master Password deve contenere almeno 12 caratteri.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            NewPasswordBox.Focus();
            return;
        }

        if (newPassword != confirmPassword)
        {
            MessageBox.Show(
                "Le nuove Master Password non coincidono.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            ConfirmPasswordBox.Clear();
            ConfirmPasswordBox.Focus();
            return;
        }

        if (currentPassword == newPassword)
        {
            MessageBox.Show(
                "La nuova Master Password deve essere diversa da quella attuale.",
                "GDRVault",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            NewPasswordBox.Focus();
            return;
        }

        NewPassword = newPassword;

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