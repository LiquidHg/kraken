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
  }
}
