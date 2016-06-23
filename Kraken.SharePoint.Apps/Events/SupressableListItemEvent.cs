namespace Kraken.SharePoint.Apps {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Web;

  using Microsoft.SharePoint.Client;
  using Microsoft.SharePoint.Client.EventReceivers;
  using Kraken.Apps.Models;

  public class SupressableListItemEvent {

    protected static DatabaseTrace Log = new DatabaseTrace();

    public virtual void SetLogContext(string appName, SPRemoteItemEventProperties properties) {
      // TODO add something for the user's context
      // TODO rapid fire events will scew this up pretty well
      Log.SetSessionContext(appName, this.GetWebPropertyName(properties));
    }
    public virtual void SetLogContext(string appName, SPRemoteAppEventProperties properties) {
      // TODO user session possible here??
      Log.SetSessionContext(appName, properties.AppWebFullUrl.ToString());
    }

    public virtual void SetLogContext(string appName, SPRemoteEventProperties properties) {
      if (properties.AppEventProperties != null)
        SetLogContext(appName, properties.AppEventProperties);
      else if (properties.ItemEventProperties != null)
        SetLogContext(appName, properties.ItemEventProperties);
    }

    /*
    private bool _EventFiringEnabled = false;
    public bool EventFiringEnabled {
      get {
        _EventFiringEnabled;
      }
    }
     */

    protected string GetWebPropertyName(SPRemoteItemEventProperties properties) {
      string propertyName = "RemoteListItemEvent_" + properties.ListId + "_" + properties.ListItemId;
      return propertyName;
    }
    /*
    protected string GetWebPropertyName(SPRemoteAppEventProperties properties) {
      string propertyName = "RemoteListItemEvent_" + properties.AppWebFullUrl;
      return propertyName;
    }
     */

    protected void DisableRemoteListItemEvents(Web web, SPRemoteItemEventProperties properties) {
      // the property name must match the list guid and the item id
      string propertyName = GetWebPropertyName(properties);
      SetProperty(web, propertyName, DateTime.Now);
    }
    protected void EnableRemoteListItemEvents(Web web, SPRemoteItemEventProperties properties) {
      // the property name must match the list guid and the item id
      string propertyName = GetWebPropertyName(properties);
      SetProperty(web, propertyName, null);
    }
    protected bool CheckEventFiringEnabled(Web web, SPRemoteItemEventProperties properties) {
      // the property name must match the list guid and the item id
      string propertyName = GetWebPropertyName(properties);
      // we have nothing like this in RER so we have to rely on HasFieldChanged with pretty tight logic to prevent circular class
      //EventFiringEnabled = false;
      object eventFiredAt = GetProperty(web, propertyName);
      if (null != eventFiredAt) {
        // if > 0 we are still in the 1 minute cooling off period and should not fire again
        // anything else and probably the damned event crashed or got stuck or somthing
        if (DateTime.Now.AddMinutes(-1).CompareTo((DateTime)eventFiredAt) > 0) {
          return false; //_EventFiringEnabled = false;
        }
      }
      return true;
      //_EventFiringEnabled = true;
      //return _EventFiringEnabled;
    }

    protected object GetProperty(Web web, string propertyName) {
      //Web web = clientContext.Site.RootWeb;
      ClientContext clientContext = (ClientContext)web.Context;
      clientContext.Load(web, w => w.AllProperties);
      clientContext.ExecuteQuery();
      if (!web.AllProperties.FieldValues.ContainsKey(propertyName)) {
        return 0;
      } else {
        return web.AllProperties[propertyName];
      }
    }

    protected void SetProperty(Web web, string propertyName, object value) {
      //Web web = clientContext.Site.RootWeb;
      ClientContext clientContext = (ClientContext)web.Context;
      /* Add successfully, but not persistantly. Cannot find this new property when retrieve property bag
      clientContext.Load(web, w=>web.AppProperties);
      clientContext.ExecuteQuery();
      if (!web.AllProperties.FieldValues.ContainsKey("Customized"))
         web.AllProperties.FieldValues.Add("Customizedag);
      else
         web.AllProperties["Customized"] = flag;
      */
      // Correct Approach
      var allProperties = web.AllProperties;
      allProperties[propertyName] = value;
      web.Update();
      clientContext.ExecuteQuery();
    }

  } // class

}