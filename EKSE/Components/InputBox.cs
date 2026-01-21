using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EKSE.Components
{
    public static class InputBox
    {
        public static string? Show(string prompt, string title, string defaultResponse = "")
        {
            // 创建窗口
            var window = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            // 创建布局容器
            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center
            };

            // 创建提示文本
            var promptText = new TextBlock
            {
                Text = prompt,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = System.Windows.TextWrapping.Wrap
            };
            stackPanel.Children.Add(promptText);

            // 创建输入框
            var inputBox = new TextBox
            {
                Width = double.NaN,
                Text = defaultResponse,
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackPanel.Children.Add(inputBox);

            // 创建按钮容器
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // 创建取消按钮
            var cancelButton = new Button
            {
                Content = "取消",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0)
            };
            cancelButton.Click += (s, e) => { window.DialogResult = false; window.Close(); };
            buttonPanel.Children.Add(cancelButton);

            // 创建确定按钮
            var okButton = new Button
            {
                Content = "确定",
                Width = 80,
                IsDefault = true
            };
            okButton.Click += (s, e) => { window.DialogResult = true; window.Close(); };
            buttonPanel.Children.Add(okButton);

            stackPanel.Children.Add(buttonPanel);
            window.Content = stackPanel;

            // 设置默认焦点
            window.Loaded += (s, e) => inputBox.Focus();

            // 显示对话框并返回结果
            var result = window.ShowDialog();
            return result == true ? inputBox.Text : null;
        }
    }
}