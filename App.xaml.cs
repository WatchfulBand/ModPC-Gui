using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ModPC_Gui.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;  // 保留System.IO.Path
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinRT.Interop;

namespace ModPC_Gui
{
    public sealed partial class App : Application
    {
        private const string ThemePreferenceKey = "ThemePreference";
        private const string NavigationPositionKey = "NavigationPosition";

        // 第一个文件中的成员
        public static new App Current => (App)Application.Current;

        // 修复CS0272：将set改为private
        public UserSession CurrentSession { get; private set; }

        public bool IsLoggedIn => CurrentSession != null && CurrentSession.IsValid;

        public static string DataPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ModPC_Gui");

        // 临时数据存储

        public static class Temp
        {
            public static string PE_url = "https://g79apigatewayobt.minecraft.cn";
            public static string engineVersion = "1.14.6.45947";
            public static string libminecraftpe = "1.0.0";
            public static string patchVersion = "2023.10";
            public static string patch = "001";
            public static int offset = 2;
            public static int rounds = 9;
        }

        // 读取JSON文件
        public static string ReadJson(string fileName)
        {
            string filePath = Path.Combine(DataPath, fileName);
            return File.Exists(filePath) ? File.ReadAllText(filePath) : "{}";
        }

        // 用户会话类
        public class UserSession
        {
            public string Uid { get; set; }
            public string Token { get; set; }
            public string Nickname { get; set; }
            public DateTime ExpiresAt { get; set; }

            public bool IsValid => !string.IsNullOrEmpty(Token) && ExpiresAt > DateTime.UtcNow;

            public UserSession(LoginResult result)
            {
                Uid = result.data.uid;
                Token = result.data.token;
                Nickname = result.data.nickname;
                ExpiresAt = result.data.expires_at;
            }
        }
        
        // 第二个文件中的成员
        public static Window MainWindow { get; private set; }

        private static List<Window> _windows = new List<Window>();
        private static UISettings _uiSettings;
        private static DispatcherQueue _dispatcherQueue;
        private static bool _isInitialized = false;

        // 添加ApplicationData初始化检查
        public static bool IsApplicationDataAvailable => ApplicationData.Current != null;

        // 合并构造函数
        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += OnUnhandledException;

            // 来自第一个文件的初始化
            // 确保数据目录存在
            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine($"全局异常: {e.Exception}");
            e.Handled = true;
        }

        // 修复CS0104：使用完全限定名
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // 来自第一个文件
            MainWindow = new MainWindow();
            MainWindow.Activate();

            // 来自第二个文件
            _windows.Add(MainWindow);
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            ConfigureTitleBar(MainWindow);

