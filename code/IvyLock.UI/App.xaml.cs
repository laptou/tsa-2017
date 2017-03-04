using IvyLock.Native;
using System;
using System.Windows;

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

			GlobalHook.Initialize();
			GlobalHook.SetHook(HookType.CBT, info =>
			{
				return info;
			});
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			GlobalHook.Stop();
		}
	}
}