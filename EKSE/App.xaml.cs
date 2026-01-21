using System;
using System.Windows;
using EKSE.Services;

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
        
        /// <summary>
        /// 主题管理服务
        /// </summary>
        public ThemeManager? ThemeManager { get; private set; }
        
        /// <summary>
        /// 按钮样式管理服务
        /// </summary>
        public ButtonStyleManager? ButtonStyleManager { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 初始化设置管理器（使用单例模式）
            SettingsManager = SettingsManager.Instance;
            
            // 加载保存的设置
            var settings = SettingsManager.LoadSettings();
            
            // 初始化主题管理服务
            ThemeManager = new ThemeManager();
            ThemeManager.InitializeTheme(settings);
            
            // 初始化按钮样式管理服务
            ButtonStyleManager = new ButtonStyleManager(ThemeManager);
            ButtonStyleManager.UpdateGradientButtonStyle();
            
            // 直接创建并显示主窗口
            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
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