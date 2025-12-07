using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using MaterialDesignColors;
using EKSE.Services;
using EKSE.Views;

namespace EKSE
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 设置管理器实例
        /// </summary>
        public SettingsManager SettingsManager { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 初始化设置管理器
            SettingsManager = new SettingsManager();
            
            // 加载保存的设置
            var settings = SettingsManager.LoadSettings();
            
            // 初始化Material Design主题
            InitializeTheme(settings);
            
            // 直接创建并显示主窗口
            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
        }
        
        private void InitializeTheme(AppSettings settings)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"初始化主题，设置内容: ThemeColor={settings.ThemeColor}, ThemeType={settings.ThemeType}");
                
                // 获取调色板助手并设置默认主题
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                
                // 根据保存的设置确定主题类型
                theme.SetBaseTheme(settings.ThemeType == "Dark" ? 
                    MaterialDesignThemes.Wpf.BaseTheme.Dark : 
                    MaterialDesignThemes.Wpf.BaseTheme.Light);
                
                // 解析并应用保存的主题颜色
                if (!string.IsNullOrEmpty(settings.ThemeColor))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(settings.ThemeColor);
                        theme.PrimaryLight = new ColorPair(
                            Color.FromArgb(100, color.R, color.G, color.B),
                            color.R + color.G + color.B > 382 ? Colors.Black : Colors.White);
                        theme.PrimaryMid = new ColorPair(color, 
                            color.R + color.G + color.B > 382 ? Colors.Black : Colors.White);
                        theme.PrimaryDark = new ColorPair(
                            Color.FromArgb(255,
                                Math.Max((byte)0, (byte)(color.R * 0.7)),
                                Math.Max((byte)0, (byte)(color.G * 0.7)),
                                Math.Max((byte)0, (byte)(color.B * 0.7))),
                            Colors.White);
                        
                        // 初始化标题栏背景色资源
                        InitializeTitleBarColor(color);
                        
                        System.Diagnostics.Debug.WriteLine($"主题颜色已应用: {settings.ThemeColor}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"解析主题颜色时出错: {ex.Message}");
                        // 使用默认紫色
                        InitializeTitleBarColor(Colors.Purple);
                    }
                }
                else
                {
                    // 使用默认紫色
                    System.Diagnostics.Debug.WriteLine("主题颜色为空，使用默认紫色");
                    InitializeTitleBarColor(Colors.Purple);
                }
                
                paletteHelper.SetTheme(theme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"主题初始化失败: {ex.Message}");
                // 确保即使出错也设置默认标题栏颜色
                InitializeTitleBarColor(Colors.Purple);
            }
        }
        
        private void InitializeTitleBarColor(Color color)
        {
            try
            {
                if (Application.Current != null && Application.Current.Resources != null)
                {
                    // 创建标题栏背景色画笔
                    var titleBarBrush = new SolidColorBrush(color);
                    
                    // 添加或更新标题栏背景色资源
                    Application.Current.Resources["TitleBarBackground"] = titleBarBrush;
                    System.Diagnostics.Debug.WriteLine($"标题栏颜色已初始化: {color}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Application.Current或Resources为null，无法初始化标题栏颜色");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化标题栏颜色时出错: {ex.Message}");
            }
        }
    }
}