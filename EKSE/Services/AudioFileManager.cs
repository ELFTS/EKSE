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
        private readonly List<string> _audioFiles = new();
        private static readonly string[] SupportedExtensions = { ".wav", ".mp3", ".aac", ".wma", ".flac" };

        public event EventHandler? AudioFilesChanged;
        public IReadOnlyList<string> AudioFiles => _audioFiles.AsReadOnly();

        public AudioFileManager(ProfileManager profileManager)
        {
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            _profileManager.ProfilesChanged += (_, _) => Refresh();
            _profileManager.CurrentProfileChanged += (_, _) => Refresh();
            Refresh();
        }

        private string? GetSoundsDirectory()
        {
            var profile = _profileManager.CurrentProfile;
            return !string.IsNullOrEmpty(profile?.FilePath) 
                ? Path.Combine(profile.FilePath, "sounds") 
                : null;
        }

        private void LoadAudioFiles()
        {
            _audioFiles.Clear();
            try
            {
                var soundsDir = GetSoundsDirectory();
                if (soundsDir != null && Directory.Exists(soundsDir))
                {
                    var files = Directory.GetFiles(soundsDir, "*.*", SearchOption.AllDirectories)
                        .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
                    _audioFiles.AddRange(files);
                }
            }
            catch { }
        }

        public string? AddAudioFile(string sourcePath)
        {
            var result = _profileManager.ImportSound(sourcePath);
            if (!string.IsNullOrEmpty(result)) Refresh();
            return result;
        }

        public void DeleteAudioFile(string filePath)
        {
            try
            {
                var profile = _profileManager.CurrentProfile;
                var soundsDir = GetSoundsDirectory();
                if (profile == null || soundsDir == null) return;

                var normalizedPath = Path.GetFullPath(filePath);
                if (!File.Exists(normalizedPath) || !normalizedPath.StartsWith(Path.GetFullPath(soundsDir), StringComparison.OrdinalIgnoreCase))
                    return;

                File.Delete(normalizedPath);
                _audioFiles.Remove(normalizedPath);
                _audioFiles.Remove(filePath);

                var keyToRemove = profile.KeySounds.FirstOrDefault(kvp => 
                    kvp.Value.Equals(filePath, StringComparison.OrdinalIgnoreCase)).Key;
                if (keyToRemove != default)
                {
                    profile.KeySounds.Remove(keyToRemove);
                    _profileManager.SaveProfile(profile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除音频文件失败: {ex.Message}");
            }
        }

        public bool FileExists(string filePath)
        {
            try
            {
                var soundsDir = GetSoundsDirectory();
                if (soundsDir == null) return false;

                var normalizedPath = Path.GetFullPath(filePath);
                return File.Exists(normalizedPath) && 
                       normalizedPath.StartsWith(Path.GetFullPath(soundsDir), StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public string? RenameAudioFile(string oldPath, string newName)
        {
            try
            {
                if (!File.Exists(oldPath)) return null;

                var profile = _profileManager.CurrentProfile;
                var soundsDir = GetSoundsDirectory();
                if (profile == null || soundsDir == null) return null;

                // 清理非法字符
                var cleanName = string.Concat(newName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                var newPath = Path.Combine(soundsDir, cleanName + Path.GetExtension(oldPath));

                if (File.Exists(newPath)) File.Delete(newPath);
                File.Move(oldPath, newPath);

                // 更新按键映射
                var keyToUpdate = profile.KeySounds.FirstOrDefault(kvp => 
                    kvp.Value.Equals(oldPath, StringComparison.OrdinalIgnoreCase)).Key;

                if (keyToUpdate != default)
                {
                    profile.KeySounds.Remove(keyToUpdate);
                    profile.KeySounds[keyToUpdate] = newPath;

                    var assigned = profile.AssignedSounds?.FirstOrDefault(a => 
                        a?.Sound.Equals(Path.GetFileName(oldPath), StringComparison.OrdinalIgnoreCase) == true);
                    if (assigned != null) assigned.Sound = Path.GetFileName(newPath);

                    _profileManager.SaveProfile(profile);
                }

                Refresh();
                return newPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重命名音频文件失败: {ex.Message}");
                return null;
            }
        }

        public void Refresh()
        {
            LoadAudioFiles();
            AudioFilesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
