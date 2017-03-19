using IvyLock.Model;
using IvyLock.Service;
using IvyLock.UI.View;
using IvyLock.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
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

		private static Mutex mutex = new Mutex(true, "{37EFBF56-B711-42E3-B3D0-0DCDA7BC09BA}");

		protected override void OnStartup(StartupEventArgs e)
		{
			if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				try
				{
					if (mutex.WaitOne(TimeSpan.Zero, true))
						mutex.ReleaseMutex();
					else
					{
						Shutdown();
						return;
					}
				}
				catch (AbandonedMutexException)
				{
					mutex.ReleaseMutex();
				}
				base.OnStartup(e);

				// don't initialise statically! XmlSettingsService depends
				// on Application.Current
				ips = ManagedProcessService.Default;
				iss = XmlSettingsService.Default;
				ips.ProcessChanged += ProcessChanged;

				ni = new NotifyIcon();
				ni.Click += (s, e1) => MainWindow.Show();
				ni.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location);
				ni.Visible = true;
			}
			else
			{
				base.OnStartup(e);
			}
		}

		private void ProcessChanged(int pid, string path, ProcessOperation po)
		{
			ProcessSettings ps = iss.OfType<ProcessSettings>().FirstOrDefault(s => s.Path?.Equals(path) == true);
			if (ps == null)
				return;

			if (!ps.UsePassword)
				return;

			Task.Run(() =>
			{
				switch (po)
				{
					case ProcessOperation.Started:
						if (path == null) return;

						Dispatcher?.Invoke(async () =>
						{
							if (views.ContainsKey(path))
							{
								views[path].Show();
								return;
							}

							AuthenticationView av = views.ContainsKey(path) ? views[path] : new AuthenticationView();
							AuthenticationViewModel avm = av.DataContext as AuthenticationViewModel;
							avm.ProcessPath = path;
							avm.Processes.Add(Process.GetProcessById(pid));

							await avm.Lock();

							if (!avm.Locked)
							{
								avm.Dispose();
								return;
							}

							if (!views.ContainsKey(path))
								av.Closed += (s, e) => views.Remove(path);

							views[path] = av;
							av.Show();
						});
						break;

					case ProcessOperation.Modified:
						break;

					case ProcessOperation.Deleted:
						if (path == null || !views.ContainsKey(path)) return;

						if (!Process.GetProcesses().Any(process =>
						 {
							 try
							 {
								 return process.MainModule.FileName.Equals(path);
							 }
							 catch (Win32Exception) { return false; }
						 }))
						{
							Dispatcher?.Invoke(views[path].Close);
							((AuthenticationViewModel)views[path].DataContext).Dispose();
							if (views.ContainsKey(path))
								views.Remove(path);
						}
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