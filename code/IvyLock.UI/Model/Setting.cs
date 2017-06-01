using IvyLock.Service;
using IvyLock.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

namespace IvyLock.Model
{
    public enum SettingCategory
    {
        Security, Misc,
        Aesthetic
    }

    public enum SettingType
    {
        Number, Enum, String, Password, Boolean, Action
    }

    public enum Theme
    {
        Dark, Light
    }

    public class IvyLockSettings : SettingGroup
    {
        #region Fields

        private string _hash;
        private SecureString _password;
        private Theme _theme;
        private int _timeout;

        #endregion Fields

        #region Constructors

        public IvyLockSettings()
        {
            Name = "IvyLock";
        }

        public IvyLockSettings(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        #endregion Constructors

        #region Properties

        [Setting(Hide = true)]
        public string Hash
        {
            get { return _hash; }
            set { Set(value, ref _hash); }
        }

        private string _salt;

        [Setting(Hide = true)]
        public string Salt
        {
            get { return _salt; }
            set { Set(value, ref _salt); }
        }


        [XmlIgnore]
        [Setting(Category = SettingCategory.Security, Description = "Password used to unlock IvyLock settings.")]
        public SecureString Password
        {
            get
            {
                return _password;
            }
            set
            {
                Set(value, ref _password);

                Salt = EncryptionService.Default.Salt();

                if (value != null)
                    Hash = EncryptionService.Default.Hash(Password);

                RaisePropertyChanged("Hash");
            }
        }

        [Setting(Category = SettingCategory.Aesthetic)]
        public Theme Theme
        {
            get
            {
                return _theme;
            }
            set
            {
                Set(value, ref _theme);
            }
        }

        [Setting(Category = SettingCategory.Security, Description = "Amount of time before this window requires a password again.")]
        public int Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                Set(value, ref _timeout);
            }
        }

