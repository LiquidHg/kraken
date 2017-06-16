using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !DOTNET_V35
using System.Threading.Tasks;
#endif
using System.IO;
using DSOFile;

namespace Kraken.SharePoint.Client.Helpers {
  public static class FileInfoHelper {

    private static OleDocumentProperties CreateOleDocumentProperties() {
      // Done to resolve COM+ Class Not Registered issue
      try {
        return new DSOFile.OleDocumentProperties();
      } catch {
        return null;
      }
    }

    public static void SaveProperty(this FileInfo fi, string key, object value) {
      OleDocumentProperties file = CreateOleDocumentProperties();
      if (file == null)
        return;
      try {
        file.Open(fi.FullName, false, DSOFile.dsoFileOpenOptions.dsoOptionDefault);
        bool hasProperty = false;
        foreach (DSOFile.CustomProperty p in file.CustomProperties) {
          if (p.Name == key) {
            p.set_Value(value);
            hasProperty = true;
          }
        }
        if (!hasProperty) {
          file.CustomProperties.Add(key, ref value);
        }
      } finally {
        if (file != null)
          file.Close(true);
      }
    }

    public static object LoadProperty(this FileInfo fi, string key) {
      OleDocumentProperties file = CreateOleDocumentProperties();
      if (file == null)
        return null;
      try {
        file.Open(fi.FullName, false, DSOFile.dsoFileOpenOptions.dsoOptionDefault);

        foreach (DSOFile.CustomProperty p in file.CustomProperties) {
          if (p.Name == key) {
            return p.get_Value();
          }
        }
        return null;
      } finally {
        if (file != null)
          file.Close(false);
      }
    }

  }
}