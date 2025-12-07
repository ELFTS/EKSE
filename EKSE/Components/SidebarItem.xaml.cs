using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Reflection;

namespace EKSE.Components
{
    /// <summary>
    /// SidebarItem.xaml 的交互逻辑
    /// </summary>
    public partial class SidebarItem : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(SidebarItem), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(string), typeof(SidebarItem), new PropertyMetadata(default(string), OnIconChanged));

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(SidebarItem), new PropertyMetadata(false, OnIsActiveChanged));

        public event EventHandler Click;

        // 定义默认颜色
        private static readonly Brush ActiveBackgroundBrush = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0));
        private static readonly Brush InactiveBackgroundBrush = Brushes.Transparent;
        private static readonly Brush HoverBackgroundBrush = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0));
        private static readonly Brush DefaultForegroundBrush = Brushes.Black;
        private static readonly Brush ActiveForegroundBrush = Brushes.Blue; // 默认激活前景色

        // 动画相关
        private Storyboard _backgroundStoryboard;
        private Storyboard _foregroundStoryboard;
        private Storyboard _iconSizeStoryboard;

        public SidebarItem()
        {
            InitializeComponent();

            // 获取动画资源
            _backgroundStoryboard = (Storyboard)Resources["BackgroundAnimation"];
            _foregroundStoryboard = (Storyboard)Resources["ForegroundAnimation"];
            _iconSizeStoryboard = (Storyboard)Resources["IconSizeAnimation"];

            // 绑定属性
            TextPresenter.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Text") { Source = this });
            SetBinding(IconProperty, new System.Windows.Data.Binding("Icon") { Source = this });

            // 设置默认前景色
            TextPresenter.Foreground = DefaultForegroundBrush;

            // 设置鼠标事件
            MouseLeftButtonUp += SidebarItem_MouseLeftButtonUp;
            MouseEnter += SidebarItem_MouseEnter;
            MouseLeave += SidebarItem_MouseLeave;
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SidebarItem)d;
            var imagePath = e.NewValue as string;

            if (!string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"正在加载图标: {imagePath}");
                    
                    // 检查是否是资源路径
                    if (imagePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
                    {
                        // 从嵌入资源加载图片
                        var resourceName = imagePath.Substring(6); // 移除 "res://" 前缀
                        
                        // 构造完整的资源路径
                        var fullResourcePath = $"EKSE.{resourceName.Replace("/", ".").Replace("\\", ".")}";
                        var assembly = Assembly.GetExecutingAssembly();
                        
                        System.Diagnostics.Debug.WriteLine($"尝试加载嵌入资源: {fullResourcePath}");
                        
                        using (var stream = assembly.GetManifestResourceStream(fullResourcePath))
                        {
                            if (stream != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"成功找到嵌入资源: {fullResourcePath}");
                                
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.StreamSource = stream;
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();
                                bitmap.Freeze(); // 冻结以提高性能
                                control.IconPresenter.Source = bitmap;
                                System.Diagnostics.Debug.WriteLine($"成功加载图标: {imagePath}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"未找到嵌入资源: {fullResourcePath}，尝试使用pack URI方式加载");
                                
                                // 如果资源未找到，尝试使用pack URI方式加载
                                var packUri = new Uri($"pack://application:,,,/EKSE;component/{resourceName}");
                                var bitmap = new BitmapImage(packUri);
                                control.IconPresenter.Source = bitmap;
                                System.Diagnostics.Debug.WriteLine($"通过pack URI方式加载图标: {imagePath}");
                            }
                        }
                    }
                    else if (imagePath.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
                    {
                        // 从应用程序资源加载图片
                        var bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                        control.IconPresenter.Source = bitmap;
                        System.Diagnostics.Debug.WriteLine($"通过pack URI绝对路径加载图标: {imagePath}");
                    }
                    else
                    {
                        // 从文件系统加载图片
                        var bitmap = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
                        control.IconPresenter.Source = bitmap;
                        System.Diagnostics.Debug.WriteLine($"通过文件系统路径加载图标: {imagePath}");
                    }
                }
                catch (Exception ex)
                {
                    // 如果加载图片失败，则清空图片
                    System.Diagnostics.Debug.WriteLine($"加载图标失败: {ex.Message}");
                    control.IconPresenter.Source = null;
                }
            }
            else
            {
                // 如果没有图标路径，则清空图片
                control.IconPresenter.Source = null;
            }
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SidebarItem)d;
            var isActive = (bool)e.NewValue;

            // 使用动画更新背景色
            control.AnimateBackgroundColor(isActive);

            // 使用动画更新前景色
            control.AnimateForegroundColor(isActive);
            
            // 使用动画更新图标大小
            control.AnimateIconSize(isActive);
        }

        // 动画背景色变化
        private void AnimateBackgroundColor(bool isActive)
        {
            if (_backgroundStoryboard != null)
            {
                var colorAnimation = (ColorAnimation)_backgroundStoryboard.Children[0];
                colorAnimation.From = ((SolidColorBrush)MainBorder.Background).Color;
                colorAnimation.To = isActive ? ((SolidColorBrush)ActiveBackgroundBrush).Color : ((SolidColorBrush)InactiveBackgroundBrush).Color;
                _backgroundStoryboard.Begin();
            }
            else
            {
                // 如果动画不可用，则直接设置颜色
                MainBorder.Background = isActive ? ActiveBackgroundBrush : InactiveBackgroundBrush;
            }
        }

        // 动画前景色变化
        private void AnimateForegroundColor(bool isActive)
        {
            if (_foregroundStoryboard != null)
            {
                var colorAnimation = (ColorAnimation)_foregroundStoryboard.Children[0];
                colorAnimation.From = ((SolidColorBrush)TextPresenter.Foreground).Color;
                colorAnimation.To = isActive ? ((SolidColorBrush)ActiveForegroundBrush).Color : ((SolidColorBrush)DefaultForegroundBrush).Color;
                _foregroundStoryboard.Begin();
            }
            else
            {
                // 如果动画不可用，则直接设置颜色
                TextPresenter.Foreground = isActive ? ActiveForegroundBrush : DefaultForegroundBrush;
            }
        }

        // 动画图标大小变化
        private void AnimateIconSize(bool isActive)
        {
            if (_iconSizeStoryboard != null)
            {
                var widthAnimation = (DoubleAnimation)_iconSizeStoryboard.Children[0];
                var heightAnimation = (DoubleAnimation)_iconSizeStoryboard.Children[1];
                
                widthAnimation.From = IconPresenter.Width;
                heightAnimation.From = IconPresenter.Height;
                
                // 激活时稍微放大图标
                widthAnimation.To = isActive ? 28 : 24;
                heightAnimation.To = isActive ? 28 : 24;
                
                _iconSizeStoryboard.Begin();
            }
        }

        private void SidebarItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Click?.Invoke(this, EventArgs.Empty);
        }

        private void SidebarItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsActive)
            {
                AnimateBackgroundColor(true); // 临时显示悬停效果
            }
        }

        private void SidebarItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!IsActive)
            {
                AnimateBackgroundColor(false); // 恢复到非激活状态
            }
        }
    }
}