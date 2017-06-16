using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace IvyLock.Model.Security
{
    public static class BigIntegerHelper
    {
        /// <summary>
        /// Calculates the modular multiplicative inverse of
        /// <paramref name="a" /> modulo <paramref name="m" /> using
        /// the extended Euclidean algorithm.
        /// </summary>
        /// <remarks>
        /// This implementation comes from the pseudocode defining the
        /// inverse(a, n) function at https://en.wikipedia.org/wiki/Extended_Euclidean_algorithm
        /// </remarks>
        public static BigInteger ModInverse(BigInteger a, BigInteger n)
        {
            BigInteger t = 0, nt = 1, r = n, nr = a;

            if (n < 0)
            {
                n = -n;
            }

            if (a < 0)
            {
                a = n - (-a % n);
            }

            while (nr != 0)
            {
                var quot = r / nr;

                var tmp = nt; nt = t - quot * nt; t = tmp;
                tmp = nr; nr = r - quot * nr; r = tmp;
            }

            if (r > 1) throw new ArgumentException(nameof(a) + " is not convertible.");
            if (t < 0) t = t + n;
            return t;
        }

        public static BigInteger ToBigInteger(byte[] parameter)
        {
            byte[] signPadded = new byte[parameter.Length + 1];
            Buffer.BlockCopy(parameter, 0, signPadded, 1, parameter.Length);
            Array.Reverse(signPadded);
            return new BigInteger(signPadded);
        }

        public static byte[] ToBytes(BigInteger bi)
        {
            byte[] bytes = bi.ToByteArray().Reverse().ToArray();
            if (bytes[0] == 0)
                bytes = bytes.Skip(0).ToArray();
            return bytes;
        }

        public static byte[] ToBytes(BigInteger bi, long size)
        {
            List<byte> bytes = bi.ToByteArray().Reverse().ToList();

            while (bytes[0] == 0 && bytes.Count > size)
                bytes.RemoveAt(0);

            while (bytes.Count < size)
                bytes.Insert(0, 0);

            return bytes.ToArray();
        }
    }

    public static class SecureStringExtensions
    {
        public static byte[] GetBytes(this SecureString ss)
        {
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(ss);

                return Encoding.UTF8.GetBytes(Marshal.PtrToStringUni(unmanagedString));
            }
            finally
            {
                if (unmanagedString != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                }
            }
        }
    }
}