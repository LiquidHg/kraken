namespace Microsoft.SharePoint.Client {

	using System;
	using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
	using System.Text;

	public static class KrakenFileExtensions {

    public static void MoveTo(this File file, Uri uri) {
      // TODO validate that the URL is a valid one
      // TODO reflection to determine what is being done in the back end?
      string newUrl = uri.ToString();
      //ResourcePath p = new ResourcePath();
      //file.CopyToUsingPath()
      file.MoveTo(newUrl, MoveOperations.AllowBrokenThickets);
    }

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
        ListItem fileItem = file.EnsureProperty(f => f.ListItemAllFields).ListItemAllFields;
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
