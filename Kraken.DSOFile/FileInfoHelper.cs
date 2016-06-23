using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !DOTNET_V35
using System.Threading.Tasks;
#endif
using System.IO;
using DSOFile;

namespace Kraken.SharePoint.Client.Helpers
{
    public static class FileInfoHelper
    {
        public static void SaveProperty(this FileInfo fi, string key, object value)
        {
            OleDocumentProperties file = new DSOFile.OleDocumentProperties();

            try
            {
                file.Open(fi.FullName, false, DSOFile.dsoFileOpenOptions.dsoOptionDefault);

                bool hasProperty = false;

                foreach (DSOFile.CustomProperty p in file.CustomProperties)
                {
                    if (p.Name == key)
                    {
                        p.set_Value(value);
                        hasProperty = true;
                    }
                }

                if (!hasProperty)
                {
                    file.CustomProperties.Add(key, ref value);
                }
            }
            finally
            {
                file.Close(true);
            }
        }

        public static object LoadProperty(this FileInfo fi, string key)
        {
            OleDocumentProperties file = new DSOFile.OleDocumentProperties();

            try
            {
                file.Open(fi.FullName, false, DSOFile.dsoFileOpenOptions.dsoOptionDefault);

                foreach (DSOFile.CustomProperty p in file.CustomProperties)
                {
                    if (p.Name == key)
                    {
                        return p.get_Value();
                    }
                }

                return null;
            }
            finally
            {
                file.Close(false);
            }
        }
    }
}