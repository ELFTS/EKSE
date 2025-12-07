using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using EKSE.Services;

namespace EKSE.Views
{
    /// <summary>
    /// SponsorView.xaml 的交互逻辑
    /// </summary>
    public partial class SponsorView : UserControl
    {
        public SponsorView()
        {
            InitializeComponent();
            Loaded += SponsorView_Loaded;
        }

        private void SponsorView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadImages();
        }

        private void LoadImages()
        {
            try
            {
                // 加载微信收款码
                WeChatQRCode.Source = ResourceImageLoader.LoadImageFromResource("res://Assets/wechat_qr.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"无法加载微信收款码: {ex.Message}");
            }

            try
            {
                // 加载支付宝收款码
                AlipayQRCode.Source = ResourceImageLoader.LoadImageFromResource("res://Assets/alipay_qr.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"无法加载支付宝收款码: {ex.Message}");
            }
        }
    }
}