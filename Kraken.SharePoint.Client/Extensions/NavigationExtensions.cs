/* Older versions of CSOM did not include this API */
#if !DOTNET_V35

namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Kraken.Tracing;
  using Kraken.SharePoint.Client;

  using Microsoft.SharePoint.Client.Publishing;
  using Microsoft.SharePoint.Client.Publishing.Navigation;
  using Microsoft.SharePoint.Client.Taxonomy;

  public static class KrakenNavigationExtensions {

    // credits to:
    // http://johnjayaseelan.blogspot.com/2013_03_01_archive.html
    // http://bexgordon.com/?p=11
    // https://social.technet.microsoft.com/Forums/scriptcenter/en-US/7b0d69f0-a3d9-4021-b770-e0d0279d5e5b/manipulate-show-pages-on-navigation-setting-using-csom?forum=sharepointgeneral

    public static void SetNavigation(
      this Web web,
      NavigationProperties globalProperties,
      NavigationProperties currentProperties
    ) {
      if (globalProperties == null && currentProperties == null)
        return;
      ClientContext context = (ClientContext)web.Context;
      TaxonomySession taxonomySession = TaxonomySession.GetTaxonomySession(context);
      WebNavigationSettings settings = new WebNavigationSettings(context, web);
      if (globalProperties != null)
        SetGlobalNavigation(web, settings, taxonomySession, globalProperties);
      if (currentProperties != null)
        SetQuickLaunchNavigation(web, settings, taxonomySession, currentProperties);
      context.ExecuteQuery();
    }

    // TODO __NavigationShowSiblings

    // __IncludeSubSitesInNavigation shouldn't be needed
    // __IncludePagesInNavigation shouldn't be needed

    public static void SetQuickLaunchNavigation(this Web web, NavigationProperties currentProperties) {
      ClientContext context = (ClientContext)web.Context;
      TaxonomySession taxonomySession = TaxonomySession.GetTaxonomySession(context);
      WebNavigationSettings settings = new WebNavigationSettings(context, web);
      SetQuickLaunchNavigation(web, settings, taxonomySession, currentProperties);
      context.ExecuteQuery();
    }
    private static void SetQuickLaunchNavigation(this Web web, WebNavigationSettings settings, TaxonomySession taxonomySession, NavigationProperties currentProperties) {
      settings.CurrentNavigation.Source = currentProperties.Source;
      // __InheritCurrentNavigation is taken care of by changing Source and applying update
      settings.Update(taxonomySession);
      web.AllProperties["__CurrentNavigationIncludeTypes"] = (int)currentProperties.IncludeTypes;
      if (currentProperties.DynamicChildLimit >= 0)
        web.AllProperties["__CurrentDynamicChildLimit"] = currentProperties.DynamicChildLimit;
      // TODO __CurrentNavigationExcludes
      web.Update();
    }

    public static void SetGlobalNavigation(this Web web, NavigationProperties globalProperties) {
      ClientContext context = (ClientContext)web.Context;
      TaxonomySession taxonomySession = TaxonomySession.GetTaxonomySession(context);
      WebNavigationSettings settings = new WebNavigationSettings(context, web);
      SetGlobalNavigation(web, settings, taxonomySession, globalProperties);
      context.ExecuteQuery();
    }
    private static void SetGlobalNavigation(Web web, WebNavigationSettings settings, TaxonomySession taxonomySession, NavigationProperties globalProperties) {
      settings.GlobalNavigation.Source = globalProperties.Source;
      // __InheritCurrentNavigation is taken care of by changing Source and applying update
      settings.Update(taxonomySession);
      web.AllProperties["__GlobalNavigationIncludeTypes"] = (int)globalProperties.IncludeTypes;
      if (globalProperties.DynamicChildLimit >= 0)
        web.AllProperties["__GlobalDynamicChildLimit"] = globalProperties.DynamicChildLimit;
      // TODO __GlobalNavigationExcludes
      web.Update();
    }

    /// <summary>
    /// Gets an editable term set and supporting objects for 
    /// working with friendly URLs and Managed Navigation term sets.
    /// </summary>
    /// <param name="web">A Web with managed navigation configured</param>
    /// <param name="termSet">Outputs the retrieved term set</param>
    /// <param name="taxSession">Outputs taxonomy session</param>
    /// <param name="providerName">A value from StandardNavigationProviderNames</param>
    /// <returns>The editable term set</returns>
    /// <remarks>
    /// NOTE: this method is NOT efficient, but when using globalnavTaxTermSet with the FindTermForUrl() method, 
    ///  it always fails due to the fact that somehow the term set has no child nodes in it...
    ///  so for now we do this roundabout method of creating a new context and re-retrieving the "editable" term set.
    /// </remarks>
    public static NavigationTermSet GetEditableNavigationTermSet(
      this Web web,
      out NavigationTermSet termSet,
      out TaxonomySession taxSession,
      string providerName = StandardNavigationProviderNames.GlobalNavigationTaxonomyProvider
    ) {
      if (web == null)
        throw new ArgumentNullException("web");
      if (string.IsNullOrEmpty(providerName))
        throw new ArgumentNullException("providerName");
      // TODO test providerName fpr valid values
      ClientContext context = (ClientContext)web.Context;
      taxSession = null;

      // grab the managed navigation term set configured for the current web 
      // (if it's not properly configured, this will fail)
      termSet = TaxonomyNavigation.GetTermSetForWeb(context, web, providerName, true);
      if (termSet == null)
        return null;

      // NOTE: this method is NOT efficient, but when I use globalnavTaxTermSet with the FindTermForUrl() method, 
      //  it always fails due to the fact that somehow the term set has no child nodes in it...
      //  so for now we do this roundabout method of creating a new context and re-retrieving the "editable" term set.
      taxSession = TaxonomySession.GetTaxonomySession(context); //new TaxonomySession(web.Site, true);
      context.Load(taxSession);
      context.ExecuteQuery();
      if (taxSession == null)
        return null;

      NavigationTermSet editableNavTs = termSet.GetAsEditable(taxSession);
      TermSet taxTsFromNavTs = editableNavTs.GetTaxonomyTermSet();
      NavigationTermSet resolvedNavTs = NavigationTermSet.GetAsResolvedByWeb(context, taxTsFromNavTs, web, providerName);
      
      context.Load(editableNavTs);
      context.Load(taxTsFromNavTs);
      context.Load(resolvedNavTs);
      context.ExecuteQuery();
      return resolvedNavTs;
    }

    public static bool CreateFriendlyUrl(ListItem item, out string newFriendlyUrl, string itemTitle, Uri parentTermFriendlyUrl, ITrace trace) {
      if (item == null)
        throw new ArgumentNullException("item");
      if (parentTermFriendlyUrl.IsAbsoluteUri)
        throw new ArgumentException("You must specify a server-relative Uri", "parentTermFriendlyUrl");
      ClientContext context = (ClientContext)item.Context;
      Web web = context.Web;
      newFriendlyUrl = string.Empty;
      try {
        TaxonomySession taxSession;
        NavigationTermSet globalNavTaxTermSet;
        NavigationTermSet resolvedNavTs = web.GetEditableNavigationTermSet(
          out globalNavTaxTermSet,
          out taxSession
        );
        if (globalNavTaxTermSet == null || resolvedNavTs == null) {
          trace.TraceWarning("Couldn't create navigation term set or editable navigation term set from web '{0}'. Can't continue. ", web.UrlSafeFor2010());
          return false;
        }

        // retrieve the parent navigation term
        NavigationTerm parentTerm = resolvedNavTs.FindTermForUrl(parentTermFriendlyUrl.ToString());
        context.Load(parentTerm);
        ClientResult<string> parentResult = parentTerm.GetResolvedDisplayUrl(string.Empty);
        context.ExecuteQuery();
        if (parentTerm == null) {
          trace.TraceWarning("Parent term with web relative  url '{0}' not found. Can't add child term. ", parentTermFriendlyUrl);
          return false;
        } else {
          trace.TraceVerbose("Parent term with url '{0}' found. ", (parentResult != null) ? parentResult.Value : string.Empty);
        }

        //PublishingWeb publishingWeb = PublishingWeb.GetPublishingWeb(context, web);  
        PublishingPage page = PublishingPage.GetPublishingPage(context, item);
        if (page == null) {
          trace.TraceWarning("Couldn't get publishing page. Can't continue. ");
          return false;
        }
        ClientResult<string> asyncResult = page.AddFriendlyUrl(itemTitle, parentTerm, true);
        context.ExecuteQuery();
        if (asyncResult != null) {
          string result = asyncResult.Value;
          trace.TraceVerbose("result = '{0}'", result);
          newFriendlyUrl = result;
        }
        return true;
      } catch (Exception ex) {
        trace.TraceError(ex);
        //newFriendlyUrl = null;
        //termId = Guid.Empty;
        trace.TraceVerbose("Error. Returning fail. ");
        return false;
      }
      // unfortunately there is no such animal in CSOM
      //Guid listId = PublishingWeb.GetPagesListId(web);
    }

    /// <summary>
    /// Goes into the taxonomy and creates & associates a navigation term with the specified information
    /// </summary>
    /// <param name="web">A Web with managed navigation configured</param>
    /// <param name="newFriendlyUrl">Outputs the friendly URL created/edited</param>
    /// <param name="termId">Outputs the ID of the created/edited term</param>
    /// <param name="itemTitle">Value to use as the final URL fragment. Spaces will be replaced with dashes.</param>
    /// <param name="parentTermFriendlyUrl">This must be a server-relative URL</param>
    /// <param name="existingFriendlyUrl">If provided, attempts to find existing term using its URL; This must be a server-relative URL</param>
    /// <param name="trace">Trace provider for logging purposes</param>
    /// <returns>True if success; false if failed</returns>
    public static bool CreateOrUpdateFriendlyUrl(this Web web, out Uri newFriendlyUrl, out Guid termId, string itemTitle, Uri parentTermFriendlyUrl, Uri existingFriendlyUrl = null, ITrace trace = null) {
      if (web == null)
        throw new ArgumentNullException("web");
      if (parentTermFriendlyUrl == null)
        throw new ArgumentNullException("parentTermFriendlyUrl");
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)web.Context;

      if (existingFriendlyUrl != null && existingFriendlyUrl.IsAbsoluteUri)
        throw new ArgumentException("You must specify a server-relative Uri", "existingFriendlyUrl");
      if (parentTermFriendlyUrl.IsAbsoluteUri)
        throw new ArgumentException("You must specify a server-relative Uri", "parentTermFriendlyUrl");
      newFriendlyUrl = null;
      termId = Guid.Empty;
      try {
        TaxonomySession taxSession;
        NavigationTermSet globalNavTaxTermSet;
        NavigationTermSet resolvedNavTs = web.GetEditableNavigationTermSet(
          out globalNavTaxTermSet,
          out taxSession
        );
        if (globalNavTaxTermSet == null || resolvedNavTs == null) {
          trace.TraceWarning("Couldn't create navigation term set or editable navigation term set from web '{0}'. Can't continue. ", web.UrlSafeFor2010());
          return false;
        }

        // retrieve the parent navigation term
        NavigationTerm parentTerm = resolvedNavTs.FindTermForUrl(parentTermFriendlyUrl.ToString());
        context.Load(parentTerm);
        ClientResult<string> parentResult = parentTerm.GetResolvedDisplayUrl(string.Empty);
        context.ExecuteQuery();
        if (parentTerm == null) {
          trace.TraceWarning("Parent term with web relative  url '{0}' not found. Can't add child term. ", parentTermFriendlyUrl);
          return false;
        } else {
          trace.TraceVerbose("Parent term with url '{0}' found. ", (parentResult != null) ? parentResult.Value : string.Empty);
        }

        // either create or modify the navigation term underneath the parent
        NavigationTerm newOrExistingTerm = null;
        if (existingFriendlyUrl != null) {
          trace.TraceVerbose("Attempt to find existing term with url '{0}'", existingFriendlyUrl);
          // term has already been created, find it!
          newOrExistingTerm = globalNavTaxTermSet.FindTermForUrl(existingFriendlyUrl.ToString());
          context.Load(newOrExistingTerm);
          context.ExecuteQuery();
        }
        if (newOrExistingTerm == null) {
          trace.TraceVerbose("No existing term provided or found; creating a new one. ");
          // term doesn't exist, create a new one (physical url inherited from parent term)
          termId = Guid.NewGuid();
          trace.TraceVerbose("Creating a new friendly URL term with ID='{0}' title='{1}'", termId, itemTitle);
          newOrExistingTerm = parentTerm.CreateTerm(itemTitle, NavigationLinkType.FriendlyUrl, termId);
          parentTerm.GetTaxonomyTerm().TermStore.CommitAll();
          context.Load(newOrExistingTerm, t => t.Id);
          //context.ExecuteQuery();
          ClientResult<string> asyncResult = newOrExistingTerm.GetResolvedDisplayUrl(string.Empty); //.GetWebRelativeFriendlyUrl();
          //context.Load(asyncResult);
          context.ExecuteQuery();
          string result = asyncResult.Value;
          trace.TraceVerbose("result = '{0}'", result);
          if (!string.IsNullOrEmpty(result))
            Uri.TryCreate(result, UriKind.RelativeOrAbsolute, out newFriendlyUrl);
          termId = newOrExistingTerm.Id; // this should match newFriendlyUrlTermId above
          trace.TraceVerbose("Created new friendly URL term with ID='{0}' url='{1}'", termId, newFriendlyUrl);
        } else {
          trace.TraceVerbose("Existing term found; modifying properties. ");
          // term already exists, simply modify the title
          NavigationTerm editableNavTerm = newOrExistingTerm.GetAsEditable(taxSession);
          itemTitle = itemTitle.Replace(' ', '-');
          trace.TraceVerbose("modifying itemTitle='{0}'", itemTitle);
          editableNavTerm.FriendlyUrlSegment.Value = itemTitle;
          Term taxTerm = editableNavTerm.GetTaxonomyTerm();
          context.Load(taxTerm);
          taxTerm.Name = itemTitle;
          taxTerm.TermStore.CommitAll();
          context.Load(editableNavTerm, t => t.Id);
          ClientResult<string> asyncResult = editableNavTerm.GetResolvedDisplayUrl(string.Empty); //..GetWebRelativeFriendlyUrl();
          context.ExecuteQuery();
          string result = asyncResult.Value;
          trace.TraceVerbose("result = '{0}'", result);
          if (!string.IsNullOrEmpty(result))
            Uri.TryCreate(result, UriKind.RelativeOrAbsolute, out newFriendlyUrl);
          termId = editableNavTerm.Id;
          trace.TraceVerbose("Updated friendly URL term with ID='{0}' url='{1}'", termId, newFriendlyUrl);
        }
        trace.TraceVerbose("Done. Returning success. ");
        return true;
      } catch (Exception ex) {
        trace.TraceError(ex);
        //newFriendlyUrl = null;
        //termId = Guid.Empty;
        trace.TraceVerbose("Error. Returning fail. ");
        return false;
      }
    }

  }
}
#endif