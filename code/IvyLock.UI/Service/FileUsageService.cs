using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static IvyLock.Service.Native;

namespace IvyLock.Service
{
    public static class FileUsageService
    {
        #region Methods

        public static async Task<IEnumerable<string>> GetOpenFiles(int pid)
        {
            List<string> files = new List<string>();
            Parallel.ForEach(
                await GetHandles(pid),
                myHandle =>
                {
                    NTStatus status;

                    IntPtr myProcessHandle = OpenProcess(ProcessAccessFlags.DuplicateHandle, false, myHandle.GetPid());
                    IntPtr currentProcessHandle = GetCurrentProcess();
                    IntPtr targetHandle = Marshal.AllocHGlobal(Marshal.SizeOf<uint>());
                    status = NtDuplicateObject(myProcessHandle, myHandle.HandleValue, currentProcessHandle, targetHandle, 0, 0, 0);
                    CloseHandle(myProcessHandle);
                    CloseHandle(currentProcessHandle);

                    if (status != NTStatus.Success)
                    {
                        Marshal.FreeHGlobal(targetHandle);
                        return;
                    }

                    IntPtr handle = Marshal.PtrToStructure<IntPtr>(targetHandle);
                    Marshal.FreeHGlobal(targetHandle);

                    IntPtr objectTypeInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ObjectTypeInformation>());
                    uint returnLength = 0;
                    status = NtQueryObject(
                        handle,
                        ObjectInformationClass.ObjectTypeInformation,
                        objectTypeInfo,
                        (uint)Marshal.SizeOf<ObjectTypeInformation>(),
                        ref returnLength);

                    if (status != NTStatus.Success)
                    {
                        objectTypeInfo = Marshal.ReAllocHGlobal(objectTypeInfo, (IntPtr)returnLength);
                        status = NtQueryObject(
                            handle,
                            ObjectInformationClass.ObjectTypeInformation,
                            objectTypeInfo,
                            returnLength,
                            ref returnLength);

                        if (status != NTStatus.Success)
                        {
                            Marshal.FreeHGlobal(objectTypeInfo);
                            CloseHandle(handle);
                            return;
                        }
                    }

                    ObjectTypeInformation oti = Marshal.PtrToStructure<ObjectTypeInformation>(objectTypeInfo);
                    string typeName = oti.Name.ToString();
                    Marshal.FreeHGlobal(objectTypeInfo);

                    IntPtr objectNameInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ObjectNameInformation>());
                    returnLength = (uint)Marshal.SizeOf<ObjectNameInformation>();
                    status = NtQueryObject(
                        handle,
                        ObjectInformationClass.ObjectNameInformation,
                        objectNameInfo,
                        (uint)Marshal.SizeOf<ObjectNameInformation>(),
                        ref returnLength);

                    if (status != NTStatus.Success)
                    {
                        objectNameInfo = Marshal.ReAllocHGlobal(objectNameInfo, (IntPtr)returnLength);
                        status = NtQueryObject(
                            handle,
                            ObjectInformationClass.ObjectNameInformation,
                            objectNameInfo,
                            returnLength,
                            ref returnLength);
                        if (status != NTStatus.Success)
                        {
                            Marshal.FreeHGlobal(objectNameInfo);
                            CloseHandle(handle);
                            return;
                        }
                    }

                    ObjectNameInformation oni = Marshal.PtrToStructure<ObjectNameInformation>(objectNameInfo);
                    string objName = oni.Name.ToString();
                    Marshal.FreeHGlobal(objectNameInfo);

                    if (typeName.Equals("File"))
                    {
                        uint fnLength = 0;
                        StringBuilder sb = new StringBuilder(0);
                        fnLength = GetFinalPathNameByHandle(handle, sb, fnLength, 0);
                        sb.Capacity = (int)fnLength;

                        if (GetFinalPathNameByHandle(handle, sb, fnLength, 0) > 1)
                            files.Add(sb.ToString());
                    }

                    CloseHandle(handle);
                });
            return files;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint GetFinalPathNameByHandle(
            IntPtr hFile,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath,
            uint cchFilePath,
            uint dwFlags);

