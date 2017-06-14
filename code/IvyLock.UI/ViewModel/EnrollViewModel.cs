using IvyLock.Native;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.Linq;
using System.Threading.Tasks;

namespace IvyLock.ViewModel
{
    public class EnrollViewModel : ViewModel
    {
        #region Fields

        private BiometricSubtype bioSubtype = BiometricSubtype.Unknown;

        private string message;

        #endregion Fields

        #region Constructors

        public EnrollViewModel()
        {
            BiometricSubtypeNames =
                new Dictionary<BiometricSubtype, string>
                {
                    { BiometricSubtype.LeftIndexFinger, "Left Index Finger" },
                    { BiometricSubtype.LeftMiddleFinger, "Left Middle Finger" },
                    { BiometricSubtype.LeftRingFinger, "Left Ring Finger" },
                    { BiometricSubtype.LeftLittleFinger, "Left Little Finger" },
                    { BiometricSubtype.LeftThumb, "Left Thumb" },
                    { BiometricSubtype.RightIndexFinger, "Right Index Finger" },
                    { BiometricSubtype.RightMiddleFinger, "Right Middle Finger" },
                    { BiometricSubtype.RightRingFinger, "Right Ring Finger" },
                    { BiometricSubtype.RightLittleFinger, "Right Little Finger" },
                    { BiometricSubtype.RightThumb, "Right Thumb" }
                };

            PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == "Finger" && Finger != BiometricSubtype.Any)
                    await Task.Run(() =>
                    {
                        try
                        {
                            WBF.Enroll(WBF.GetCurrentIdentity(), Sensor, Finger, ep =>
                            {
                                switch (ep)
                                {
                                    case EnrollPrompt.UseSensor:
                                        Message = "Please touch or swipe the sensor again.";
                                        break;

                                    case EnrollPrompt.NeedMoreData:
                                        Message = "Please touch or swipe the sensor again, I need more data.";
                                        UI(() => FingerprintAccepted?.Invoke(this, null));
                                        break;

                                    case EnrollPrompt.BadCapture:
                                        Message = "Please touch or swipe the sensor again, I didn't catch that.";
                                        UI(() => FingerprintRejected?.Invoke(this, null));
                                        break;

                                    case EnrollPrompt.Success:
                                        Message = "Done!";
                                        UI(() => FingerprintCompleted?.Invoke(this, null));
                                        break;
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Message = ex.Message;
                        }
                    });
            };

            Task.Run(() =>
            {
                Message = "Swipe or touch the sensor that you wish to use.";
                Sensor = WBF.LocateSensor();
                Finger = BiometricSubtype.Any;

                var fingers = WBF.GetEnrollments(WBF.GetCurrentIdentity(), Sensor);
                BiometricSubtypeNames =
                   (from kv in BiometricSubtypeNames
                    where !fingers.Contains(kv.Key)
                    select kv).ToDictionary(kv => kv.Key, kv => kv.Value);
                RaisePropertyChanged(nameof(BiometricSubtypeNames));
            });
        }

        #endregion Constructors

        #region Events

        public event EventHandler FingerprintAccepted;

        public event EventHandler FingerprintCompleted;

        public event EventHandler FingerprintRejected;

        #endregion Events

        #region Properties

        public BiometricSubtype Finger
        {
            get => bioSubtype;
            set => Set(value, ref bioSubtype);
        }

        public Dictionary<BiometricSubtype, string> BiometricSubtypeNames
        {
            get;
            private set;
        }

        public string Message
        {
            get => message;
            set => Set(value, ref message);
        }

        public uint Sensor { get; set; }

        #endregion Properties
    }
}