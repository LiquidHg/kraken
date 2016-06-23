using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.SharePoint.Client;
using System.Net;

namespace Kraken.SharePoint.Client {

  class MyClientContext : ClientContext {

    public MyClientContext(string webFullUrl) : base(webFullUrl) {
    }
    public MyClientContext(Uri webFullUrl) : base(webFullUrl) {
    }

    internal void MyFireExecutingWebRequestEventInternal(WebRequestEventArgs args) {
      this.OnExecutingWebRequest(args);
    }

    /*
    protected override void OnExecutingWebRequest(WebRequestEventArgs args) {
      if ((args != null) && (args.WebRequestExecutor != null)) {
        if (args.WebRequestExecutor.WebRequest != null) {
          args.WebRequestExecutor.WebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
        }
        if (!string.IsNullOrEmpty(this.TraceCorrelationId)) {
          args.WebRequestExecutor.RequestHeaders["SPResponseGuid"] = this.TraceCorrelationId;
        }
        if (!string.IsNullOrEmpty(this.ClientTag)) {
          args.WebRequestExecutor.RequestHeaders["X-ClientService-ClientTag"] = this.ClientTag;
        }
      }
      EventHandler<WebRequestEventArgs> executingWebRequest = this.ExecutingWebRequest;
      if (executingWebRequest != null) {
        executingWebRequest(this, args);
      }
    }
     */

  }

}
