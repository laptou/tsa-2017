using System;

namespace IvyLock.ViewModel
{
    public class FileAuthenticationViewModel : PasswordValidationViewModel
    {
        #region Fields

        private bool locked;
        private string path;

        #endregion Fields

        #region Properties

        public bool Locked { get => locked; set => Set(value, ref locked); }
        public string Path { get => path; set => Set(value, ref path); }
        public string FileName { get => System.IO.Path.GetFileName(Path); }

        #endregion Properties

        #region Methods 

        public override string GetPasswordHash()
        {
            throw new NotImplementedException();
        }

        public override string GetUserPasswordHash()
        {
            throw new NotImplementedException();
        }

        #endregion Methods
    }
}