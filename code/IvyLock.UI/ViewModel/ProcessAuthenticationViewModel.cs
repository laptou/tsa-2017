﻿using IvyLock.Model;
using IvyLock.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

using SC = System.StringComparison;

namespace IvyLock.ViewModel
{
    public sealed class ProcessAuthenticationViewModel : PasswordValidationViewModel, IDisposable
    {
        #region Fields

        private static Dictionary<string, DateTime> unlockTimes = new Dictionary<string, DateTime>();
        private SecureString password;
        private IEncryptionService ies = EncryptionService.Default;
        private IProcessService ips = ManagedProcessService.Default;
        private ISettingsService iss = XmlSettingsService.Default;
        private string path;
        private Dictionary<int, bool> suspended = new Dictionary<int, bool>();
        private Dictionary<IntPtr, ShowWindow> windowStates = new Dictionary<IntPtr, ShowWindow>();

        #endregion Fields

        #region Constructors

        public ProcessAuthenticationViewModel()
        {
            RunTime(() =>
            {
                ies = EncryptionService.Default;
                ips = ManagedProcessService.Default;
                iss = XmlSettingsService.Default;

                ips.ProcessChanged += ProcessChanged;
                Processes.CollectionChanged += ProcessCollectionChanged;

                PropertyChanged += async (s, e) =>
                {
                    if (e.PropertyName == "Password")
                        await ValidatePassword();
                };

                EventHandler verified = async (s, e) =>
                {
                    await Unlock();
                    UI(CloseView);
                };

                PasswordVerified += verified;
                BiometricVerified += verified;
            });
        }

        #endregion Constructors

        #region Properties

        public bool Locked { get; private set; }

        public SecureString Password
        {
            get { return password; }
            set { Set(value, ref password); }
        }

        public ObservableCollection<Process> Processes { get; set; } = new ObservableCollection<Process>();

        public NotifyTaskCompletion<ImageSource> ProcessIcon
        {
            get
            {
                return ProcessPath == null ? null :
                    new NotifyTaskCompletion<ImageSource>(
                        Task.Factory.StartNew(() =>
                        {
                            if (ProcessPath.StartsWith(
                                Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps"
                                    )))
                                return GetModernAppIcon(ProcessPath);

                            return System.Drawing.Icon.ExtractAssociatedIcon(ProcessPath).ToImageSource();
                        }));
            }
        }

        public NotifyTaskCompletion<string> ProcessName
        {
            get
            {
                return ProcessPath == null ?
                    null :
                    new NotifyTaskCompletion<string>(
                        Task.Factory.StartNew(
                            () => FileVersionInfo.GetVersionInfo(ProcessPath).FileDescription
                        ));
            }
        }

        public string ProcessPath
        {
            get
            {
                return path;
            }
            set
            {
                Set(value, ref path);
                RaisePropertyChanged("ProcessName");
                
                BiometricsEnabled = iss?.FindByPath(ProcessPath).UseBiometricAuthentication == true;
            }
        }

        #endregion Properties

        #region Methods

        public static ImageSource GetModernAppIcon(string path)
        {
            // get folder where actual app resides
            var dir = Path.GetDirectoryName(path);
            var manifestPath = Path.Combine(dir, "AppxManifest.xml");
            if (File.Exists(manifestPath))
            {
                // this is manifest file
                string pathToLogo;
                using (var fs = File.OpenRead(manifestPath))
                {
                    var manifest = XDocument.Load(fs);
                    const string ns = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
                    // rude parsing - take more care here
                    pathToLogo = manifest.Root.Element(XName.Get("Properties", ns)).Element(XName.Get("Logo", ns)).Value;
                }
                // now here it is tricky again - there are several
                // files that match logo, for example black, white,
                // contrast white. Here we choose first, but you might
                // do differently
                string finalLogo = null;
                // serach for all files that match file name in Logo
                // element but with any suffix (like "Logo.black.png,
                // Logo.white.png etc)
                foreach (var logoFile in Directory.GetFiles(Path.Combine(dir, Path.GetDirectoryName(pathToLogo)),
                    Path.GetFileNameWithoutExtension(pathToLogo) + "*" + Path.GetExtension(pathToLogo)))
                {
                    finalLogo = logoFile;
                    break;
                }

                if (File.Exists(finalLogo))
                {
                    using (var fs = File.OpenRead(finalLogo))
                    {
                        var img = new BitmapImage();
                        img.BeginInit();
                        img.StreamSource = fs;
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        return img;
                    }
                }
            }
            return null;
        }

