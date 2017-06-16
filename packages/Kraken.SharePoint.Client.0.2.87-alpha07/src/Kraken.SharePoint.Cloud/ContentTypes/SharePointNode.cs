using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Kraken.SharePoint.Cloud {

  /// <summary>
  /// This class is used to store informtion about objects in SharePoint
  /// such as site columns, content types, or lists. It is used to populate
  /// checkbox lists, combo boxes, and tree views - or basically anywhere
  /// a quick-and-dirty implementation is needed.
  /// </summary>
  public class SharePointNode {

    public string NameOrID;
    public string Group;
    public string DisplayName;
    public string AltNameOrID;
    public string Description;
    public XElement XmlSchema;

    public override string ToString() {
      string name = DisplayName ?? NameOrID;
      if (string.IsNullOrEmpty(Group)) {
        return name;
      } else {
        return string.Format("{0} ({1})", name, Group);
      }
    }

  }

}
