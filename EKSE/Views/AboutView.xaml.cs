using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Media.Imaging;
using EKSE.Services;

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
    }
}