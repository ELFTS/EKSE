using System.Windows;
using System.Windows.Media;

namespace EKSE.Services
{
    /// <summary>
    /// 画笔帮助类，用于创建各种类型的画笔
    /// </summary>
    public static class BrushHelper
    {
        /// <summary>
        /// 创建统一的渐变画笔，用于所有需要渐变效果的地方
        /// </summary>
        /// <param name="color">基础颜色</param>
        /// <returns>线性渐变画笔</returns>
        public static LinearGradientBrush CreateGradientBrush(Color color)
        {
            return new LinearGradientBrush(
                Color.FromArgb(255, (byte)Math.Min(255, (int)(color.R * 1.2)), (byte)Math.Min(255, (int)(color.G * 1.2)), (byte)Math.Min(255, (int)(color.B * 1.2))),
                color,
                new Point(0, 0),
                new Point(0, 1)
            );
        }
        
        /// <summary>
        /// 创建标题栏背景色画笔（为了兼容性保留此方法，实际调用统一实现）
        /// </summary>
        /// <param name="color">基础颜色</param>
        /// <returns>线性渐变画笔</returns>
        public static LinearGradientBrush CreateTitleBarBrush(Color color)
        {
            return CreateGradientBrush(color);
        }
    }
}
