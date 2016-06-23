using System;
using System.Text;

// TODO implement some real encryption

namespace Kraken.SharePoint.Security {

  public static class EncryptionUtility {

    /// <summary>
    /// Encrypt a string to store safely in SharePoint
    /// </summary>
    /// <param name="plainText"></param>
    /// <returns></returns>
    public static string Encrypt(this string plainText) {
      // Check for an actual value in plainText  
      if (!String.IsNullOrEmpty(plainText)) {
        // This is where you would do your own custom encryption.  
        // For this example, we just Base64 encode the string.  
        byte[] bytes = Encoding.ASCII.GetBytes(plainText);
        string cryptText = Convert.ToBase64String(bytes);
        // We add a prefix to the value that will be stored,  
        // signifying that this encrypted text.  
        return String.Format("CRYPT:{0}", cryptText);
      } else {
        // If plainText was null or empty, we just return it.  
        return plainText;
      }
    }

    /// <summary>
    /// Decrypt an encrypted string that was created with EncryptionUtility.Decrypt
    /// </summary>
    /// <param name="cypherText"></param>
    /// <returns></returns>
    public static string Decrypt(this string cypherText) {
      // If cypherText is not prefixed, then it is not  
      // encrypted text, so just return it.  
      if (!cypherText.StartsWith("CRYPT:"))
        return cypherText;

      // Check for an actual value in cypherText  
      if (!String.IsNullOrEmpty(cypherText)) {
        // Strip the prefix so that all we have is the   
        // encrypted value.  
        cypherText = cypherText.Remove(0, 6);
        // This is where you would do your own custom decryption.  
        // For this example, we just Base64 decode the string.  
        byte[] bytes = Convert.FromBase64String(cypherText);
        return Encoding.ASCII.GetString(bytes);
      } else {
        // If cypherText was null or empty, we just return it.  
        return cypherText;
      }
    }
  }
}

