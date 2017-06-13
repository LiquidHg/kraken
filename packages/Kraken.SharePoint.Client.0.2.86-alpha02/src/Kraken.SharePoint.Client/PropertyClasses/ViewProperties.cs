using Kraken.Tracing;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client {
  /* In older versions of CSOM some classes are sealed
   * which makes life difficult for us, but we'll have to make-do.
   */
#if !DOTNET_V35
  public class ViewProperties : ViewCreationInformation {
#else
  public class ViewProperties {

    public bool Paged { get; set; }
    public bool PersonalView { get; set; }
    public string Query { get; set; }
    public uint RowLimit { get; set; }
    public bool SetAsDefaultView { get; set; }
    public string Title { get; set; }
    string[] ViewFields { get; set; }
    public ViewType ViewTypeKind { get; set; }

#endif

    /// <summary>
    /// The InnerXml of a CAML Query tag
    /// </summary>
    /// <remarks>
    /// Query property should be in the following format:
    ///   <WHERE></WHERE><ORDERBY></ORDERBY>
    /// Not this:
    ///   <VIEW><QUERY><WHERE></WHERE><ORDERBY></ORDERBY></QUERY></VIEW>
    /// </remarks>
    new public string Query {
      get {
        return base.Query;
      }
      set {
        base.Query = value;
      }
    }

    public const string SKIP_PROPERTY = "[SKIP_PROPERTY]";

    public string JSLink { get; set; }
    public bool? TabularView { get; set; }

    public bool HasExtendedSettings {
      get {
        return (TabularView.HasValue
          || JSLink != SKIP_PROPERTY);
      }
    }

    public bool Validate(ITrace trace = null) {
      bool isValid = true;
      if (this.RowLimit < 0 && this.RowLimit >= 5000) {
        trace.TraceWarning("Invalid RowLimit = {0}; must be between 0 and 5000 (exclusive)", this.RowLimit);
        isValid = false;
      }
      if (string.IsNullOrWhiteSpace(this.Title)) {
        trace.TraceWarning("Title must have a value");
        isValid = false;
      }
      if (string.IsNullOrWhiteSpace(this.Query)) {
        trace.TraceWarning("Query must have a value");
        isValid = false;
      }
      return isValid;
    }

    public void CopyFrom(View view) {
      this.RowLimit = view.RowLimit;
      this.Paged = view.Paged;
      // This one can't be set, but we can hold it as a property
      this.PersonalView = view.PersonalView;
      this.Query = view.ViewQuery;
      this.Title = view.Title;
      // this is left as null unless provided
      //this.ViewFields = view.ViewFields;
      //this.ViewTypeKind
    }

    public ViewCreationInformation ConvertSP14Safe() {
      return new ViewCreationInformation() {
        Paged = this.Paged,
        PersonalView = this.PersonalView,
        Query = this.Query,
        RowLimit = this.RowLimit,
        SetAsDefaultView = this.SetAsDefaultView,
        Title = this.Title,
        ViewFields = this.ViewFields,
        ViewTypeKind = this.ViewTypeKind
      };
    }

  } // class
}
