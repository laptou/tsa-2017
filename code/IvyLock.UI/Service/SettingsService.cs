using IvyLock.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
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
        private List<SettingGroup> _values;
        private XmlSerializer xs;
        private Stream stream;
        private string path;
        private volatile static bool initialised = false;

        public static ISettingsService Default { get; private set; }

        static XmlSettingsService()
        {
            if (!initialised && Default == null && !App.IsDesigner)
                Default = new XmlSettingsService();
        }

        public XmlSettingsService()
        {
            initialised = true;

            path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Combine(path, "IvyLock");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, "settings.xml");

            _values = new List<SettingGroup>();
            xs = new XmlSerializer(typeof(SettingGroup[]));

            try
            {
                stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }
            catch (IOException ioex)
            {
                if (ioex.HResult == unchecked((int)0x80070020)) // file is in use
                {
                    MessageBox.Show("IvyLock settings file is in use.",
                        "IvyLock", MessageBoxButton.OK);
                    Application.Current.Shutdown();
                    return;
                }
            }

            Deserialize(false);
        }

        private void Deserialize(bool notify)
        {
            if (stream == null) return;

            try
            {
                if (stream.Length == 0)
                {
                    IvyLockSettings ils = new IvyLockSettings();
                    ils.Initialize();

                    _values = new List<SettingGroup>(new SettingGroup[] { ils });
                }
                else
                {
                    stream.Position = 0;

                    _values = new List<SettingGroup>(xs.Deserialize(stream) as SettingGroup[]);
                }

                foreach (SettingGroup sg in _values)
                {
                    sg.Initialize();
                    sg.PropertyChanged += SettingGroupChanged;
                }
            }
            catch
            {
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
                xs.Serialize(stream, _values.ToArray());
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
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
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