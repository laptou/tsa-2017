using System;

namespace IvyLock.Service
{
	public interface ISecureSettingsService
	{
		object this[string name] { set; }

		bool Match<T>(string name, T value);

		bool Set<T>(string name, T value);
	}

	public interface ISettingsService
	{
		event Action<string> SettingChanged;

		object this[string name] { get; set; }

		T Get<T>(string name);

		bool Set<T>(string name, T value);
	}

	public class NETSecureSettingsService : ISecureSettingsService
	{
		private ISettingsService _nss = NETSettingsService.Default;

		public static ISecureSettingsService Default { get; set; }

		public object this[string name]
		{
			set
			{
				Set(name, value);
			}
		}

		public bool Match<T>(string name, T value)
		{
			IEncryptionService ies = EncryptionService.Default;

			return _nss.Get<string>(name).Equals(ies.Hash(value));
		}

		public bool Set<T>(string name, T value)
		{
			IEncryptionService ies = EncryptionService.Default;

			return _nss.Set(name, ies.Hash(value));
		}

		static NETSecureSettingsService()
		{
			Default = new NETSecureSettingsService();
		}
	}

	public class NETSettingsService : ISettingsService
	{
		static NETSettingsService()
		{
			Default = new NETSettingsService();
		}

		private NETSettingsService()
		{
			Properties.Settings.Default.PropertyChanged += (s, e) => SettingChanged(e.PropertyName);
		}

		public event Action<string> SettingChanged;

		public static ISettingsService Default { get; set; }

		public object this[string name]
		{
			get { return Get<object>(name); }

			set { Set(name, value); }
		}

		public T Get<T>(string name)
		{
			object obj = Properties.Settings.Default[name];
			return obj is T ? (T)obj : default(T);
		}

		public bool Set<T>(string name, T value)
		{
			try
			{
				Properties.Settings.Default[name] = value;
				Properties.Settings.Default.Save();
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}