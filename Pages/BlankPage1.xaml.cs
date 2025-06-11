using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModPC_Gui.Pages
{
    public sealed partial class BlankPage1 : Page
    {
        // 静态变量记录访问状态
        private static bool isFirstVisit = true;

        // 定义关闭导航窗格的事件
        public static event EventHandler CloseNavigationPaneRequested;

        public BlankPage1()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (isFirstVisit)
            {
                // 使用 DispatcherQueue 延迟执行，确保所有界面加载完成
                DispatcherQueue.TryEnqueue(() =>
                {
                    // 延迟一点时间确保所有界面元素加载完成
                    var timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(100); // 适当延迟
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        // 触发关闭导航窗格事件
                        CloseNavigationPaneRequested?.Invoke(this, EventArgs.Empty);
                        isFirstVisit = false;
                    };
                    timer.Start();
                });
            }
        }
    }
}
