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
        // ��̬������¼����״̬
        private static bool isFirstVisit = true;

        // ����رյ���������¼�
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
                // ʹ�� DispatcherQueue �ӳ�ִ�У�ȷ�����н���������
                DispatcherQueue.TryEnqueue(() =>
                {
                    // �ӳ�һ��ʱ��ȷ�����н���Ԫ�ؼ������
                    var timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(100); // �ʵ��ӳ�
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        // �����رյ��������¼�
                        CloseNavigationPaneRequested?.Invoke(this, EventArgs.Empty);
                        isFirstVisit = false;
                    };
                    timer.Start();
                });
            }
        }
    }
}
