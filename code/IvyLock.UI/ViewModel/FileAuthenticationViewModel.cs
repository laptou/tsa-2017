using IvyLock.Model;
using IvyLock.Model.Security;
using IvyLock.Service;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

namespace IvyLock.ViewModel
{
    public class FileAuthenticationViewModel : PasswordValidationViewModel
    {
        #region Fields

        private bool encrypt;
        private EncryptionService es = new EncryptionService();
        private bool locked;
        private SecureString password;
        private string path;

        private NotifyTaskCompletion transformTask;

        #endregion Fields

        #region Constructors

        public FileAuthenticationViewModel()
        {
            PropertyChanged += async (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case "Password":
                        if (Hash != null) await VerifyPassword();
                        TransformTask = new NotifyTaskCompletion(TransformFile);
                        break;
                }
            };
        }

        #endregion Constructors

        #region Properties

        public bool Encrypt { get => encrypt; set => Set(value, ref encrypt); }
        public string FileName { get => IOPath.GetFileName(Path); }
        public string Hash { get; private set; }
        public bool Locked { get => locked; set => Set(value, ref locked); }
        public SecureString Password { get => password; set => Set(value, ref password); }
        public string Path { get => path; set { Set(value, ref path); RaisePropertyChanged("FileName"); } }
        public NotifyTaskCompletion TransformTask { get => transformTask; set => Set(value, ref transformTask); }

        #endregion Properties

        #region Methods

        public override string GetPasswordHash()
        {
            return Hash;
        }

        public override string GetUserPasswordHash()
        {
            return es.Hash(Password);
        }

        public async Task TransformFile(IProgress<double> progress)
        {
            var fs = File.OpenRead(Path);

            Aes aes = Aes.Create();
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CFB;

            var pass = Password.GetBytes();
            var rfc = new Rfc2898DeriveBytes(pass, Encoding.Unicode.GetBytes("victoria"), 1000);
            Array.Clear(pass, 0, pass.Length);
            aes.Key = rfc.GetBytes(aes.KeySize / 8);
            aes.IV = rfc.GetBytes(aes.BlockSize / 8);

            if (Encrypt)
            {
                string newPath = IOPath.ChangeExtension(Path, IOPath.GetExtension(Path) + ".ivy");

                FileStream newFs = File.Create(newPath);

                ICryptoTransform ict = aes.CreateEncryptor();

                using (CryptoStream cs = new CryptoStream(newFs, ict, CryptoStreamMode.Write))
                {
                    await newFs.WriteAsync(es.HashBytes(Password), 0, 64);

                    byte[] block = new byte[81920];

                    int x = 0;
                    do
                    {
                        await cs.WriteAsync(block, 0, x = await fs.ReadAsync(block, 0, 81920));
                        progress.Report((double)fs.Position / fs.Length * 100);
                    }
                    while (fs.Position < fs.Length);

                    cs.FlushFinalBlock();
                }

                fs.Dispose();

                if (XmlSettingsService.Default != null &&
                    XmlSettingsService.Default?.OfType<IvyLockSettings>().First().DeleteFileOnEncrypt == true)
                    File.Delete(path);
            }
            else
            {
                byte[] hash = new byte[64];
                await fs.ReadAsync(hash, 0, 64);

                Hash = Convert.ToBase64String(hash);

                if (!await VerifyPassword())
                {
                    TransformTask = null;
                    return;
                }

                string newPath = IOPath.ChangeExtension(Path, IOPath.GetExtension(Path).Replace(".ivy", ""));

                using (FileStream newFs = File.Create(newPath))
                {
                    ICryptoTransform ict = aes.CreateDecryptor();

                    using (CryptoStream cs = new CryptoStream(newFs, ict, CryptoStreamMode.Write))
                    {
                        byte[] block = new byte[81920];

                        do
                        {
                            await cs.WriteAsync(block, 0, await fs.ReadAsync(block, 0, 81920));
                            progress.Report((double)fs.Position / fs.Length * 100);
                        }
                        while (fs.Position < fs.Length);

                        cs.FlushFinalBlock();
                    }
                }

                fs.Dispose();

                var settings = XmlSettingsService.Default != null ? XmlSettingsService.Default.OfType<IvyLockSettings>().First() : null;

                if (settings != null)
                {
                    if (settings.DeleteFileOnDecrypt)
                        File.Delete(path);

                    if (settings.OpenFileOnDecrypt)
                        Process.Start(newPath);
                }
            }
        }

        #endregion Methods
    }
}