using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client {
  public class ResolveLookupOptions {
    public ResolveLookupOptions() {
      AllowMultipleResults = false;
      LookupFieldName = "Title";
      LookupFieldType = "Text";
    }

    public void ValidateOptions() {
      if (string.IsNullOrEmpty(LookupFieldName))
        LookupFieldName = "Title";
      if (string.IsNullOrEmpty(LookupFieldType))
        LookupFieldType = "Text";
    }

    public string LookupFieldName { get; set; }
    public string LookupFieldType { get; set; }

    public bool AllowMultipleResults { get; set; }

  }
}
