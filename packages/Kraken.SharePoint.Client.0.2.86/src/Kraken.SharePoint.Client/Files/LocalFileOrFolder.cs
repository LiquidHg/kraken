using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client.Files {

  public enum LocalFileSystemObjectType {
    Unspecified,
    File,
    Folder
  }

  public class LocalFileOrFolder {

    public LocalFileOrFolder(string path) {
      this.EntryPath = path;
      Refresh();
    }

    public string FolderPath { get; private set; }
    public string FileName { get; private set; }
    public string EntryPath { get; private set; }
    public bool IsFolder { get; private set; }
    public long Size { get; private set; }
    public bool Exists { get; private set; }
    public string Hash { get; set; }

    private DateTime created;
    public DateTime Created {
      get {
        return created;
      }
      private set {
        // This is done to equalize values, because SharePoint strips the miliseconds from datetime values
        created = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second);
      }
    }
    private DateTime modified;
    public DateTime Modified {
      get {
        return modified;
      }
      private set {
        // This is done to equalize values, because SharePoint strips the miliseconds from datetime values
        modified = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second);
      }
    }
    public bool ValidateFullPath() {
      string value = this.EntryPath;
      if (!Path.IsPathRooted(value))
        return false;
      try {
        this.EntryPath = Path.GetFullPath(value);
        Refresh();
      } catch (Exception) {
        // TODO report problem??
        return false;
      }
      return true;
    }

    public System.IO.FileInfo FileInfo {
      get {
        return new System.IO.FileInfo(this.EntryPath);
      }
    }
    public System.IO.DirectoryInfo FolderInfo {
      get {
        return new System.IO.DirectoryInfo(this.FolderPath);
      }
    }

    public void Refresh(string newPath = "") {
      if (!string.IsNullOrEmpty(newPath) && newPath != this.EntryPath)
        this.EntryPath = newPath;
      if (!string.IsNullOrEmpty(this.EntryPath)) {
        string localPath = this.EntryPath;
        this.IsFolder = System.IO.Directory.Exists(localPath);
        this.Exists = (this.IsFolder || System.IO.File.Exists(localPath));
        //if (this.IsFolder) {
        //  System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(localPath);
        //}
        this.FolderPath = (this.IsFolder) ? this.EntryPath : System.IO.Path.GetDirectoryName(localPath); // fi.DirectoryName;
        this.FileName = System.IO.Path.GetFileName(localPath);
        this.Created = System.IO.File.GetCreationTime(localPath);
        this.Modified = System.IO.File.GetLastWriteTime(localPath);
        this.Size = 0;
        if (!this.IsFolder && this.Exists) {
          System.IO.FileInfo fi = new System.IO.FileInfo(localPath);
          this.Size = fi.Length;
        }
        //this.LocalHash
      }
    }

    public void EnsureParentFolder() {
      if (this.Exists)
        return;
      else {
        string folderPath = System.IO.Path.GetDirectoryName(this.EntryPath);
        if (!Directory.Exists(folderPath))
          Directory.CreateDirectory(folderPath);
      }
    }

  }
}
