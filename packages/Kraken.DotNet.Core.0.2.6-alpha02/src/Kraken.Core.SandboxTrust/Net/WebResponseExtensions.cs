using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace Kraken.Net {

  public static class WebResponseExtensions {

    public static Stream GetStreamAndDecompressIfNeeded(this WebResponse response) {
		  Stream stream = response.GetResponseStream();
      if (!(string.IsNullOrEmpty(response.Headers["Content-Encoding"]))) {
        if (response.Headers["Content-Encoding"].ToLower().Contains("gzip")) {
          stream = new GZipStream(stream, CompressionMode.Decompress);
        } else if (response.Headers["Content-Encoding"].ToLower().Contains("deflate")) {
          stream = new DeflateStream(stream, CompressionMode.Decompress);
        }
      }
      return stream;
    }

  }

}
