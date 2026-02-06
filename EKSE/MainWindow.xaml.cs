using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EKSE.Views;
using EKSE.Components;
using EKSE.Services;
using EKSE.Commands;

namespace EKSE
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<SidebarItem> sidebarItems = new List<SidebarItem>();
        
        // 服务和管理器
        private readonly SoundService _soundService;
        private readonly ProfileManager _profileManager;
        private readonly AudioFileManager _audioFileManager;

        // 用于保存设置视图实例，避免重复创建
        private SettingsView? _settingsView;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // 初始化服务和管理器
            _profileManager = new ProfileManager();
            _audioFileManager = new AudioFileManager(_profileManager);
            _soundService = new SoundService(_profileManager);
            
            // 初始化导航
            InitializeNavigation();
            
            Loaded += MainWindow_Loaded;
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;
            
            // 初始化托盘图标命令
            InitializeNotifyIconCommands();

        }
        
        /// <summary>
        /// 初始化托盘图标命令
        /// </summary>
        private void InitializeNotifyIconCommands()
        {
            // 设置托盘图标命令
            if (MyNotifyIcon != null)
            {
                // 显示窗口命令
                MyNotifyIcon.DataContext = this;
            }
        }
        

        
        /// <summary>
        /// 显示主窗口命令
        /// </summary>
        public ICommand ShowWindowCommand => new RelayCommand(ShowWindow);
        
        /// <summary>
        /// 退出应用程序命令
        /// </summary>
        public ICommand ExitApplicationCommand => new RelayCommand(ExitApplication);
        
        /// <summary>
        /// 显示窗口
        /// </summary>
        private void ShowWindow()
        {
            // 确保窗口可见
            Show();

            // 恢复窗口状态
            WindowState = WindowState.Normal;

            // 激活并置于顶层
            Activate();
            Topmost = true;
            Topmost = false;
        }
        
        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void ExitApplication()
        {
            // 直接清理托盘图标并关闭应用程序
            MyNotifyIcon?.Dispose();
            
            try
            {
                // 强制关闭应用程序
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"退出应用程序失败: {ex.Message}");
                
                // 最后的保障措施
                Environment.Exit(0);
            }
        }
        
        /// <summary>
        /// 窗口状态改变事件处理
        /// </summary>
        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            try
            {
                var settings = GetCurrentSettings();
                if (settings?.MinimizeToTray == true && WindowState == WindowState.Minimized)
                    Hide();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理窗口状态改变事件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                var settings = GetCurrentSettings();
                if (settings?.MinimizeToTray == true)
                {
                    e.Cancel = true;
                    Hide();
                }
                else
                {
                    // 正常关闭，清理托盘图标
                    MyNotifyIcon?.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理窗口关闭事件失败: {ex.Message}");
                Application.Current.Shutdown();
            }
        }
        
        /// <summary>
        /// 窗口加载完成事件处理
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSidebar();
            // 默认加载主页内容
            LoadContent("Home");
        }

        // 初始化导航
        private void InitializeNavigation()
        {
            Services.WindowNavigationHelper.InitializeNavigation(MainContentArea);
        }

        private void InitializeSidebar()
        {
            Services.WindowNavigationHelper.InitializeSidebar(SidebarPanel, sidebarItems, LoadContent);
        }

        // 加载指定类型的内容到主区域
        private void LoadContent(string contentType)
        {
            UserControl? content = null;

            switch (contentType)
            {
                case "Home":
                    content = new HomeView();
                    break;
                case "SoundSettings":
                    var soundSettingsView = new SoundSettingsView();
                    // 为声音设置页面设置服务引用
                    soundSettingsView.SetServices(_soundService, _profileManager, _audioFileManager);
                    content = soundSettingsView;
                    break;
                case "Settings":
                    // 重用SettingsView实例，避免重复创建导致的颜色重置
                    if (_settingsView == null)
                    {
                        _settingsView = new SettingsView();
                    }
                    else
                    {
                        // 如果SettingsView已经存在，则刷新设置
                        _settingsView.RefreshSettings();
                    }
                    content = _settingsView;
                    break;
                case "Sponsor":
                    content = new SponsorView();
                    break;
                case "About":
                    content = new AboutView();
                    break;
            }

            // 将内容加载到主区域
            MainContentArea.Content = content;

            // 更新侧边栏选中状态
            Services.WindowNavigationHelper.UpdateSidebarSelection(sidebarItems, contentType);
        }

        /// <summary>
        /// 获取当前设置
        /// </summary>
        private Services.AppSettings? GetCurrentSettings()
        {
            return ((App)Application.Current).SettingsManager?.GetCurrentSettings();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // 释放资源
            _soundService?.Dispose();
        }
        
        private void ShowWindowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }
        
        private void ExitApplicationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }
    }
}
