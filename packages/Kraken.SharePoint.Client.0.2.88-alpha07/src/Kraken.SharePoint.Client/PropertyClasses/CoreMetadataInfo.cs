using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Kraken.SharePoint.Client.Helpers;
using Kraken.Tracing;

namespace Kraken.SharePoint.Client {

  public class CoreMetadataInfo {

    public static string DefaultLocalFilePathFieldName { get; set; }

    private ITrace Trace { get; set; }

    public CoreMetadataInfo(ITrace trace = null) {
      if (trace == null) trace = NullTrace.Default;
      this.Trace = trace;
      if (string.IsNullOrEmpty(DefaultLocalFilePathFieldName))
        DefaultLocalFilePathFieldName = "MetadataSourceURL";
    }
    public CoreMetadataInfo(ListItem item, ITrace trace = null)
      : this(trace) {
      //TODO figure out why this is killing the app
      //this.Created = (DateTime)item["Created"];
      //this.Modified = (DateTime)item["Modified"];
    }
    public CoreMetadataInfo(ListItem item, List list, bool ensureLocalFilePathField, ITrace trace)
      : this(item, trace) {
      if (ensureLocalFilePathField)
        EnsureLocalFilePathField(list);
    }
    public CoreMetadataInfo(ListItem item, bool ensureLocalFilePathField, ITrace trace)
      : this(item, trace) {
      // TODO might need to do a context.Load(item.ParentList) here
      if (ensureLocalFilePathField)
        EnsureLocalFilePathField(item.ParentList);
    }

    public CoreMetadataInfo(string localFilePath, List list, bool ensureLocalFilePathField, ITrace trace = null)
      : this(localFilePath, trace) {
      if (ensureLocalFilePathField)
        EnsureLocalFilePathField(list);
    }

    public CoreMetadataInfo(string localFilePath, ITrace trace) : this(trace) {
      InitFromLocalFile(localFilePath);
    }

    internal bool EnsureLocalFilePathField(List list) {
      if (list == null) {
        Trace.TraceWarning("List was not specified. Cannot check for Source Metadata field. Information about the orginal location of files prior to upload will not be saved.");
        return false;
      } else {
        Field field = list.GetField(DefaultLocalFilePathFieldName, true);
        if (field == null) {
          Trace.TraceWarning("Source Metadata field '{0}' was not present in list. Trying to add it. If you do not have permissions, this operation will fail and information about the orginal location of files prior to upload may be lost.", DefaultLocalFilePathFieldName);
          try {
            field = list.EnsureField(DefaultLocalFilePathFieldName, "Text");
          } catch (Exception ex) {
            Trace.TraceError(ex);
          }
        }
        LocalFilePathFieldExists = (field != null);
        return (field != null);
      }
    }

    public DateTime Modified = DateTime.Now;
    public DateTime Created = DateTime.Now;
    public string ModifiedBy = string.Empty;
    public string CreatedBy = string.Empty;
    public string LocalFilePathFieldName = string.Empty;
    public bool LocalFilePathFieldExists = false;
    public string LocalFilePath = string.Empty;

    private void InitFromLocalFile(string localFilePath) {
      if (!string.IsNullOrEmpty(localFilePath)) {
        this.Created = System.IO.File.GetCreationTime(localFilePath);
        this.Modified = System.IO.File.GetLastWriteTime(localFilePath);
        this.LocalFilePathFieldName = DefaultLocalFilePathFieldName;
        this.LocalFilePath = localFilePath;
      }
    }

    public void SetListItemMetadata(ListItem item) {
      item["Created"] = this.Created;
      item["Modified"] = this.Modified;
      if (this.LocalFilePathFieldExists && !string.IsNullOrEmpty(LocalFilePathFieldName) && !string.IsNullOrEmpty(LocalFilePath))
        item[LocalFilePathFieldName] = this.LocalFilePath;
      /*
      item["Author"] = 66;
      item["Editor"] = 67;
       */
    }

  }

}
