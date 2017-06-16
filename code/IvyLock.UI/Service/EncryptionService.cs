using IvyLock.Model.Security;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Cryptography;

namespace IvyLock.Service
{
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
            Aes aes = Aes.Create();

            byte[] keyBytes = HashBytes(key);

            Array.Copy(keyBytes, aes.Key, aes.Key.Length);
            Array.Copy(keyBytes, aes.Key.Length, aes.IV, 0, aes.IV.Length);
            Array.Clear(keyBytes, 0, keyBytes.Length);

            byte[] inputBytes = Convert.FromBase64String(s);

            ICryptoTransform ict = aes.CreateDecryptor();

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
            Aes aes = Aes.Create();

            byte[] keyBytes = HashBytes(key);

            Array.Copy(keyBytes, aes.Key, aes.Key.Length);
            Array.Copy(keyBytes, aes.Key.Length, aes.IV, 0, aes.IV.Length);
            Array.Clear(keyBytes, 0, keyBytes.Length);

            ICryptoTransform ict = aes.CreateEncryptor();

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
            return Convert.ToBase64String(HashBytes(ss));
        }

        public byte[] HashBytes(SecureString ss)
        {
            byte[] result;
            byte[] bytes = ss.GetBytes();
            using (SHA512 sha512 = SHA512.Create())
                result = sha512.ComputeHash(bytes);

            Array.Clear(bytes, 0, bytes.Length);

            return result;
        }
    }
}