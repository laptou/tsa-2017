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