using System;
using System.Text;

namespace LinkFrameWork.Extentions
{
    public static class StringExtension
    {
        private static System.Security.Cryptography.SHA1 hash =
            new System.Security.Cryptography.SHA1CryptoServiceProvider();

        public static int GenHashCode(this string str)
        {
            int hashCode = 0;
            if (string.IsNullOrEmpty(str)) return hashCode;
            var bytes = Encoding.Unicode.GetBytes(str);
            byte[] hashText = hash.ComputeHash(bytes);
            int hashCodeStart = BitConverter.ToInt32(hashText, 0);
            int hashCodeMedium = BitConverter.ToInt32(hashText, 8);
            int hashCodeEnd = BitConverter.ToInt32(hashText, 16);
            hashCode = (hashCodeStart * 31 + hashCodeMedium) * 17 + hashCodeEnd;
            return int.MaxValue - hashCode;
        }
    }
}