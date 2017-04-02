using IvyLock.UI.ViewModel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace IvyLock.UI.View
{
	/// <summary>
	/// Interaction logic for AuthenticationView.xaml
	/// </summary>
	public partial class AuthenticationView : Window
	{
		AuthenticationViewModel avm;

		public AuthenticationView()
		{
			InitializeComponent();
			avm = DataContext as AuthenticationViewModel;
		}

		private void AuthenticationViewModel_CloseRequested()
		{
			if(IsLoaded)
				Close();
		}

		protected override void OnClosed(EventArgs e)
		{
			Task.Run(() =>
			{
				if (avm.Locked)
					foreach (Process process in avm.Processes)
						try { process.Kill(); } catch { }
			});

			base.OnClosed(e);
		}

		private void AuthenticationViewModel_ShowRequested()
		{
			Show();
		}
	}
}