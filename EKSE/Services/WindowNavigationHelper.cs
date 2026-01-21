using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using EKSE.Views;

namespace EKSE.Services
{
    public static class WindowNavigationHelper
    {
        /// <summary>
        /// 初始化导航
        /// </summary>
        public static void InitializeNavigation(System.Windows.Controls.ContentPresenter mainContentArea)
        {
            // 设置默认视图
            mainContentArea.Content = new HomeView();
        }

        /// <summary>
        /// 初始化侧边栏
        /// </summary>
        public static void InitializeSidebar(System.Windows.Controls.Panel sidebarPanel, List<Components.SidebarItem> sidebarItems, Action<string> loadContentAction)
        {
            // 清空现有项目
            sidebarPanel.Children.Clear();
            sidebarItems.Clear();

            // 添加侧边栏菜单项
            var homeItem = new Components.SidebarItem
            {
                Text = "主页",
                Icon = "res://Assets/Icons/home.png"
            };
            homeItem.Click += (sender, e) => loadContentAction("Home");
            sidebarPanel.Children.Add(homeItem);
            sidebarItems.Add(homeItem);

            var soundItem = new Components.SidebarItem
            {
                Text = "音效设置",
                Icon = "res://Assets/Icons/music.png"
            };
            soundItem.Click += (sender, e) => loadContentAction("SoundSettings");
            sidebarPanel.Children.Add(soundItem);
            sidebarItems.Add(soundItem);

            var settingsItem = new Components.SidebarItem
            {
                Text = "系统设置",
                Icon = "res://Assets/Icons/settings.png"
            };
            settingsItem.Click += (sender, e) => loadContentAction("Settings");
            sidebarPanel.Children.Add(settingsItem);
            sidebarItems.Add(settingsItem);

            var sponsorItem = new Components.SidebarItem
            {
                Text = "赞助",
                Icon = "res://Assets/Icons/sponsor.png"
            };
            sponsorItem.Click += (sender, e) => loadContentAction("Sponsor");
            sidebarPanel.Children.Add(sponsorItem);
            sidebarItems.Add(sponsorItem);

            var aboutItem = new Components.SidebarItem
            {
                Text = "关于",
                Icon = "res://Assets/Icons/info.png"
            };
            aboutItem.Click += (sender, e) => loadContentAction("About");
            sidebarPanel.Children.Add(aboutItem);
            sidebarItems.Add(aboutItem);

            // 设置默认选中项
            if (sidebarItems.Count > 0)
            {
                sidebarItems[0].IsActive = true;
            }
        }

        /// <summary>
        /// 更新侧边栏选择状态
        /// </summary>
        public static void UpdateSidebarSelection(List<Components.SidebarItem> sidebarItems, string contentType)
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

        /// <summary>
        /// 执行页面切换动画
        /// </summary>
        public static void PlayPageTransition(System.Windows.FrameworkElement window)
        {
            // 使用滑入动画
            var slideInStoryboard = (Storyboard)window.Resources["SlideInFromLeftStoryboard"];
            slideInStoryboard.Begin();
        }
    }
}