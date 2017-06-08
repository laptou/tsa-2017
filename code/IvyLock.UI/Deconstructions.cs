using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyLock
{
    public static class Deconstructions
    {
        public static (StreamReader reader, StreamWriter writer) OpenStream(this Stream stream)
        {
            return (new StreamReader(stream), new StreamWriter(stream));
        }

        public static void Deconstruct<T>(this T[] str, out T s1, out T s2)
        {
            s1 = str[0];
            s2 = str[1];
        }

        public static void Deconstruct<T>(this T[] str, out T s1, out T s2, out T s3)
        {
            s1 = str[0];
            s2 = str[1];
            s3 = str[2];
        }

        public static void Deconstruct<T>(this T[] str, out T s1, out T s2, out T s3, out T s4)
        {
            s1 = str[0];
            s2 = str[1];
            s3 = str[2];
            s4 = str[3];
        }

        public static void Deconstruct<T>(this T[] str, out T s1, out T s2, out T s3, out T s4,
            out T s5)
        {
            s1 = str[0];
            s2 = str[1];
            s3 = str[2];
            s4 = str[3];
            s5 = str[4];
        }

        public static void Deconstruct<T>(this T[] str, out T s1, out T s2, out T s3, out T s4,
            out T s5, out T s6)
        {
            s1 = str[0];
            s2 = str[1];
            s3 = str[2];
            s4 = str[3];
            s5 = str[4];
            s6 = str[5];
        }
    }
}
