using IvyLock.Native;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace IvyLock.UI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		// Need to ensure delegate is not collected while we're using it,
		// storing it in a class field is simplest way to do this.
		static WinEventDelegate procDelegate = new WinEventDelegate(WinEventProc);
		static IntPtr hookHandle;

		private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, 
			int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);

			// hookHandle = WindowsHook.SetGlobalHook((uint)WM.CREATE, procDelegate);
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			// WindowsHook.ClearGlobalHook(hookHandle);
		}
	}
}