using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Web;

namespace Kraken.Net.WebApi {
	public class NewtJsonContentTypeMapper : WebContentTypeMapper {
		public override WebContentFormat GetMessageFormatForContentType(string contentType) {
			return WebContentFormat.Raw;
		}
	}

}