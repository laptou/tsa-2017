using IvyLock.Model;
using IvyLock.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;

using System.Linq;

using System.Security;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace IvyLock.ViewModel
{
    public sealed class AuthenticationViewModel : ViewModel, IDisposable
    {
        #region Fields

        private SecureString _pass;
        private IEncryptionService ies = EncryptionService.Default;
        private IProcessService ips = ManagedProcessService.Default;
        private ISettingsService iss = XmlSettingsService.Default;
        private string path;
        private Dictionary<int, bool> suspended = new Dictionary<int, bool>();
        private Dictionary<IntPtr, ShowWindow> windowStates = new Dictionary<IntPtr, ShowWindow>();
        private static Dictionary<string, DateTime> unlockTimes = new Dictionary<string, DateTime>();

        #endregion Fields

        #region Constructors

        public AuthenticationViewModel()
        {
            ips.ProcessChanged += ProcessChanged;
            Processes.CollectionChanged += ProcessCollectionChanged;
        }

        private void ProcessCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
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

        #endregion Constructors

        #region Properties

        public bool Locked { get; private set; }

        public SecureString Password
        {
            get { return _pass; }
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            set { _pass = value; ValidatePassword(); }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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
                RaisePropertyChanged("ProcessIcon");
                RaisePropertyChanged("ProcessName");
            }
        }

        #endregion Properties

        #region Methods

        public async Task Lock()
        {
            await Task.Run(() =>
            {
                lock (Processes)
                {
                    ProcessSettings ps = iss.OfType<ProcessSettings>().FirstOrDefault(s => s.Path.Equals(ProcessPath));

                    if (unlockTimes.ContainsKey(ProcessPath) &&
                        (!ps.UseLockTimeOut ||
                            (DateTime.Now - unlockTimes[ProcessPath]).TotalMinutes < ps.LockTimeOut))
                        return;

                    if (Locked)
                        return;

                    Locked = true;
                    List<Process> list = Processes.Where(p => !suspended[p.Id]).Distinct(new ProcessExtensions.PidComparer()).ToList();
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

        public async Task ValidatePassword()
        {
            if (Password == null) return;

            await Task.Run(async () =>
            {
                string hash = ies.Hash(Password);
                IvyLockSettings ivs = iss.OfType<IvyLockSettings>().FirstOrDefault();
                ProcessSettings ps = iss.OfType<ProcessSettings>().FirstOrDefault(s => s.Path.Equals(ProcessPath));
                if (ps.UsePassword)
                {
                    if (string.IsNullOrWhiteSpace(ps.Hash) ? ivs.Hash.Equals(hash) : ps.Hash.Equals(hash))
                    {
                        await Unlock();
                        UI(CloseView);
                    }
                }
            });
        }

        private void ProcessChanged(int pid, string path, ProcessOperation po)
        {
            try
            {
                if (po == ProcessOperation.Started)
                {
                    if (path?.Equals(ProcessPath) == true)
                    {
                        Process p = Process.GetProcessById(pid);
                        Processes.Add(p);
                    }
                }

                if (po == ProcessOperation.Deleted)
                {
                    if (path?.Equals(ProcessPath) == true)
                    {
                        Processes.Remove(Processes.FirstOrDefault(p => p.Id == pid));
                    }
                }
            }
            catch
            { }
        }

        public void Dispose()
        {
            ips.ProcessChanged -= ProcessChanged;
        }

        #endregion Methods
    }
}