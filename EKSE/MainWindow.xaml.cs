using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Threading;
using EKSE.Views;
using EKSE.Components;
using EKSE.Services;
using EKSE.Models;
using MaterialDesignThemes.Wpf;
using MaterialDesignColors;

namespace EKSE
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<SidebarItem> sidebarItems = new List<SidebarItem>();
        
        // 平滑滚动相关字段
        private double _sideBarScrollOffset = 0;
        private ScrollViewer _sidebarScrollViewer;
        private DispatcherTimer _sidebarScrollTimer;
        private double _sidebarTargetOffset;
        private double _sidebarStartOffset;
        private DateTime _sidebarScrollStartTime;
        private const int ScrollDuration = 300; // 滚动持续时间（毫秒）
        
        // 服务和管理器
        private readonly SoundService _soundService;
        private readonly ProfileManager _profileManager;
        private readonly AudioFileManager _audioFileManager;

        // 用于存储窗口正常状态时的位置和大小
        private Rect _normalWindowState = Rect.Empty;
        
        // 用于保存设置视图实例，避免重复创建
        private SettingsView _settingsView;
        
        // Windows API 常量
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        public MainWindow()
        {
            InitializeComponent();
            
            // 初始化服务和管理器
            _profileManager = new ProfileManager();
            _audioFileManager = new AudioFileManager();
            _soundService = new SoundService(_profileManager);
            
            // 初始化导航
            InitializeNavigation();
            
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSidebar();
            // 默认加载主页内容
            LoadContent("Home");
            
            // 确保标题栏颜色正确应用
            ApplySavedTitleBarColor();
            
            // 记录窗口正常状态时的位置和大小
            UpdateNormalWindowState();
            
            // 获取侧边栏ScrollViewer引用
            _sidebarScrollViewer = FindVisualChild<ScrollViewer>(SidebarPanel.Parent as DependencyObject);
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
                        // 创建标题栏背景色画笔
                        var titleBarBrush = new SolidColorBrush(color);
                        
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
                    var titleBarBrush = new SolidColorBrush(defaultColor);
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
                try
                {
                    var defaultColor = Colors.Purple;
                    var titleBarBrush = new SolidColorBrush(defaultColor);
                    if (Application.Current != null && Application.Current.Resources != null)
                    {
                        Application.Current.Resources["TitleBarBackground"] = titleBarBrush;
                    }
                }
                catch (Exception innerEx)
                {
                    System.Diagnostics.Debug.WriteLine($"应用默认标题栏颜色时出错: {innerEx.Message}");
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
                Icon = "res://home.png"
            };
            homeItem.Click += (sender, e) => LoadContent("Home");
            SidebarPanel.Children.Add(homeItem);
            sidebarItems.Add(homeItem);

            var soundItem = new SidebarItem
            {
                Text = "音效设置",
                Icon = "res://music.png"
            };
            soundItem.Click += (sender, e) => LoadContent("SoundSettings");
            SidebarPanel.Children.Add(soundItem);
            sidebarItems.Add(soundItem);

            var settingsItem = new SidebarItem
            {
                Text = "系统设置",
                Icon = "res://settings.png"
            };
            settingsItem.Click += (sender, e) => LoadContent("Settings");
            SidebarPanel.Children.Add(settingsItem);
            sidebarItems.Add(settingsItem);

            var aboutItem = new SidebarItem
            {
                Text = "关于",
                Icon = "res://info.png"
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
                case "About":
                    if (sidebarItems.Count > 3)
                        sidebarItems[3].IsActive = true;
                    break;
            }
        }

        // 加载指定类型的内容到主区域
        private void LoadContent(string contentType)
        {
            UserControl content = null;
            
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
                    content = _settingsView;
                    break;
                case "About":
                    content = new AboutView();
                    break;
            }
            
            if (content != null)
            {
                // 应用淡入动画
                var fadeInStoryboard = (Storyboard)Resources["FadeInStoryboard"];
                MainContentArea.Content = content;
                fadeInStoryboard.Begin();
                
                // 更新侧边栏选择状态并平滑滚动到对应项
                UpdateSidebarSelection(contentType);
                ScrollToSelectedItem(contentType);
                
                // 确保标题栏颜色正确应用（除了设置页面，它自己会处理颜色）
                if (contentType != "Settings")
                {
                    ApplySavedTitleBarColor();
                }
            }
        }

        /// <summary>
        /// 平滑滚动到选中的侧边栏项
        /// </summary>
        private void ScrollToSelectedItem(string contentType)
        {
            int targetIndex = -1;
            
            // 根据内容类型找到对应的侧边栏项索引
            switch (contentType)
            {
                case "Home":
                    targetIndex = 0;
                    break;
                case "SoundSettings":
                    targetIndex = 1;
                    break;
                case "Settings":
                    targetIndex = 2;
                    break;
                case "About":
                    targetIndex = 3;
                    break;
            }

            if (targetIndex >= 0 && targetIndex < SidebarPanel.Children.Count)
            {
                // 获取目标项
                var targetItem = SidebarPanel.Children[targetIndex] as UIElement;

                if (targetItem != null && _sidebarScrollViewer != null)
                {
                    // 设置目标偏移量
                    _sidebarTargetOffset = CalculateScrollOffset(targetItem);
                    _sidebarStartOffset = _sidebarScrollViewer.VerticalOffset;
                    _sidebarScrollStartTime = DateTime.Now;

                    // 启动滚动定时器
                    if (_sidebarScrollTimer == null)
                    {
                        _sidebarScrollTimer = new DispatcherTimer();
                        _sidebarScrollTimer.Interval = TimeSpan.FromMilliseconds(16); // 大约60fps
                        _sidebarScrollTimer.Tick += SidebarScrollTimer_Tick;
                    }
                    _sidebarScrollTimer.Start();
                }
            }
        }

        /// <summary>
        /// 计算目标项的垂直偏移量以实现居中显示
        /// </summary>
        private double CalculateScrollOffset(UIElement targetItem)
        {
            if (targetItem == null || _sidebarScrollViewer == null)
                return 0;

            double targetItemTop = targetItem.TranslatePoint(new Point(0, 0), SidebarPanel).Y;
            double targetItemHeight = targetItem.RenderSize.Height;
            double viewerHeight = _sidebarScrollViewer.RenderSize.Height;

            return targetItemTop - (viewerHeight - targetItemHeight) / 2;
        }

        /// <summary>
        /// 侧边栏平滑滚动事件处理
        /// </summary>
        private void SidebarScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // 计算目标滚动偏移量
                double newOffset = scrollViewer.VerticalOffset - (e.Delta > 0 ? 50 : -50);
                
                // 限制滚动范围
                newOffset = Math.Max(0, newOffset);
                newOffset = Math.Min(scrollViewer.ScrollableHeight, newOffset);
                
                // 启动平滑滚动动画
                StartSmoothScroll(scrollViewer, newOffset);
                
                // 标记事件已处理，防止默认滚动行为
                e.Handled = true;
            }
        }
        
        /// <summary>
        /// 启动平滑滚动动画
        /// </summary>
        private void StartSmoothScroll(ScrollViewer scrollViewer, double targetOffset)
        {
            // 初始化滚动动画参数
            _sidebarStartOffset = scrollViewer.VerticalOffset;
            _sidebarTargetOffset = targetOffset;
            _sidebarScrollStartTime = DateTime.Now;
            
            // 如果计时器尚未创建，则创建它
            if (_sidebarScrollTimer == null)
            {
                _sidebarScrollTimer = new DispatcherTimer();
                _sidebarScrollTimer.Interval = TimeSpan.FromMilliseconds(10); // 10毫秒更新一次
                _sidebarScrollTimer.Tick += SidebarScrollTimer_Tick;
            }
            
            // 启动计时器
            _sidebarScrollTimer.Start();
        }
        
        /// <summary>
        /// 侧边栏滚动计时器事件处理
        /// </summary>
        private void SidebarScrollTimer_Tick(object sender, EventArgs e)
        {
            // 计算经过的时间
            double elapsed = (DateTime.Now - _sidebarScrollStartTime).TotalMilliseconds;
            
            // 计算进度（0到1之间）
            double progress = Math.Min(elapsed / ScrollDuration, 1.0);
            
            // 应用缓动函数（使用立方缓动）
            double easedProgress = 1 - Math.Pow(1 - progress, 3);
            
            // 计算当前偏移量
            double currentOffset = _sidebarStartOffset + (_sidebarTargetOffset - _sidebarStartOffset) * easedProgress;
            
            // 设置滚动位置
            _sidebarScrollViewer?.ScrollToVerticalOffset(currentOffset);
            
            // 如果滚动完成，停止计时器
            if (progress >= 1.0)
            {
                _sidebarScrollTimer?.Stop();
            }
        }
        
        // 当主题颜色更改时的处理方法
        private void OnThemeColorChanged(object sender, Color color)
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
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 窗口控制按钮鼠标进入事件
        /// </summary>
        private void WindowControlButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button button)
            {
                var storyboard = (Storyboard)this.FindResource("ButtonHoverStoryboard");
                if (storyboard != null)
                {
                    storyboard.Begin(button, true);
                }
            }
        }

        /// <summary>
        /// 窗口控制按钮鼠标离开事件
        /// </summary>
        private void WindowControlButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button button)
            {
                var storyboard = (Storyboard)this.FindResource("ButtonNormalStoryboard");
                if (storyboard != null)
                {
                    storyboard.Begin(button, true);
                }
            }
        }

        /// <summary>
        /// 关闭按钮鼠标进入事件
        /// </summary>
        private void CloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button button)
            {
                var storyboard = (Storyboard)this.FindResource("CloseButtonHoverStoryboard");
                if (storyboard != null)
                {
                    storyboard.Begin(button, true);
                }
            }
        }

        /// <summary>
        /// 关闭按钮鼠标离开事件
        /// </summary>
        private void CloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button button)
            {
                var storyboard = (Storyboard)this.FindResource("CloseButtonNormalStoryboard");
                if (storyboard != null)
                {
                    storyboard.Begin(button, true);
                }
            }
        }
        
        /// <summary>
        /// 在可视化树中查找指定类型的子元素
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var correctlyTyped = child as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }
                else
                {
                    var result = FindVisualChild<T>(child);
                    if (result != null)
                        return result;
                }
            }
            
            return null;
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
            var monitor = NativeMethods.MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
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
    }
    
    // Windows API 调用所需的结构和方法
    internal static class NativeMethods
    {
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