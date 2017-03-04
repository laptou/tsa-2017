using IvyLock.Service;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

namespace IvyLock.Model
{
	public abstract class SettingGroup : ObservableCollection<ISetting>
	{
		public string Name { get; set; }
		public BitmapSource Icon { get; set; }
	}

	public class IvySettingGroup : SettingGroup
	{
		public IvySettingGroup()
		{
			Name = "IvyLock";
			Add(new SecureSetting(NETSecureSettingsService.Default, "Passcode"));
			Add(new Setting<string>(NETSettingsService.Default, "TestString"));
			Add(new Setting<int>(NETSettingsService.Default, "TestNumber"));
			Add(new Setting<bool>(NETSettingsService.Default, "TestBool"));
		}
	}

	public interface ISetting : INotifyPropertyChanged
	{
		string Name { get; }
		SettingType Type { get; }
		object Value { set; }
	}

	public interface ISetting<T> : ISetting
	{
		new T Value { set; }
	}

	public class Setting<T> : ISetting<T>
	{
		private ISettingsService _service;

		public Setting(ISettingsService service, string name)
		{
			_service = service;
			_service.SettingChanged += SettingChanged;

			Type type = typeof(T);

			if (type == typeof(string))
				Type = SettingType.String;
			else if (type == typeof(bool))
				Type = SettingType.Boolean;
			else if (type == typeof(SecureString))
				throw new InvalidOperationException("Use SecureSetting instead!");
			else if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long))
				Type = SettingType.Number;
			else if (type.IsEnum)
				Type = SettingType.Enum;
			else
				Type = SettingType.Object;
		}

		private void SettingChanged(string setting)
		{
			if (string.Equals(setting, Name))
				PropertyChanged(this, new PropertyChangedEventArgs("Value"));
		}

		public string Name { get; protected set; }
		public SettingType Type { get; protected set; }
		public T Value
		{
			get { return _service.Get<T>(Name); }
			set { _service.Set(Name, value); }
		}
		public IEnumerable<T> EnumValues
		{
			get
			{
				if (Type != SettingType.Enum)
					throw new InvalidOperationException();

				return typeof(T).GetEnumValues().Cast<T>();
			}
		}

		object ISetting.Value
		{
			set
			{
				if (value is T)
					Value = (T)value;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	public class SecureSetting : ISetting<SecureString>
	{
		private ISecureSettingsService _service;

		public SecureSetting(ISecureSettingsService service, string name)
		{
			_service = service;
		}

		public string Name { get; protected set; }
		public SettingType Type { get { return SettingType.SecureString; } }

		public SecureString Value
		{
			set { _service.Set(Name, value); }
		}

		object ISetting.Value
		{
			set
			{
				if (value is SecureString)
					Value = (SecureString)value;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	public enum SettingType
	{
		Enum, String, SecureString, Boolean, Number, Object
	}
}