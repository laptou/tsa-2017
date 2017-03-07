using IvyLock.Model;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using IvyLock.Service;

namespace IvyLock.UI.ViewModel
{
	public class SettingsViewModel : ViewModel
	{

		#region Fields

		ISettingsService iss = XmlSettingsService.Default;
		SettingGroup _settingGroup;
		ObservableCollection<SettingGroup> _settings = new ObservableCollection<SettingGroup>();

		#endregion Fields


		#region Constructors

		public SettingsViewModel()
		{
			if (!iss.Any(s => s is IvyLockSettings))
				iss.Set(new IvyLockSettings());
			
			RunTimeAsync(LoadProcesses).ContinueWith(LoadSettings);
		}

		private void LoadSettings(Task processTask)
		{
			UI(() =>
			{
				foreach (SettingGroup sg in iss)
					Settings.Add(sg);
			});
		}

		#endregion Constructors


		#region Properties

		public SettingGroup SettingGroup { get { return _settingGroup; } set { Set(value, ref _settingGroup); } }
		public ObservableCollection<SettingGroup> Settings { get { return _settings; } set { Set(value, ref _settings); } }

		#endregion Properties


		#region Methods

		private async Task LoadProcesses()
		{
			await Task.Run(() =>
			{
				Func<Process, bool> f = p =>
				{
					try {
						return 
							p.MainModule.FileVersionInfo.FileDescription != null &&
							p.MainWindowHandle != IntPtr.Zero;
					}
					catch { return false; }
				};

				foreach (
					Process process in
					from process in Process.GetProcesses()
					where  f(process)
					orderby process.MainModule.FileVersionInfo.FileDescription
					select process)
				{
					try
					{
						ProcessSettings ps = new ProcessSettings(process);

						if (!iss.Any(s => s is ProcessSettings && ((ProcessSettings)s).Path == ps.Path))
							iss.Set(ps);
					}
					catch (Exception)
					{
					}
				}
			});
		}

		#endregion Methods
	}
}