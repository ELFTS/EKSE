using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Hardcodet.Wpf.TaskbarNotification;

namespace EKSE.Services
{
    public static class WindowAnimationHelper
    {
        /// <summary>
        /// 应用垂直滑入动画
        /// </summary>
        public static void ApplyVerticalSlideInAnimation(Window window)
        {
            try
            {
                // 查找最外层的Border元素作为动画目标
                var rootBorder = VisualTreeHelper.GetChild(window, 0) as FrameworkElement;
                if (rootBorder != null)
                {
                    // 创建一个新的Storyboard来管理动画
                    Storyboard fadeInStoryboard = new Storyboard();
                    
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
                    fadeInStoryboard.Children.Add(opacityAnimation);
                    
                    // 开始动画
                    fadeInStoryboard.Begin();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用淡入动画失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用启动动画效果
        /// </summary>
        public static void ApplyStartupAnimation(Window window)
        {
            try
            {
                // 启动垂直滑动动画
                StartVerticalSlideAnimation(window);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动动画失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动垂直滑动动画
        /// </summary>
        public static void StartVerticalSlideAnimation(Window window)
        {
            try
            {
                // 查找最外层的Border元素作为动画目标
                var rootBorder = VisualTreeHelper.GetChild(window, 0) as FrameworkElement;
                if (rootBorder != null)
                {
                    // 创建一个新的Storyboard来控制动画
                    var storyboard = new Storyboard();
                    
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
                System.Diagnostics.Debug.WriteLine($"淡入动画失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动最小化滑出动画
        /// </summary>
        /// <param name="window">要应用动画的窗口</param>
        /// <param name="hideAfterAnimation">动画结束后是否隐藏窗口</param>
        public static void StartMinimizeSlideOutAnimation(Window window, bool hideAfterAnimation)
        {
            try
            {
                // 查找最外层的Border元素作为动画目标
                var rootBorder = VisualTreeHelper.GetChild(window, 0) as FrameworkElement;
                if (rootBorder != null)
                {
                    // 获取最小化滑出动画故事板
                    Storyboard minimizeStoryboard = (Storyboard)window.FindResource("MinimizeSlideOutStoryboard");
                    
                    // 添加动画完成事件处理
                    EventHandler? completedHandler = null;
                    completedHandler = (s, e) => {
                        // 移除事件处理程序以避免内存泄漏
                        minimizeStoryboard.Completed -= completedHandler;
                        
                        // 动画结束后隐藏窗口
                        if (hideAfterAnimation)
                        {
                            window.Hide();
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
                        window.Hide();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"最小化滑出动画失败: {ex.Message}");
                // 如果动画失败，直接隐藏窗口
                if (hideAfterAnimation)
                {
                    window.Hide();
                }
            }
        }

        /// <summary>
        /// 启动关闭滑出动画
        /// </summary>
        /// <param name="window">要应用动画的窗口</param>
        /// <param name="hideAfterAnimation">动画结束后是否隐藏窗口</param>
        /// <param name="notifyIcon">托盘图标，用于在关闭前清理资源</param>
        public static void StartCloseSlideOutAnimation(Window window, bool hideAfterAnimation, TaskbarIcon? notifyIcon = null)
        {
            try
            {
                // 查找最外层的Border元素作为动画目标
                var rootBorder = VisualTreeHelper.GetChild(window, 0) as FrameworkElement;
                if (rootBorder != null)
                {
                    // 获取关闭滑出动画故事板
                    Storyboard closeStoryboard = (Storyboard)window.FindResource("CloseSlideOutStoryboard");
                    
                    // 添加动画完成事件处理
                    EventHandler? completedHandler = null;
                    completedHandler = (s, e) => {
                        // 移除事件处理程序以避免内存泄漏
                        closeStoryboard.Completed -= completedHandler;
                        
                        // 动画结束后隐藏窗口或关闭应用程序
                        if (hideAfterAnimation)
                        {
                            window.Hide();
                        }
                        else
                        {
                            // 关闭应用程序前清理托盘图标
                            if (notifyIcon != null)
                            {
                                notifyIcon.Dispose();
                            }
                            window.Close();
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
                        window.Hide();
                    }
                    else
                    {
                        // 关闭应用程序前清理托盘图标
                        if (notifyIcon != null)
                        {
                            notifyIcon.Dispose();
                        }
                        window.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"关闭滑出动画失败: {ex.Message}");
                // 如果动画失败，直接隐藏窗口或关闭应用程序
                if (hideAfterAnimation)
                {
                    window.Hide();
                }
                else
                {
                    // 关闭应用程序前清理托盘图标
                    if (notifyIcon != null)
                    {
                        notifyIcon.Dispose();
                    }
                    window.Close();
                }
            }
        }
    }
}
