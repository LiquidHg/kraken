using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.SharePoint;

namespace Kraken.SharePoint {

  public static class SPQueryExtensions_SandboxSafe {

    public static string BuildViewFieldsXml(this SPQuery query, params string[] fieldNames) {
      const string TEMPLATE = @"<FieldRef Name='{0:S}' />";
      StringBuilder sb = new StringBuilder();
      foreach (string fieldName in fieldNames) {
        sb.AppendFormat(TEMPLATE, fieldName);
      }
      return sb.ToString();
    }

  }
}