        public void Dispose()
        {
            ips.ProcessChanged -= ProcessChanged;
        }

        public override string GetPasswordHash()
        {
            IvyLockSettings ivs = iss.OfType<IvyLockSettings>().FirstOrDefault();
            ProcessSettings ps = iss.FindByPath(ProcessPath);

            return ps?.Hash ?? ivs.Hash;
        }

        public override string GetUserPasswordHash()
        {
            return ies.Hash(Password);
        }

        public async Task Lock()
        {
            await Task.Run(() =>
            {
                lock (Processes)
                {
                    ProcessSettings ps = iss.FindByPath(ProcessPath);

                    if (unlockTimes.ContainsKey(ProcessPath) &&
                        (!ps.UseLockTimeOut ||
                            (DateTime.Now - unlockTimes[ProcessPath]).TotalMinutes < ps.LockTimeOut))
                        return;

                    if (Locked)
                        return;

                    Locked = true;
                    List<Process> list = Processes
                        .Where(p => !suspended.ContainsKey(p.Id) || !suspended[p.Id])
                        .Distinct(new ProcessExtensions.PidComparer()).ToList();
                    list.ForEach(p =>
                    {
                        p.EnumerateProcessWindowHandles().ForEach(hWnd =>
                        {
                            windowStates[hWnd] = NativeWindow.GetWindowState(hWnd);
                            NativeWindow.ShowWindow(hWnd, ShowWindow.Hide);
                        });

                        p.Suspend();
                        suspended[p.Id] = true;
                    });
                }
            });
        }

        public async Task Unlock()
        {
            await Task.Run(() =>
            {
                lock (Processes)
                {
                    if (!Locked) return;

                    Locked = false;

                    List<Process> list = Processes.Where(p => suspended[p.Id]).Distinct(new ProcessExtensions.PidComparer()).ToList();

                    list.ForEach(p =>
                        {
                            if (p.HasExited) return;

                            p.EnumerateProcessWindowHandles().ForEach(hWnd =>
                                NativeWindow.ShowWindowAsync(hWnd, windowStates.ContainsKey(hWnd) ? windowStates[hWnd] : ShowWindow.Show));
                            p.Resume();
                            suspended[p.Id] = false;
                        });

                    unlockTimes[ProcessPath] = DateTime.Now;
                }
            });
        }

        private void ProcessChanged(int pid, string path, ProcessOperation po)
        {
            try
            {
                if (po == ProcessOperation.Started)
                {
                    if (path?.Equals(ProcessPath, SC.InvariantCultureIgnoreCase) == true)
                    {
                        Process p = Process.GetProcessById(pid);
                        Processes.Add(p);

                        if (Locked)
                        {
                            p.EnumerateProcessWindowHandles().ForEach(hWnd =>
                            {
                                windowStates[hWnd] = NativeWindow.GetWindowState(hWnd);
                                NativeWindow.ShowWindow(hWnd, ShowWindow.Hide);
                            });

                            p.Suspend();
                            suspended[p.Id] = true;

                            UI(ShowView);
                        }
                    }
                }

                if (po == ProcessOperation.Deleted)
                {
                    if (path?.Equals(ProcessPath, SC.InvariantCultureIgnoreCase) == true)
                    {
                        Processes.Remove(Processes.FirstOrDefault(p => p.Id == pid));
                        suspended.Remove(pid);
                    }
                }
            }
            catch
            { }
        }

        private void ProcessCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            return;

            if (e.Action == NotifyCollectionChangedAction.Add)
                foreach (Process process in e.NewItems)
                {
                    suspended[process.Id] = Locked;

                    if (Locked)
                        process.Suspend();
                }

            if (e.Action == NotifyCollectionChangedAction.Remove)
                foreach (Process process in e.OldItems)
                    suspended.Remove(process.Id);
        }

        #endregion Methods
    }
}