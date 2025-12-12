using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace EKSE.Services
{
    public class AudioFileManager
    {
        private readonly ProfileManager _profileManager;
        private readonly List<string> _audioFiles;
        
        public AudioFileManager(ProfileManager profileManager)
        {
            _profileManager = profileManager;
            _audioFiles = new List<string>();
            
            // 加载现有音频文件
            Refresh();
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
                var currentProfile = _profileManager.CurrentProfile;
                if (currentProfile != null)
                {
                    var keySoundsDirectory = Path.Combine(currentProfile.FilePath, "sounds");
                    
                    // 只有当目录存在时才尝试加载文件
                    if (Directory.Exists(keySoundsDirectory))
                    {
                        // 支持的音频文件扩展名
                        var supportedExtensions = new[] { ".wav", ".mp3", ".aac", ".wma", ".flac" };
                        
                        // 获取目录中的所有文件
                        var allFiles = Directory.GetFiles(keySoundsDirectory, "*.*", SearchOption.AllDirectories)
                            .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));
                        
                        _audioFiles.Clear();
                        _audioFiles.AddRange(allFiles);
                        
                        System.Diagnostics.Debug.WriteLine($"成功加载 {_audioFiles.Count} 个音频文件从目录: {keySoundsDirectory}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"音频目录不存在: {keySoundsDirectory}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("当前没有选择声音方案");
                }
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
                // 使用ProfileManager的方法将音频文件导入到当前配置中
                return _profileManager.ImportSoundToCurrentProfile(sourceFilePath);
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
                var currentProfile = _profileManager.CurrentProfile;
                if (currentProfile != null)
                {
                    var keySoundsDirectory = Path.Combine(currentProfile.FilePath, "sounds");
                    
                    // 标准化路径以确保比较准确
                    var normalizedFilePath = Path.GetFullPath(filePath);
                    var normalizedAudioDir = Path.GetFullPath(keySoundsDirectory);
                    
                    // 检查文件是否存在且在正确的目录中
                    if (File.Exists(normalizedFilePath) && normalizedFilePath.StartsWith(normalizedAudioDir, StringComparison.OrdinalIgnoreCase))
                    {
                        // 删除文件
                        File.Delete(normalizedFilePath);
                        
                        // 从列表中移除
                        _audioFiles.Remove(normalizedFilePath);
                        
                        // 也可以使用原始路径尝试移除（以防列表中存储的是原始路径）
                        _audioFiles.Remove(filePath);
                        
                        // 同时从当前配置中移除该键的映射
                        var keyToRemove = currentProfile.KeySounds.FirstOrDefault(kvp => kvp.Value.Equals(filePath, StringComparison.OrdinalIgnoreCase)).Key;
                        if (keyToRemove != default(System.Windows.Input.Key))
                        {
                            currentProfile.KeySounds.Remove(keyToRemove);
                            _profileManager.SaveProfile(currentProfile);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"文件不存在或不在正确的目录中: {filePath}");
                    }
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
                var currentProfile = _profileManager.CurrentProfile;
                if (currentProfile != null)
                {
                    var keySoundsDirectory = Path.Combine(currentProfile.FilePath, "sounds");
                    var normalizedFilePath = Path.GetFullPath(filePath);
                    var normalizedAudioDir = Path.GetFullPath(keySoundsDirectory);
                    return File.Exists(normalizedFilePath) && normalizedFilePath.StartsWith(normalizedAudioDir, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 重命名音频文件
        /// </summary>
        /// <param name="oldFilePath">原文件路径</param>
        /// <param name="newFileNameWithoutExtension">新文件名（不含扩展名）</param>
        /// <returns>重命名后的文件路径，失败则返回null</returns>
        public string RenameAudioFile(string oldFilePath, string newFileNameWithoutExtension)
        {
            try
            {
                if (!File.Exists(oldFilePath))
                    return null;

                var currentProfile = _profileManager.CurrentProfile;
                if (currentProfile == null)
                    return null;

                var keySoundsDirectory = Path.Combine(currentProfile.FilePath, "sounds");
                if (!Directory.Exists(keySoundsDirectory))
                    return null;

                // 确保新文件名不包含非法字符
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    newFileNameWithoutExtension = newFileNameWithoutExtension.Replace(c, '_');
                }

                var extension = Path.GetExtension(oldFilePath);
                var newFilePath = Path.Combine(keySoundsDirectory, newFileNameWithoutExtension + extension);

                // 如果新文件已存在，则先删除
                if (File.Exists(newFilePath))
                {
                    File.Delete(newFilePath);
                }

                // 重命名文件
                File.Move(oldFilePath, newFilePath);

                // 更新当前方案中的按键映射
                var keyToUpdate = currentProfile.KeySounds.FirstOrDefault(kvp => 
                    kvp.Value.Equals(oldFilePath, StringComparison.OrdinalIgnoreCase)).Key;

                if (!keyToUpdate.Equals(default(Key)))
                {
                    // 移除旧映射
                    currentProfile.KeySounds.Remove(keyToUpdate);
                    // 添加新映射
                    currentProfile.KeySounds[keyToUpdate] = newFilePath;
                    
                    // 更新AssignedSounds中的条目
                    var assignedSound = currentProfile.AssignedSounds?.FirstOrDefault(a => 
                        a.Sound.Equals(Path.GetFileName(oldFilePath), StringComparison.OrdinalIgnoreCase));
                    
                    if (assignedSound != null)
                    {
                        assignedSound.Sound = Path.GetFileName(newFilePath);
                    }
                    
                    // 保存方案
                    _profileManager.SaveProfile(currentProfile);
                }

                // 刷新音频文件列表
                Refresh();

                return newFilePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重命名音频文件失败: {ex.Message}");
                return null;
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