using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.SharePoint;

namespace Kraken.SharePoint {

  public static class SPListExtensions_SandboxSafe {

    public static string GetProperty(this SPList list, string propertyName) {
      if (!list.RootFolder.Properties.ContainsKey(propertyName) || list.RootFolder.Properties[propertyName] == null)
        return string.Empty;
      return list.RootFolder.GetProperty(propertyName).ToString();
      //return list.RootFolder.Properties[propertyName].ToString();
    }
    public static void SetProperty(this SPList list, string propertyName, string value) {
      if (list.RootFolder.Properties.ContainsKey(propertyName))
        list.RootFolder.SetProperty(propertyName, value); // list.RootFolder.Properties[propertyName] = value;
      else
        list.RootFolder.AddProperty(propertyName, value); // list.RootFolder.Properties.Add(propertyName, value);
    }

  }
}
