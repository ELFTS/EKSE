using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using MaterialDesignColors;

namespace EKSE.Services
{
    /// <summary>
    /// 主题管理服务，负责处理应用程序的主题初始化和更新
    /// </summary>
    public class ThemeManager
    {
        /// <summary>
        /// 当前主题颜色
        /// </summary>
        public Color CurrentThemeColor { get; private set; } = Colors.Purple;
        
        private readonly PaletteHelper _paletteHelper;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public ThemeManager()
        {
            _paletteHelper = new PaletteHelper();
        }
        
        /// <summary>
        /// 初始化主题
        /// </summary>
        /// <param name="settings">应用程序设置</param>
        public void InitializeTheme(AppSettings settings)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"初始化主题，设置内容: ThemeColor={settings.ThemeColor}, ThemeType={settings.ThemeType}");
                
                var theme = _paletteHelper.GetTheme();
                
                // 设置主题类型（浅色/深色）
                theme.SetBaseTheme(settings.ThemeType == "Dark" ? 
                    MaterialDesignThemes.Wpf.BaseTheme.Dark : 
                    MaterialDesignThemes.Wpf.BaseTheme.Light);
                
                // 解析并应用主题颜色
                Color appColor;
                if (!string.IsNullOrEmpty(settings.ThemeColor))
                {
                    try
                    {
                        appColor = (Color)ColorConverter.ConvertFromString(settings.ThemeColor);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"解析主题颜色时出错: {ex.Message}");
                        appColor = Colors.Purple; // 使用默认紫色
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("主题颜色为空，使用默认紫色");
                    appColor = Colors.Purple;
                }
                
                CurrentThemeColor = appColor;
                
                // 设置主题颜色
                ApplyThemeColors(theme, appColor);
                
                // 初始化标题栏颜色
                InitializeTitleBarColor(appColor);
                
                _paletteHelper.SetTheme(theme);
                System.Diagnostics.Debug.WriteLine($"主题颜色已应用: {appColor}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"主题初始化失败: {ex.Message}");
                // 确保即使出错也设置默认标题栏颜色
                InitializeTitleBarColor(Colors.Purple);
            }
        }
        
        /// <summary>
        /// 应用主题颜色到主题对象
        /// </summary>
        /// <param name="theme">主题对象</param>
        /// <param name="color">主题颜色</param>
        private void ApplyThemeColors(Theme theme, Color color)
        {
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
        }
        
        /// <summary>
        /// 初始化标题栏颜色
        /// </summary>
        /// <param name="color">主题颜色</param>
        private void InitializeTitleBarColor(Color color)
        {
            if (Application.Current?.Resources != null)
            {
                var titleBarBrush = BrushHelper.CreateTitleBarBrush(color);
                Application.Current.Resources["TitleBarBackground"] = titleBarBrush;
                System.Diagnostics.Debug.WriteLine($"标题栏颜色已初始化: {color}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Application.Current或Resources为null，无法初始化标题栏颜色");
            }
        }
        
        /// <summary>
        /// 更新主题颜色
        /// </summary>
        /// <param name="color">新的主题颜色</param>
        public void UpdateThemeColor(Color color)
        {
            CurrentThemeColor = color;
            
            var theme = _paletteHelper.GetTheme();
            ApplyThemeColors(theme, color);
            _paletteHelper.SetTheme(theme);
            
            // 更新标题栏颜色
            InitializeTitleBarColor(color);
        }
    }
}