/*
  Project Kraken: SPARK for Microsoft SharePoint 2010
  Copyright (C) 2003-2011 Thomas Carpe. <http://www.ThomasCarpe.com/>
  Maintained by: <http://www.LiquidMercurySolutions.com/>

  This file is part of SPARK: SharePoint Application Resource Kit.
  SPARK projects are distributed via CodePlex: <http://www.codeplex.com/spark/>

  You may use this code for commercial purposes and derivative works, 
  provided that you maintain all copyright notices.

  SPARK is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version. You should have received a copy of
  the GNU General Public License along with SPARK.  If not, see
  <http://www.gnu.org/licenses/>.

  SPARK is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.
  
  We worked hard on all SPARK code, and we don't make any profit from
  sharing it with the world. Please do us a favor amd give us credit
  where credit is due, by leaving this notice unchanged. We all stand
  on the backs of giants. Wherever we have used someone else's code or
  blog article as the basis of our work, we have provided references
  to our source.
*/

namespace Kraken.SharePoint {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Administration;
  using Microsoft.SharePoint.Utilities;

  public static class SPListItemExtensions_SandboxSafe {

    /// <summary>
    /// Updates a list item, with check-in and check-out if required.
    /// The caller is responsible for exception ahndling.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public static bool UpdateWithCheckOutAndIn(this SPListItem item) {
      if (item.ParentList.ForceCheckout && item.FileSystemObjectType.Equals(SPFileSystemObjectType.File)) {
        item.File.CheckOut();
        item.Update();
        item.File.CheckIn("Updated metadata programmatically."); // AppDomain.CurrentDomain.FriendlyName
      } else {
        item.Update();
      }
      return true;
    }

    public static Uri GetUri(this SPListItem item) {
      string url = item.GetItemUrl(PAGETYPE.PAGE_DISPLAYFORM);
      try {
        Uri uri = new Uri(url);
        return uri;
      } catch (Exception ex) {
        throw new Exception(string.Format("Error generating Uri from string '{0}'", url), ex);
      }
    }

    public static Uri GetUriFromXml(this SPListItem item) {
      System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
      xmlDoc.LoadXml(item.Xml);
      System.Xml.XmlNamespaceManager nsm = new System.Xml.XmlNamespaceManager(xmlDoc.NameTable);
      nsm.AddNamespace("z", "#RowsetSchema");
      string url = xmlDoc.SelectSingleNode("z:row", nsm).Attributes["ows_EncodedAbsUrl"].Value;
      try {
        Uri uri = new Uri(url);
        return uri;
      } catch (Exception ex) {
        throw new Exception(string.Format("Error generating Uri from string '{0}'", url), ex);
      }
    }

    public static string GetDisplayFormUrl(this SPListItem item) {
      string url = item.Web.Url + item.ParentList.DefaultDisplayFormUrl + "?ID=" + item.ID;
      return url;
    }
    public static string GetEditFormUrl(this SPListItem item) {
      string url = item.Web.Url + item.ParentList.DefaultEditFormUrl + "?ID=" + item.ID;
      return url;
    }
    public static string GetNewFormUrl(this SPListItem item) {
      string url = item.Web.Url + item.ParentList.DefaultNewFormUrl + "?ID=" + item.ID;
      return url;
    }

    /// <summary>
    /// Get item's full url based on the Item's parent list base type 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="pageType"></param>
    /// <returns></returns>
    public static string GetItemUrl(this SPListItem item, PAGETYPE pageType) {
      //get a url for the item
      string itemUrl = null;
      switch (item.ParentList.BaseType) {
        case SPBaseType.DocumentLibrary:
          itemUrl = SPUrlUtility.CombineUrl(item.Web.Url, item.Url);
          break;
        case SPBaseType.DiscussionBoard:
        case SPBaseType.Issue:
        case SPBaseType.Survey:
        case SPBaseType.GenericList:
          itemUrl = item.Web.Url + "/" + item.ParentList.Forms[pageType].Url + "?ID=" + item.ID.ToString();
          break;
        case SPBaseType.UnspecifiedBaseType:
        case SPBaseType.Unused:
          itemUrl = null;
          break;
      }
      return itemUrl;
    }

  } // class
} // namespace
