using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

using Microsoft.SharePoint.Client;

using Kraken.SharePoint.Client;
//using Kraken.Security;

// TODO move this test routie to an LMS proprietary utility
/*
using LiquidMercury.Licensing.Client;
using LiquidMercury.Office365.Management;
 */

namespace Kraken.SharePoint.Client.Test {

  class Program {

    static void Main(string[] args) {

        /*
      Office365PartnerManager test = new Office365PartnerManager();
      //test.ConnectAndGetSubscriptions();
      test.ScrapeSubscriptionPartners();
         */

      /*
      SecureStringMarshaller s = new SecureStringMarshaller("IbiwIsi^5288");
      SecureString userPass = s.SecureData;

      //ClientContext context = ClientTools.Connect("https://spliquidmercury.sharepoint.com/sites/clients", "thomas.carpe@liquidmercurysolutions.com", userPass, true);
      WebContextManager cm = new WebContextManager() {
        TargetWebUrl = "https://spliquidmercury.sharepoint.com",
        AuthType = ClientAuthenticationType.SPOCredentials,
        UserName = "thomas.carpe@liquidmercurysolutions.com",
        UserPassword = userPass
      };
      MetadataImportExportUtility exporter = new MetadataImportExportUtility() {
        TargetLibraryName = "Shared Documents",
        Match = new ItemMatchSettings() {
          FieldName = "ID",
          FieldValue = "2",
          FieldType = "Number",
          Operator = Caml.CAML.Operator.Eq
        }
      };
      cm.CopyTo(exporter.ContextManager);
       */
      /*
      exporter.Connect();
      exporter.Init(false);
       */
      //List<ListItem> items = exporter.GetItemMetadata(false);
      /*
      BulkFileUploader copier = new BulkFileUploader() {
        SourceFolderPath = @"C:\LA\ACN",
        TargetWebUrl = "https://spliquidmercury.sharepoint.com",
        TargetLibraryName = "Documents",
        SourcePathFieldName = "MigrationSourceURL",
        RootFolderContentTypeName = "", // "Folder"
        OverwriteFiles = true,
        IsOffice365 = true,
        UserName = "thomas.carpe@liquidmercurysolutions.com", // "dirsyncservice@rrg.com", // 
        UserPassword = userPass
      };
      copier.Connect();
      copier.Init();
      //copier.AnalizeForSync(false);
      copier.Copy(false);
       */

      /*
      List<User> users = ClientTools.GetUsersInGroup(context, "All Clients");
      foreach (User user in users) {
        Debug.WriteLine("User: {0}  ID: {1} Email: {2} Login Name: {3}",
                   user.Title, user.Id, user.Email, user.LoginName);
        Console.WriteLine("User: {0}  ID: {1} Email: {2} Login Name: {3}",
                   user.Title, user.Id, user.Email, user.LoginName);
      }
      */
            
      Console.WriteLine("Press any key.");
      Console.ReadKey();
    }

  }

}
