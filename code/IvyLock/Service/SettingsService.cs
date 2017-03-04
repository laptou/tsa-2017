using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

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

		private void Create<T>(string name)
		{
			SettingsProperty sp = new SettingsProperty(name);

			sp.DefaultValue = default(T);
			sp.Provider = new LocalFileSettingsProvider() { ApplicationName = "IvyLock" };
			sp.Attributes.Add(typeof(UserScopedSettingAttribute),
									new UserScopedSettingAttribute());
			sp.PropertyType = typeof(T);
			Properties.Settings.Default.Properties.Add(sp);
			Properties.Settings.Default.Save();
			Properties.Settings.Default.Reload();
		}

		public T Get<T>(string name)
		{
			if (!Properties.Settings.Default.Properties.Cast<SettingsProperty>().Any(x => x.Name.Equals(name)))
			{
				Create<T>(name);
				return default(T);
			}

			object obj = Properties.Settings.Default[name];
			return obj is T ? (T)obj : default(T);
		}

		public bool Set<T>(string name, T value)
		{
			try
			{
				if (!Properties.Settings.Default.Properties.Cast<SettingsProperty>().Any(x => x.Name.Equals(name)))
				{
					Create<T>(name);
				}

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

	public class XmlSettingsService : ISettingsService
	{
		private FileSystemWatcher _fsw;
		private Dictionary<string, object> _values;
		private string path;

		public static ISettingsService Default { get; internal set; } = new XmlSettingsService();

		// Have to use a custom KVP class b/c KeyValuePair`2 is not serializable
		[Serializable]
		[XmlType(TypeName = "setting")]
		public struct XmlKVP<K, V>
		{
			public XmlKVP(K key, V value)
			{
				Key = key;
				Value = value;
			}

			public K Key
			{ get; set; }

			public V Value
			{ get; set; }
		}

		public XmlSettingsService()
		{
			path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			path = Path.Combine(path, "IvyLock");

			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			path = Path.Combine(path, "settings.xml");

			//_fsw = new FileSystemWatcher(path);
			//_fsw.Filter = "settings.xml";
			//_fsw.Changed += FileChanged;

			_values = new Dictionary<string, object>();

			Deserialize(false);
		}

		private void Deserialize(bool notify)
		{
			List<XmlKVP<string, object>> newValues;
			XmlSerializer xs = new XmlSerializer(typeof(List<XmlKVP<string, object>>));
			using (Stream s = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read))
			{
				try
				{
					newValues = xs.Deserialize(s) as List<XmlKVP<string, object>>;

					foreach (XmlKVP<string, object> kv in newValues)
					{
						if (!_values.ContainsKey(kv.Key) || !_values[kv.Key].Equals(kv.Value))
						{
							_values[kv.Key] = kv.Value;
						
							if(notify)
								SettingChanged?.Invoke(kv.Key);
						}
					}
				}
				catch
				{
				}
			}
		}

		private void Serialize()
		{
			XmlSerializer xs = new XmlSerializer(typeof(List<XmlKVP<string, object>>));
			using (Stream s = File.Open(path, FileMode.Create, FileAccess.Write))
			{
				try
				{
					xs.Serialize(s, _values.Select(x => new XmlKVP<string, object>(x.Key, x.Value)).ToList());
				}
				catch
				{
				}
			}
		}

		private void FileChanged(object sender, FileSystemEventArgs e)
		{
			Deserialize(true);
		}

		~XmlSettingsService()
		{
			if(_fsw != null)
				_fsw.Dispose();
		}

		public object this[string name]
		{
			get
			{
				return Get<object>(name);
			}

			set
			{
				Set(name, value);
			}
		}

		public event Action<string> SettingChanged;

		public T Get<T>(string name)
		{
			return _values.ContainsKey(name) && _values[name] is T ? (T)_values[name] : default(T);
		}

		public bool Set<T>(string name, T value)
		{
			try
			{
				_values[name] = value;
				Serialize();
				return true;
			}
			catch
			{
				return false;
			}
		}
	}

	public class XmlSecureSettingsService : XmlSettingsService, ISecureSettingsService
	{
		new public static ISecureSettingsService Default { get; internal set; } = new XmlSecureSettingsService();

		new public object this[string name]
		{
			set
			{
				Set(name, value);
			}
		}

		public bool Match<T>(string name, T value)
		{
			IEncryptionService ies = EncryptionService.Default;

			return base.Get<string>(name).Equals(ies.Hash(value));
		}

		new public bool Set<T>(string name, T value)
		{
			IEncryptionService ies = EncryptionService.Default;

			return base.Set(name, ies.Hash(value));
		}
	}
}