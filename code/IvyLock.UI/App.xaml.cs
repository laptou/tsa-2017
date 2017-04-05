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

        private static Mutex mutex = new Mutex(true, "{37EFBF56-B711-42E3-B3D0-0DCDA7BC09BD}");
        private IProcessService ips;
        private ISettingsService iss;
        private NotifyIcon ni;
        private Dictionary<string, AuthenticationViewModel> views = new Dictionary<string, AuthenticationViewModel>();

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
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
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

                base.OnStartup(e);

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
                        if (e3.PropertyName == "Locked" && !svm.Locked)
                            Shutdown();
                    };

                    svm.Locked = true;
                    svm.CurrentScreen = SettingsViewModel.Screen.EnterPassword;

                    MainWindow.Show();
                    MainWindow.Activate();
                });
                ni.Visible = true;
            }
            else
            {
                base.OnStartup(e);
            }
        }

        private void ProcessChanged(int pid, string path, ProcessOperation po)
        {
            ProcessSettings ps = iss.OfType<ProcessSettings>().FirstOrDefault(s => s.Path?.Equals(path) == true);
            if (ps == null)
                return;

            if (!ps.UsePassword)
                return;

            Task.Run(() =>
            {
                switch (po)
                {
                    case ProcessOperation.Started:
                        if (path == null) return;

                        Dispatcher?.Invoke(async () =>
                        {
                            if (views.ContainsKey(path))
                            {
                                views[path].ShowView();
                                return;
                            }

                            AuthenticationViewModel avm = views.ContainsKey(path) ? views[path] :
                                (AuthenticationViewModel)new AuthenticationView().DataContext;
                            avm.ProcessPath = path;
                            avm.Processes.Add(Process.GetProcessById(pid));

                            await avm.Lock();

                            if (!avm.Locked)
                            {
                                avm.Dispose();
                                return;
                            }

                            if (!views.ContainsKey(path))
                                avm.CloseRequested += () => views.Remove(path);

                            views[path] = avm;
                            avm.ShowView();
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
                                 return process.MainModule.FileName.Equals(path);
                             }
                             catch (Win32Exception) { return false; }
                         }))
                        {
                            views[path].CloseView();
                            views[path].Dispose();

                            if (views.ContainsKey(path))
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