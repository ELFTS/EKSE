using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Media.Imaging;
using EKSE.Services;
using System.Windows;

namespace EKSE.Views
{
    /// <summary>
    /// AboutView.xaml 的交互逻辑
    /// </summary>
    public partial class AboutView : UserControl
    {
        public AboutView()
        {
            InitializeComponent();
            Loaded += AboutView_Loaded;
        }
        
        private void AboutView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadImages();
        }

        private void LoadImages()
        {
            try
            {
                // 加载Logo图片
                var logoImage = ResourceImageLoader.LoadImageFromResource("res://Assets/Icons/logo.png");
                LogoImage.Source = logoImage;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"无法加载Logo图片: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理超链接导航事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // 使用系统默认浏览器打开链接
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        
        /// <summary>
        /// Github按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            // 使用系统默认浏览器打开Github链接
            Process.Start(new ProcessStartInfo("https://github.com/ELFTS/keysound2") { UseShellExecute = true });
        }
        
        /// <summary>
        /// QQ群按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QQGroupButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("QQ群: 957042500", "联系信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        /// <summary>
        /// 问题反馈按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            // 使用系统默认浏览器打开反馈链接
            Process.Start(new ProcessStartInfo("https://github.com/ELFTS/keysound2/issues") { UseShellExecute = true });
        }
    }
}