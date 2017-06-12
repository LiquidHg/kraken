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
  public class WebProperties : WebCreationInformation {
#else
  public class WebProperties {

    public string Description { get; set; }
    public int Language { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public bool UseSamePermissionsAsParentSite { get; set; }
    public string WebTemplate { get; set; }

#endif

    public WebCreationInformation ConvertSP14Safe() {
      return new WebCreationInformation() {
        Description = this.Description,
        Language = this.Language,
        Title = this.Title,
        Url = this.Url,
        UseSamePermissionsAsParentSite = this.UseSamePermissionsAsParentSite,
        WebTemplate = this.WebTemplate
      };
    }

  }

}
