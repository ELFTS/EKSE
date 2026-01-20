using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
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
        public SettingsManager? SettingsManager { get; private set; }
        
        // 存储当前主题颜色
        private Color _currentThemeColor = Colors.Purple;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 初始化设置管理器（使用单例模式）
            SettingsManager = SettingsManager.Instance;
            
            // 加载保存的设置
            var settings = SettingsManager.LoadSettings();
            
            // 初始化Material Design主题
            InitializeTheme(settings);
            
            // 创建渐变按钮样式
            CreateGradientButtonStyle();
            
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
                        _currentThemeColor = color; // 保存当前主题颜色
                        
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
                    // 创建标题栏背景色画笔 - 使用线性渐变而不是纯色
                    var titleBarBrush = BrushHelper.CreateTitleBarBrush(color);
                    
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
        
        /// <summary>
        /// 获取当前主题颜色
        /// </summary>
        /// <returns>当前主题颜色</returns>
        public Color GetCurrentThemeColor()
        {
            return _currentThemeColor;
        }
        
        /// <summary>
        /// 设置当前主题颜色
        /// </summary>
        /// <param name="color">新的主题颜色</param>
        public void SetCurrentThemeColor(Color color)
        {
            _currentThemeColor = color;
        }
        
        /// <summary>
        /// 创建渐变按钮样式
        /// </summary>
        private void CreateGradientButtonStyle()
        {
            try
            {
                if (Application.Current != null && Application.Current.Resources != null)
                {
                    // 创建新的样式，基于MaterialDesignRaisedButton
                    var gradientButtonStyle = new Style(typeof(Button), 
                        (Style)Application.Current.Resources["MaterialDesignRaisedButton"]);
                    
                    // 设置默认背景为渐变色
                    var backgroundSetter = new Setter();
                    backgroundSetter.Property = Control.BackgroundProperty;
                    backgroundSetter.Value = BrushHelper.CreateGradientBrush(GetCurrentThemeColor());
                    gradientButtonStyle.Setters.Add(backgroundSetter);
                    
                    // 创建鼠标悬停触发器
                    var mouseOverTrigger = new Trigger();
                    mouseOverTrigger.Property = UIElement.IsMouseOverProperty;
                    mouseOverTrigger.Value = true;
                    var mouseOverSetter = new Setter();
                    mouseOverSetter.Property = Control.BackgroundProperty;
                    mouseOverSetter.Value = BrushHelper.CreateGradientBrush(GetCurrentThemeColor());
                    mouseOverTrigger.Setters.Add(mouseOverSetter);
                    gradientButtonStyle.Triggers.Add(mouseOverTrigger);
                    
                    // 创建按钮按下触发器
                    var pressedTrigger = new Trigger();
                    pressedTrigger.Property = Button.IsPressedProperty;
                    pressedTrigger.Value = true;
                    var pressedSetter = new Setter();
                    pressedSetter.Property = Control.BackgroundProperty;
                    // 使用稍暗的颜色作为按下状态
                    var baseColor = GetCurrentThemeColor();
                    var darkerColor = Color.FromArgb(255,
                        (byte)Math.Max(0, (int)(baseColor.R * 0.8)),
                        (byte)Math.Max(0, (int)(baseColor.G * 0.8)),
                        (byte)Math.Max(0, (int)(baseColor.B * 0.8)));
                    pressedSetter.Value = BrushHelper.CreateGradientBrush(darkerColor);
                    pressedTrigger.Setters.Add(pressedSetter);
                    gradientButtonStyle.Triggers.Add(pressedTrigger);
                    
                    // 添加样式到应用程序资源
                    Application.Current.Resources["GradientButtonStyle"] = gradientButtonStyle;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建渐变按钮样式时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新渐变按钮样式以反映主题颜色变化
        /// </summary>
        public void UpdateGradientButtonStyle()
        {
            CreateGradientButtonStyle();
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            
            // 清理托盘图标
            try
            {
                if (MainWindow is MainWindow mainWindow && mainWindow.MyNotifyIcon != null)
                {
                    mainWindow.MyNotifyIcon.Dispose();
                    mainWindow.MyNotifyIcon = null;
                }
            }
            catch (Exception ex)
            {
                // 忽略托盘图标清理过程中的任何异常
                System.Diagnostics.Debug.WriteLine($"清理托盘图标时出错: {ex.Message}");
            }
        }
    }
}