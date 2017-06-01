using IvyLock.Model;
using IvyLock.Service;
using IvyLock.View;
using IvyLock.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace IvyLock
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        #region Fields

        private static Mutex mutex = new Mutex(true, "{37EFBF56-B711-42E3-B3D0-0DCDA7BC09CB}");
        private IProcessService ips;
        private ISettingsService iss;
        private NotifyIcon ni;
        private Dictionary<string, AuthenticationView> views = new Dictionary<string, AuthenticationView>();

        public static bool IsDesigner { get { return LicenseManager.UsageMode == LicenseUsageMode.Designtime; } }

        #endregion Fields

        #region Methods

        protected override void OnExit(ExitEventArgs e)
        {
            ips?.Dispose();
            iss?.Dispose();
            ni?.Dispose();

            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!IsDesigner)
            {
                try
                {
                    if (mutex.WaitOne(TimeSpan.Zero, true))
                        mutex.ReleaseMutex();
                    else
                    {
                        Shutdown();
                        return;
                    }
                }
                catch (AbandonedMutexException)
                {
                    mutex.ReleaseMutex();
                }

                AppDomain.CurrentDomain.ProcessExit += (s, e2) =>
                {
                    mutex.Dispose();
                };

                // don't initialise statically! XmlSettingsService
                // depends on Application.Current
                ips = ManagedProcessService.Default;
                iss = XmlSettingsService.Default;

                IvyLockSettings ils = iss.OfType<IvyLockSettings>().First();
                switch (ils.Theme)
                {
                    case Theme.Light:
                        Resources.MergedDictionaries.Clear();
                        Resources.MergedDictionaries.Add(new ResourceDictionary()
                        {
                            Source = new Uri("pack://application:,,,/IvyLock;component/Content/Theme.Light.xaml")
                        });
                        break;
                }

                ips.ProcessChanged += ProcessChanged;

                ni = new NotifyIcon();
                ni.Click += (s, e1) => MainWindow.Show();
                ni.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location);
                ni.ContextMenu = new ContextMenu();
                ni.ContextMenu.MenuItems.Add("Exit", (s, e2) =>
                {
                    SettingsViewModel svm = MainWindow.DataContext as SettingsViewModel;

                    svm.PropertyChanged += (s2, e3) =>
                    {
                        if (e3.PropertyName == "CurrentScreen" && svm.CurrentScreen == ViewModel.Screen.Main)
                            Shutdown();
                    };
                    
                    svm.CurrentScreen = ViewModel.Screen.EnterPassword;

                    MainWindow.Show();
                    MainWindow.Activate();
                });
                ni.Visible = true;
            }
        }

        private void ProcessChanged(int pid, string path, ProcessOperation po)
        {
            if (po == ProcessOperation.Modified)
                return;

            ProcessSettings ps = iss.FindByPath(path);
           
            if (ps == null || !ps.UsePassword)
                return;

            Task.Run(() =>
            {
                switch (po)
                {
                    case ProcessOperation.Started:
                        if (path == null) return;

                        Dispatcher?.Invoke(async () =>
                        {
                            AuthenticationView av = views.ContainsKey(path) ? views[path] : new AuthenticationView();
                            AuthenticationViewModel avm = av.DataContext as AuthenticationViewModel;

                            avm.ProcessPath = path;
                            avm.Processes.Add(Process.GetProcessById(pid));

                            await avm.Lock();

                            if (!avm.Locked)
                            {
                                avm.Dispose();
                                return;
                            }

                            if (!views.ContainsKey(path))
                                av.Closed += (s, e) => views.Remove(path);

                            views[path] = av;
                            views[path].Show();
                            views[path].Activate();
                        });
                        break;

                    case ProcessOperation.Modified:
                        break;

                    case ProcessOperation.Deleted:
                        if (path == null || !views.ContainsKey(path)) return;

                        if (!Process.GetProcesses().Any(process =>
                         {
                             try
                             {
                                 return process.MainModule.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase);
                             }
                             catch (Win32Exception) { return false; }
                         }))
                        {
                            views[path].Close();
                            views.Remove(path);
                        }
                        break;

                    default:
                        break;
                }
            });
        }

        #endregion Methods
    }
}