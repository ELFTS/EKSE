using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EKSE.Services
{
    /// <summary>
    /// 按钮样式管理服务，负责创建和更新应用程序的渐变按钮样式
    /// </summary>
    public class ButtonStyleManager
    {
        private readonly ThemeManager _themeManager;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="themeManager">主题管理服务</param>
        public ButtonStyleManager(ThemeManager themeManager)
        {
            _themeManager = themeManager;
        }
        
        /// <summary>
        /// 创建或更新渐变按钮样式
        /// </summary>
        public void UpdateGradientButtonStyle()
        {
            try
            {
                if (Application.Current?.Resources == null) return;
                
                // 创建新的样式
                var gradientButtonStyle = new Style(typeof(Button));
                
                // 设置默认背景为渐变色
                var baseColor = _themeManager.CurrentThemeColor;
                var gradientBrush = BrushHelper.CreateGradientBrush(baseColor);
                
                gradientButtonStyle.Setters.Add(new Setter
                {
                    Property = Control.BackgroundProperty,
                    Value = gradientBrush
                });
                
                gradientButtonStyle.Setters.Add(new Setter
                {
                    Property = Control.ForegroundProperty,
                    Value = Brushes.White
                });
                
                gradientButtonStyle.Setters.Add(new Setter
                {
                    Property = Control.PaddingProperty,
                    Value = new Thickness(10, 5, 10, 5)
                });
                
                // 创建鼠标悬停触发器
                gradientButtonStyle.Triggers.Add(new Trigger
                {
                    Property = UIElement.IsMouseOverProperty,
                    Value = true,
                    Setters = {
                        new Setter
                        {
                            Property = Control.BackgroundProperty,
                            Value = gradientBrush
                        }
                    }
                });
                
                // 创建按钮按下触发器
                var darkerColor = Color.FromArgb(255,
                    (byte)Math.Max(0, (int)(baseColor.R * 0.8)),
                    (byte)Math.Max(0, (int)(baseColor.G * 0.8)),
                    (byte)Math.Max(0, (int)(baseColor.B * 0.8)));
                
                gradientButtonStyle.Triggers.Add(new Trigger
                {
                    Property = Button.IsPressedProperty,
                    Value = true,
                    Setters = {
                        new Setter
                        {
                            Property = Control.BackgroundProperty,
                            Value = BrushHelper.CreateGradientBrush(darkerColor)
                        }
                    }
                });
                
                // 添加样式到应用程序资源
                Application.Current.Resources["DefaultButtonStyle"] = gradientButtonStyle;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新渐变按钮样式时出错: {ex.Message}");
            }
        }
    }
}
