using System;
using System.Linq;
using System.Security.Cryptography;

namespace IvyLock.Model.Security
{
    public enum RSAKeyType
    {
        Public, Private
    }

    public struct RSAKey
    {
        #region Constructors

        public RSAKey(RSAParameters p, RSAKeyType type, string name)
        {
            Type = type;
            Name = name;

            P = null;
            Q = null;
            E = p.Exponent;
            N = p.Modulus;

            if (type == RSAKeyType.Private)
            {
                P = p.P;
                Q = p.Q;
            }
        }

        #endregion Constructors

        #region Properties

        public byte[] P { get; set; }
        public byte[] Q { get; set; }
        public byte[] E { get; set; }
        public byte[] N { get; set; }
        public RSAKeyType Type { get; set; }
        public string Name { get; set; }

        #endregion Properties

        #region Methods

        public static implicit operator RSAKey(string str)
        {
            (var p, var q, var e, var n) = str.Split(':');

            RSAKey key = new RSAKey()
            {
                E = Convert.FromBase64String(e),
                N = Convert.FromBase64String(n)
            };

            if (!string.IsNullOrWhiteSpace(p))
            {
                key.Type = RSAKeyType.Private;
                key.P = Convert.FromBase64String(p);
                key.Q = Convert.FromBase64String(q);
            }

            return key;
        }

        public static implicit operator RSAParameters(RSAKey key)
        {
            RSAParameters param = new RSAParameters()
            {
                Exponent = key.E,
                Modulus = key.N
            };

            if (key.Type == RSAKeyType.Private)
            {
                var p = BigIntegerHelper.ToBigInteger(key.P);
                var q = BigIntegerHelper.ToBigInteger(key.Q);
                var e = BigIntegerHelper.ToBigInteger(key.E);
                var n = BigIntegerHelper.ToBigInteger(key.N);
                var phi = n - p - q + 1; // OR: (p - 1) * (q - 1);

                var d = BigIntegerHelper.ModInverse(e, phi);

                param.P = key.P;
                param.Q = key.Q;
                param.D = BigIntegerHelper.ToBytes(d, 128);
                param.DP = BigIntegerHelper.ToBytes(d % (p - 1), 64);
                param.DQ = BigIntegerHelper.ToBytes(d % (q - 1), 64);
                param.InverseQ = BigIntegerHelper.ToBytes(BigIntegerHelper.ModInverse(q, p), 64);
            }

            return param;
        }

        public static implicit operator string(RSAKey key)
        {
            return string.Join(
                ":",
                new byte[][] { key.P, key.Q, key.E, key.N }
                    .Select(x => x != null ? Convert.ToBase64String(x) : ""));
        }

        public override string ToString()
        {
            return this;
        }

        public static RSAKey Get(string name)
        {
            CspParameters csp = new CspParameters()
            {
                KeyContainerName = name
            };
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(csp) { PersistKeyInCsp = true };
            return new RSAKey(rsa.ExportParameters(true), RSAKeyType.Private, name);
        }

        public void Save()
        {
            CspParameters cp = new CspParameters()
            {
                KeyContainerName = Name
            };
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cp) { PersistKeyInCsp = true };
            rsa.ImportParameters(this);
        }

        public void Remove()
        {
            CspParameters cp = new CspParameters()
            {
                KeyContainerName = Name
            };
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cp)
            {
                PersistKeyInCsp = false
            };
        }

        public RSAKey ToPublicKey()
        {
            return new RSAKey()
            {
                Type = RSAKeyType.Public,
                E = E,
                N = N,
                Name = Name
            };
        }

        #endregion Methods
    }
}