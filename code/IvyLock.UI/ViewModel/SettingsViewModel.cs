using IvyLock.Model;
using IvyLock.Service;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace IvyLock.UI.ViewModel
{
	public class SettingsViewModel : ViewModel
	{
		#region Fields

		private ISettingsService iss = XmlSettingsService.Default;
		private IProcessService ips = ManagedProcessService.Default;
		private SettingGroup _settingGroup;
		private ObservableCollection<SettingGroup> _settings = new ObservableCollection<SettingGroup>();

		#endregion Fields

		#region Constructors

		public SettingsViewModel()
		{
			RunTimeAsync(LoadProcesses).ContinueWith(LoadSettings);
		}

		private void LoadSettings(Task processTask)
		{
			foreach (SettingGroup sg in iss)
				if (sg.Valid)
					UI(() => Settings.Add(sg));

			SettingGroup = _settings.OfType<IvyLockSettings>().FirstOrDefault();
		}

		#endregion Constructors

		#region Properties

		public SettingGroup SettingGroup { get { return _settingGroup; } set { Set(value, ref _settingGroup); } }
		public ObservableCollection<SettingGroup> Settings { get { return _settings; } set { Set(value, ref _settings); } }

		#endregion Properties

		#region Methods

		private async Task LoadProcesses()
		{

			Func<Process, bool> f = p =>
			{
				try
				{
					return
						FileVersionInfo.GetVersionInfo(p.GetPath()).FileDescription != null &&
						p.MainWindowHandle != IntPtr.Zero;
				}
				catch { return false; }
			};


			iss = XmlSettingsService.Default;
			ips = ManagedProcessService.Default;
			ips.ProcessChanged += (pid, path, type) =>
			{
				if(type == ProcessOperation.Started)
				{
					if(!iss.OfType<ProcessSettings>().Any(ps => ps.Path.Equals(path)))
					{
						try
						{
							Process p = Process.GetProcessById(pid);

							if (f(p))
							{
								ProcessSettings ps = new ProcessSettings(p);
								iss.Set(ps);
								Settings.Add(ps);
							}
						}
						catch (Exception)
						{
						}
					}
				}
			};

			if (!iss.Any(s => s is IvyLockSettings))
				iss.Set(new IvyLockSettings());

			await Task.Run(() =>
			{
				foreach (
					Process process in
					from process in Process.GetProcesses()
					where f(process)
					orderby FileVersionInfo.GetVersionInfo(process.GetPath()).FileDescription
					select process)
				{
					if (process.MainModule.FileName.Equals(Assembly.GetEntryAssembly().Location))
						continue;
					try
					{
						ProcessSettings ps = new ProcessSettings(process);

						if (!iss.Any(s => s is ProcessSettings && ((ProcessSettings)s).Path == ps.Path))
							iss.Set(ps);
					}
					catch 
					{
					}
				}
			});
		}

		#endregion Methods
	}
}