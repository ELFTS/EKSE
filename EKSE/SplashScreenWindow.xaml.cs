using System;
using System.Threading.Tasks;
using System.Windows;

namespace EKSE
{
    /// <summary>
    /// SplashScreenWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
            
            // 防止用户手动关闭启动窗口
            this.Closing += SplashScreenWindow_Closing;
        }
        
        /// <summary>
        /// 处理窗口关闭事件
        /// </summary>
        private void SplashScreenWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 如果是用户手动关闭启动窗口，则取消关闭操作
            // 只有在程序控制下才能关闭
            if (Application.Current.MainWindow == null)
            {
                e.Cancel = true;
            }
        }
        
        /// <summary>
        /// 更新启动状态文本
        /// </summary>
        /// <param name="status">状态文本</param>
        public void UpdateStatus(string status)
        {
            if (Dispatcher.CheckAccess())
            {
                StatusTextBlock.Text = status;
            }
            else
            {
                Dispatcher.Invoke(() => StatusTextBlock.Text = status);
            }
        }
        
        /// <summary>
        /// 模拟加载过程
        /// </summary>
        /// <returns></returns>
        public async Task SimulateLoading()
        {
            UpdateStatus("正在初始化...");
            await Task.Delay(800);
            
            UpdateStatus("正在加载组件...");
            await Task.Delay(1200);
            
            UpdateStatus("正在配置界面...");
            await Task.Delay(1000);
            
            UpdateStatus("即将完成...");
            await Task.Delay(800);
        }
    }
}