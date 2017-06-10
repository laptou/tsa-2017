﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IvyLock
{
    [Flags]
    public enum ProcessAccessFlags : uint
    {
        All = 0x001F0FFF,
        Terminate = 0x00000001,
        CreateThread = 0x00000002,
        VirtualMemoryOperation = 0x00000008,
        VirtualMemoryRead = 0x00000010,
        VirtualMemoryWrite = 0x00000020,
        DuplicateHandle = 0x00000040,
        CreateProcess = 0x000000080,
        SetQuota = 0x00000100,
        SetInformation = 0x00000200,
        QueryInformation = 0x00000400,
        QueryLimitedInformation = 0x00001000,
        Synchronize = 0x00100000
    }

    public enum ShowWindow
    {
        Hide = 0,
        Maximize = 3,
        Minimize = 6,
        Show = 5,
        Restore = 9,
        ShowDefault = 10,
        ShowMinNoActive = 7,
        ShowNoActive = 8,
        Normal = 1,
        ShowNormalNoActive = 4,
        ForceMinimize = 11,
        ShowMin = 2,
    }

    [Flags]
    public enum ThreadAccess : int
    {
        TERMINATE = (0x0001),
        SUSPEND_RESUME = (0x0002),
        GET_CONTEXT = (0x0008),
        SET_CONTEXT = (0x0010),
        SET_INFORMATION = (0x0020),
        QUERY_INFORMATION = (0x0040),
        SET_THREAD_TOKEN = (0x0080),
        IMPERSONATE = (0x0100),
        DIRECT_IMPERSONATION = (0x0200)
    }

    public static class NativeWindow
    {
        #region Methods

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindow nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindowAsync(IntPtr hWnd, ShowWindow nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr hWnd);

        public static ShowWindow GetWindowState(IntPtr hWnd)
        {
            if (!IsWindowVisible(hWnd))
                return IvyLock.ShowWindow.Hide;
            else if (IsIconic(hWnd))
                return IvyLock.ShowWindow.Minimize;
            else if (IsZoomed(hWnd))
                return IvyLock.ShowWindow.Maximize;
            else
                // not hidden, minimized or zoomed, so we are a normal
                // visible window last ShowWindow flag could have been
                // SW_RESTORE, SW_SHOW, SW_SHOWNA, etc no way to tell
                return IvyLock.ShowWindow.Show;
        }

        #endregion Methods
    }

    public static class ProcessExtensions
    {
        #region Delegates

        private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        #endregion Delegates

        #region Methods

        public static List<IntPtr> EnumerateProcessWindowHandles(this Process process)
        {
            var handles = new List<IntPtr>();

            foreach (ProcessThread thread in process.Threads)
                EnumThreadWindows(thread.Id,
                    (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);

            return handles;
        }

        public static string GetPath(this Process process)
        {
            if (process == null) return null;

            var buffer = new StringBuilder(1024);
            IntPtr hprocess = OpenProcess(ProcessAccessFlags.QueryLimitedInformation,
                                          false, process.Id);
            if (hprocess != IntPtr.Zero)
            {
                try
                {
                    int size = buffer.Capacity;
                    if (QueryFullProcessImageName(hprocess, 0, buffer, out size))
                    {
                        return buffer.ToString();
                    }
                    else
                    {
                        // throw new Win32Exception(Marshal.GetLastWin32Error());
                        return null;
                    }
                }
                finally
                {
                    CloseHandle(hprocess);
                }
            }
            else
            {
                // throw new Win32Exception(Marshal.GetLastWin32Error());
                return null;
            }
        }

        public static async Task<string> GetDescription(this Process process)
        {
            return await Task.Run(() =>
            {
                try { return FileVersionInfo.GetVersionInfo(process.GetPath()).FileDescription; }
                catch { return null; }
            });
        }

        public static async Task<bool> HasGUI(this Process process)
        {
            try
            {
                if (process.MainWindowHandle != default(IntPtr))
                    return true;

                uint result = await Task.Run(() => WaitForInputIdle(process.Handle, 15000));
                switch (result)
                {
                    case 0xFFFFFFFF: // WAIT_FAILED
                        return false;

                    default:
                        return process.MainWindowHandle != default(IntPtr);
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool IsUWP(this Process process)
        {
            return IsImmersiveProcess(process.Handle);
        }

        public static void Resume(this Process process)
        {
            if (process == null) return;

            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                    continue;
                ResumeThread(pOpenThread);
            }
        }

        public static void Suspend(this Process process)
        {
            if (process == null) return;

            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                    continue;
                SuspendThread(pOpenThread);
            }
        }

        [DllImport("user32.dll")]
        internal static extern uint WaitForInputIdle(IntPtr hProcess, uint dwMilliseconds);

        [DllImport("kernel32.dll")]
        internal static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        internal static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn,
            IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess,
                       bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern bool QueryFullProcessImageName(IntPtr hprocess, int dwFlags,
                       StringBuilder lpExeName, out int size);

        [DllImport("user32.dll")]
        private static extern bool IsImmersiveProcess(IntPtr hProcess);

        #endregion Methods

        #region Classes

        public class PidComparer : IEqualityComparer<Process>
        {
            #region Methods

            public bool Equals(Process x, Process y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(Process obj)
            {
                return obj.Id;
            }

            #endregion Methods
        }

        #endregion Classes
    }
}