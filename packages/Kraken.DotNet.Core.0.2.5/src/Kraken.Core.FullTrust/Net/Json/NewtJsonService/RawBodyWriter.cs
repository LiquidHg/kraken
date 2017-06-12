using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Web;
using System.Xml;

namespace Kraken.Net.WebApi {
	public class RawBodyWriter : BodyWriter {
		byte[] content;
		public RawBodyWriter(byte[] content)
			: base(true) {
			this.content = content;
		}

		protected override void OnWriteBodyContents(XmlDictionaryWriter writer) {
			writer.WriteStartElement("Binary");
			writer.WriteBase64(content, 0, content.Length);
			writer.WriteEndElement();
		}
	}
}