using Kraken.SharePoint.Client;
using Kraken.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.Core.TestConsole {
  class Program {
    static void Main(string[] args) {

      ContentTypeProperties props = new ContentTypeProperties() {
        Description = "la la!",
        Name = "MyType",
        JSLink = "foo",
        DocumentTemplate = "bar",
        Hidden = true,
        ReadOnly = false,
      };
      SimpleTrace trace = new SimpleTrace();
      Hashtable ht = props.ExportProperties();
      ContentTypeProperties props2 = new ContentTypeProperties();
      ht["MyGroupProperty"] = "MyGroup";
      PropertyMap<Hashtable, ContentTypeProperties> map 
        = new PropertyMap<Hashtable, ContentTypeProperties>(ht, props2) {
        Trace = trace
      };
      map.IncludeTargetWhen = (
        // for example p.GetCustomAttributes(true)
        p => p.Name != "LeaveMeAlone!"
      );
      map["MyGroupProperty"] = "Group";
      props2.ImportProperties(ht, map);

    }

  }
}
