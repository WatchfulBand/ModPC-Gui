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
using Microsoft.UI; // 包含 Colors 类
using Microsoft.UI.Xaml.Media; // 包含 SolidColorBrush 类

namespace ModPC_Gui.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private bool _isSettingsLoaded = false;
        private string _appDirectory;
        private string _configPath;
        private Dictionary<string, JsonElement> _configData;
        private Window _currentWindow; // 缓存当前窗口

        public SettingsPage()
        {
            this.InitializeComponent();
            this.Loaded += SettingsPage_Loaded;

            // 1. 获取应用程序所在目录（退出时不保存）
            _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Debug.WriteLine($"应用程序目录: {_appDirectory}");

            // 2. 构建配置文件路径
            _configPath = Path.Combine(_appDirectory, "config.json");
            Debug.WriteLine($"配置文件路径: {_configPath}");
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isSettingsLoaded) return;

            // 在页面加载时获取当前窗口
            _currentWindow = GetCurrentWindow();

            DispatcherQueue.TryEnqueue(() =>
            {
                LoadThemeSetting();
                LoadNavPositionSetting();
                LoadClientPath();
                _isSettingsLoaded = true;
            });

            // 添加事件处理
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
                // 检查配置文件是否存在
                if (File.Exists(_configPath))
                {
                    // 读取并解析配置文件
                    string json = File.ReadAllText(_configPath);

                    // 解析整个JSON文档
                    using (JsonDocument document = JsonDocument.Parse(json))
                    {
                        // 将整个JSON文档转换为字典
                        _configData = new Dictionary<string, JsonElement>();
                        foreach (var property in document.RootElement.EnumerateObject())
                        {
                            _configData[property.Name] = property.Value.Clone();
                        }

                        // 获取路径值
                        if (_configData.TryGetValue("path", out JsonElement pathElement))
                        {
                            string path = pathElement.GetString();
                            ClientPathBox.Text = path;
                            ValidatePath(path);
                        }
                        else
                        {
                            PathErrorText.Text = "配置文件中未找到路径信息";
                            PathErrorText.Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("配置文件不存在");
                    PathErrorText.Text = "配置文件不存在，请设置路径";
                    PathErrorText.Visibility = Visibility.Visible;

                    // 初始化空配置
                    _configData = new Dictionary<string, JsonElement>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载客户端路径失败: {ex}");
                PathErrorText.Text = $"加载配置失败: {ex.Message}";
                PathErrorText.Visibility = Visibility.Visible;

                // 初始化空配置
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

                // 确保配置数据存在
                if (_configData == null)
                {
                    _configData = new Dictionary<string, JsonElement>();
                }

                // 创建新的JSON对象（紧凑格式）
                using (MemoryStream stream = new MemoryStream())
                {
                    // 使用紧凑格式（无缩进）
                    using (Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions
                    {
                        Indented = false // 禁用缩进，生成紧凑JSON
                    }))
                    {
                        writer.WriteStartObject();

                        // 写入所有配置项
                        foreach (var kvp in _configData)
                        {
                            writer.WritePropertyName(kvp.Key);

                            // 特殊处理path字段
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

                    // 保存到文件
                    File.WriteAllBytes(_configPath, stream.ToArray());
                }

                // 显示成功消息
                PathErrorText.Text = "路径已保存";

                // 修复：使用正确的颜色引用
                PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
                PathErrorText.Visibility = Visibility.Visible;

                // 3秒后隐藏成功消息
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
                Debug.WriteLine($"保存客户端路径失败: {ex}");
                PathErrorText.Text = $"保存失败: {ex.Message}";

                // 修复：使用正确的颜色引用
                PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                PathErrorText.Visibility = Visibility.Visible;
            }
        }

        private bool ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                PathErrorText.Text = "路径不能为空";

                // 修复：使用正确的颜色引用
                PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                PathErrorText.Visibility = Visibility.Visible;
                return false;
            }

            try
            {
                if (!Directory.Exists(path))
                {
                    PathErrorText.Text = "路径不存在或无法访问";

                    // 修复：使用正确的颜色引用
                    PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    PathErrorText.Visibility = Visibility.Visible;
                    return false;
                }
            }
            catch (Exception ex)
            {
                PathErrorText.Text = $"路径验证错误: {ex.Message}";

                // 修复：使用正确的颜色引用
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
                // 创建文件夹选择器
                var folderPicker = new FolderPicker();

                // 设置文件类型过滤器（选择所有类型）
                folderPicker.FileTypeFilter.Add("*");

                // 设置建议起始位置（计算机文件夹）
                folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;

                // 设置选择器标题
                folderPicker.ViewMode = PickerViewMode.List;
                folderPicker.CommitButtonText = "选择文件夹";
                folderPicker.SettingsIdentifier = "ClientPathPicker";

                // 使用缓存的当前窗口
                if (_currentWindow == null)
                {
                    Debug.WriteLine("无法获取当前窗口");
                    PathErrorText.Text = "无法获取当前窗口，请重试";
                    PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    PathErrorText.Visibility = Visibility.Visible;
                    return;
                }

                // 获取当前窗口句柄
                var hwnd = WindowNative.GetWindowHandle(_currentWindow);

                // 使用窗口句柄初始化文件夹选择器
                InitializeWithWindow.Initialize(folderPicker, hwnd);

                // 显示文件夹选择器并等待用户选择
                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                // 处理用户选择
                if (folder != null)
                {
                    // 获取选择的文件夹路径
                    string selectedPath = folder.Path;

                    // 更新UI中的路径显示
                    ClientPathBox.Text = selectedPath;

                    // 验证并保存路径
                    ValidatePath(selectedPath);
                    SaveClientPath(selectedPath);
                }
                else
                {
                    Debug.WriteLine("用户取消了文件夹选择");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"浏览文件夹失败: {ex}");
                PathErrorText.Text = $"浏览文件夹失败: {ex.Message}";

                // 修复：使用正确的颜色引用
                PathErrorText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                PathErrorText.Visibility = Visibility.Visible;
            }
        }

        // 安全获取当前窗口的方法
        private Window GetCurrentWindow()
        {
            try
            {
                // 方法1：尝试获取当前激活的窗口
                if (Window.Current != null)
                {
                    return Window.Current;
                }

                // 方法2：尝试使用App类中的静态属性MainWindow
                if (App.MainWindow != null)
                {
                    return App.MainWindow;
                }

                // 方法3：尝试使用ApplicationView.GetForCurrentView()
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
                Debug.WriteLine($"获取当前窗口失败: {ex}");
            }

            return null;
        }

        private void ClientPathBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            SaveClientPath(sender.Text);
        }

        private void ClientPathBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // 每次文本变化时验证路径
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
                // 忽略错误
            }

            base.OnNavigatedFrom(e);
        }
    }
}