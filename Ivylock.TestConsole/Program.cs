using IvyLock.Native;
using System;
using System.ComponentModel;
using System.Linq;

namespace Ivylock.TestConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //try
            //{
            var schemas = WBF.GetBiometricUnits(BiometricType.Fingerprint);
            var session = WBF.OpenSession(BiometricType.Fingerprint, BiometricPoolType.System, BiometricSessionFlags.Default,
               null, BiometricDatabaseType.None);
            var identity = WBF.GetCurrentIdentity();
            var match = WBF.Verify(session, identity, BiometricSubtype.Any,
                out uint schema, out BiometricRejectDetail rejectDetail, out BiometricError error);
            if (error == BiometricError.None)
                Console.WriteLine("Match: {0}", match);
            else
            {
                Console.WriteLine("Biometric Error: {0}", error);
                Console.WriteLine("Reject detail: {0}", rejectDetail);
            }
            WBF.CloseSession(session);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Error: {0}", ex.Message);
            //}
            Console.ReadLine();
        }

        private static void Check(int hresult)
        {
            if (hresult != 0)
            {
                Win32Exception ex = new Win32Exception(hresult);
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }
    }
}