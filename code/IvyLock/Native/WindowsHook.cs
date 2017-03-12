using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IvyLock.Native
{
	public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
		IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

	public class Interop
	{
		public enum ShellEvents : int
		{
			HSHELL_WINDOWCREATED = 1,
			HSHELL_WINDOWDESTROYED = 2,
			HSHELL_ACTIVATESHELLWINDOW = 3,
			HSHELL_WINDOWACTIVATED = 4,
			HSHELL_GETMINRECT = 5,
			HSHELL_REDRAW = 6,
			HSHELL_TASKMAN = 7,
			HSHELL_LANGUAGE = 8,
			HSHELL_ACCESSIBILITYSTATE = 11,
			HSHELL_APPCOMMAND = 12
		}
		[DllImport("user32.dll", EntryPoint = "RegisterWindowMessageA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		public static extern int RegisterWindowMessage(string lpString);
		[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
		public static extern int DeregisterShellHookWindow(IntPtr hWnd);
		[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
		public static extern int RegisterShellHookWindow(IntPtr hWnd);
	}
}
