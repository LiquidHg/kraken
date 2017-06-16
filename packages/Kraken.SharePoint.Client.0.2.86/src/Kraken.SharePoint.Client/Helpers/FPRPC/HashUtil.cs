using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Kraken.SharePoint.Client.Helpers.FPRPC
{
    public static class HashUtil
    {
        public static string GetHash(Stream s)
        {
            MD5 md5 = MD5.Create();
            return GetHash(md5.ComputeHash(s));
        }

        public static string GetHash(string s)
        {
            MD5 md5 = MD5.Create();
            return GetHash(md5.ComputeHash(Encoding.UTF8.GetBytes(s)));
        }

        private static string GetHash(byte[] hash)
        {
            StringBuilder encodedHash = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                encodedHash.Append(hash[i].ToString("X2"));
            }

            return encodedHash.ToString();
        }
    }
}
