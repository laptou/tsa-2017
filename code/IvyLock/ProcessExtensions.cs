using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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

	public static class ProcessExtensions
	{
		#region Methods

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

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hHandle);

		[DllImport("kernel32.dll")]
		private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess,
					   bool bInheritHandle, int dwProcessId);

		[DllImport("kernel32.dll")]
		private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

		[DllImport("kernel32.dll")]
		private static extern bool QueryFullProcessImageName(IntPtr hprocess, int dwFlags,
					   StringBuilder lpExeName, out int size);

		[DllImport("kernel32.dll")]
		private static extern int ResumeThread(IntPtr hThread);

		[DllImport("kernel32.dll")]
		private static extern uint SuspendThread(IntPtr hThread);

		#endregion Methods

		public class PidComparer : IEqualityComparer<Process>
		{
			public bool Equals(Process x, Process y)
			{
				return x.Id == y.Id;
			}

			public int GetHashCode(Process obj)
			{
				return obj.Id;
			}
		}
	}
}