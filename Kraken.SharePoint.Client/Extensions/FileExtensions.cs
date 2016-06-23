namespace Kraken.SharePoint.Client {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Net;
	using System.Security;
	using System.Xml.Linq;

	using Microsoft.SharePoint.Client;
	using System.Diagnostics;
	//using Microsoft.SharePoint.Client.DocumentSet;
	using Kraken.SharePoint.Client;
	using Kraken.Security.Cryptography;

	public static class FileExtensions {

		public static void Rename(this File file, string newTitle) {
			var ctx = file.Context;
			try {
				ListItem listitem = file.ListItemAllFields;
				listitem["FileLeafRef"] = newTitle;
				listitem.Update();
				ctx.ExecuteQuery();
			} catch (Microsoft.SharePoint.Client.ServerException ex) {

			}
		}

    public static Folder GetParentFolder(this File file) {
      var ctx = file.Context;
      Folder folder = null;
      try {
        file.EnsureProperty(null, f => f.ListItemAllFields);
        ListItem fileItem = file.ListItemAllFields;
        folder = fileItem.GetListItemFolder();
      } catch (Microsoft.SharePoint.Client.ServerException ex) {

      }
      return folder;
    }

		public static string GetParentFolderUrl(this File file) {
      Folder folder = file.GetParentFolder();
      if (folder == null)
        return string.Empty;
      return folder.ServerRelativeUrl;
		}
	
	}

}
