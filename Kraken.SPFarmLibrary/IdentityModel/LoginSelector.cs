using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Administration;
using System.Web;
using Microsoft.SharePoint.Utilities;

using Kraken.SharePoint.IdentityModel.Pages;

namespace Kraken.SharePoint.IdentityModel.Controls {

  /// <summary>
  /// We made a copy of the LoginSleector from Microsoft.SharePoint.IdentityModel
  /// so that we can make it work with our own page classes and also extend it.
  /// </summary>
  public class LogonSelector : DropDownList {

    protected override void OnPreRender(EventArgs e) {
      //MultiLogonPage page = this.Page as MultiLogonPage;
      AutoRealmDiscoPageBase page = this.Page as AutoRealmDiscoPageBase;
      if (page != null) {
        SPIisSettings iisSettings = page.IisSettings;
        this.AutoPostBack = true;
        this.Items.Add(new ListItem("", "none"));

        Dictionary<string, string> providers = iisSettings.GetClaimsAuthenticationProviderNameList();
        foreach (string name in providers.Keys) {
          this.Items.Add(new ListItem(providers[name], name));
        }
        if (this.Items.Count == 1) {
          base.Style.Add("display", "none");
        }
      }
    }

    /// <summary>
    /// This event handler is provided to give inheriting 
    /// classes a change to do something when the authentication
    /// provider has been picked, but before redirect to the 
    /// log page occurs.
    /// </summary>
    public EventHandler RealmSelected;
    protected virtual void OnRealmSelected(EventArgs e) {
      if (RealmSelected != null)
        RealmSelected(this, e);
    }

    SPAuthenticationProvider provider;
    public SPAuthenticationProvider SelectedProvider {
      get {
        return provider;
      }
    }

    protected override void OnSelectedIndexChanged(EventArgs e) {
      //MultiLogonPage page = this.Page as MultiLogonPage;
      AutoRealmDiscoPageBase page = this.Page as AutoRealmDiscoPageBase;
      if (page != null) {
        SPIisSettings iisSettings = page.IisSettings;
        string selectedValue = this.SelectedValue;
        if (!string.IsNullOrEmpty(selectedValue) && ((provider = iisSettings.GetClaimsAuthenticationProvider(selectedValue, AuthenticationProviderSearchProperty.Name)) != null)) {
          OnRealmSelected(new EventArgs());
          provider.RedirectToLoginPage(this.Context);
        }
      }
    }

  }

}


