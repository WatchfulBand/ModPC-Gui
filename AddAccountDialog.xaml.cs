using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace ModPC_Gui.Pages
{
    public sealed partial class AddAccountDialog : ContentDialog
    {
        public string Username => UsernameBox.Text;
        public string Password => PasswordBox.Password;
        public string FileName => RemarkBox.Text;
        public string AccountType { get; private set; } = "Cookie";

        public AddAccountDialog()
        {
            this.InitializeComponent();
            UpdatePasswordVisibility();
        }

        private void UpdatePasswordVisibility()
        {
            PasswordPanel.Visibility = AccountType == "Cookie"
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void AccountTypeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem)
            {
                AccountType = menuItem.Text;
                SelectedTypeText.Text = AccountType;
                UpdatePasswordVisibility();
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ShowError("�˺Ų���Ϊ��");
                args.Cancel = true;
                return;
            }

            if (AccountType != "Cookie" && string.IsNullOrWhiteSpace(Password))
            {
                ShowError("���벻��Ϊ��");
                args.Cancel = true;
                return;
            }

            // ��֤ͨ�������ش�����Ϣ
            ErrorInfoBar.IsOpen = false;
        }

        private void ShowError(string message)
        {
            ErrorInfoBar.Message = message;
            ErrorInfoBar.IsOpen = true;
        }
    }
}