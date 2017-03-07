using IvyLock.Native;
using System;
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

			GlobalHook.Initialize();
			GlobalHook.SetHook(HookType.CBT, info =>
			{
				if((CBTType)info.nCode == CBTType.CreateWnd)
				{

				}
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