using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !DOTNET_V35
using System.Threading.Tasks;
#endif
using System.Xml.Serialization;
using System.IO;

namespace Kraken.SharePoint.Client.Helpers
{
    public static class XmlSerialization<T>
    {
        public static T DeserializeFromFile(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(stream);
            }
        }

        public static void SerializeToFile(T data, string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(stream, data);
            }
        }

        public static T Deserialize(string xmlString)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlString)))
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(stream);
            }
        }

        public static string Serialize(T data)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(stream, data);
                stream.Position = 0;
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                return Encoding.UTF8.GetString(bytes);
            }
        }
    }
}
