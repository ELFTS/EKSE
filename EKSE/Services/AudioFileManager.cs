using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EKSE.Services
{
    /// <summary>
    /// 音频文件管理器
    /// </summary>
    public class AudioFileManager
    {
        private readonly string _audioFilesDirectory;
        private readonly List<string> _audioFiles;
        
        public AudioFileManager()
        {
            _audioFilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds");
            _audioFiles = new List<string>();
            
            // 确保音频文件目录存在
            if (!Directory.Exists(_audioFilesDirectory))
            {
                Directory.CreateDirectory(_audioFilesDirectory);
            }
            
            // 加载现有音频文件
            LoadAudioFiles();
        }
        
        /// <summary>
        /// 获取所有音频文件路径
        /// </summary>
        public IReadOnlyList<string> AudioFiles => _audioFiles.AsReadOnly();
        
        /// <summary>
        /// 加载所有音频文件
        /// </summary>
        private void LoadAudioFiles()
        {
            try
            {
                // 支持的音频文件扩展名
                var supportedExtensions = new[] { ".wav", ".mp3", ".aac", ".wma", ".flac" };
                
                // 获取目录中的所有文件
                var allFiles = Directory.GetFiles(_audioFilesDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));
                
                _audioFiles.Clear();
                _audioFiles.AddRange(allFiles);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载音频文件时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 添加音频文件
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <returns>添加后的文件路径</returns>
        public string AddAudioFile(string sourceFilePath)
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                {
                    return string.Empty;
                }
                
                // 获取文件名
                var fileName = Path.GetFileName(sourceFilePath);
                var destFilePath = Path.Combine(_audioFilesDirectory, fileName);
                
                // 如果目标文件已存在，则先删除它以实现覆盖
                if (File.Exists(destFilePath))
                {
                    File.Delete(destFilePath);
                }
                
                // 复制文件（覆盖模式）
                File.Copy(sourceFilePath, destFilePath);
                
                // 如果列表中不包含该文件，则添加到列表
                if (!_audioFiles.Contains(destFilePath))
                {
                    _audioFiles.Add(destFilePath);
                }
                
                // 重新排序列表以确保一致性
                _audioFiles.Sort();
                
                return destFilePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加音频文件失败: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// 删除音频文件
        /// </summary>
        /// <param name="filePath">要删除的文件路径</param>
        public void DeleteAudioFile(string filePath)
        {
            try
            {
                // 标准化路径以确保比较准确
                var normalizedFilePath = Path.GetFullPath(filePath);
                var normalizedAudioDir = Path.GetFullPath(_audioFilesDirectory);
                
                // 检查文件是否存在且在正确的目录中
                if (File.Exists(normalizedFilePath) && normalizedFilePath.StartsWith(normalizedAudioDir, StringComparison.OrdinalIgnoreCase))
                {
                    // 删除文件
                    File.Delete(normalizedFilePath);
                    
                    // 从列表中移除
                    _audioFiles.Remove(normalizedFilePath);
                    
                    // 也可以使用原始路径尝试移除（以防列表中存储的是原始路径）
                    _audioFiles.Remove(filePath);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"文件不存在或不在正确的目录中: {filePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除音频文件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查音频文件是否存在
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否存在</returns>
        public bool FileExists(string filePath)
        {
            try
            {
                var normalizedFilePath = Path.GetFullPath(filePath);
                var normalizedAudioDir = Path.GetFullPath(_audioFilesDirectory);
                return File.Exists(normalizedFilePath) && normalizedFilePath.StartsWith(normalizedAudioDir, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 刷新音频文件列表
        /// </summary>
        public void Refresh()
        {
            LoadAudioFiles();
        }
    }
}