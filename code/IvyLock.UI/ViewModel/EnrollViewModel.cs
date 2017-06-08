using IvyLock.Native;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IvyLock.ViewModel
{
    public class EnrollViewModel : ViewModel
    {
        #region Fields

        private BiometricSubtype bioSubtype = BiometricSubtype.Any;

        private string message;

        #endregion Fields

        #region Constructors

        public EnrollViewModel()
        {
            PropertyChanged += async (s, e) =>
            {
                if(e.PropertyName == "Finger")
                    await Task.Run(() =>
                    {
                        try
                        {
                            WBF.Enroll(Finger, ep =>
                            {
                                switch (ep)
                                {
                                    case EnrollPrompt.LocateSensor:
                                        Message = "Please touch or swipe the sensor you want to use.";
                                        break;

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
            get =>
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
        }

        public string Message
        {
            get => message;
            set => Set(value, ref message);
        }

        #endregion Properties
    }
}