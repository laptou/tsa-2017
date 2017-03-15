using IvyLock.Model;
using IvyLock.Native;
using IvyLock.Service;
using IvyLock.UI.View;
using IvyLock.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace IvyLock.UI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : System.Windows.Application
	{
		private Dictionary<string, AuthenticationView> views = new Dictionary<string, AuthenticationView>();
		private IProcessService ips;
		private ISettingsService iss;
		private NotifyIcon ni;

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);

			// don't initialise statically! XmlSettingsService depends on Application.Current
			ips = ManagedProcessService.Default;
			iss = XmlSettingsService.Default;
			ips.ProcessChanged += ProcessChanged;

			ni = new NotifyIcon();
			ni.Click += (s, e1) => MainWindow.Show();
			ni.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location);
			ni.Visible = true;
		}

		private void ProcessChanged(int pid, string path, ProcessOperation po)
		{
			Dispatcher?.Invoke(() =>
			{
				ProcessSettings ps = iss.OfType<ProcessSettings>().FirstOrDefault(s => s.Path?.Equals(path) == true);
				if (ps == null)
					return;

				if (!ps.UsePassword)
					return;

				switch (po)
				{
					case ProcessOperation.Started:
						if (path == null || views.ContainsKey(path)) return;

						AuthenticationView av = new AuthenticationView();
						AuthenticationViewModel avm = av.DataContext as AuthenticationViewModel;
						avm.Process = Process.GetProcessById(pid);
						avm.Lock();
						views.Add(path, av);
						av.Show();
						break;
					case ProcessOperation.Modified:
						break;
					case ProcessOperation.Deleted:
						if (path == null || !views.ContainsKey(path)) return;

						views[path].Close();
						views.Remove(path);
						break;
					default:
						break;
				}
			});
		}

		protected override void OnExit(ExitEventArgs e)
		{
			ips.Dispose();
			iss.Dispose();
			ni.Dispose();
			base.OnExit(e);
		}
	}
}