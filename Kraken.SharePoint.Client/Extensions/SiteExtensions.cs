
namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using Kraken.SharePoint.Client;

  public static class KrakenSiteExtensions {

    // site.GetWebTemplates not implemented in older versions of CSOM
#if !DOTNET_V35
    /// <summary>
    /// Get the web templates available for the site collection
    /// </summary>
    /// <param name="site"></param>
    /// <param name="customTemplates">True for custom templates only, false for OOTB templates only, null for both</param>
    /// <param name="localeCode"></param>
    /// <param name="overrideCompatabilityLevel"></param>
    /// <returns></returns>
    public static List<WebTemplate> GetWebTemplates(this Site site, bool? customTemplates = null, uint localeCode = 1033, int overrideCompatabilityLevel = 0) {
      ClientContext context = (ClientContext)site.Context;
      WebTemplateCollection templates = site.GetWebTemplates(localeCode, overrideCompatabilityLevel);
      context.Load(templates);
      context.ExecuteQuery();
      if (!customTemplates.HasValue)
        return templates.ToList();
      if (customTemplates.Value)
        return templates.Where(t => t.Name.Contains("{")).ToList();
      else
        return templates.Where(t => !t.Name.Contains("{")).ToList();
    }
#endif

  }
}
