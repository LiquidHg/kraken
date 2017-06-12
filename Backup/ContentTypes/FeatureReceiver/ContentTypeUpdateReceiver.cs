
namespace Kraken.SharePoint.ContentTypes {

  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Diagnostics.CodeAnalysis;
  using System.Text;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Utilities;

  using Kraken.Configuration;
  using Kraken.SharePoint.Logging;

  /// <summary>
  /// This receiver updates a content type or site columns using
  /// the element file, but also ensures that the API is called so
  /// that the type is "unghosted" and the database will have the
  /// same definition as the Xml in the feature folder.
  /// </summary>
  /// <remarks>
  /// ReceiverAssembly="Behemoth.SharePoint.ARK, Version=1.?.?.?, Culture=neutral, PublicKeyToken=ecab56691def8148"
  /// ReceiverClass="Behemoth.SharePoint.ContentTypes.ContentTypeUpdateReceiver"
  /// </remarks>
  public class ContentTypeUpdateReceiver : SPFeatureReceiver {

    private KrakenLoggingService uls = KrakenLoggingService.CreateNew(LoggingCategories.KrakenContentTypes);

    private ContentTypeUpdateProperties recvProps;
    private ContentTypeUpdateProperties TypedProperties {
      get { return recvProps; }
    }

    private void EnsureProperties(SPFeatureReceiverProperties properties) {
      uls.Write("Invoking EnsureProperties.");
      if (recvProps == null)
        recvProps = new ContentTypeUpdateProperties(properties);
      else if (recvProps.InitStatus != ConfigurationReaderStatus.Initialized)
        throw new Exception("property reader is not initialized. Can't continue.");
      uls.Write("Leaving EnsureProperties.");
    }

    [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage", Justification = "Use of SPContentTypeFeatureTools is provided for backward compatibility, and appropriate docuemntation has been added to the caller.")]
    protected void RefreshListContentTypes(object web, ListContentTypeRefreshEventArgs args) {
      args.ForceUpdate = this.TypedProperties.ListContentTypeForceUpdate;
      args.RecurseSubWebs = this.TypedProperties.ListContentTypeRecurseSubWebs;
      args.UseTimerJob = this.TypedProperties.ListContentTypeUseTimerJob;
      args.UpdateFields = true; // TODO make configurable?
      args.RemoveFields = false; // TODO make configurable?
#if LegacyXmlContentTypeFunctions
            if (this.TypedProperties.UseXmlLinqMethods)
                SPContentTypeFeatureToolsX.DoRefreshListContentTypes(web, args);
            else
                SPContentTypeFeatureTools.DoRefreshListContentTypes(web, args);
#else
      SPContentTypeManager.DoRefreshListContentTypes(web, args);
#endif
    }

    [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage", Justification = "Use of SPContentTypeFeatureTools is provided for backward compatibility, and appropriate docuemntation has been added to the caller.")]
    public override void FeatureActivated(SPFeatureReceiverProperties properties) {
      EnsureProperties(properties);
      SPContentTypeManager mgr = new SPContentTypeManager(this.TypedProperties.Site.RootWeb);
      if (this.TypedProperties.CreateSiteColumnsByWebService) {
        uls.Write("Entering EnsureSiteColumns.");
#if LegacyXmlContentTypeFunctions
                if (this.TypedProperties.UseXmlLinqMethods) {
                    SPContentTypeFeatureToolsX.EnsureSiteColumns(this.TypedProperties.Site.RootWeb, this.TypedProperties.SiteColumnElementFilePath);
                } else {
                    SPContentTypeFeatureTools.EnsureSiteColumns(this.TypedProperties.Site.RootWeb, this.TypedProperties.SiteColumnElementFilePath);
                }
#else
        mgr.EnsureSiteColumns(this.TypedProperties.SiteColumnElementFilePath);
#endif
        uls.Write("Leaving EnsureSiteColumns.");
      }
      if (this.TypedProperties.UpdateContentTypesByWebService) {
        uls.Write("Entering EnsureContentTypes.");
#if LegacyXmlContentTypeFunctions
                    if (this.TypedProperties.UseXmlLinqMethods) {
                        SPContentTypeFeatureToolsX.RemoveAllRefreshListContentTypes();
                        if (this.TypedProperties.EnableRefreshListContentTypes) {
                            SPContentTypeFeatureToolsX.RefreshListContentTypes += new ListContentTypeRefreshEventHandler(RefreshListContentTypes);
                        }                
                        SPContentTypeFeatureToolsX.EnsureContentTypes(this.TypedProperties.Site.RootWeb, this.TypedProperties.ContentTypeElementFilePath);
                    } else {
                        SPContentTypeFeatureTools.RemoveAllRefreshListContentTypes();
                        if (this.TypedProperties.EnableRefreshListContentTypes) {
                            SPContentTypeFeatureTools.RefreshListContentTypes += new ListContentTypeRefreshEventHandler(RefreshListContentTypes);
                        }
                        SPContentTypeFeatureTools.EnsureContentTypes(this.TypedProperties.Site.RootWeb, this.TypedProperties.ContentTypeElementFilePath);
                    }
#else
        mgr.RemoveAllRefreshListContentTypes();
        if (this.TypedProperties.EnableRefreshListContentTypes) {
          mgr.RefreshListContentTypes += new ListContentTypeRefreshEventHandler(RefreshListContentTypes);
        }
        mgr.EnsureContentTypes(this.TypedProperties.ContentTypeElementFilePath);
#endif
        uls.Write("Leaving EnsureContentTypes.");
      }
    }

    public override void FeatureDeactivating(SPFeatureReceiverProperties properties) {
      //EnsureProperties(properties);
      //throw new Exception("The method or operation is not implemented.");
    }

    public override void FeatureInstalled(SPFeatureReceiverProperties properties) {
      //EnsureProperties(properties);
      //throw new Exception("The method or operation is not implemented.");
    }

    public override void FeatureUninstalling(SPFeatureReceiverProperties properties) {
      //EnsureProperties(properties);
      //throw new Exception("The method or operation is not implemented.");
    }

  } // class
} // namespace
