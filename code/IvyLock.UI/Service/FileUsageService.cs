using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static IvyLock.Service.Native.NativeMethods;

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

        #endregion Methods
    }
}