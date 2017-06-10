using IvyLock.Native;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace IvyLock.ViewModel
{
    public enum PasswordVerificationStatus
    {
        None, Verified, Rejected, Delayed
    }

    public abstract class PasswordValidationViewModel : ViewModel
    {
        #region Fields

        private int attempts = 0;
        private int attemptTimeout = 5;
        private System.Timers.Timer attemptTimer;
        private CancellationTokenSource biometricCts = new CancellationTokenSource();
        private bool biometricEnabled;
        private Task biometricTask;
        private PasswordVerificationStatus pvStatus;
        private string pwError;

        #endregion Fields

        #region Constructors

        public PasswordValidationViewModel()
        {
            attemptTimer = new System.Timers.Timer();
            attemptTimer.Elapsed += (s, e) => attempts = Math.Max(0, --attempts);
            attemptTimer.Interval = attemptTimeout * 1000;
            attemptTimer.AutoReset = true;
        }

        #endregion Constructors

        #region Events

        public event EventHandler PasswordDelayed;

        public event EventHandler PasswordRejected;

        public event EventHandler PasswordVerified;

        public event EventHandler BiometricDelayed;

        public event EventHandler BiometricRejected;

        public event EventHandler BiometricVerified;

        #endregion Events

        #region Properties

        public int AttemptLimit { get; set; } = 6;

        public Duration AttemptWait { get { return TimeSpan.FromSeconds(attemptTimeout); } }

        public bool BiometricsEnabled
        {
            get { return biometricEnabled; }
            set
            {
                Set(value, ref biometricEnabled);
                if (value && (biometricTask == null || biometricTask.IsCompleted))
                    biometricTask = Task.Factory.StartNew(ValidateFingerprint, biometricCts.Token);
            }
        }

        public string PasswordErrorMessage
        {
            get { return pwError; }
            set { Set(value, ref pwError); }
        }

        public PasswordVerificationStatus PasswordVerificationStatus
        {
            get { return pvStatus; }
            set { Set(value, ref pvStatus); }
        }

        #endregion Properties

        #region Methods

        protected void RaisePasswordVerified(string message = null)
        {
            UI(() => PasswordVerified?.Invoke(this, null));
            PasswordVerificationStatus = PasswordVerificationStatus.Verified;
            PasswordErrorMessage = message;
        }

        protected void RaisePasswordDelayed(string message)
        {
            UI(() => PasswordDelayed?.Invoke(this, null));
            PasswordVerificationStatus = PasswordVerificationStatus.Delayed;
            PasswordErrorMessage = message;
        }

        protected void RaisePasswordRejected(string message)
        {
            UI(() => PasswordRejected?.Invoke(this, null));
            PasswordVerificationStatus = PasswordVerificationStatus.Rejected;
            PasswordErrorMessage = message;
        }

        public abstract string GetPasswordHash();

        public abstract string GetUserPasswordHash();

        public virtual async Task<bool> ValidatePassword()
        {
            string pw = GetPasswordHash();
            string attempt = GetUserPasswordHash();

            if (string.IsNullOrEmpty(pw)) return false;

            if (attempts < AttemptLimit && string.Equals(pw, attempt))
            {
                attempts = 0;
                RaisePasswordVerified();
                return true;
            }
            else
            {
                if (attempts >= AttemptLimit)
                {
                    if (PasswordVerificationStatus != PasswordVerificationStatus.Delayed)
                    {
                        attemptTimer.Stop();
                        attemptTimeout *= 2;
                        attemptTimer.Interval = attemptTimeout * 1000;
                        attemptTimer.Start();
                        RaisePropertyChanged("AttemptWait");
                        RaisePasswordRejected(null);
                        return false;
                    }

                    RaisePasswordDelayed("Stop guessing.");
                    return false;
                }
                else
                {
                    attempts++;

                    RaisePasswordRejected("The password is incorrect.");
                    return false;
                }
            }
        }

        private async Task ValidateFingerprint()
        {
            BiometricsEnabled =
                BiometricsEnabled &&
                WBF.GetBiometricUnits(BiometricType.Fingerprint).Length > 0;

            if (!BiometricsEnabled) return;

            await Task.Run(() =>
            {
                var session =
                    WBF.OpenSession(BiometricType.Fingerprint,BiometricSessionFlags.Default,
                        null, BiometricDatabaseType.None);

                var match = false;

                try
                {
                    while (!WBF.Verify(session, WBF.GetCurrentIdentity(), BiometricSubtype.Any,
                        out uint unitId, out BiometricRejectDetail rejectDetail, out BiometricError error))
                    {
                        PasswordVerificationStatus = PasswordVerificationStatus.Rejected;
                        UI(() => BiometricRejected?.Invoke(this, null));

                        switch (error)
                        {
                            case BiometricError.None:
                                PasswordErrorMessage = "There was no error?";
                                break;

                            case BiometricError.BadCapture:
                                switch (rejectDetail)
                                {
                                    case BiometricRejectDetail.TooHigh:
                                        PasswordErrorMessage = "Your finger was too high.";
                                        break;

                                    case BiometricRejectDetail.TooLow:
                                        PasswordErrorMessage = "Your finger was too low.";
                                        break;

                                    case BiometricRejectDetail.TooLeft:
                                        PasswordErrorMessage = "Your finger was too far to the left.";
                                        break;

                                    case BiometricRejectDetail.TooRight:
                                        PasswordErrorMessage = "Your finger was too far to the right.";
                                        break;

                                    case BiometricRejectDetail.TooFast:
                                        PasswordErrorMessage = "You moved your finger too quickly.";
                                        break;

                                    case BiometricRejectDetail.TooSlow:
                                        PasswordErrorMessage = "You moved your finger too slowly.";
                                        break;

                                    case BiometricRejectDetail.PoorQuality:
                                        PasswordErrorMessage = "Your fingerprint could not be read properly.";
                                        break;

                                    case BiometricRejectDetail.TooSkewed:
                                        PasswordErrorMessage = "The fingerprint capture was too skewed.";
                                        break;

                                    case BiometricRejectDetail.TooShort:
                                        PasswordErrorMessage = "Your finger was too short.";
                                        break;

                                    case BiometricRejectDetail.MergeFailure:
                                        PasswordErrorMessage = "Try again.";
                                        break;

                                    default:
                                        break;
                                }
                                break;

                            case BiometricError.EnrollmentInProgress:
                                PasswordErrorMessage = "The sensor is currently enrolling a new fingerprint.";
                                break;

                            case BiometricError.NoMatch:
                                PasswordErrorMessage = "The fingerprint did not match.";
                                break;

                            default:
                                break;
                        }
                    }
                    match = true;
                }
                catch
                {
                }

                if (match)
                {
                    PasswordVerificationStatus = PasswordVerificationStatus.Verified;
                    UI(() => BiometricVerified?.Invoke(this, null));
                }

                WBF.CloseSession(session);
            });
        }

        #endregion Methods
    }
}