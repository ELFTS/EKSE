using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media;
using EKSE.Views;
using EKSE.Components;
using EKSE.Services;
using EKSE.Models;
using EKSE.Commands;
using MaterialDesignThemes.Wpf;
using MaterialDesignColors;
using Hardcodet.Wpf.TaskbarNotification;

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
        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;
        
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
            try
            {
                // 设置托盘图标命令
                if (MyNotifyIcon != null)
                {
                    // 显示窗口命令
                    MyNotifyIcon.DataContext = this;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化托盘图标命令失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化系统托盘图标
        /// </summary>
        private void InitializeNotifyIcon()
        {
            if (MyNotifyIcon != null)
            {
                // 创建命令
                MyNotifyIcon.DataContext = this;
                
                // 由于我们使用命令绑定，所以不需要手动处理事件
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
            try
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
                ApplyVerticalSlideInAnimation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示窗口失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 应用垂直滑入动画
        /// </summary>
        private void ApplyVerticalSlideInAnimation()
        {
            try
            {
                // 查找最外层的Border元素作为动画目标
                var rootBorder = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
                if (rootBorder != null)
                {
                    // 创建一个新的Storyboard来管理动画
                    Storyboard slideInStoryboard = new Storyboard();
                    
                    // 创建厚度动画实现垂直滑入效果
                    ThicknessAnimation thicknessAnimation = new ThicknessAnimation
                    {
                        From = new Thickness(0, -ActualHeight, 0, 0),
                        To = new Thickness(0, 0, 0, 0),
                        Duration = TimeSpan.FromMilliseconds(500)
                    };
                    thicknessAnimation.EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut };
                    Storyboard.SetTarget(thicknessAnimation, rootBorder);
                    Storyboard.SetTargetProperty(thicknessAnimation, new PropertyPath("(FrameworkElement.Margin)"));
                    
                    // 创建透明度动画实现淡入效果
                    DoubleAnimation opacityAnimation = new DoubleAnimation
                    {
                        From = 0.0,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(500)
                    };
                    Storyboard.SetTarget(opacityAnimation, rootBorder);
                    Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("(UIElement.Opacity)"));
                    
                    // 将动画添加到Storyboard中
                    slideInStoryboard.Children.Add(thicknessAnimation);
                    slideInStoryboard.Children.Add(opacityAnimation);
                    
                    // 开始动画
                    slideInStoryboard.Begin();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用垂直滑入动画失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void ExitApplication()
        {
            try
            {
                // 直接清理托盘图标并关闭应用程序
                if (MyNotifyIcon != null)
                {
                    MyNotifyIcon.Dispose();
                }
                
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
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            try
            {
                // 从设置管理器获取保存的设置
                var settings = ((App)Application.Current).SettingsManager.GetCurrentSettings();
                
                // 如果启用了最小化到托盘功能且窗口被最小化
                if (settings.MinimizeToTray && WindowState == WindowState.Minimized)
                {
                    // 隐藏窗口而不是最小化到任务栏
                    this.Hide();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理窗口状态改变事件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // 从设置管理器获取保存的设置
                var settings = ((App)Application.Current).SettingsManager?.GetCurrentSettings();
                
                // 如果启用了最小化到托盘功能且设置不为null
                if (settings?.MinimizeToTray == true)
                {
                    // 取消关闭操作
                    e.Cancel = true;
                    
                    // 如果窗口可见，启动关闭滑出动画并隐藏窗口
                    if (IsVisible)
                    {
                        StartCloseSlideOutAnimation(true);
                    }
                    else
                    {
                        // 如果窗口已经不可见，直接隐藏
                        Hide();
                    }
                }
                else
                {
                    // 正常关闭，清理托盘图标
                    if (MyNotifyIcon != null)
                    {
                        MyNotifyIcon.Dispose();
                    }
                    
                    // 取消任何挂起的动画操作并关闭窗口
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
                // 出现错误时，确保窗口能正常关闭
                Application.Current.Shutdown();
            }
        }
        
        /// <summary>
        /// 窗口加载完成事件处理
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 应用启动动画
            ApplyStartupAnimation();
            
            InitializeSidebar();
            // 默认加载主页内容
            LoadContent("Home");
            
            // 确保标题栏颜色正确应用
            ApplySavedTitleBarColor();
        }
        
        /// <summary>
        /// 应用启动动画效果
        /// </summary>
        private void ApplyStartupAnimation()
        {
            try
            {
                // 启动垂直滑动动画
                StartVerticalSlideAnimation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动动画失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 启动垂直滑动动画
        /// </summary>
        private void StartVerticalSlideAnimation()
        {
            try
            {
                // 查找最外层的Border元素作为动画目标
                var rootBorder = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
                if (rootBorder != null)
                {
                    // 创建一个新的Storyboard来控制动画
                    var storyboard = new Storyboard();
                    
                    // 创建厚度动画实现垂直滑动效果
                    var thicknessAnimation = new ThicknessAnimation
                    {
                        From = new Thickness(0, -ActualHeight, 0, 0),
                        To = new Thickness(0, 0, 0, 0),
                        Duration = TimeSpan.FromSeconds(0.8),
                        EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(thicknessAnimation, rootBorder);
                    Storyboard.SetTargetProperty(thicknessAnimation, new PropertyPath("(FrameworkElement.Margin)"));
                    storyboard.Children.Add(thicknessAnimation);
                    
                    // 创建透明度动画实现淡入效果
                    var opacityAnimation = new DoubleAnimation
                    {
                        From = 0.0,
                        To = 1.0,
                        Duration = TimeSpan.FromSeconds(0.8)
                    };
                    Storyboard.SetTarget(opacityAnimation, rootBorder);
                    Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("(UIElement.Opacity)"));
                    storyboard.Children.Add(opacityAnimation);
                    
                    // 开始动画
                    storyboard.Begin();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"垂直滑动动画失败: {ex.Message}");
            }
        }
        
        private void ApplySavedTitleBarColor()
        {
            try
            {
                // 从设置管理器获取保存的颜色设置
                var settings = ((App)Application.Current).SettingsManager.GetCurrentSettings();
                System.Diagnostics.Debug.WriteLine($"从设置管理器获取的设置: ThemeColor={settings?.ThemeColor}");
                
                if (settings != null && !string.IsNullOrEmpty(settings.ThemeColor))
                {
                    var color = (Color)ColorConverter.ConvertFromString(settings.ThemeColor);
                    if (Application.Current != null && Application.Current.Resources != null)
                    {
                        // 创建标题栏背景色画笔 - 使用线性渐变而不是纯色
                        var titleBarBrush = BrushHelper.CreateTitleBarBrush(color);
                        
                        // 更新标题栏背景色资源
                        Application.Current.Resources["TitleBarBackground"] = titleBarBrush;
                        System.Diagnostics.Debug.WriteLine($"窗口加载时应用标题栏颜色: {color}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Application.Current或Resources为null，无法应用标题栏颜色");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("设置或主题颜色为空，使用默认紫色");
                    // 使用默认紫色
                    var defaultColor = Colors.Purple;
                    var titleBarBrush = BrushHelper.CreateTitleBarBrush(defaultColor);
                    if (Application.Current != null && Application.Current.Resources != null)
                    {
                        Application.Current.Resources["TitleBarBackground"] = titleBarBrush;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用保存的标题栏颜色时出错: {ex.Message}");
                // 出错时使用默认紫色
                var defaultColor = Colors.Purple;
                var titleBarBrush = BrushHelper.CreateTitleBarBrush(defaultColor);
                if (Application.Current != null && Application.Current.Resources != null)
                {
                    Application.Current.Resources["TitleBarBackground"] = titleBarBrush;
                }
            }
        }

        // 初始化导航
        private void InitializeNavigation()
        {
            // 设置默认视图
            MainContentArea.Content = new HomeView();
            
            // 绑定侧边栏项目事件
            // 注意：这里我们不直接绑定控件事件，因为控件是在InitializeSidebar中动态创建的
        }

        private void InitializeSidebar()
        {
            // 清空现有项目
            SidebarPanel.Children.Clear();
            sidebarItems.Clear();

            // 添加侧边栏菜单项
            var homeItem = new SidebarItem
            {
                Text = "主页",
                Icon = "res://Assets/Icons/home.png"
            };
            homeItem.Click += (sender, e) => LoadContent("Home");
            SidebarPanel.Children.Add(homeItem);
            sidebarItems.Add(homeItem);

            var soundItem = new SidebarItem
            {
                Text = "音效设置",
                Icon = "res://Assets/Icons/music.png"
            };
            soundItem.Click += (sender, e) => LoadContent("SoundSettings");
            SidebarPanel.Children.Add(soundItem);
            sidebarItems.Add(soundItem);

            var settingsItem = new SidebarItem
            {
                Text = "系统设置",
                Icon = "res://Assets/Icons/settings.png"
            };
            settingsItem.Click += (sender, e) => LoadContent("Settings");
            SidebarPanel.Children.Add(settingsItem);
            sidebarItems.Add(settingsItem);

            var sponsorItem = new SidebarItem
            {
                Text = "赞助",
                Icon = "res://Assets/Icons/sponsor.png"
            };
            sponsorItem.Click += (sender, e) => LoadContent("Sponsor");
            SidebarPanel.Children.Add(sponsorItem);
            sidebarItems.Add(sponsorItem);

            var aboutItem = new SidebarItem
            {
                Text = "关于",
                Icon = "res://Assets/Icons/info.png"
            };
            aboutItem.Click += (sender, e) => LoadContent("About");
            SidebarPanel.Children.Add(aboutItem);
            sidebarItems.Add(aboutItem);

            // 设置默认选中项
            if (sidebarItems.Count > 0)
            {
                sidebarItems[0].IsActive = true;
            }
        }

        // 更新侧边栏选择状态
        private void UpdateSidebarSelection(string contentType)
        {
            // 取消所有项目的选中状态
            foreach (var item in sidebarItems)
            {
                item.IsActive = false;
            }

            // 根据内容类型设置对应的侧边栏项目为选中状态
            switch (contentType)
            {
                case "Home":
                    if (sidebarItems.Count > 0)
                        sidebarItems[0].IsActive = true;
                    break;
                case "SoundSettings":
                    if (sidebarItems.Count > 1)
                        sidebarItems[1].IsActive = true;
                    break;
                case "Settings":
                    if (sidebarItems.Count > 2)
                        sidebarItems[2].IsActive = true;
                    break;
                case "Sponsor":
                    if (sidebarItems.Count > 3)
                        sidebarItems[3].IsActive = true;
                    break;
                case "About":
                    if (sidebarItems.Count > 4)
                        sidebarItems[4].IsActive = true;
                    break;
            }
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
            PlayPageTransition();
            
            // 将内容加载到主区域
            MainContentArea.Content = content;
            
            // 更新侧边栏选中状态
            UpdateSidebarSelection(contentType);
        }
        
        // 执行页面切换动画
        private void PlayPageTransition()
        {
            // 使用滑入动画
            var slideInStoryboard = (Storyboard)Resources["SlideInFromLeftStoryboard"];
            slideInStoryboard.Begin();
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
            // 记录鼠标点击时的位置
            var position = e.GetPosition(this);
            
            // 判断是单击还是双击
            if (e.ClickCount == 2 && position.Y <= 30) // 30是标题栏高度
            {
                // 双击标题栏最大化/还原窗口
                ToggleWindowState();
            }
            else if (position.Y <= 30) // 点击在标题栏区域
            {
                // 拖动窗口
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否启用了最小化到托盘功能
            var settings = ((App)Application.Current).SettingsManager?.GetCurrentSettings();
            bool minimizeToTray = settings?.MinimizeToTray ?? true;
            
            if (minimizeToTray)
            {
                // 启动最小化滑出动画
                StartMinimizeSlideOutAnimation(true);
            }
            else
            {
                WindowState = WindowState.Minimized;
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否启用了最小化到托盘功能
            var settings = ((App)Application.Current).SettingsManager?.GetCurrentSettings();
            bool minimizeToTray = settings?.MinimizeToTray ?? true;
            
            if (minimizeToTray)
            {
                // 启动关闭滑出动画并隐藏窗口
                StartCloseSlideOutAnimation(true);
            }
            else
            {
                // 启动关闭滑出动画并关闭应用程序
                StartCloseSlideOutAnimation(false);
            }
        }
        
        /// <summary>
        /// 启动最小化滑出动画
        /// </summary>
        /// <param name="hideAfterAnimation">动画结束后是否隐藏窗口</param>
        private void StartMinimizeSlideOutAnimation(bool hideAfterAnimation)
        {
            try
            {
                // 查找最外层的Border元素作为动画目标
                var rootBorder = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
                if (rootBorder != null)
                {
                    // 获取最小化滑出动画故事板
                    Storyboard minimizeStoryboard = (Storyboard)FindResource("MinimizeSlideOutStoryboard");
                    
                    // 添加动画完成事件处理
                    EventHandler? completedHandler = null;
                    completedHandler = (s, e) => {
                        // 移除事件处理程序以避免内存泄漏
                        minimizeStoryboard.Completed -= completedHandler;
                        
                        // 动画结束后隐藏窗口
                        if (hideAfterAnimation)
                        {
                            Hide();
                        }
                    };
                    
                    minimizeStoryboard.Completed += completedHandler;
                    
                    // 开始最小化滑出动画
                    minimizeStoryboard.Begin(rootBorder);
                }
                else
                {
                    // 如果找不到根元素，直接隐藏窗口
                    if (hideAfterAnimation)
                    {
                        Hide();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"最小化滑出动画失败: {ex.Message}");
                // 如果动画失败，直接隐藏窗口
                if (hideAfterAnimation)
                {
                    Hide();
                }
            }
        }
        
        /// <summary>
        /// 启动关闭滑出动画
        /// </summary>
        /// <param name="hideAfterAnimation">动画结束后是否隐藏窗口</param>
        private void StartCloseSlideOutAnimation(bool hideAfterAnimation)
        {
            try
            {
                // 查找最外层的Border元素作为动画目标
                var rootBorder = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
                if (rootBorder != null)
                {
                    // 获取关闭滑出动画故事板
                    Storyboard closeStoryboard = (Storyboard)FindResource("CloseSlideOutStoryboard");
                    
                    // 添加动画完成事件处理
                    EventHandler? completedHandler = null;
                    completedHandler = (s, e) => {
                        // 移除事件处理程序以避免内存泄漏
                        closeStoryboard.Completed -= completedHandler;
                        
                        // 动画结束后隐藏窗口或关闭应用程序
                        if (hideAfterAnimation)
                        {
                            Hide();
                        }
                        else
                        {
                            // 关闭应用程序前清理托盘图标
                            if (MyNotifyIcon != null)
                            {
                                MyNotifyIcon.Dispose();
                            }
                            Close();
                        }
                    };
                    
                    closeStoryboard.Completed += completedHandler;
                    
                    // 开始关闭滑出动画
                    closeStoryboard.Begin(rootBorder);
                }
                else
                {
                    // 如果找不到根元素，直接隐藏窗口或关闭应用程序
                    if (hideAfterAnimation)
                    {
                        Hide();
                    }
                    else
                    {
                        // 关闭应用程序前清理托盘图标
                        if (MyNotifyIcon != null)
                        {
                            MyNotifyIcon.Dispose();
                        }
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"关闭滑出动画失败: {ex.Message}");
                // 如果动画失败，直接隐藏窗口或关闭应用程序
                if (hideAfterAnimation)
                {
                    Hide();
                }
                else
                {
                    // 关闭应用程序前清理托盘图标
                    if (MyNotifyIcon != null)
                    {
                        MyNotifyIcon.Dispose();
                    }
                    Close();
                }
            }
        }
        
        private void ToggleWindowState()
        {
            if (WindowState == WindowState.Maximized)
            {
                // 还原窗口
                WindowState = WindowState.Normal;
                MaximizeIcon.Kind = PackIconKind.WindowMaximize;
            }
            else
            {
                // 记录当前窗口状态
                UpdateNormalWindowState();
                
                // 最大化窗口
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
            {
                _normalWindowState = new Rect(Left, Top, Width, Height);
            }
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
                case WM_GETMINMAXINFO:
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
            var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new NativeMethods.MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
                
                if (NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var minMaxInfo = Marshal.PtrToStructure<NativeMethods.MINMAXINFO>(lParam);
                    
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
    
    // Windows API 调用所需的结构和方法
    internal static class NativeMethods
    {
        // 常量定义
        public const int MONITOR_DEFAULTTONEAREST = 0x00000002;
        public const int WM_GETMINMAXINFO = 0x0024;

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }
    }
}