            // 确保ApplicationData就绪后再初始化主题
            _dispatcherQueue.TryEnqueue(() =>
            {
                ApplyTheme();
                WatchSystemThemeChanges();
            });
            // 确保数据目录存在
            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }

            // 确保 sa_data_pe.json 存在
            var saDataPath = Path.Combine(DataPath, "sa_data_pe.json");
            if (!File.Exists(saDataPath))
            {
                var defaultSaData = @"{
                    ""app_channel"": ""netease"",
                    ""app_ver"": ""3.1.12.261439"",
                    ""core_num"": ""8"",
                    ""cpu_digit"": ""64"",
                    ""cpu_hz"": ""1882000"",
                    ""cpu_name"": ""vendor Kirin810"",
                    ""device_height"": ""2000"",
                    ""device_model"": ""HUAWEI BAH3-W09"",
                    ""device_width"": ""1200"",
                    ""disk"": """",
                    ""emulator"": 0,
                    ""first_udid"": ""11ff2c22e0b4b5a6"",
                    ""is_guest"": 0,
                    ""launcher_type"": ""PE_C++"",
                    ""mac_addr"": ""02:00:00:00:00:00"",
                    ""network"": ""CHANNEL_UNKNOW"",
                    ""os_name"": ""android"",
                    ""os_ver"": ""7.1.2"",
                    ""ram"": ""6130167808"",
                    ""rom"": ""114965872640"",
                    ""root"": false,
                    ""sdk_ver"": ""5.2.0"",
                    ""start_type"": ""default"",
                    ""udid"": ""11ff2c22e0b4b5a6""
                }";
                File.WriteAllText(saDataPath, defaultSaData);
            }
                _isInitialized = true;
        }

        private void ConfigureTitleBar(Window window)
        {
            if (window == null) return;

            try
            {
                window.ExtendsContentIntoTitleBar = true;
                var hwnd = WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                if (appWindow?.TitleBar != null)
                {
                    appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
                    UpdateTitleBarButtonColors(appWindow.TitleBar);
                }
            }
            catch (Exception ex)
            {
                DebugWriteLine($"ConfigureTitleBar error: {ex.Message}");
            }
        }

        public static void ApplyTheme()
        {
            if (!_isInitialized) return;
            if (_dispatcherQueue == null) return;

            if (_dispatcherQueue.HasThreadAccess)
            {
                ApplyThemeInternal();
            }
            else
            {
                _dispatcherQueue.TryEnqueue(() => ApplyThemeInternal());
            }
        }

        private static void ApplyThemeInternal()
        {
            try
            {
                string themePreference = GetCurrentThemePreference();
                ApplyThemeToAllWindows(themePreference);
                UpdateAllTitleBarButtonColors();
            }
            catch (Exception ex)
            {
                DebugWriteLine($"ApplyThemeInternal error: {ex.Message}");
            }
        }

        private static void ApplyThemeToAllWindows(string themePreference)
        {
            if (_windows == null) return;

            foreach (var window in _windows)
            {
                if (window?.Content is FrameworkElement rootElement)
                {
                    try
                    {
                        switch (themePreference)
                        {
                            case "Light":
                                rootElement.RequestedTheme = ElementTheme.Light;
                                break;
                            case "Dark":
                                rootElement.RequestedTheme = ElementTheme.Dark;
                                break;
                            default:
                                rootElement.RequestedTheme = ElementTheme.Default;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugWriteLine($"ApplyThemeToWindow error: {ex.Message}");
                    }
                }
            }
        }

        private static void UpdateAllTitleBarButtonColors()
        {
            if (_windows == null) return;

            foreach (var window in _windows)
            {
                UpdateTitleBarButtonColors(window);
            }
        }

        private static void UpdateTitleBarButtonColors(Window window)
        {
            if (window == null) return;

            try
            {
                var hwnd = WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                if (appWindow?.TitleBar != null)
                {
                    UpdateTitleBarButtonColors(appWindow.TitleBar);
                }
            }
            catch (Exception ex)
            {
                DebugWriteLine($"UpdateTitleBarButtonColors error: {ex.Message}");
            }
        }

        private static void UpdateTitleBarButtonColors(AppWindowTitleBar titleBar)
        {
            if (titleBar == null) return;

            try
            {
                bool isDarkMode = GetActualTheme() == ElementTheme.Dark;

                if (isDarkMode)
                {
                    titleBar.ButtonForegroundColor = Colors.White;
                    titleBar.ButtonHoverForegroundColor = Colors.White;
                    titleBar.ButtonPressedForegroundColor = Colors.White;
                    titleBar.ButtonInactiveForegroundColor = Colors.Gray;
                    titleBar.ButtonBackgroundColor = Colors.Transparent;
                    titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 40, 40, 40);
                    titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 60, 60, 60);
                }
                else
                {
                    titleBar.ButtonForegroundColor = Colors.Black;
                    titleBar.ButtonHoverForegroundColor = Colors.Black;
                    titleBar.ButtonPressedForegroundColor = Colors.Black;
                    titleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
                    titleBar.ButtonBackgroundColor = Colors.Transparent;
                    titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 230, 230, 230);
                    titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 210, 210, 210);
                }
            }
            catch (Exception ex)
            {
                DebugWriteLine($"UpdateTitleBarColors error: {ex.Message}");
            }
        }

        private static ElementTheme GetActualTheme()
        {
            try
            {
                string themePreference = GetCurrentThemePreference();

                if (themePreference == "System")
                {
                    var uiSettings = new UISettings();
                    var background = uiSettings.GetColorValue(UIColorType.Background);
                    return background == Colors.Black ? ElementTheme.Dark : ElementTheme.Light;
                }

                return themePreference == "Dark" ? ElementTheme.Dark : ElementTheme.Light;
            }
            catch
            {
                return ElementTheme.Light;
            }
        }

        public static string GetCurrentThemePreference()
        {
            // 检查ApplicationData是否可用
            if (!IsApplicationDataAvailable) return "System";

            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                return localSettings.Values.TryGetValue(ThemePreferenceKey, out object value)
                    ? value as string
                    : "System";
            }
            catch
            {
                return "System";
            }
        }

        public static void SetThemePreference(string preference)
        {
            if (!IsApplicationDataAvailable) return;

            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[ThemePreferenceKey] = preference;
                ApplyTheme();
            }
            catch (Exception ex)
            {
                DebugWriteLine($"SetThemePreference error: {ex.Message}");
            }
        }

        public static string GetNavigationPosition()
        {
            if (!IsApplicationDataAvailable) return "Left";

            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                return localSettings.Values.TryGetValue(NavigationPositionKey, out object value)
                    ? value as string
                    : "Left";
            }
            catch
            {
                return "Left";
            }
        }

        public static void SetNavigationPosition(string position)
        {
            if (!IsApplicationDataAvailable) return;

            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[NavigationPositionKey] = position;
                NavigationPositionChanged?.Invoke(position);
            }
            catch (Exception ex)
            {
                DebugWriteLine($"SetNavigationPosition error: {ex.Message}");
            }
        }

        public static void WatchSystemThemeChanges()
        {
            try
            {
                _uiSettings = new UISettings();
                _uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
            }
            catch (Exception ex)
            {
                DebugWriteLine($"WatchSystemThemeChanges error: {ex.Message}");
            }
        }

        private static void UiSettings_ColorValuesChanged(UISettings sender, object args)
        {
            try
            {
                if (GetCurrentThemePreference() == "System")
                {
                    if (_dispatcherQueue == null) return;

                    if (_dispatcherQueue.HasThreadAccess)
                    {
                        ApplyThemeInternal();
                    }
                    else
                    {
                        _dispatcherQueue.TryEnqueue(() => ApplyThemeInternal());
                    }
                }
            }
            catch (Exception ex)
            {
                DebugWriteLine($"ColorValuesChanged error: {ex.Message}");
            }
        }

        public static void RegisterWindow(Window window)
        {
            if (window == null) return;

            if (_windows == null)
                _windows = new List<Window>();

            if (!_windows.Contains(window))
            {
                _windows.Add(window);
            }
        }

        public static void UnregisterWindow(Window window)
        {
            if (window == null || _windows == null) return;
            _windows.Remove(window);
        }

        private static void DebugWriteLine(string message)
        {
            Debug.WriteLine(message);
        }

        public static event Action<string> NavigationPositionChanged;

        // 添加设置会话的方法
        public void SetSession(LoginResult result)
        {
            CurrentSession = new UserSession(result);
        }
    }
}