using System;
using System.IO;
using System.Text;

namespace Kraken.SharePoint.Client.Helpers.FPRPC
{
    /// <summary>
    /// Contains document information used when putting or getting a document.
    /// </summary>
    public class DocumentInfo
    {
        private const string STR_NEWLINE = "\n";
        private const string STR_SEMICOLON = ";";

        private string destinationFileName;
        private bool isFolder;
        private readonly DocumentPropertyCollection properties = new DocumentPropertyCollection();

        public DocumentPropertyCollection Properties 
        {
            get 
            {
                return properties;
            }
        }

        public string DestinationFileName
        {
            get
            {
                return destinationFileName;
            }
            set
            {
                destinationFileName = value;
            }
        }

        public string ModifiedBy
        {
            get
            {
                return GetPropertyValue<string>("vti_modifiedby", string.Empty);
            }
            set
            {
                SetPropertyValue("vti_modifiedby", value);
            }
        }

        public DateTime ModifiedDate
        {
            get
            {
                return GetPropertyValue<DateTime>("vti_timelastmodified", null);
            }
            set
            {
                SetPropertyValue("vti_timelastmodified", value);
            }
        }

        public string Title
        {
            get
            {
                return GetPropertyValue<string>("vti_title", string.Empty);
            }
            set
            {
                SetPropertyValue("vti_title", value);
            }
        }

        public bool ContainsKey(string keyName)
        {
            return properties.ContainsKey(keyName);
        }


        public bool Exists
        {
            get
            {
                return ContainsKey("vti_timecreated");
            }
        }

        public int FileSize
        {
            get
            {
                return GetPropertyValue<int>("vti_filesize", null);
            }
        }

        public bool IsFolder
        {
            get { return isFolder; }
            set { isFolder = value; }
        }

        public bool IsPublishingPage
        {
            get { return ContainsKey("PublishingPageLayout"); }
        }

        /// <summary>
        /// Writes document header data to the data stream.
        /// </summary>
        /// <param name="tw"></param>
        public void WriteDocumentData(TextWriter tw) 
        {
            tw.Write("&document=[document_name=" + DestinationFileName + STR_SEMICOLON);
            tw.Write("meta_info=" + GetMetaInfoList());
            tw.Write("]" + STR_NEWLINE);
        }

        internal string GetMetaInfoList(bool encode) 
        {
            StringBuilder metaInfo = new StringBuilder();
			
            metaInfo.Append("[");
            string separator = "";
            foreach(DocumentProperty prop in properties.Values) 
            {
                metaInfo.Append(separator);
                separator = ";";
                metaInfo.Append(prop.GetPropertyFP(encode));
            }		
            metaInfo.Append("]");
            return metaInfo.ToString();
        }

        internal string GetMetaInfoList() 
        {
            return GetMetaInfoList(true);
        }

        private T GetPropertyValue<T>(string propertyName, object defaultValue) 
        {
            DocumentProperty prop;
            if (properties.TryGetValue(propertyName, out prop))
            {
                return (T)prop.PropertyValue;
            }
            return (T) defaultValue;
        }

        private void SetPropertyValue<T>(string propertyName, T propertyValue) 
        {
            DocumentProperty prop;
            if (properties.TryGetValue(propertyName, out prop)) 
            {
                prop.PropertyValue = propertyValue;
            }
            else 
            {
                properties.Add(new DocumentProperty(propertyName, propertyValue));
            }
        }
    }
}