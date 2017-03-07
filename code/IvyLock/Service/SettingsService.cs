using IvyLock.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security;
using System.Xml.Serialization;
using System.Collections;

namespace IvyLock.Service
{
	public interface ISettingsService : IEnumerable<SettingGroup>
	{
		event Action<SettingGroup, string> GroupChanged;

		SettingGroup this[string name] { get; }

		SettingGroup Get(string name);

		bool Set(SettingGroup value);
	}

	public class XmlSettingsService : ISettingsService
	{ 
		private List<SettingGroup> _values;
		private XmlSerializer xs;
		private string path;

		public static ISettingsService Default { get; internal set; } = new XmlSettingsService();

		public XmlSettingsService()
		{
			path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			path = Path.Combine(path, "IvyLock");

			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			path = Path.Combine(path, "settings.xml");

			_values = new List<SettingGroup>();
			xs = new XmlSerializer(typeof(SettingGroup[]));

			Deserialize(false);
		}

		private void Deserialize(bool notify)
		{
			using (Stream s = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read))
			{
				try
				{
					_values = new List<SettingGroup>(xs.Deserialize(s) as SettingGroup[]);
					foreach (SettingGroup sg in _values)
					{
						sg.PropertyChanged += SettingGroupChanged;
					}
				}
				catch
				{
				}
			}
		}

		private void SettingGroupChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			Serialize();
		}

		private void Serialize()
		{
			
			using (Stream s = File.Open(path, FileMode.Create, FileAccess.Write))
			{
				try
				{
					xs.Serialize(s, _values.ToArray());
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

		public SettingGroup this[string name]
		{
			get
			{
				return Get(name);
			}

			set
			{
				Set(value);
			}
		}

		public event Action<SettingGroup, string> GroupChanged;

		public SettingGroup Get(string name)
		{
			return _values.FirstOrDefault(sg => sg.Name == name);
		}

		public bool Set(SettingGroup value)
		{
			try
			{
				SettingGroup sg = Get(value.Name);
				if (sg != null)
					_values.Remove(sg);

				_values.Add(value);
				
				Serialize();
				return true;
			}
			catch
			{
				return false;
			}
		}

		public IEnumerator<SettingGroup> GetEnumerator()
		{
			return _values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _values.GetEnumerator();
		}
	}
}