using System;
using System.Globalization;
using System.Web;

namespace Kraken.SharePoint.Client.Helpers.FPRPC
{
    public class DocumentProperty
        : ICloneable
    {
        public enum PropertyAccessLevel
        {
            ReadOnly = 0,
            Excluded,
            ReadWrite
        }

        public enum PropertyDataType
        {
            Boolean = 0,
            FileSystemTime,
            Integer,			
            String,
            DateTime,
            StringVector 
        }

        private string propertyName;
        private object propertyValue;
        private PropertyAccessLevel propertyAccess;
        private PropertyDataType dataType;

        private const int ServerLocaleID = 1033;
        private const string FP_DATE_FORMAT = "dd MMM yyyy HH':'mm':'ss '-0000'";
        private readonly static string[] propertyAccessMap = { "R", "X", "W" };
        private readonly static string[] propertyDataTypeMap = { "B", "F", "I", "S", "T", "V" };

        /* Ctors */

        internal DocumentProperty(string propertyName, string propertyTypeAndAccess, string propertyValue)
        {
            this.propertyName = propertyName;
            SetAccessAndTypeFromString(propertyTypeAndAccess);
            SetTypeAppropriateValue(propertyValue);
        }

        internal DocumentProperty(string propertyName, PropertyAccessLevel propertyAccess, PropertyDataType dataType, object propertyValue)
        {
            this.propertyName = propertyName;
            this.propertyValue = propertyValue;
            this.propertyAccess = propertyAccess;
            this.dataType = dataType;
        }

        public DocumentProperty(string propertyName, object propertyValue) 
        {
            this.propertyName = propertyName;
            this.propertyValue = propertyValue;
            SetPropertyTypeAndValue(propertyValue);
        }

        /* Properties */

        public string PropertyName
        {
            get { return propertyName; }
            set { propertyName = value; }
        }

        public object PropertyValue
        {
            get { return propertyValue; }
            set { SetPropertyTypeAndValue(value); }
        }

        public PropertyAccessLevel PropertyAccess
        {
            get {  return propertyAccess;}
        }

        public PropertyDataType DataType
        {
            get { return dataType; }
        }
        
        /* Methods */

        private void SetAccessAndTypeFromString(string propTypeAndAccess)
        {
            string typeValue = propTypeAndAccess.Substring(0, 1);
            string accessValue = propTypeAndAccess.Substring(1, 1);

            dataType = PropertyDataType.String;
            for (int i = 0; i < propertyDataTypeMap.Length; i++)
            {
                if (typeValue == propertyDataTypeMap[i])
                {
                    dataType = (PropertyDataType)i;
                    break;
                }
            }

            propertyAccess = PropertyAccessLevel.ReadOnly;
            for (int i = 0; i < propertyAccessMap.Length; i++)
            {
                if (accessValue == propertyAccessMap[i])
                {
                    propertyAccess = (PropertyAccessLevel)i;
                    break;
                }
            }
        }

        private static CultureInfo ServerCulture
        {
            get { return new CultureInfo(ServerLocaleID); }
        }

        /// <summary>
        /// Builds the FrontPage RPC appropriate encoding for a property value.
        /// </summary>
        /// <remarks>
        /// Properties are generally encoded as: 
        /// <code>PropertyName;SW|PropertyValue</code>
        /// </remarks>
        /// <param name="encode"></param>
        /// <returns></returns>
        internal string GetPropertyFP(bool encode)
        {
            string type = propertyDataTypeMap[(int) DataType] + propertyAccessMap[(int) PropertyAccess];
            string data;

            switch (DataType)
            {
                case PropertyDataType.DateTime:
                case PropertyDataType.FileSystemTime:
                    data = ((DateTime)PropertyValue).ToString(FP_DATE_FORMAT, ServerCulture);
                    break;

                default:
                    data = null == PropertyValue ? String.Empty : PropertyValue.ToString();
                    break;
            }

            return string.Format("{0};{1}|{2}", propertyName, type, (encode ? Encode(data) : data));
        }

        private static string Encode(string value)
        {
            return HttpUtility.UrlEncode(value.Replace("\\", "\\\\"));
        }

        internal string GetPropertyFP() 
        {
            return GetPropertyFP(true);
        }

        private void SetPropertyTypeAndValue(object value) 
        {
            propertyValue = value;

            if (value is String || value is string) 
            {
                dataType = PropertyDataType.String;
            }
            else if (value is DateTime) 
            {
                dataType = PropertyDataType.DateTime;
            }
            else if (PropertyValue is Int32 || PropertyValue is Int16)
            {
                dataType = PropertyDataType.Integer;
            }
            else if (PropertyValue is bool || PropertyValue is Boolean)
            {
                dataType = PropertyDataType.Boolean;
            }
            else
            {
                dataType = PropertyDataType.String;
            }
        }

        private void SetTypeAppropriateValue(string value) 
        {
            switch(dataType) 
            {
                case PropertyDataType.Boolean:
                    PropertyValue = bool.Parse(value);
                    break;

                case PropertyDataType.DateTime:
                    DateTime dateValue = DateTime.ParseExact(value, FP_DATE_FORMAT, ServerCulture);
                    PropertyValue = dateValue;
                    break;
                case PropertyDataType.FileSystemTime:
                    PropertyValue = value; // just put it as string value.
                    break;

                case PropertyDataType.Integer:
                    PropertyValue = int.Parse(value);
                    break;

                case PropertyDataType.String:
                case PropertyDataType.StringVector:
                    PropertyValue = value;
                    break;

                default:
                    throw new ApplicationException("Unable to parse value " + value + " to a valid type.");
            }
        }

        public object Clone()
        {
            return new DocumentProperty(propertyName, propertyAccess, dataType, propertyValue);
        }
    }
}