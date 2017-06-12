
namespace Kraken.SharePoint.Services {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    //using Microsoft.SharePoint;

    public class SPServiceUrl {

        const string ASMX_PATH = "/_vti_bin/";

        /*
        public static string Generate(SPWeb web, SharePointService svc) {
            return Generate(web.Url, svc);
        
        }
        */
        public static Uri Generate(Uri webUri, SharePointService svc, bool addAsmxExtension = false) {
          string webUrl = webUri.ToString();
          if (webUrl.EndsWith("/"))
            webUrl = webUrl.Substring(0, webUrl.Length - 1);
          webUrl = webUrl + ASMX_PATH + svc.ToString() + ((addAsmxExtension) ? ".asmx" : "");
          return new Uri(webUrl);
        }
        public static Uri GenerateAsmx(Uri webUrl, SharePointService svc) {
          return Generate(webUrl, svc, true);
        }

    }

    public enum SharePointService {
        alerts,
        Authentication,
        bdcfieldsresolver,
        businessdatacatalog,
        contentAreaToolboxService,
        Copy,
        DspSts,
        DWS,
        ExcelService,
        Forms,
        FormsServiceProxy,
        FormsServices,
        Imaging,
        Lists,
        Meetings,
        officialfile,
        People,
        Permissions,
        publishedlinksservice,
        PublishingService,
        search,
        sharepointemailws,
        SiteData,
        sites,
        SlideLibrary,
        SpellCheck,
        spscrawl,
        spsearch,
        UserGroup,
        userprofilechangeservice,
        userprofileservice,
        versions,
        Views,
        webpartpages,
        Webs,
        workflow
    }

}
