using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace IvyLock.Service
{
    public interface IBiometricService
    {
    }

    public class BiometricService
    {
        [DllImport("winbio.dll")]
        public static extern int WinBioOpenSession([In] BiometricType type, [In] BiometricPoolType poolType,
            [In] BiometricSessionFlags flags, [In] IntPtr units, [In] UIntPtr unitCount, [In] IntPtr databaseId, 
            [Out] out int sessionHandle);

        [DllImport("winbio.dll")]
        public static extern int WinBioCloseSession(int sessionHandle);

        [DllImport("winbio.dll")]
        public static extern int WinBioVerify(int sessionHandle, IntPtr identity, BiometricSubtype subfactor,
            [Out, Optional] out int unitId, [Out, Optional] out bool match, [Out, Optional] out BiometricRejectDetail rejectDetail);

        [DllImport("winbio.dll")]
        public static extern int WinBioEnumBiometricUnits(BiometricType factor, [Out] out IntPtr units, [Out] out int count);

        [DllImport("winbio.dll")]
        public static extern int WinBioFree([In] IntPtr address);

        // 0x20008
        [DllImport("advapi32.dll")]
        private static extern bool OpenProcessToken(IntPtr processHandle, int accessToken, IntPtr tokenHandle);

        [DllImport("advapi32.dll")]
        private static extern bool GetTokenInformation(IntPtr handle, int tokenInfoClass, IntPtr tokenInfo, 
            int tokenInfoLength, [Out] out int returnLength);

        [DllImport("advapi32.dll")]
        private static extern bool CopySid(int destSidLength, IntPtr dest, IntPtr src);

        [DllImport("advapi32.dll")]
        private static extern uint GetLengthSid(IntPtr sid);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool ConvertSidToStringSid(IntPtr pSID, out IntPtr ptrSid);

        [StructLayout(LayoutKind.Sequential)]
        private struct SidAndAttributes
        {
            public IntPtr Sid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TokenUser
        {
            public SidAndAttributes User;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TokenInfoBuffer
        {
            public TokenUser User;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 68)]
            public byte[] Buffer;
        }

        public static BiometricIdentity GetCurrentUserIdentity()
        {
            BiometricIdentity ident = new BiometricIdentity()
            {
                Type = BiometricIdentityType.SID,
                Value2 = new BiometricIdentity.ValueStruct2()
                {
                    AccountSid = new BiometricIdentity.AccountSid()
                    {
                        Data = WindowsIdentity.GetCurrent().User.AccountDomainSid.Value,
                        Size = (uint)WindowsIdentity.GetCurrent().User.AccountDomainSid.Value.Length
                    }
                }
            };

            return ident;

            IntPtr tokenHandle = IntPtr.Zero;

            IntPtr bufPtr = Marshal.AllocHGlobal(Marshal.SizeOf<TokenInfoBuffer>());

            OpenProcessToken(GetCurrentProcess(), 0x00020008, tokenHandle);
            GetTokenInformation(WindowsIdentity.GetCurrent().Token, 1, bufPtr, Marshal.SizeOf<TokenInfoBuffer>(), out int bytesReturned);
            TokenInfoBuffer buf = Marshal.PtrToStructure<TokenInfoBuffer>(bufPtr);
            ConvertSidToStringSid(buf.User.User.Sid, out IntPtr stringSid);
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct NativeGuid
    {
        public Int32 Data1;
        public Int16 Data2;
        public Int16 Data3;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Data4;
    }

    public enum BiometricType : uint
    {
        Fingerprint = 8
    }

    public enum BiometricRejectDetail : uint
    {
        TooHigh = 1,
        TooLow = 2,
        TooLeft = 3,
        TooRight = 4,
        TooFast = 5,
        TooSlow = 6,
        PoorQuality = 7,
        TooSkewed = 8,
        TooShort = 9,
        MergeFailure = 10
    }

    public enum BiometricSensorSubtype : uint
    {
        Unknown = 0,
        Swipe = 1,
        Touch = 2
    }

    public enum BiometricSubtype : uint
    {
        Unknown = 0,
        RightThumb = 1,
        RightIndexFinger = 2,
        RightMiddleFinger = 3,
        RightRingFinger = 4,
        RightLittleFinger = 5,
        LeftThumb = 6,
        LeftIndexFinger = 7,
        LeftMiddleFinger = 8,
        LeftRingFinger = 9,
        LeftLittleFinger = 10,
        RightFourFingers = 13,
        LeftFourFingers = 14,
        TwoThumbs = 15,
        Any = 0xFF
    }

    public enum BiometricPoolType : uint
    {
        System = 1,
        Private = 2
    }

    [Flags]
    public enum BiometricSessionFlags : uint
    {
        Default = 0x00000000,
        Raw = 0x00000001,
        Maintenance = 0x00000002,
        Basic = 0x00010000,
        Advanced = 0x00020000
    }

    public enum BiometricIdentityType : uint
    {
        Null = 0, Wildcard, Guid, SID
    }

    [Flags]
    public enum BiometricCapabilities : uint
    {
        Sensor = 0x00000001,
        Matching = 0x00000002,
        Database = 0x00000004,
        Processing = 0x00000008,
        Encryption = 0x00000010,
        Navigation = 0x00000020,
        Indicator = 0x00000040,
        VirtualSensor = 0x00000080
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct BiometricUnitSchema
    {
        public int UnitId;
        public BiometricPoolType PoolType;
        public BiometricType BiometricFactor;
        public BiometricSubtype SensorSubType;
        public BiometricCapabilities Capabilities;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string DeviceInstanceId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Description;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Manufacturer;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Model;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string SerialNumber;

        public BiometricVersion FirmwareVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BiometricVersion
    {
        public int MajorVersion;
        public int MinorVersion;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct BiometricIdentity
    {
        [FieldOffset(0)]
        public BiometricIdentityType Type;
        [FieldOffset(8)]
        public ValueStruct Value;
        [FieldOffset(8)]
        public ValueStruct2 Value2;

        [StructLayout(LayoutKind.Explicit)]
        public struct ValueStruct
        {
            [FieldOffset(0)]
            public int Null;

            [FieldOffset(0)]
            public int Wildcard;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ValueStruct2
        {
            [FieldOffset(0)]
            public NativeGuid TemplateGuid;

            [FieldOffset(0)]
            public AccountSid AccountSid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AccountSid
        {
            public uint Size;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 68)]
            public string Data;
        }
    }
}