        #endregion Properties
    }

    public class ProcessSettings : SettingGroup
    {
        #region Fields

        private string _hash;
        private SecureString _password;
        private string _path;
        private bool _usePassword;

        private bool hasLockTimeOut = true;
        private int lockTimeOut = 15;
        private bool _allowBiometrics = true;

        #endregion Fields

        #region Constructors

        public ProcessSettings()
        {
        }

        public ProcessSettings(Process process)
        {
            Path = process.GetPath();
            Name = FileVersionInfo.GetVersionInfo(Path).FileDescription;
        }

        public ProcessSettings(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        #endregion Constructors

        #region Properties

        [XmlIgnore]
        [Setting(
            Name = "Clear Unique Password",
            Category = SettingCategory.Security)]
        public AsyncDelegateCommand ClearPassword
        {
            get
            {
                return new AsyncDelegateCommand(() => Task.Run(() => Hash = null));
            }
        }

        [Setting(Hide = true)]
        public string Hash { get { return _hash; } set { Set(value, ref _hash); } }

        [Setting(
            Name = "Timeout",
            Category = SettingCategory.Security,
            Description = "The amount of time (in minutes) before this app requires a password to start again.",
            MinValue = 1, MaxValue = 300)]
        public int LockTimeOut
        {
            get { return lockTimeOut; }
            set { Set(value, ref lockTimeOut); }
        }

        [XmlIgnore]
        [Setting(
            Category = SettingCategory.Security,
            Description = "Unique password used to unlock this app.")]
        public SecureString Password
        {
            get
            {
                return _password;
            }
            set
            {
                Set(value, ref _password);

                if (value != null)
                    Hash = EncryptionService.Default.Hash(Password);
            }
        }

        [Setting(Hide = true)]
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                _path = value;

                if (!File.Exists(_path))
                {
                    Valid = false;
                    return;
                }

                Name = FileVersionInfo.GetVersionInfo(value).FileDescription;
            }
        }

        [Setting(
            Name = "Use Timeout",
            Category = SettingCategory.Security,
            Description = "Whether this app should require a password to start after the first entry.")]
        public bool UseLockTimeOut
        {
            get { return hasLockTimeOut; }
            set { Set(value, ref hasLockTimeOut); }
        }

        [Setting(
            Category = SettingCategory.Security,
            Name = "Use Password",
            Description = "Whether this app is locked by IvyLock.")]
        public bool UsePassword
        {
            get { return _usePassword; }
            set { Set(value, ref _usePassword); }
        }

        [Setting(
            Category = SettingCategory.Security,
            Name = "Allow Biometric Authentication",
            Description = "Whether this app can be unlocked using a fingerprint.")]
        public bool AllowBiometricAuthentication
        {
            get { return _allowBiometrics; }
            set { Set(value, ref _allowBiometrics); }
        }

        #endregion Properties
    }

    public class Setting : Model
    {
        #region Fields

        private SettingGroup _settings;

        private object _value;

        #endregion Fields

        #region Constructors

        public Setting(SettingGroup settings)
        {
            _settings = settings;
        }

        #endregion Constructors

        #region Properties

        public SettingCategory Category { get; set; }
        public string Description { get; set; }
        public string Key { get; set; }
        public int MaxValue { get; set; }
        public int MinValue { get; set; }
        public string Name { get; set; }
        public Array Options { get; internal set; }
        public SettingType Type { get; set; }

        public object Value
        {
            get { return _value; }
            set { Set(value, ref _value); }
        }

        #endregion Properties

        #region Methods

        private void Update()
        {
        }

        #endregion Methods
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class SettingAttribute : Attribute
    {
        #region Constructors

        public SettingAttribute()
        {
        }

        #endregion Constructors

        #region Properties

        public SettingCategory Category { get; set; }
        public string DependsOn { get; set; }
        public string Description { get; set; }
        public bool Hide { get; set; }
        public bool Ignore { get; set; }
        public int MaxValue { get; set; } = int.MaxValue;
        public int MinValue { get; set; } = int.MinValue;
        public string Name { get; set; }
        public Func<object, bool> Predicate { get; set; }

        #endregion Properties
    }

    [XmlInclude(typeof(IvyLockSettings)), XmlInclude(typeof(ProcessSettings))]
    public abstract class SettingGroup : Model, ISerializable
    {
        #region Fields

        private Dictionary<string, Setting> settings = new Dictionary<string, Setting>();

        #endregion Fields

        #region Constructors

        public SettingGroup()
        {
        }

        public SettingGroup(SerializationInfo info, StreamingContext context)
        {
            PropertyInfo[] properties = GetType().GetProperties();

            foreach (PropertyInfo pi in properties)
            {
                if (pi.PropertyType == typeof(SecureString))
                    continue;

                pi.SetValue(this, info.GetValue(pi.Name, pi.PropertyType));
            }
        }

        #endregion Constructors

        #region Properties

        [Setting(Hide = true)]
        public string Name { get; set; }

        [XmlIgnore]
        [Setting(Ignore = true)]
        public IEnumerable<Setting> Settings
        {
            get
            {
                return settings.Values;
            }
        }

        [XmlIgnore]
        [Setting(Ignore = true)]
        public bool Valid { get; protected set; } = true;

        #endregion Properties

        #region Methods

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            foreach (Setting s in GetSettings(true))
            {
                info.AddValue(s.Key, s.Value, s.Value.GetType());
            }
        }

        private IEnumerable<Setting> GetSettings(bool includeHidden = false)
        {
            PropertyInfo[] properties = GetType().GetProperties();

            foreach (PropertyInfo pi in properties)
            {
                Setting s = new Setting(this)
                {
                    Key = pi.Name,
                    Name = pi.Name
                };

                if (pi.PropertyType == typeof(string))
                    s.Type = SettingType.String;
                if (pi.PropertyType == typeof(SecureString))
                    s.Type = SettingType.Password;
                if (pi.PropertyType == typeof(bool))
                    s.Type = SettingType.Boolean;
                if (pi.PropertyType == typeof(int))
                    s.Type = SettingType.Number;
                if (pi.PropertyType == typeof(DelegateCommand) ||
                    pi.PropertyType == typeof(AsyncDelegateCommand))
                    s.Type = SettingType.Action;
                if (pi.PropertyType.IsEnum)
                    s.Type = SettingType.Enum;

                SettingAttribute attr = pi.GetCustomAttribute<SettingAttribute>();
                if (attr != null)
                {
                    if (attr.Ignore || (attr.Hide && !includeHidden))
                        continue;

                    s.Name = attr.Name ?? s.Name;
                    s.Category = attr.Category;
                    s.Description = attr.Description;

                    if (s.Type != SettingType.Password)
                        s.Value = pi.GetValue(this);

                    if (s.Type == SettingType.Number)
                    {
                        s.MinValue = attr.MinValue;
                        s.MaxValue = attr.MaxValue;
                    }

                    if (s.Type == SettingType.Enum)
                    {
                        s.Options = pi.PropertyType.GetEnumValues();
                    }
                }
                s.PropertyChanged += OnSettingChanged;

                yield return s;
            }
        }

        public void Initialize()
        {
            foreach (Setting setting in GetSettings())
            {
                settings.Add(setting.Key, setting);
            }

            RaisePropertyChanged("Settings");
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (settings.ContainsKey(e.PropertyName))
                settings[e.PropertyName].Value = GetType().GetProperty(e.PropertyName)?.GetValue(this);
        }

        private void OnSettingChanged(object sender, PropertyChangedEventArgs e)
        {
            Setting s = sender as Setting;
            GetType().GetProperty(s.Key)?.SetValue(this, s.Value);
            RaisePropertyChanged(s.Key);
        }

        #endregion Methods

        public override string ToString()
        {
            return Name;
        }
    }
}