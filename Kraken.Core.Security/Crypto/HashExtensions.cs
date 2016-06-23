namespace Kraken.Security.Cryptography {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  public static class HashExtensions {

    #region Hash Computation Support

    public static string ComputeCrc32(this byte[] buffer) {
      Crc32 crc32 = new Crc32();
      string hash = string.Empty;
      foreach (byte b in crc32.ComputeHash(buffer)) {
        hash += b.ToString("x2").ToLower();
      }
      return hash;
    }
#if DOTNET_V35
    public static string ComputeCrc32(this System.IO.Stream stream) {
      return ComputeCrc32(stream, true);
    }
    public static string ComputeCrc32(this System.IO.Stream stream, bool resetToBeginning) {
#else
    public static string ComputeCrc32(this System.IO.Stream stream, bool resetToBeginning = true) {
#endif
      Crc32 crc32 = new Crc32();
      string hash = string.Empty;
      foreach (byte b in crc32.ComputeHash(stream)) {
        hash += b.ToString("x2").ToLower();
      }
      if (resetToBeginning)
        stream.Position = 0; // stream.Seek(0, System.IO.SeekOrigin.Begin);
      return hash;
    }
    public static string ComputeMD5Hash(this byte[] buffer) {
      System.Security.Cryptography.MD5Cng md5 = new System.Security.Cryptography.MD5Cng();
      byte[] hash = md5.ComputeHash(buffer);
      return System.Convert.ToBase64String(hash);
    }
#if DOTNET_V35
    public static string ComputeMD5Hash(this System.IO.Stream stream) {
      return ComputeMD5Hash(stream, true);
    }
    public static string ComputeMD5Hash(this System.IO.Stream stream, bool resetToBeginning) {
#else
    public static string ComputeMD5Hash(this System.IO.Stream stream, bool resetToBeginning = true) {
#endif
      System.Security.Cryptography.MD5Cng md5 = new System.Security.Cryptography.MD5Cng();
      byte[] hash = md5.ComputeHash(stream);
      if (resetToBeginning)
        stream.Position = 0; // stream.Seek(0, System.IO.SeekOrigin.Begin);
      return System.Convert.ToBase64String(hash);
    }

    #endregion

  }

}
