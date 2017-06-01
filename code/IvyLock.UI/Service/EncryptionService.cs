using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace IvyLock.Service
{
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

    public interface IEncryptionService
    {
        string Encrypt<T>(T o, SecureString key);

        string Hash<T>(T o);

        T Decrypt<T>(string s, SecureString key);
    }

    public class EncryptionService : IEncryptionService
    {
        static EncryptionService()
        {
            Default = new EncryptionService();
        }

        public static EncryptionService Default { get; set; }

        public T Decrypt<T>(string s, SecureString key)
        {
            TripleDES tdes = TripleDES.Create();

            byte[] keyBytes = key.GetBytes();
            byte[] keyBytes16 = new byte[16];
            byte[] keyBytes8 = new byte[8];

            Array.Copy(keyBytes, keyBytes16, Math.Min(16, keyBytes.Length));
            Array.Copy(keyBytes, keyBytes8, Math.Min(8, keyBytes.Length));
            Array.Clear(keyBytes, 0, keyBytes.Length);

            tdes.Key = keyBytes16;
            tdes.IV = keyBytes8;

            byte[] inputBytes = Convert.FromBase64String(s);

            ICryptoTransform ict = tdes.CreateDecryptor();

            Array.Clear(keyBytes16, 0, 16);
            Array.Clear(keyBytes8, 0, 8);

            MemoryStream ms = new MemoryStream(Convert.FromBase64String(s), false);
            CryptoStream cs = new CryptoStream(ms, ict, CryptoStreamMode.Read);

            BinaryFormatter bf = new BinaryFormatter();
            object o = bf.Deserialize(cs);

            cs.Dispose();
            ms.Dispose();

            return o is T ? (T)o : default(T);
        }

        public string Salt()
        {
            byte[] salt = new byte[32];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        public string Encrypt<T>(T o, SecureString key)
        {
            TripleDES tdes = TripleDES.Create();

            byte[] keyBytes = key.GetBytes();
            byte[] keyBytes16 = new byte[16];
            byte[] keyBytes8 = new byte[8];

            Array.Copy(keyBytes, keyBytes16, Math.Min(16, keyBytes.Length));
            Array.Copy(keyBytes, keyBytes8, Math.Min(8, keyBytes.Length));
            Array.Clear(keyBytes, 0, keyBytes.Length);

            tdes.Key = keyBytes16;
            tdes.IV = keyBytes8;

            ICryptoTransform ict = tdes.CreateEncryptor();

            Array.Clear(keyBytes16, 0, 16);
            Array.Clear(keyBytes8, 0, 8);

            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, ict, CryptoStreamMode.Write);

            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(cs, o);

            using (StreamReader sr = new StreamReader(ms))
                return Convert.ToBase64String(ms.ToArray());
        }

        public string Hash<T>(T o)
        {
            if (o == null)
                return null;
            if (o is SecureString)
                return Hash(o as SecureString);

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, o);

                byte[] result;
                using (SHA512 sha512 = SHA512.Create())
                    result = sha512.ComputeHash(ms);
                return Convert.ToBase64String(result);
            }
        }

        private string Hash(SecureString ss)
        {
            byte[] result;
            byte[] bytes = ss.GetBytes();
            using (SHA512 sha512 = SHA512.Create())
                result = sha512.ComputeHash(bytes);

            Array.Clear(bytes, 0, bytes.Length);

            return Convert.ToBase64String(result);
        }
    }
}