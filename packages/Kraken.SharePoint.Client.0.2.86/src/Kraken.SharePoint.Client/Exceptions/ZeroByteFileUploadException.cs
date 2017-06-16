using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Kraken.SharePoint.Client {

  public class ZeroByteFileUploadException : Exception {

    public ZeroByteFileUploadException() : base() { }
    public ZeroByteFileUploadException(string message) : base(message) { }
    public ZeroByteFileUploadException(string message, Exception innerException) : base(message, innerException) { }
    public ZeroByteFileUploadException(SerializationInfo info, StreamingContext context) : base(info, context) { }

  }

}
