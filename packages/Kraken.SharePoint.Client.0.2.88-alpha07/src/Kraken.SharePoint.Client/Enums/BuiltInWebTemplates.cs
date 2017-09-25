using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client {
  public class BuiltInWebTemplates : Dictionary<string, string> {

    public string Global { get { return this["GLOBAL#0"]; } }
    public string TeamSite { get { return this["STS#0"]; } }
    public string TeamSiteSPOConfig { get { return this["EHS#1"]; } }
    public string BlankSite { get { return this["STS#1"]; } }
    public string DocumentWorkspace { get { return this["STS#2"]; } }
    public string WikiSite { get { return this["WIKI#0"]; } }
    public string Blog { get { return this["BLOG#0"]; } }
    public string DocumentCenter { get { return this["BDR#0"]; } }
    public string PublicPublishingPortal { get { return this["EHS#2"]; } }
    public string ExpressHostedSite { get { return this["EHS#0"]; } }

    public BuiltInWebTemplates() {
      this.Add("GLOBAL#0", "Global template"); // *
      this.Add("STS#0", "Team Site"); // *
      this.Add("STS#1", "Blank Site");  // *
      this.Add("STS#2", "Document Workspace"); // *
      this.Add("MPS#0", "Basic Meeting Workspace");
      this.Add("MPS#1", "Blank Meeting Workspace");
      this.Add("MPS#2", "Decision Meeting Workspace");
      this.Add("MPS#3", "Social Meeting Workspace");
      this.Add("MPS#4", "Multipage Meeting Workspace");
      this.Add("CENTRALADMIN#0", "Central Admin Site");
      this.Add("WIKI#0", "Wiki Site"); // *
      this.Add("BLOG#0", "Blog"); // *
      this.Add("SGS#0", "Group Work Site");
      this.Add("TENANTADMIN#0", "Tenant Admin Site");
      this.Add("APP#0", "App Template");
      this.Add("APPCATALOG#0", "App Catalog Site");
      this.Add("ACCSRV#0", "Access Services Site");
      this.Add("ACCSVC#0", "Access Services Site Internal");
      this.Add("ACCSVC#1", "Access Services Site");
      this.Add("BDR#0", "Document Center"); // *
      this.Add("TBH#0", "In-Place Hold Policy Center");
      this.Add("DEV#0", "Developer Site");
      this.Add("EDISC#0", "eDiscovery Center");
      this.Add("EDISC#1", "eDiscovery Case");
      this.Add("EXPRESS#0", "Express Team Site");
      this.Add("FunSite#0", "SharePoint Online Tenant Fundamental Site");
      this.Add("OFFILE#0", "(obsolete) Records Center");
      this.Add("OFFILE#1", "Records Center");
      this.Add("EHS#0", "Express Hosted Site"); // *
      this.Add("EHS#2", "Public Publishing Portal"); // *
      this.Add("EHS#1", "Team Site - SharePoint Online configuration"); // *
      this.Add("OSRV#0", "Shared Services Administration Site");
      this.Add("PPSMASite#0", "PerformancePoint");
      this.Add("BICenterSite#0", "Business Intelligence Center");
      this.Add("PWA#0", "Project Web App Site");
      this.Add("PWS#0", "Microsoft Project Site");
      this.Add("POLICYCTR#0", "Compliance Policy Center");
      this.Add("SPS#0", "SharePoint Portal Server Site");
      this.Add("SPSPERS#0", "SharePoint Portal Server Personal Space");
      this.Add("SPSPERS#2", "Storage And Social SharePoint Portal Server Personal Space");
      this.Add("SPSPERS#3", "Storage Only SharePoint Portal Server Personal Space");
      this.Add("SPSPERS#4", "Social Only SharePoint Portal Server Personal Space");
      this.Add("SPSPERS#5", "Empty SharePoint Portal Server Personal Space");
      this.Add("SPSPERS#6", "Storage And Social SharePoint Portal Server Personal Space");
      this.Add("SPSPERS#7", "Storage And Social SharePoint Portal Server Personal Space");
      this.Add("SPSPERS#8", "Storage And Social SharePoint Portal Server Personal Space");
      this.Add("SPSPERS#9", "Storage And Social SharePoint Portal Server Personal Space");
      this.Add("SPSPERS#10", "Storage And Social SharePoint Portal Server Personal Space");
      this.Add("SPSMSITE#0", "Personalization Site");
      this.Add("SPSTOC#0", "Contents area Template");
      this.Add("SPSTOPIC#0", "Topic area template");
      this.Add("SPSNEWS#0", "News Site");
      this.Add("CMSPUBLISHING#0", "Publishing Site");
      this.Add("BLANKINTERNET#0", "Publishing Site");
      this.Add("BLANKINTERNET#1", "Press Releases Site");
      this.Add("BLANKINTERNET#2", "Publishing Site with Workflow");
      this.Add("SPSNHOME#0", "News Site");
      this.Add("SPSSITES#0", "Site Directory");
      this.Add("SPSCOMMU#0", "Community area template");
      this.Add("SPSREPORTCENTER#0", "Report Center");
      this.Add("SPSPORTAL#0", "Collaboration Portal");
      this.Add("SRCHCEN#0", "Enterprise Search Center");
      this.Add("PROFILES#0", "Profiles");
      this.Add("BLANKINTERNETCONTAINER#0", "Publishing Portal");
      this.Add("SPSMSITEHOST#0", "My Site Host");
      this.Add("ENTERWIKI#0", "Enterprise Wiki");
      this.Add("PROJECTSITE#0", "Project Site");
      this.Add("PRODUCTCATALOG#0", "Product Catalog");
      this.Add("COMMUNITY#0", "Community Site");
      this.Add("COMMUNITYPORTAL#0", "Community Portal");
      this.Add("GROUP#0", "Group");
      this.Add("POINTPUBLISHINGHUB#0", "PointPublishing Hub");
      this.Add("POINTPUBLISHINGPERSONAL#0", "PointPublishing Personal");
      this.Add("POINTPUBLISHINGTOPIC#0", "PointPublishing Topic");
      this.Add("SRCHCENTERLITE#0", "Basic Search Center");
      this.Add("SRCHCENTERLITE#1", "Basic Search Center");
      this.Add("TenantAdminSpo#0", "SharePoint Online TenantAdmin");
      this.Add("visprus#0", "Visio Process Repository");
      this.Add("SAPWorkflowSite#0", "SAP Workflow Site");
    }

  }
}
