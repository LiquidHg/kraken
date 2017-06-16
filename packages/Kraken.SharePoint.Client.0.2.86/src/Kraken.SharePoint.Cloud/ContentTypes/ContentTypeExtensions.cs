using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Kraken.SharePoint.Cloud.ContentTypes {

  public static class ContentTypeExtentions {

    /// <summary>
    /// This method gets the longest available content ID that matches the start
    /// of the provided content type ID. This should always be the direct parent
    /// of the content type, assuming that all content types are available
    /// </summary>
    /// <param name="childID"></param>
    /// <returns></returns>
    public static string GetParentContentTypeId(this XElement contentTypes, string childID) {
      // TODO how do we query web service for cType's parent ID?
      //   get all the content type ids from server
      //   loop through them looking for new content id starts with each ID
      //   if it is the longest id so far, it is the best parent
      string longestParent = (from XElement ct in contentTypes.Descendants()
                              where ct.Name.LocalName == "ContentType"
                                && childID != ct.Attribute("ID").Value
                                && childID.StartsWith(ct.Attribute("ID").Value)
                              orderby ct.Attribute("ID").Value.Length descending
                              select ct.Attribute("ID").Value).FirstOrDefault();
      return longestParent;
    }

  }
}
