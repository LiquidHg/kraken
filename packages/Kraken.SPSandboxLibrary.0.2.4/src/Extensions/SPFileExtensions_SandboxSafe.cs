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
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Text;

    using Microsoft.SharePoint;

    //using Kraken.SharePoint.Logging;

  /*
   * This class writes to the .NET Trace provider, so if you want to
   * log that data in SharePoint ULS logs, you will need to attach
   * the SharepointTraceListener to the listeners collection before
   * making calls. Use code like this:
   * 
   * SharepointTraceListener.EnsureListener(true, true);
   * // Note we never implemented code to remove the listener.
   */

  public static class SPFileExtensions_SandboxSafe {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="web"></param>
    /// <param name="folderUrl">The url relative from the web site.</param>
    /// <returns></returns>
    public static SPFolder GetSubFolderIfExists(this SPFolder fileFolder, string folderName) {
      SPFolder subFolder = null;
      string url = fileFolder.Url + "/" + folderName;
      try {
        subFolder = fileFolder.SubFolders[url];
      } catch (ArgumentException /* ex */) { /* Value does not fall within the expected range. */ }
      return subFolder;
    }

    public const int MAX_RENAMED_FILES = 2000;

    public static string GenerateCopyFileName(string fileName, int attempt) {
      int pos = fileName.LastIndexOf('.');
      string part1 = string.Empty;
      string part2 = string.Empty;
      if (pos <= 0)
        part1 = fileName;
      else {
        part1 = fileName.Substring(0, pos);
        part2 = fileName.Substring(pos);
      }
      fileName = part1 + "_copy(" + attempt + ")" + part2;
      return fileName;
    }

    public static SPFile MoveFileToSubfolder(this SPWeb web, string fileUrl, string destFolderName) {
      SPFile file = web.GetFile(fileUrl);
      return MoveFileToSubfolder(file, destFolderName);
    }
    public static SPFile MoveFileToSubfolder(this SPFile file, string destFolderName) {
      if (!file.Exists)
        throw new Exception(string.Format("attempted to move a file that does not exist at url {0}.", file.Url));
      SPFolder fileFolder = file.ParentFolder;
      SPFolder destFolder = GetSubFolderIfExists(fileFolder, destFolderName);
      if (destFolder == null) {
        Trace.Write(string.Format("Creating folder {0} in {1}.", destFolderName, fileFolder.Url));
        destFolder = fileFolder.SubFolders.Add(destFolderName);
      }

      Trace.Write(string.Format("Moving file {0} to temp folder {1}.", file.Name, destFolder.Url));
      bool moved = false;
      int attempt = 0;
      string fileName = file.Name;
      while (!moved && (attempt < MAX_RENAMED_FILES || MAX_RENAMED_FILES == 0)) {
        if (attempt > 0) {
          fileName = GenerateCopyFileName(file.Name, attempt);
          Trace.Write(string.Format("Attempting to rename as {0}.", fileName));
        }
        string url = destFolder.Url + "/" + fileName;
        try {
          file.MoveTo(url);
          moved = true;
        } catch (SPException ex) {
          // You might get SPException like this: "A file with the name /Style Library/Temp123/Test.txt already exists. It was last modified by SPDEV\administrator on 04 Apr 2009 19:40:59 -0000."
          if (!ex.Message.Contains("already exists"))
            throw ex;
          attempt++;
        }
      }
      if (!moved)
        throw new Exception(string.Format("Move operation for file at {0} exceeded the maximum number of rename attempts.", file.Url));

      SPFile destFile = destFolder.Files[fileName];
      if (!destFile.Exists)
        throw new Exception(string.Format("File move operation completed but the file did not exist at url {0} after operation.", destFile.Url));
      return destFile;
    }

    /// <summary>
    /// Creates a temp folder and moves a stubborn (locked) file into it,
    /// then deletes the folder. Workaround for SharePoint issue 
    /// http://support.microsoft.com/kb/926812 as solved by 
    /// http://www.novolocus.com/2008/03/05/error-this-item-cannot-be-deleted-because-it-is-still-referenced-by-other-pages/
    /// http://www.katriendg.com/aboutdotnet/2007_1_cannot_delete_page_layout.aspx
    /// </summary>
    /// <param name="web"></param>
    /// <param name="fileUrl"></param>
    /// <param name="file"></param>
    public static void ForceFileDelete(this SPWeb web, string fileUrl) {
      SPFile file = web.GetFile(fileUrl);
      ForceFileDelete(file);
    }
    public static void ForceFileDelete(this SPFile file) {
      string tempFolderName = "Temp" + Guid.NewGuid().ToString();
      SPFile destFile = MoveFileToSubfolder(file, tempFolderName);
      SPFolder destFolder = destFile.ParentFolder;
      Trace.Write(string.Format("Deleting folder {0}.", destFolder.Url));
      destFolder.Delete();
    }

    public static bool FileExists(this SPWeb web, string url) {
      /*
      bool found = true;
      try {
        SPFile file = web.GetFile(url);
        found = file.Exists;
      } catch (Exception ex) {
        Trace.Write("Exception thrown in FileExists.");
        Trace.Write(ex);
        found = false;
      }
       */
      SPFile file = web.GetFile(url);
      bool found = (file != null);
      if (found)
        found = file.Exists;
      Trace.WriteIf(!found, string.Format("File '{0}' not found in web '{1}'.", url, web));
      return found;
    }

      // Got these ideas from:
      // http://dotnet.org.za/zlatan/archive/2007/08/31/document-list-folder-file-manipulation-in-sharepoint-2007.aspx
      // Maybe they aren't very useful to call, but the code conventions could be good to copy/paste
      #region Some File and Folder Manipulation Utilities

      /// <summary>
      /// This lets you iterate through all the documents/files in the list, irrespective of which folder they belong to.
      /// </summary>
      /// <param name="docLibrary"></param>
      public static List<SPFile> GetAllFilesInLibrary(this SPList docLibrary) {
        SPListItemCollection items = docLibrary.Items;
        List<SPFile> files = new List<SPFile>();
        foreach (SPListItem item in items) {
          if (item.File == null)
            throw new Exception("Whoops! This situation should never happen. We must've made a mistake about the SPAPI works!");
          files.Add(item.File);
        }
        return files;
      }

      /// <summary>
      /// This lets you iterate through the top level folders in the list.
      /// </summary>
      /// <param name="docLibrary"></param>
      /// <returns></returns>
      public static List<SPFolder> GetTopLevelFoldersInLibrary(this SPList docLibrary) {
        SPListItemCollection folderItems = docLibrary.Folders;
        List<SPFolder> folders = new List<SPFolder>();
        foreach (SPListItem item in folderItems) {
          if (item.Folder == null)
            throw new Exception("Whoops! This situation should never happen. We must've made a mistake about the SPAPI works!");
          folders.Add(item.Folder);
        }
        return folders;
      }

      /// <summary>
      /// This will give you a list of all objects (docs/files and
      /// folders) within a particular folder.
      /// Note: for items returned, item.File == null for SPFolders
      /// and item.Folder == null for SPFiles.
      /// </summary>
      /// <param name="docLibrary"></param>
      /// <param name="folder"></param>
      /// <param name="includeSubFolders">
      /// Includes files and folders contained in subfolders.
      /// Note: this will be a flat view.
      /// </param>
      /// <returns></returns>
      public static SPListItemCollection GetChildFilesAndFolders(this SPList docLibrary, bool includeSubFolders) {
          return GetChildFilesAndFolders(docLibrary, docLibrary.RootFolder, includeSubFolders);
      }
      public static SPListItemCollection GetChildFilesAndFolders(this SPFolder folder, bool includeSubFolders) {
          return GetChildFilesAndFolders(folder.Item.ParentList, folder, includeSubFolders);
      }
      public static SPListItemCollection GetChildFilesAndFolders(this SPList docLibrary, SPFolder folder, bool includeSubFolders) {
        SPQuery query = new SPQuery();
        query.Query = "<OrderBy><FieldRef Name='ID'/></OrderBy>";
        if (includeSubFolders)
            query.ViewAttributes = "Scope=\"Recursive\"";
        query.Folder = folder;
        SPListItemCollection items = docLibrary.GetItems(query);
        return items;
      }

      /// <summary>
      /// You can use this helper method to quick test for
      /// SPFile, SPFolder, or just a vanilla item.
      /// </summary>
      /// <remarks>
      /// Where you normally use (item.File != null) etc.
      /// use (SPItemType(item) == typeof(SPFile))
      /// </remarks>
      /// <param name="item"></param>
      /// <returns>type SPFile, SPFolder, or SPListItem</returns>
      public static Type ItemType(this SPListItem item) {
          if (item.File != null)
              return typeof(SPFile);
          if (item.Folder != null)
              return typeof(SPFolder);
          return typeof(SPListItem);
      }

      /// <summary>
      /// Gets the top level folder of a item's or folder's ParentFolder
      /// hierarchy. Returns null if the item/folder lives in the root folder.
      /// </summary>
      /// <param name="folder"></param>
      /// <param name="item"></param>
      /// <returns></returns>
      public static SPFolder GetTopLevelFolder(this SPFolder folder) {
          if (folder == null || IsRootFolder(folder))
              return null;
          bool top = false;
          do {
              top = IsTopLevelFolder(folder);
              if (!top)
                  folder = folder.ParentFolder;
          } while (!top && folder != null && !IsRootFolder(folder));
          return folder;              
      }
      public static SPFolder GetTopLevelFolder(this SPListItem item) {
        if (item.Folder != null)
          return GetTopLevelFolder(item.Folder);
        else if (item.File != null)
          return GetTopLevelFolder(item.File.ParentFolder);
        else
          throw new ArgumentException("This method is meant only for file/folder items in document libraries.", "item");
      }

      /// <summary>
      /// Safely determine if a sharepoint folder is a top level folder.
      /// Top level folders are children of the SPList.RootFolder object.
      /// </summary>
      /// <returns></returns>
      public static bool IsTopLevelFolder(this SPListItem item) {
          SPFolder folder = item.Folder;
          if (folder == null)
              return false;
          return IsTopLevelFolder(folder);
      }
      public static bool IsTopLevelFolder(this SPFolder folder) {
          SPFolder foldersParent = folder.ParentFolder;
          // if folder itself has no parent folder, then maybe it's the root 
          // folder, but it sure isn't a top-level folder!
          if (foldersParent == null)
              return false;
          // basically barring all the weird stuff above, if the folder's parent has the same
          // Id as the root folder, then it is under the root folder, so it's a top level folder.
          return IsRootFolder(foldersParent);
      }

      public static bool IsRootFolder(this SPFolder folder) {
          // if this folder doesn't have am Item or ParentList, we have bigger problems
          if (folder.ContainingDocumentLibrary == null || folder.ContainingDocumentLibrary == Guid.Empty)
              throw new ArgumentException("Folder passed to this method should belong to a SharePoint list (1).", "folder");
          SPList parentList = folder.ParentWeb.Lists[folder.ContainingDocumentLibrary];
          //if (folder.Item == null || folder.Item.ParentList == null)
          //    throw new ArgumentException("Folder passed to this method should belong to a SharePoint list.(2)", "folder");
          SPFolder rootFolder = parentList.RootFolder; // folder.Item.ParentList.RootFolder;
          if (rootFolder == null)
              throw new ArgumentException("Folder's parent list does not have a root folder. WTF?!.", "folder");
          return (folder.UniqueId == rootFolder.UniqueId);
      }

      #endregion

  } // class

} // namespace
