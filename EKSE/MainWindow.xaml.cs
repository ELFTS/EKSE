using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using EKSE.Views;
using EKSE.Components;
using EKSE.Services;
using EKSE.Commands;
using MaterialDesignThemes.Wpf;

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

        // 用于存储窗口正常状态时的位置和大小
        private Rect _normalWindowState = Rect.Empty;
        
        // 用于保存设置视图实例，避免重复创建
        private SettingsView? _settingsView;
        
        // Windows API 常量
        private const int WM_GETMINMAXINFO = 0x0024;
        
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
            
            // 应用垂直滑入动画
            Services.WindowAnimationHelper.ApplyVerticalSlideInAnimation(this);
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
                    
                    if (IsVisible)
                        Services.WindowAnimationHelper.StartCloseSlideOutAnimation(this, true);
                    else
                        Hide();
                }
                else
                {
                    // 正常关闭，清理托盘图标
                    MyNotifyIcon?.Dispose();
                    
                    // 取消任何挂起的动画操作
                    var rootBorder = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
                    if (rootBorder != null)
                    {
                        rootBorder.BeginAnimation(FrameworkElement.MarginProperty, null);
                        rootBorder.BeginAnimation(UIElement.OpacityProperty, null);
                    }
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
            // 应用启动动画
            Services.WindowAnimationHelper.ApplyStartupAnimation(this);
            
            InitializeSidebar();
            // 默认加载主页内容
            LoadContent("Home");
            
            // 确保标题栏颜色正确应用
            ApplySavedTitleBarColor();
        }
        
        private void ApplySavedTitleBarColor()
        {
            try
            {
                // 从设置管理器获取保存的颜色设置
                if (Application.Current is App app && app.SettingsManager != null)
                {
                    var settings = app.SettingsManager.GetCurrentSettings();
                    System.Diagnostics.Debug.WriteLine($"从设置管理器获取的设置: ThemeColor={settings?.ThemeColor}");
                    
                    Color color;
                    if (settings != null && !string.IsNullOrEmpty(settings.ThemeColor))
                    {
                        color = (Color)ColorConverter.ConvertFromString(settings.ThemeColor);
                        System.Diagnostics.Debug.WriteLine($"窗口加载时应用标题栏颜色: {color}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("设置或主题颜色为空，使用默认紫色");
                        color = Colors.Purple;
                    }
                    
                    // 应用标题栏颜色
                    ApplyTitleBarColor(color);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用保存的标题栏颜色时出错: {ex.Message}");
                // 出错时使用默认紫色
                ApplyTitleBarColor(Colors.Purple);
            }
        }
        
        /// <summary>
        /// 应用标题栏颜色
        /// </summary>
        private void ApplyTitleBarColor(Color color)
        {
            if (Application.Current?.Resources != null)
            {
                var titleBarBrush = Services.BrushHelper.CreateTitleBarBrush(color);
                Application.Current.Resources["TitleBarBackground"] = titleBarBrush;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Application.Current或Resources为null，无法应用标题栏颜色");
            }
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
                        _settingsView.ThemeColorChanged += OnThemeColorChanged;
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
            
            // 执行页面滑入动画
            Services.WindowNavigationHelper.PlayPageTransition(this);
            
            // 将内容加载到主区域
            MainContentArea.Content = content;
            
            // 更新侧边栏选中状态
            Services.WindowNavigationHelper.UpdateSidebarSelection(sidebarItems, contentType);
        }
        
        // 当主题颜色更改时的处理方法
        private void OnThemeColorChanged(object? sender, Color color)
        {
            // 强制重新绘制窗口以更新标题栏颜色
            InvalidateVisual();
        }

        #region 窗口控制事件

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            
            // 判断是双击还是单击拖动
            if (e.ClickCount == 2 && position.Y <= 30)
                ToggleWindowState();
            else if (position.Y <= 30)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = GetCurrentSettings();
            if (settings?.MinimizeToTray ?? true)
                Services.WindowAnimationHelper.StartMinimizeSlideOutAnimation(this, true);
            else
                WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = GetCurrentSettings();
            if (settings?.MinimizeToTray ?? true)
                Services.WindowAnimationHelper.StartCloseSlideOutAnimation(this, true);
            else
                Services.WindowAnimationHelper.StartCloseSlideOutAnimation(this, false, MyNotifyIcon);
        }
        
        private void ToggleWindowState()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeIcon.Kind = PackIconKind.WindowMaximize;
            }
            else
            {
                UpdateNormalWindowState();
                WindowState = WindowState.Maximized;
                MaximizeIcon.Kind = PackIconKind.WindowRestore;
            }
        }
        
        /// <summary>
        /// 更新窗口正常状态时的位置和大小
        /// </summary>
        private void UpdateNormalWindowState()
        {
            if (WindowState == WindowState.Normal)
                _normalWindowState = new Rect(Left, Top, Width, Height);
        }
        
        /// <summary>
        /// 获取当前设置
        /// </summary>
        private Services.AppSettings? GetCurrentSettings()
        {
            return ((App)Application.Current).SettingsManager?.GetCurrentSettings();
        }

        #endregion

        #region 窗口消息处理

        // 重写此方法以处理窗口消息
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 获取窗口句柄
            var handle = new WindowInteropHelper(this).Handle;
            
            // 添加窗口消息钩子
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
        }

        // 窗口消息处理函数
        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case Services.NativeMethods.WM_GETMINMAXINFO:
                    // 当窗口最大化时，调整其大小以避免遮挡任务栏
                    if (WindowState == WindowState.Normal)
                    {
                        UpdateNormalWindowState();
                    }
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        // 处理WM_GETMINMAXINFO消息
        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            // 获取屏幕工作区（不包括任务栏）
            var monitor = Services.NativeMethods.MonitorFromWindow(hwnd, Services.NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new Services.NativeMethods.MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
                
                if (Services.NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var minMaxInfo = Marshal.PtrToStructure<Services.NativeMethods.MINMAXINFO>(lParam);
                    
                    // 设置最大化时的窗口位置和大小，避免遮挡任务栏
                    minMaxInfo.ptMaxPosition.X = Math.Abs(monitorInfo.rcWork.Left - monitorInfo.rcMonitor.Left);
                    minMaxInfo.ptMaxPosition.Y = Math.Abs(monitorInfo.rcWork.Top - monitorInfo.rcMonitor.Top);
                    minMaxInfo.ptMaxSize.X = Math.Abs(monitorInfo.rcWork.Right - monitorInfo.rcWork.Left);
                    minMaxInfo.ptMaxSize.Y = Math.Abs(monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top);
                    
                    // 设置最小尺寸限制
                    minMaxInfo.ptMinTrackSize.X = (int)this.MinWidth;
                    minMaxInfo.ptMinTrackSize.Y = (int)this.MinHeight;
                    
                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                }
            }
        }

        #endregion

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