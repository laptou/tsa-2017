using IvyLock.Model;
using IvyLock.Service;
using IvyLock.View;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace IvyLock.ViewModel
{
    public enum Screen
    {
        EnterPassword, SetupPassword, Main
    }

    public sealed class SettingsViewModel : PasswordValidationViewModel
    {
        #region Fields

        private Screen _currentScreen = Screen.SetupPassword;
        private PasswordVerificationStatus _pvStatus;
        private SettingGroup _settingGroup;
        private ObservableCollection<SettingGroup> _settings = new ObservableCollection<SettingGroup>();
        private CancellationTokenSource biometricCts;
        private IEncryptionService ies;
        private IProcessService ips;
        private ISettingsService iss;

        #endregion Fields

        #region Constructors

        public SettingsViewModel()
        {
            DesignTime(() => iss = DesignerSettingsService.Default);
            RunTime(async () =>
            {
                iss = XmlSettingsService.Default;
                ips = ManagedProcessService.Default;
                ies = EncryptionService.Default;

                PasswordVerified += (s, e) =>
                {
                    Task.Run(() =>
                    {
                        Password = null;
                        RaisePropertyChanged("Password");

                        if (CurrentScreen == Screen.SetupPassword)
                            IvyLockSettings.Hash = GetUserPasswordHash();

                        CurrentScreen = Screen.Main;
                    });
                };

                await LoadProcesses();
                await LoadSettings();
            });
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

        public IvyLockSettings IvyLockSettings { get { return Settings.OfType<IvyLockSettings>().FirstOrDefault(); } }

        public SecureString Password
        {
            get;
            set;
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

            Func<Process, bool> f = p =>
            {
                try
                {
                    string path = p.GetPath();
                    return
                        p.Id != myPid &&
                        path != null &&
                        FileVersionInfo.GetVersionInfo(path).FileDescription != null &&
                        p.MainWindowHandle != IntPtr.Zero;
                }
                catch (Win32Exception) { return false; }
                catch (FileNotFoundException) { return false; }
                catch (InvalidOperationException) { return false; }
            };

            iss = XmlSettingsService.Default;
            ips = ManagedProcessService.Default;
            ips.ProcessChanged += (pid, path, type) =>
            {
                if (type == ProcessOperation.Started)
                {
                    if (!iss.OfType<ProcessSettings>().Any(ps => ps.Path.Equals(path)))
                    {
                        try
                        {
                            Process p = Process.GetProcessById(pid);

                            if (f(p))
                            {
                                ProcessSettings ps = new ProcessSettings(p);
                                ps.Initialize();
                                iss.Set(ps);
                                Settings.Add(ps);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            };

            if (!iss.Any(s => s is IvyLockSettings))
                iss.Set(new IvyLockSettings());

            await Task.Run(() =>
            {
                foreach (
                    Process process in
                    from process in Process.GetProcesses()
                    where f(process)
                    orderby FileVersionInfo.GetVersionInfo(process.GetPath()).FileDescription
                    select process)
                {
                    string path = process.GetPath();

                    if (Assembly.GetEntryAssembly().Location.Equals(path))
                        continue;

                    try
                    {
                        if (path != null)
                        {
                            ProcessSettings ps = new ProcessSettings(process);

                            if (!iss.Any(s => s is ProcessSettings && ((ProcessSettings)s).Path == ps.Path))
                                iss.Set(ps);
                        }
                    }
                    catch
                    {
                    }
                }
            });
        }

        private async Task LoadSettings()
        {
            await Task.Run(() =>
            {
                foreach (SettingGroup sg in iss.OrderByDescending(sg => sg is IvyLockSettings).ThenBy(sg => sg.Name))
                    if (sg.Valid)
                        UI(() => Settings.Add(sg));

                SettingGroup = _settings.OfType<IvyLockSettings>().FirstOrDefault();
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

                if (string.IsNullOrWhiteSpace(IvyLockSettings.Hash))
                    CurrentScreen = Screen.SetupPassword;
                else CurrentScreen = Screen.EnterPassword;

                RaisePropertyChanged("IvyLockSettings");
            });
        }

        #endregion Methods
    }
}