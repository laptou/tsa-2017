using IvyLock.Model;
using IvyLock.Service;
using IvyLock.View;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace IvyLock.ViewModel
{
    public enum Screen
    {
        Loading, EnterPassword, SetupPassword, Main
    }

    public sealed class SettingsViewModel : PasswordValidationViewModel
    {
        #region Fields

        private Screen _currentScreen = Screen.Loading;
        private SettingGroup _settingGroup;
        private ObservableCollection<SettingGroup> _settings = new ObservableCollection<SettingGroup>();
        private IEncryptionService ies;
        private IProcessService ips;
        private ISettingsService iss;

        #endregion Fields

        #region Constructors

        public SettingsViewModel()
        {
            DesignTime(() => iss = DesignerSettingsService.Default);
            RunTime(() => Task.Run(async () =>
            {
                iss = XmlSettingsService.Default;
                ips = ManagedProcessService.Default;
                ies = EncryptionService.Default;

                PropertyChanged += async (s, e) =>
                {
                    if (e.PropertyName == "Password" && Password != null)
                        await ValidatePassword();
                };

                PasswordVerified += (s, e) =>
                {
                    Task.Run(() =>
                    {
                        Password = null;
                        RaisePropertyChanged("Password");

                        CurrentScreen = Screen.Main;
                    });
                };

                if (string.IsNullOrWhiteSpace(IvyLockSettings.Hash))
                    CurrentScreen = Screen.SetupPassword;
                else
                    CurrentScreen = Screen.EnterPassword;

                UI(() => Settings.Add(IvyLockSettings));

                await LoadSettings();
                await LoadProcesses();
            }));
            BindingOperations.EnableCollectionSynchronization(Settings, this);
        }

        #endregion Constructors

        #region Properties

        public DelegateCommand AdvanceScreenCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    switch (CurrentScreen)
                    {
                        case Screen.SetupPassword:
                            if (!string.IsNullOrWhiteSpace(IvyLockSettings.Hash))
                                CurrentScreen = Screen.Main;
                            break;

                        case Screen.Main:
                            break;
                    }
                });
            }
        }

        public Screen CurrentScreen { get { return _currentScreen; } set { Set(value, ref _currentScreen); } }

        public IvyLockSettings IvyLockSettings { get { return iss.OfType<IvyLockSettings>().FirstOrDefault(); } }

        private SecureString password;

        public SecureString Password
        {
            get => password;
            set => Set(value, ref password);
        }

        public SettingGroup SettingGroup { get { return _settingGroup; } set { Set(value, ref _settingGroup); } }

        public ObservableCollection<SettingGroup> Settings { get { return _settings; } set { Set(value, ref _settings); } }

        public SettingsView View
        {
            get;
            set;
        }

        public override string GetPasswordHash()
        {
            if (CurrentScreen == Screen.SetupPassword)
                return GetUserPasswordHash();
            else return IvyLockSettings?.Hash;
        }

        public override string GetUserPasswordHash()
        {
            return ies.Hash(Password);
        }

        #endregion Properties

        #region Methods

        private async Task LoadProcesses()
        {
            int myPid = Process.GetCurrentProcess().Id;

            Func<Process, Task<bool>> f = async p =>
            {
                string path = p.GetPath();
                return
                    p.Id != myPid &&
                    path != null &&
                    !p.IsUWP() &&
                    await p.GetDescription() != null &&
                    await p.HasGUI();
            };

            ips.ProcessChanged += async (pid, path, type) =>
            {
                if (type == ProcessOperation.Started)
                {
                    if (iss.FindByPath(path) == null)
                    {
                        try
                        {
                            Process proc = Process.GetProcessById(pid);

                            if (await f(proc))
                            {
                                ProcessSettings s = new ProcessSettings(proc);
                                s.Initialize();
                                iss.Set(s);

                                var x = Settings.FirstOrDefault(sg => sg.Name.CompareTo(s.Name) > 0);
                                if (x != null)
                                    Settings.Insert(Settings.IndexOf(x) + 1, s);
                                else
                                    Settings.Add(s);
                            }
                        }
                        finally
                        {
                        }
                    }
                }
            };

            await AsyncParallel.ForEachAsync(Process.GetProcesses(), 50, async process =>
            {
                try
                {
                    if (await f(process))
                    {
                        string path = process.GetPath()?.ToLower();

                        if (string.Equals(Assembly.GetEntryAssembly().Location.ToLower(), path))
                            return;
                        if (path != null && iss.FindByPath(path) == null)
                        {
                            ProcessSettings ps = new ProcessSettings(process);
                            ps.Initialize();
                            iss.Set(ps);

                            var x = Settings.FirstOrDefault(sg => !(sg is IvyLockSettings) && sg.Name.CompareTo(ps.Name) > 0);
                            if (x != null)
                                Settings.Insert(Settings.IndexOf(x), ps);
                            else
                                Settings.Add(ps);
                        }
                    }
                }
                finally
                {
                }
            });
        }

        private async Task LoadSettings()
        {
            await Task.Run(() =>
            {
                SettingGroup = IvyLockSettings;
                SettingGroup.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == "Theme")
                        {
                            Uri uri;
                            switch (IvyLockSettings.Theme)
                            {
                                case Theme.Light:
                                    uri = new Uri("pack://application:,,,/IvyLock;component/Content/Theme.Light.xaml");
                                    break;

                                default:
                                case Theme.Dark:
                                    uri = new Uri("pack://application:,,,/IvyLock;component/Content/Theme.Dark.xaml");
                                    break;
                            }

                            Application.Current.Resources.MergedDictionaries.Clear();
                            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = uri });
                        }
                    };

                foreach (ProcessSettings ps in iss.OfType<ProcessSettings>())
                {
                    var x = Settings.FirstOrDefault(sg => !(sg is IvyLockSettings) && sg.Name.CompareTo(ps.Name) > 0);
                    if (x != null)
                        Settings.Insert(Settings.IndexOf(x), ps);
                    else
                        Settings.Add(ps);
                }
            });
        }

        public override async Task ValidatePassword()
        {
            if (CurrentScreen != Screen.SetupPassword)
                await base.ValidatePassword();
            else
            {
                if (Password == null || Password.Length == 0)
                    RaisePasswordRejected("Enter a password");
                else if (Password.Length < 4)
                    RaisePasswordRejected("Enter a longer password.");
                else
                {
                    IvyLockSettings.Hash = GetUserPasswordHash();
                    RaisePasswordVerified();
                }
            }
        }

        #endregion Methods
    }
}