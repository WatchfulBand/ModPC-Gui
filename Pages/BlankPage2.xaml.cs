using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ModPC_Gui.Models;
using ModPC_Gui.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ModPC_Gui.Pages;

public sealed partial class BlankPage2 : Page
{
    private string _appDirectory;
    private string _userDirectory;
    private AccountItem _selectedAccount;

    public enum StatusSeverity
    {
        Informational,
        Success,
        Warning,
        Error
    }

    public BlankPage2()
    {
        this.InitializeComponent();
        Loaded += BlankPage2_Loaded;
    }

    private void BlankPage2_Loaded(object sender, RoutedEventArgs e)
    {
        _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _userDirectory = Path.Combine(_appDirectory, "user");
        Directory.CreateDirectory(_userDirectory);
        LoadAccounts();
    }

    private void LoadAccounts()
    {
        try
        {
            Task.Run(() =>
            {
                var accountFiles = Directory.GetFiles(_userDirectory, "*.json")
                    .Select(path => new AccountItem
                    {
                        FilePath = path,
                        Name = Path.GetFileNameWithoutExtension(path),
                        Type = GetAccountType(File.ReadAllText(path))
                    })
                    .OrderBy(a => a.Name)
                    .ToList();

                DispatcherQueue.TryEnqueue(() =>
                {
                    AccountsList.ItemsSource = accountFiles;
                });
            });
        }
        catch (Exception ex)
        {
            ShowStatus("����ʧ��", ex.Message, StatusSeverity.Error);
        }
    }

    private string GetAccountType(string content)
    {
        return content.StartsWith("SDK4399") ? "SDK4399����" : "Cookie����";
    }

    private async void AddAccount_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new AddAccountDialog();
            dialog.XamlRoot = this.Content.XamlRoot;
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                ShowStatus("����˺�", "���ڱ����˺���Ϣ...", StatusSeverity.Informational, true);
                string baseFileName = dialog.FileName;

                if (string.IsNullOrWhiteSpace(baseFileName))
                {
                    baseFileName = "account";
                }

                baseFileName = RemoveInvalidFileNameChars(baseFileName);
                string fileName = GenerateUniqueFileName(baseFileName);
                string filePath = Path.Combine(_userDirectory, $"{fileName}.json");
                string content;

                if (dialog.AccountType == "SDK4399")
                {
                    var accountData = new
                    {
                        username = dialog.Username,
                        password = dialog.Password
                    };
                    string json = JsonSerializer.Serialize(accountData);
                    content = $"SDK4399{Base64Encode(json)}";
                }
                else
                {
                    content = dialog.Username;
                }

