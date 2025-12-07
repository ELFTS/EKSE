using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace EKSE.Services
{
    /// <summary>
    /// 资源图片加载器，用于处理res://协议的图片加载
    /// </summary>
    public static class ResourceImageLoader
    {
        /// <summary>
        /// 从资源加载图片
        /// </summary>
        /// <param name="resourcePath">资源路径，例如 res://Assets/image.png</param>
        /// <returns>BitmapImage对象</returns>
        public static BitmapImage LoadImageFromResource(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                throw new ArgumentException("Resource path cannot be null or empty.", nameof(resourcePath));

            // 检查是否是res://协议
            if (!resourcePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Resource path must start with 'res://'", nameof(resourcePath));

            // 移除res://前缀
            var resourceName = resourcePath.Substring(6);
            
            // 获取当前程序集
            var assembly = Assembly.GetExecutingAssembly();
            
            // 尝试作为嵌入资源加载
            try
            {
                // 构造完整的资源名称
                var fullResourceName = $"EKSE.{resourceName.Replace("/", ".").Replace("\\", ".")}";
                
                using (var stream = assembly.GetManifestResourceStream(fullResourceName))
                {
                    if (stream != null)
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load image as embedded resource: {ex.Message}");
            }

            // 如果嵌入资源加载失败，尝试作为应用程序资源加载
            try
            {
                // 首先尝试作为普通资源加载
                var packUri = new Uri($"pack://application:,,,/EKSE;component/{resourceName}");
                var bitmap = new BitmapImage(packUri);
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load image as application resource: {ex.Message}");
            }

            // 尝试作为Icons文件夹中的资源加载
            try
            {
                var iconsPackUri = new Uri($"pack://application:,,,/EKSE;component/Icons/{resourceName}");
                var bitmap = new BitmapImage(iconsPackUri);
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load image from Icons folder: {ex.Message}");
            }

            // 如果两种方式都失败，返回默认图像或抛出异常
            System.Diagnostics.Debug.WriteLine($"Could not find resource: {resourcePath}");
            return new BitmapImage(); // 返回空的BitmapImage
        }
    }
}