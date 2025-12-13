using System.Windows.Controls;
using System.Windows.Media.Imaging;
using EKSE.Services;
using System.Windows;

namespace EKSE.Views
{
    /// <summary>
    /// HomeView.xaml 的交互逻辑
    /// </summary>
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            Loaded += HomeView_Loaded;
        }

        private void HomeView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadImages();
            LoadSettings();
        }

        private void LoadImages()
        {
            try
            {
                // 加载主页图片
                var homeImage = ResourceImageLoader.LoadImageFromResource("res://Assets/Icons/logo.png");
                HomeImage.Source = homeImage;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"无法加载主页图片: {ex.Message}");
            }
        }
        
        private void LoadSettings()
        {
            try
            {
                // 从应用程序设置管理器加载当前设置
                var currentSettings = ((App)Application.Current).SettingsManager.GetCurrentSettings();
                
                // 设置音效开关状态
                SoundToggle.IsChecked = currentSettings.EnableSound;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"无法加载设置: {ex.Message}");
            }
        }
        
        private void SoundToggle_Checked(object sender, RoutedEventArgs e)
        {
            SaveSettings(true);
        }
        
        private void SoundToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveSettings(false);
        }
        
        private void SaveSettings(bool enableSound)
        {
            try
            {
                // 获取当前设置
                var settingsManager = ((App)Application.Current).SettingsManager;
                var currentSettings = settingsManager.GetCurrentSettings();
                
                // 更新音效开关设置
                currentSettings.EnableSound = enableSound;
                
                // 保存设置
                settingsManager.SaveSettings(currentSettings);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"无法保存设置: {ex.Message}");
            }
        }
    }
}