                await Task.Run(() => File.WriteAllText(filePath, content));
                LoadAccounts();
                ShowStatus("��ӳɹ�", "�˺��ѳɹ����", StatusSeverity.Success);
            }
        }
        catch (Exception ex)
        {
            ShowStatus("���ʧ��", ex.Message, StatusSeverity.Error);
        }
    }

    private string RemoveInvalidFileNameChars(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(fileName
            .Where(ch => !invalidChars.Contains(ch))
            .ToArray());
    }

    private string GenerateUniqueFileName(string baseName)
    {
        string fileName = baseName;
        int counter = 1;

        while (File.Exists(Path.Combine(_userDirectory, $"{fileName}.json")))
        {
            fileName = $"{baseName}_{counter}";
            counter++;
        }

        return fileName;
    }

    private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedAccount == null)
            {
                ShowStatus("ɾ��ʧ��", "����ѡ��һ���˺�", StatusSeverity.Warning);
                return;
            }

            ShowStatus("ɾ���˺�", "����ɾ���˺�...", StatusSeverity.Informational, true);
            await Task.Run(() => File.Delete(_selectedAccount.FilePath));

            _selectedAccount = null;
            LoadAccounts();
            ShowStatus("ɾ���ɹ�", "�˺��ѳɹ�ɾ��", StatusSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus("ɾ��ʧ��", ex.Message, StatusSeverity.Error);
        }
    }

    private async void LoginAccount_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAccount == null)
        {
            ShowStatus("��¼ʧ��", "����ѡ��һ���˺�", StatusSeverity.Warning);
            return;
        }

        LoginAccountButton.IsEnabled = false;

        try
        {
            ShowStatus("��¼��", $"���ڵ�¼�˺�: {_selectedAccount.Name}", StatusSeverity.Informational, true);
            string content = File.ReadAllText(_selectedAccount.FilePath);

            if (_selectedAccount.Type == "SDK4399����")
            {
                content = DecodeSDK4399(content);
            }

            var httpService = new HttpService();

            // ʹ����ʽ����Ԫ������
            (bool success, string error, LoginResult result) = await httpService.PELoginAsync(content);

            if (success)
            {
                // ʹ��App�еĻỰ����
                App.Current.SetSession(result);
                ShowStatus("��¼�ɹ�", $"�˺� {result.data.nickname} ��¼�ɹ�", StatusSeverity.Success);

                await Task.Delay(2000);
                Frame.Navigate(typeof(BlankPage3));
            }
            else
            {
                ShowStatus("��¼ʧ��", error, StatusSeverity.Error);
            }
        }
        catch (Exception ex)
        {
            ShowStatus("��¼ʧ��", ex.Message, StatusSeverity.Error);
        }
        finally
        {
            LoginAccountButton.IsEnabled = true;
        }
    }

    private string DecodeSDK4399(string content)
    {
        if (content.StartsWith("SDK4399"))
        {
            string base64Content = content.Substring("SDK4399".Length);
            byte[] bytes = Convert.FromBase64String(base64Content);
            return Encoding.UTF8.GetString(bytes);
        }
        return content;
    }

    private void ChangeHWID_Click(object sender, RoutedEventArgs e)
    {
        ShowStatus("������ʾ", "����HWID������δʵ��", StatusSeverity.Informational);
    }

    private void AccountsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedAccount = AccountsList.SelectedItem as AccountItem;
    }

    private string Base64Encode(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(bytes);
    }

    private void ShowStatus(string title, string message, StatusSeverity severity, bool showProgress = false)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            StatusTitle.Text = title;
            StatusMessage.Text = message;

            switch (severity)
            {
                case StatusSeverity.Success:
                    StatusIcon.Glyph = "\uE73E";
                    StatusIcon.Foreground = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                    LoginStatusPanel.Background = (Brush)Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"];
                    break;
                case StatusSeverity.Error:
                    StatusIcon.Glyph = "\uEA39";
                    StatusIcon.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    LoginStatusPanel.Background = (Brush)Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"];
                    break;
                case StatusSeverity.Warning:
                    StatusIcon.Glyph = "\uE7BA";
                    StatusIcon.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
                    LoginStatusPanel.Background = (Brush)Application.Current.Resources["SystemFillColorCautionBackgroundBrush"];
                    break;
                default:
                    StatusIcon.Glyph = "\uE946";
                    StatusIcon.Foreground = (Brush)Application.Current.Resources["SystemFillColorAttentionBrush"];
                    LoginStatusPanel.Background = (Brush)Application.Current.Resources["SystemFillColorAttentionBackgroundBrush"];
                    break;
            }

            LoginProgressRing.IsActive = showProgress;
            LoginProgressRing.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
            LoginStatusPanel.Visibility = Visibility.Visible;

            if (severity == StatusSeverity.Success)
            {
                _ = DispatcherQueue.TryEnqueue(async () =>
                {
                    await Task.Delay(3000);
                    if (StatusMessage.Text == message)
                    {
                        HideStatus();
                    }
                });
            }
        });
    }

    private void HideStatus()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            LoginStatusPanel.Visibility = Visibility.Collapsed;
        });
    }

    private void CloseStatusButton_Click(object sender, RoutedEventArgs e)
    {
        HideStatus();
    }
}