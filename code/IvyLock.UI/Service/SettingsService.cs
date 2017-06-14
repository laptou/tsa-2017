using IvyLock.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace IvyLock.Service
{
    public interface ISettingsService : IEnumerable<SettingGroup>, IDisposable
    {
        SettingGroup this[string name] { get; }

        SettingGroup Get(string name);

        bool Set(SettingGroup value);

        ProcessSettings FindByPath(string path);
    }

    public class XmlSettingsService : ISettingsService
    {
        private List<SettingGroup> values;
        private XmlSerializer xs;
        private Stream stream;
        private string path;

        public static ISettingsService Default { get; private set; }

        public static void Init()
        {
            Default = new XmlSettingsService();
        }

        public XmlSettingsService()
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Combine(path, "IvyLock");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, "settings.xml");

            values = new List<SettingGroup>();
            xs = new XmlSerializer(typeof(SettingGroup[]));

            stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            
            Deserialize(false);
        }

        class SGCompare : EqualityComparer<SettingGroup>
        {
            public override bool Equals(SettingGroup x, SettingGroup y)
            {
                if (x is IvyLockSettings && y is IvyLockSettings)
                    return true;
                if (x is ProcessSettings px && y is ProcessSettings py)
                    return string.Equals(px.Path, py.Path);
                return false;
            }

            public override int GetHashCode(SettingGroup obj)
            {
                return obj.GetHashCode();
            }
        }

        private void Deserialize(bool notify)
        {
            if (stream == null) return;

            try
            {
                if (stream.Length == 0)
                {
                    IvyLockSettings ils = new IvyLockSettings();

                    values = new List<SettingGroup>(new SettingGroup[] { ils });
                }
                else
                {
                    stream.Position = 0;

                    values = new List<SettingGroup>(xs.Deserialize(stream) as SettingGroup[]);
                }

                if (!values.Any(sg => sg is IvyLockSettings))
                    values.Add(new IvyLockSettings());

                values = values.Where(sg =>
                {
                    if (sg is IvyLockSettings) return true;

                    var psg = sg as ProcessSettings;

                    if (!File.Exists(psg.Path)) return false;

                    return true;
                }).Distinct(new SGCompare()).ToList();

                foreach (SettingGroup sg in values)
                {
                    sg.Initialize();
                    sg.PropertyChanged += SettingGroupChanged;
                }
            }
            catch
            {
                IvyLockSettings ils = new IvyLockSettings();

                values = new List<SettingGroup>(new SettingGroup[] { ils });

                foreach (SettingGroup sg in values)
                {
                    sg.Initialize();
                    sg.PropertyChanged += SettingGroupChanged;
                }
            }
        }

        private void SettingGroupChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Serialize();
        }

        private void Serialize()
        {
            if (stream == null) return;

            try
            {
                stream.Position = 0;
                stream?.SetLength(0);
                xs.Serialize(stream, values.ToArray());
            }
            catch
            {
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

        public SettingGroup Get(string name)
        {
            return values.FirstOrDefault(sg => sg.Name == name);
        }

        public bool Set(SettingGroup value)
        {
            try
            {
                SettingGroup sg = Get(value.Name);
                if (sg != null)
                    values.Remove(sg);

                values.Add(value);
                value.PropertyChanged += SettingGroupChanged;

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
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        public void Dispose()
        {
            stream?.Dispose();
        }

        public ProcessSettings FindByPath(string path)
        {
            return this.OfType<ProcessSettings>()
                    .FirstOrDefault(s =>
                        string.Equals(
                            s.Path,
                            path,
                            StringComparison.InvariantCultureIgnoreCase));
        }
    }

    public class DesignerSettingsService : List<SettingGroup>, ISettingsService
    {
        public static DesignerSettingsService Default { get; private set; } = new DesignerSettingsService();

        public SettingGroup this[string name]
        {
            get
            {
                return Enumerable.FirstOrDefault(this, sg => sg.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public DesignerSettingsService()
        {
            Add(new IvyLockSettings());
        }

        public SettingGroup Get(string name)
        {
            return this[name];
        }

        public bool Set(SettingGroup value)
        {
            return true;
        }

        public void Dispose()
        {
        }

        public ProcessSettings FindByPath(string path)
        {
            throw new NotImplementedException();
        }
    }
}