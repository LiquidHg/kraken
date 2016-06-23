using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client {

  public enum UpdateItemResult {
    NoResult = 0,
    UpdateOK = 1,
    UpdatePartialFail = 2,
    UpdateFail = 3
  }

  public class UpdateItemOptions {

    public UpdateItemOptions() {
      UpdateFrequency = ItemUpdateFrequency.OncePerItem;
      TitleInternalFieldName = string.Empty;
      SkipTitleOnUpdate = false;
      SkipContentTypeIdOnUpdate = false;
      HtmlEncodeText = true;
      ResolveLookups = true;
      ResolveContentTypes = false;
      ThrowOnError = false;
      PreserveModifiedDate = false;
      SupressSkippedFieldWarnings = false;
    }

    public string TitleInternalFieldName { get; set; }
    public ItemUpdateFrequency UpdateFrequency { get; set; }

    /// <summary>
    /// When true, operations that affect the properties of a list item will 
    /// make their best effort to leave the modified date and person unchanged.
    /// </summary>
    public bool PreserveModifiedDate { get; set; }

    public bool SupressSkippedFieldWarnings { get; set; }

    public bool HtmlEncodeText { get; set; }

    //public bool IgnoreIDField { get; set; }

    public bool ResolveLookups { get; set; }
    public bool ResolveContentTypes { get; set; }

    public bool ThrowOnError { get; set; }

    /// <summary>
    /// Used by the system; when true, title is set
    /// at the time an item is initially created, then
    /// skipped when additional properties are updated.
    /// For updates, this should always be false.
    /// Otherwise Title will not be set.
    /// </summary>
    internal bool SkipTitleOnUpdate { get; set; }

    internal bool SkipContentTypeIdOnUpdate { get; set; }

    public void EnsureDefaultValues(bool isDocumentLibrary) {
      if (string.IsNullOrEmpty(this.TitleInternalFieldName)) {
        if (isDocumentLibrary)
          this.TitleInternalFieldName = "FileLeafRef";
        else
          this.TitleInternalFieldName = "Title";
      }
    }

    public int UpdateFrequencyAsNumber {
      get {
        switch (this.UpdateFrequency) {
          case ItemUpdateFrequency.OncePerItem:
            return 1;
          case ItemUpdateFrequency.Every10Items:
            return 10;
          case ItemUpdateFrequency.Every25Items:
            return 25;
          case ItemUpdateFrequency.Every50Items:
            return 50;
        }
        return 1;
      }
    }

  }

  public class CreateItemOptions : UpdateItemOptions {
    public CreateItemOptions() : base() {
      IgnoreIDField = true;
      // has no significant effect on new items, but if updating at every field it can be important
      PreserveModifiedDate = true;
      SkipTitleOnUpdate = true;
      SkipContentTypeIdOnUpdate = true;
    }

    public bool IgnoreIDField { get; set; }

  }
}
