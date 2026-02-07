using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows;
using NAudio.Wave;
using System.Collections.Concurrent;

namespace EKSE.Services
{
    public class SoundService : IDisposable
    {
        private readonly WaveOutEvent _waveOut = new();
        private AudioFileReader? _audioFileReader;
        private readonly ProfileManager _profileManager;
        private readonly ConcurrentDictionary<Key, bool> _keyStates = new();
        private IntPtr _hookID = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc;

        public event EventHandler? SoundPlayed;
        public event EventHandler? SoundError;

        public SoundService(ProfileManager profileManager)
        {
            _profileManager = profileManager;
            _profileManager.CurrentProfileChanged += (_, _) => ResetAudioState();
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        public void PlaySound(Key key)
        {
            try
            {
                if (Application.Current is App app && app.SettingsManager?.GetCurrentSettings() is { EnableSound: false })
                    return;

                var soundPath = _profileManager.GetKeySound(key);
                if (string.IsNullOrEmpty(soundPath) || !File.Exists(soundPath))
                {
                    SoundError?.Invoke(this, new SoundErrorEventArgs(key, "音效文件不存在"));
                    return;
                }

                PlayAudio(soundPath);
                SoundPlayed?.Invoke(this, new SoundEventArgs(key, soundPath));
            }
            catch (Exception ex)
            {
                SoundError?.Invoke(this, new SoundErrorEventArgs(key, ex.Message));
            }
        }

        private void PlayAudio(string path)
        {
            lock (this)
            {
                if (_waveOut.PlaybackState != PlaybackState.Stopped)
                    _waveOut.Stop();

                _audioFileReader?.Dispose();
                _audioFileReader = new AudioFileReader(path);
                _waveOut.Init(_audioFileReader);
                _waveOut.Play();
            }
        }

        public void PlayAudioFile(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                PlayAudio(path);
        }

        private void ResetAudioState()
        {
            try { _waveOut.Stop(); } catch { }
            _audioFileReader?.Dispose();
            _audioFileReader = null;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var key = KeyInterop.KeyFromVirtualKey(Marshal.ReadInt32(lParam));
                if (wParam == (IntPtr)0x0100 && (!_keyStates.TryGetValue(key, out var state) || !state))
                {
                    PlaySound(key);
                    _keyStates[key] = true;
                }
                else if (wParam == (IntPtr)0x0101)
                {
                    _keyStates[key] = false;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Refresh() => ResetAudioState();

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            using var module = process.MainModule;
            return SetWindowsHookEx(13, proc, GetModuleHandle(module?.ModuleName ?? ""), 0);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookID);
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioFileReader?.Dispose();
            _keyStates.Clear();
        }
    }

    public class SoundEventArgs(Key key, string soundPath) : EventArgs
    {
        public Key Key { get; } = key;
        public string SoundPath { get; } = soundPath;
    }

    public class SoundErrorEventArgs(Key key, string errorMessage) : EventArgs
    {
        public Key Key { get; } = key;
        public string ErrorMessage { get; } = errorMessage;
    }
}
