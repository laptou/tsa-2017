using IvyLock.Model;
using IvyLock.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Media;

namespace IvyLock.UI.ViewModel
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
			if(e.Action == NotifyCollectionChangedAction.Add)
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
			set { _pass = value; ValidatePassword(); }
		}

		public ObservableCollection<Process> Processes { get; set; } = new ObservableCollection<Process>();

		public ImageSource ProcessIcon
		{
			get
			{
				return ProcessPath == null ? null : System.Drawing.Icon.ExtractAssociatedIcon(ProcessPath).ToImageSource();
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