        private static async Task<IEnumerable<SystemHandleExtended>> GetHandles(int pid)
        {
            NTStatus status;
            uint handleInfoSize = 0x10000;
            IntPtr handleInfo = Marshal.AllocHGlobal((IntPtr)handleInfoSize);

            do
            {
                handleInfo = Marshal.ReAllocHGlobal(handleInfo, (IntPtr)handleInfoSize);
                status = NtQuerySystemInformation(
                    SystemInformationClass.SystemExtendedHandleInformation,
                    handleInfo,
                    handleInfoSize,
                    out handleInfoSize);
            } while (status == NTStatus.InfoLengthMismatch);

            if (status != NTStatus.Success)
                throw new Win32Exception("help");

            SystemHandleInformationExtended shi = Marshal.PtrToStructure<SystemHandleInformationExtended>(handleInfo);

            SystemHandleExtended[] systemHandles = new SystemHandleExtended[(long)shi.NumberOfHandles];

            int size = Marshal.SizeOf<SystemHandleExtended>();
            int offset = IntPtr.Size * 2;
            await Task.Run(
                () => Parallel.For(
                    0,
                    (int)shi.NumberOfHandles,
                    (int i) =>
                        systemHandles[i] = Marshal.PtrToStructure<SystemHandleExtended>(handleInfo + offset + i * size)));

            Marshal.FreeHGlobal(handleInfo);

            int[] pids = systemHandles.Select(x => x.GetPid()).Distinct().ToArray();

            return from handle in systemHandles where handle.GetPid() == pid select handle;
        }

        [DllImport("ntdll.dll")]
        private static extern NTStatus NtDuplicateObject(
            IntPtr SourceProcessHandle,
            IntPtr SourceHandle,
            IntPtr TargetProcessHandle,
            IntPtr TargetHandle,
            uint DesiredAccess,
            uint Attributes,
            uint Options);

        [DllImport("ntdll.dll")]
        private static extern NTStatus NtQueryObject(
            IntPtr objectHandle,
            ObjectInformationClass informationClass,
            IntPtr informationPtr,
            uint informationLength,
            ref uint returnLength);

        [DllImport("ntdll.dll")]
        private static extern NTStatus NtQuerySystemInformation(
            SystemInformationClass InfoClass,
            IntPtr Info,
            uint Size,
            out uint Length);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
             ProcessAccessFlags processAccess,
             bool bInheritHandle,
             int processId);

        #endregion Methods

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        private struct GenericMapping
        {
            private uint GenericRead;
            private uint GenericWrite;
            private uint GenericExecute;
            private uint GenericAll;
        }

        private struct ObjectNameInformation
        {
            #region Fields

            public UnicodeString Name;

            #endregion Fields
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ObjectTypeInformation
        {
            public UnicodeString Name;
            public uint TotalNumberOfObjects;
            public uint TotalNumberOfHandles;
            public uint TotalPagedPoolUsage;
            public uint TotalNonPagedPoolUsage;
            public uint TotalNamePoolUsage;
            public uint TotalHandleTableUsage;
            public uint HighWaterNumberOfObjects;
            public uint HighWaterNumberOfHandles;
            public uint HighWaterPagedPoolUsage;
            public uint HighWaterNonPagedPoolUsage;
            public uint HighWaterNamePoolUsage;
            public uint HighWaterHandleTableUsage;
            public uint InvalidAttributes;
            public GenericMapping GenericMapping;
            public uint ValidAccess;
            public bool SecurityRequired;
            public bool MaintainHandleCount;
            public ushort MaintainTypeList;
            public PoolType PoolType;
            public uint PagedPoolUsage;
            public uint NonPagedPoolUsage;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SystemHandle
        {
            public IntPtr ProcessId;
            public byte ObjectTypeNumber;
            public byte Flags;
            public ushort Handle;
            public IntPtr Object;
            public int GrantedAccess;

            internal int GetPid() => IntPtr.Size == 4 ? (int)ProcessId : (int)((long)ProcessId >> 32);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SystemHandleInformation // Size=20
        {
            public uint NumberOfHandles; // Size=4 Offset=0
            public SystemHandle Handles; // Size=16 Offset=4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SystemHandleInformationExtended // Size=20
        {
            public IntPtr NumberOfHandles;
            public IntPtr Reserved;
            public SystemHandleExtended Handles;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UnicodeString : IDisposable
        {
            public ushort Length;
            public ushort MaximumLength;
            private IntPtr buffer;

            public UnicodeString(string s)
            {
                Length = (ushort)(s.Length * 2);
                MaximumLength = (ushort)(Length + 2);
                buffer = Marshal.StringToHGlobalUni(s);
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(buffer);
                buffer = IntPtr.Zero;
            }

            public override string ToString()
            {
                return Marshal.PtrToStringUni(buffer);
            }
        }

        #endregion Structs

        #region Classes

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class SystemHandleExtended
        {
            public IntPtr Object;
            public IntPtr ProcessId;
            public IntPtr HandleValue;
            public uint GrantedAccess;
            public ushort CreatorBackTraceIndex;
            public ushort ObjectTypeIndex;
            public uint HandleAttributes;
            public uint Reserved;

            internal int GetPid() => (int)ProcessId;
        }

        #endregion Classes
    }
}