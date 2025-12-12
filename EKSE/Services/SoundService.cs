using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using NAudio.Wave;
using EKSE.Models;

namespace EKSE.Services
{
    public class SoundService : IDisposable
    {
        private readonly WaveOutEvent _waveOut;
        private AudioFileReader _audioFileReader;
        private ProfileManager _profileManager;
        
        // 全局键盘钩子相关
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private AudioFileManager _audioFileManager;
        
        // 事件定义
        public event EventHandler<SoundEventArgs> SoundPlayed;
        public event EventHandler<SoundErrorEventArgs> SoundError;

        public SoundService(ProfileManager profileManager)
        {
            _profileManager = profileManager;
            _waveOut = new WaveOutEvent();
            
            // 订阅当前方案改变事件，以便在方案改变时释放相关资源
            _profileManager.CurrentProfileChanged += OnCurrentProfileChanged;
            
            // 设置全局键盘钩子
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }
        
        public SoundService(ProfileManager profileManager, AudioFileManager audioFileManager) : this(profileManager)
        {
            _audioFileManager = audioFileManager;
        }

        public void PlaySound(Key key)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"尝试播放按键 {key} 的音效");
                
                // 获取按键对应的音效路径
                var soundPath = GetSoundPathForKey(key);
                System.Diagnostics.Debug.WriteLine($"按键 {key} 对应的音效路径: {soundPath}");
                
                // 检查音效文件是否存在
                if (string.IsNullOrEmpty(soundPath) || !File.Exists(soundPath))
                {
                    System.Diagnostics.Debug.WriteLine($"音效文件不存在或路径为空: {soundPath}");
                    // 触发音效播放错误事件
                    SoundError?.Invoke(this, new SoundErrorEventArgs(key, "音效文件不存在"));
                    return;
                }
                
                // 检查文件是否被占用
                try
                {
                    using (var stream = File.Open(soundPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // 文件可以被打开，继续播放
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"音效文件被占用或无法访问: {ex.Message}");
                    SoundError?.Invoke(this, new SoundErrorEventArgs(key, $"音效文件无法访问: {ex.Message}"));
                    return;
                }
                
                // 停止当前正在播放的音效
                _waveOut.Stop();
                _audioFileReader?.Dispose();
                
                // 加载新的音效文件
                _audioFileReader = new AudioFileReader(soundPath);
                
                _waveOut.Init(_audioFileReader);
                _waveOut.Play();
                
                // 触发音效播放事件
                SoundPlayed?.Invoke(this, new SoundEventArgs(key, soundPath));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"播放音效时出错: {ex.Message}");
                // 触发音效播放错误事件
                SoundError?.Invoke(this, new SoundErrorEventArgs(key, ex.Message));
            }
        }
        
        /// <summary>
        /// 获取指定按键的音效文件路径
        /// </summary>
        /// <param name="key">按键</param>
        /// <returns>音效文件路径</returns>
        private string GetSoundPathForKey(Key key)
        {
            System.Diagnostics.Debug.WriteLine($"获取按键 {key} 的音效路径");
            
            // 检查当前方案中是否有该按键的音效
            if (_profileManager?.CurrentProfile?.KeySounds != null)
            {
                System.Diagnostics.Debug.WriteLine($"当前方案: {_profileManager.CurrentProfile.Name}");
                System.Diagnostics.Debug.WriteLine($"当前方案路径: {_profileManager.CurrentProfile.FilePath}");
                
                // 打印所有按键信息用于调试
                foreach (var kvp in _profileManager.CurrentProfile.KeySounds)
                {
                    System.Diagnostics.Debug.WriteLine($"方案中包含按键: {kvp.Key} -> {kvp.Value}");
                }
                
                // 特别检查数字键
                for (int i = 0; i <= 9; i++)
                {
                    var digitKey = (Key)Enum.Parse(typeof(Key), "D" + i);
                    if (_profileManager.CurrentProfile.KeySounds.ContainsKey(digitKey))
                    {
                        System.Diagnostics.Debug.WriteLine($"方案中包含数字键 {i}: {digitKey} -> {_profileManager.CurrentProfile.KeySounds[digitKey]}");
                    }
                }
                
                if (_profileManager.CurrentProfile.KeySounds.ContainsKey(key))
                {
                    var soundPath = _profileManager.CurrentProfile.KeySounds[key];
                    System.Diagnostics.Debug.WriteLine($"找到按键 {key} 的音效路径: {soundPath}");
                    return soundPath;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"当前方案中未找到按键 {key} 的音效");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("当前方案或按键音效映射为空");
                if (_profileManager?.CurrentProfile == null)
                {
                    System.Diagnostics.Debug.WriteLine("当前方案为空");
                }
                else if (_profileManager.CurrentProfile.KeySounds == null)
                {
                    System.Diagnostics.Debug.WriteLine("当前方案的KeySounds为空");
                }
            }
            
            // 不再返回默认音效，如果没有找到对应音效则返回null
            return null;
        }

        private string GetSoundFile(Key key)
        {
            // 尝试获取按键对应的声音文件
            var soundFile = _profileManager?.GetKeySound(key);

            // 如果找到了按键对应的声音文件，直接返回
            if (!string.IsNullOrEmpty(soundFile))
                return soundFile;

            // 不再返回默认音效，如果没有找到对应音效则返回null
            return null;
        }
        
        /// <summary>
        /// 当前方案改变时的处理方法
        /// </summary>
        private void OnCurrentProfileChanged(object sender, EventArgs e)
        {
            // 停止当前播放的音效并释放相关资源
            _waveOut.Stop();
            _audioFileReader?.Dispose();
            _audioFileReader = null;
            
            // 注意：_profileManager引用保持不变，因为它指向的是同一个实例
            // 我们只需要确保在获取音效路径时使用的是最新的CurrentProfile
        }
        
        /// <summary>
        /// 刷新服务状态以匹配当前配置方案
        /// </summary>
        public void Refresh()
        {
            // 停止当前播放的音效并释放相关资源
            _waveOut.Stop();
            _audioFileReader?.Dispose();
            _audioFileReader = null;
            
            // 注意：_profileManager引用保持不变，因为它指向的是同一个实例
            // 我们只需要确保在获取音效路径时使用的是最新的CurrentProfile
        }
        
        // 全局键盘钩子实现
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                PlaySound(key);
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        #region Windows API 导入
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion

        public void Dispose()
        {
            // 卸载键盘钩子
            UnhookWindowsHookEx(_hookID);
            
            // 取消订阅当前方案改变事件
            if (_profileManager != null)
            {
                _profileManager.CurrentProfileChanged -= OnCurrentProfileChanged;
            }
            
            // 释放音频资源
            _waveOut?.Dispose();
            _audioFileReader?.Dispose();
        }
    }

    
    /// <summary>
    /// 声音事件参数
    /// </summary>
    public class SoundEventArgs : EventArgs
    {
        public Key Key { get; }
        public string SoundPath { get; }
        
        public SoundEventArgs(Key key, string soundPath)
        {
            Key = key;
            SoundPath = soundPath;
        }
    }
    
    /// <summary>
    /// 声音错误事件参数
    /// </summary>
    public class SoundErrorEventArgs : EventArgs
    {
        public Key Key { get; }
        public string ErrorMessage { get; }
        
        public SoundErrorEventArgs(Key key, string errorMessage)
        {
            Key = key;
            ErrorMessage = errorMessage;
        }
    }
}