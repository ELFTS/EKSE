using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace EKSE.Views
{
    /// <summary>
    /// AboutView.xaml 的交互逻辑
    /// </summary>
    public partial class AboutView : UserControl
    {
        // 平滑滚动相关字段
        private double _scrollOffset = 0;
        private ScrollViewer _scrollViewer; // 保存ScrollViewer引用
        private DispatcherTimer _scrollTimer;
        private double _targetOffset;
        private double _startOffset;
        private DateTime _scrollStartTime;
        private const int ScrollDuration = 300; // 滚动持续时间（毫秒）
        
        public AboutView()
        {
            InitializeComponent();
            
            // 在Loaded事件中获取ScrollViewer引用
            Loaded += (s, e) => {
                _scrollViewer = FindVisualChild<ScrollViewer>(this);
            };
        }
        
        /// <summary>
        /// 处理超链接导航事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // 使用系统默认浏览器打开链接
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        
        /// <summary>
        /// 平滑滚动事件处理
        /// </summary>
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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
            _startOffset = scrollViewer.VerticalOffset;
            _targetOffset = targetOffset;
            _scrollStartTime = DateTime.Now;
            
            // 如果计时器尚未创建，则创建它
            if (_scrollTimer == null)
            {
                _scrollTimer = new DispatcherTimer();
                _scrollTimer.Interval = TimeSpan.FromMilliseconds(10); // 10毫秒更新一次
                _scrollTimer.Tick += ScrollTimer_Tick;
            }
            
            // 启动计时器
            _scrollTimer.Start();
        }
        
        /// <summary>
        /// 滚动计时器事件处理
        /// </summary>
        private void ScrollTimer_Tick(object sender, EventArgs e)
        {
            // 计算经过的时间
            double elapsed = (DateTime.Now - _scrollStartTime).TotalMilliseconds;
            
            // 计算进度（0到1之间）
            double progress = Math.Min(elapsed / ScrollDuration, 1.0);
            
            // 应用缓动函数（使用立方缓动）
            double easedProgress = 1 - Math.Pow(1 - progress, 3);
            
            // 计算当前偏移量
            double currentOffset = _startOffset + (_targetOffset - _startOffset) * easedProgress;
            
            // 设置滚动位置
            _scrollViewer?.ScrollToVerticalOffset(currentOffset);
            
            // 如果滚动完成，停止计时器
            if (progress >= 1.0)
            {
                _scrollTimer.Stop();
            }
        }
        
        /// <summary>
        /// 在可视化树中查找指定类型的子元素
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent) where T : class
        {
            if (parent == null) return default(T);
            
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
            
            return default(T);
        }
    }
}