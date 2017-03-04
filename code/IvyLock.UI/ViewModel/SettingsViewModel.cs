using IvyLock.Model;
using System.Collections.ObjectModel;

namespace IvyLock.UI.ViewModel
{
	public class SettingsViewModel : ViewModel
	{
		public ObservableCollection<SettingGroup> Settings { get; set; } = new ObservableCollection<SettingGroup>();

		public SettingGroup SettingGroup { get; set; }

		public SettingsViewModel()
		{
			SettingGroup = new IvySettingGroup();
			Settings.Add(SettingGroup);
		}
	}
}