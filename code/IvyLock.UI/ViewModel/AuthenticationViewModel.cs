using IvyLock.Model;
using IvyLock.Service;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Media;

namespace IvyLock.UI.ViewModel
{
	public class AuthenticationViewModel : ViewModel
	{
		#region Fields

		private SecureString _pass;
		private IEncryptionService ies = EncryptionService.Default;
		private ISettingsService iss = XmlSettingsService.Default;

		#endregion Fields

		#region Constructors

		public AuthenticationViewModel()
		{
		}

		#endregion Constructors

		#region Properties

		public SecureString Password
		{
			get { return _pass; }
			set { _pass = value; ValidatePassword(); }
		}

		private Process process;

		public Process Process
		{
			get { return process; }
			set { Set(value, ref process); RaisePropertyChanged("ProcessIcon"); RaisePropertyChanged("ProcessName"); }
		}

		public ImageSource ProcessIcon
		{
			get
			{
				string path = Process.GetPath();
				return path == null ? null : System.Drawing.Icon.ExtractAssociatedIcon(path).ToImageSource();
			}
		}

		public NotifyTaskCompletion<string> ProcessName
		{
			get
			{
				return Process == null ?
					null :
					new NotifyTaskCompletion<string>(
						Task.Factory.StartNew(
							() => FileVersionInfo.GetVersionInfo(Process.GetPath()).FileDescription
						));
			}
		}

		#endregion Properties

		#region Methods

		public async Task Lock()
		{
			await Task.Run(() => Process.Suspend());
		}

		public async Task Unlock()
		{
			await Task.Run(() => Process.Resume());
		}

		public async Task ValidatePassword()
		{
			if (Password == null) return;

			await Task.Run(async () =>
			{
				string hash = ies.Hash(Password);
				string path = Process.GetPath();
				IvyLockSettings ivs = iss.OfType<IvyLockSettings>().FirstOrDefault();
				ProcessSettings ps = iss.OfType<ProcessSettings>().FirstOrDefault(s => s.Path.Equals(path));
				if (ps.UsePassword && (ps.Hash?.Equals(hash) == true || ivs.Hash.Equals(hash)))
				{
					await Unlock();
					UI(CloseView);
				}
			});
		}

		#endregion Methods
	}
}