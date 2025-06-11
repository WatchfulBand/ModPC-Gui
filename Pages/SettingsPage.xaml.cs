using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ModPC_Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Microsoft.UI; // ���� Colors ��
using Microsoft.UI.Xaml.Media; // ���� SolidColorBrush ��

namespace ModPC_Gui.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private bool _isSettingsLoaded = false;
        private string _appDirectory;
        private string _configPath;
        private Dictionary<string, JsonElement> _configData;
        private Window _currentWindow; // ���浱ǰ����

        public SettingsPage()
        {
            this.InitializeComponent();
            this.Loaded += SettingsPage_Loaded;

            // 1. ��ȡӦ�ó�������Ŀ¼���˳�ʱ�����棩
            _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Debug.WriteLine($"Ӧ�ó���Ŀ¼: {_appDirectory}");

            // 2. ���������ļ�·��
            _configPath = Path.Combine(_appDirectory, "config.json");
            Debug.WriteLine($"�����ļ�·��: {_configPath}");
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isSettingsLoaded) return;

            // ��ҳ�����ʱ��ȡ��ǰ����
            _currentWindow = GetCurrentWindow();

            DispatcherQueue.TryEnqueue(() =>
            {
                LoadThemeSetting();
                LoadNavPositionSetting();
                LoadClientPath();
                _isSettingsLoaded = true;
            });

            // ����¼�����
            LightThemeRadio.Checked += ThemeRadio_Checked;
            DarkThemeRadio.Checked += ThemeRadio_Checked;
            SystemThemeRadio.Checked += ThemeRadio_Checked;
        }

        private void LoadThemeSetting()
        {
            try
            {
                string themePreference = App.GetCurrentThemePreference();

                switch (themePreference)
                {
                    case "Light":
                        LightThemeRadio.IsChecked = true;
                        break;
                    case "Dark":
                        DarkThemeRadio.IsChecked = true;
                        break;
                    default:
                        SystemThemeRadio.IsChecked = true;
                        break;
                }
            }
            catch
            {
                SystemThemeRadio.IsChecked = true;
            }
        }

        private void LoadNavPositionSetting()
        {
            try
            {
                TopNavToggle.IsOn = App.GetNavigationPosition() == "Top";
            }
            catch
            {
                TopNavToggle.IsOn = false;
            }
        }

        private void LoadClientPath()
        {
            try
            {
                // ��������ļ��Ƿ����
                if (File.Exists(_configPath))
                {
                    // ��ȡ�����������ļ�
                    string json = File.ReadAllText(_configPath);

                    // ��������JSON�ĵ�
                    using (JsonDocument document = JsonDocument.Parse(json))
                    {
                        // ������JSON�ĵ�ת��Ϊ�ֵ�
                        _configData = new Dictionary<string, JsonElement>();
                        foreach (var property in document.RootElement.EnumerateObject())
                        {
                            _configData[property.Name] = property.Value.Clone();
                        }

                        // ��ȡ·��ֵ
                        if (_configData.TryGetValue("path", out JsonElement pathElement))
                        {
                            string path = pathElement.GetString();
                            ClientPathBox.Text = path;
                            ValidatePath(path);
                        }
                        else
                        {
                            PathErrorText.Text = "�����ļ���δ�ҵ�·����Ϣ";
                            PathErrorText.Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("�����ļ�������");
                    PathErrorText.Text = "�����ļ������ڣ�������·��";
                    PathErrorText.Visibility = Visibility.Visible;

                    // ��ʼ��������
                    _configData = new Dictionary<string, JsonElement>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"���ؿͻ���·��ʧ��: {ex}");
                PathErrorText.Text = $"��������ʧ��: {ex.Message}";
                PathErrorText.Visibility = Visibility.Visible;

                // ��ʼ��������
                _configData = new Dictionary<string, JsonElement>();
            }
        }

        private void SaveClientPath(string path)
        {
            try
            {
                if (!ValidatePath(path))
                {
                    return;
                }

                // ȷ���������ݴ���
                if (_configData == null)
                {
                    _configData = new Dictionary<string, JsonElement>();
                }

                // �����µ�JSON���󣨽��ո�ʽ��
                using (MemoryStream stream = new MemoryStream())
                {
                    // ʹ�ý��ո�ʽ����������
                    using (Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions
                    {
                        Indented = false // �������������ɽ���JSON
                    }))
                    {
                        writer.WriteStartObject();

                        // д������������
                        foreach (var kvp in _configData)
                        {
                            writer.WritePropertyName(kvp.Key);

                            // ���⴦��path�ֶ�
                            if (kvp.Key == "path")
                            {
                                writer.WriteStringValue(path);
                            }
                            else
                            {
                                kvp.Value.WriteTo(writer);
                            }
                        }

                        writer.WriteEndObject();
                    }

                    // ���浽�ļ�
                    File.WriteAllBytes(_configPath, stream.ToArray());
                }

                // ��ʾ�ɹ���Ϣ
                PathErrorText.Text = "·���ѱ���";

                // �޸���ʹ����ȷ����ɫ����
                PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
                PathErrorText.Visibility = Visibility.Visible;

                // 3������سɹ���Ϣ
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, e) => {
                    PathErrorText.Visibility = Visibility.Collapsed;
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"����ͻ���·��ʧ��: {ex}");
                PathErrorText.Text = $"����ʧ��: {ex.Message}";

                // �޸���ʹ����ȷ����ɫ����
                PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                PathErrorText.Visibility = Visibility.Visible;
            }
        }

        private bool ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                PathErrorText.Text = "·������Ϊ��";

                // �޸���ʹ����ȷ����ɫ����
                PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                PathErrorText.Visibility = Visibility.Visible;
                return false;
            }

            try
            {
                if (!Directory.Exists(path))
                {
                    PathErrorText.Text = "·�������ڻ��޷�����";

                    // �޸���ʹ����ȷ����ɫ����
                    PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    PathErrorText.Visibility = Visibility.Visible;
                    return false;
                }
            }
            catch (Exception ex)
            {
                PathErrorText.Text = $"·����֤����: {ex.Message}";

                // �޸���ʹ����ȷ����ɫ����
                PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                PathErrorText.Visibility = Visibility.Visible;
                return false;
            }

            PathErrorText.Visibility = Visibility.Collapsed;
            return true;
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // �����ļ���ѡ����
                var folderPicker = new FolderPicker();

                // �����ļ����͹�������ѡ���������ͣ�
                folderPicker.FileTypeFilter.Add("*");

                // ���ý�����ʼλ�ã�������ļ��У�
                folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;

                // ����ѡ��������
                folderPicker.ViewMode = PickerViewMode.List;
                folderPicker.CommitButtonText = "ѡ���ļ���";
                folderPicker.SettingsIdentifier = "ClientPathPicker";

                // ʹ�û���ĵ�ǰ����
                if (_currentWindow == null)
                {
                    Debug.WriteLine("�޷���ȡ��ǰ����");
                    PathErrorText.Text = "�޷���ȡ��ǰ���ڣ�������";
                    PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    PathErrorText.Visibility = Visibility.Visible;
                    return;
                }

                // ��ȡ��ǰ���ھ��
                var hwnd = WindowNative.GetWindowHandle(_currentWindow);

                // ʹ�ô��ھ����ʼ���ļ���ѡ����
                InitializeWithWindow.Initialize(folderPicker, hwnd);

                // ��ʾ�ļ���ѡ�������ȴ��û�ѡ��
                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                // �����û�ѡ��
                if (folder != null)
                {
                    // ��ȡѡ����ļ���·��
                    string selectedPath = folder.Path;

                    // ����UI�е�·����ʾ
                    ClientPathBox.Text = selectedPath;

                    // ��֤������·��
                    ValidatePath(selectedPath);
                    SaveClientPath(selectedPath);
                }
                else
                {
                    Debug.WriteLine("�û�ȡ�����ļ���ѡ��");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"����ļ���ʧ��: {ex}");
                PathErrorText.Text = $"����ļ���ʧ��: {ex.Message}";

                // �޸���ʹ����ȷ����ɫ����
                PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                PathErrorText.Visibility = Visibility.Visible;
            }
        }

        // ��ȫ��ȡ��ǰ���ڵķ���
        private Window GetCurrentWindow()
        {
            try
            {
                // ����1�����Ի�ȡ��ǰ����Ĵ���
                if (Window.Current != null)
                {
                    return Window.Current;
                }

                // ����2������ʹ��App���еľ�̬����MainWindow
                if (App.MainWindow != null)
                {
                    return App.MainWindow;
                }

                // ����3������ʹ��ApplicationView.GetForCurrentView()
                try
                {
                    var view = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                    if (view != null)
                    {
                        return Window.Current;
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"��ȡ��ǰ����ʧ��: {ex}");
            }

            return null;
        }

        private void ClientPathBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            SaveClientPath(sender.Text);
        }

        private void ClientPathBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // ÿ���ı��仯ʱ��֤·��
            ValidatePath(sender.Text);
        }

        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LightThemeRadio.IsChecked == true)
                {
                    App.SetThemePreference("Light");
                }
                else if (DarkThemeRadio.IsChecked == true)
                {
                    App.SetThemePreference("Dark");
                }
                else
                {
                    App.SetThemePreference("System");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ThemeRadio_Checked error: {ex.Message}");
            }
        }

        private void TopNavToggle_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                string position = TopNavToggle.IsOn ? "Top" : "Left";
                App.SetNavigationPosition(position);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NavPositionToggle error: {ex.Message}");
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            try
            {
                LightThemeRadio.Checked -= ThemeRadio_Checked;
                DarkThemeRadio.Checked -= ThemeRadio_Checked;
                SystemThemeRadio.Checked -= ThemeRadio_Checked;
                TopNavToggle.Toggled -= TopNavToggle_Toggled;
                BrowseButton.Click -= BrowseButton_Click;
                ClientPathBox.QuerySubmitted -= ClientPathBox_QuerySubmitted;
                ClientPathBox.TextChanged -= ClientPathBox_TextChanged;
            }
            catch
            {
                // ���Դ���
            }

            base.OnNavigatedFrom(e);
        }
    }
}