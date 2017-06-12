using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
#if !DOTNET_V35
using Microsoft.SharePoint.Client.DocumentSet;
#endif
using System.Net;
using System.Security;
using System.Xml.Linq;

namespace Kraken.SharePoint.Client {

  public static class ClientTools {

    #region TODO alternative Large File Support

    // WORK IN PROGRESS
    public static void AddDocument(ClientContext context, string fileToUpload, string webUrl, string docLibInternalName, string docLibUIName, string documentSetName, string filenameToSaveAs, DateTime? createdDate, DateTime? modifiedDate, Dictionary<string, object> keyValues, int timeoutInMilliseconds) {
      string urlToSaveAs = webUrl + "/" + docLibInternalName + "/" + documentSetName + "/" + filenameToSaveAs;
      AddDocument(context, fileToUpload, webUrl, urlToSaveAs, createdDate, modifiedDate, keyValues, timeoutInMilliseconds);
    }

    // Uploads most any size file to SharePoint Online (O365) using Claims Authentication. It does NOT use CSOM, and instead uses a standard PUT request 
    // that has the cookies from the Claims based authentication added to it. 
    // This solution is based on http://stackoverflow.com/questions/15077305/uploading-large-files-to-sharepoint-365 
    // and 
    // To get the claims authentiation cookie, this solution requires: http://msdn.microsoft.com/en-us/library/hh147177.aspx#SPO_RA_Introduction 
    // or if you want to get the cookies for claims authentication antoher way, you can use 
    // http://www.wictorwilen.se/Post/How-to-do-active-authentication-to-Office-365-and-SharePoint-Online.aspx 
    public static void AddDocument(ClientContext context, string fileToUpload, string webUrl, string urlToSaveAs, DateTime? createdDate, DateTime? modifiedDate, Dictionary<string, object> keyValues, int timeoutInMilliseconds) {

      //For example: byte[] data = System.IO.File.ReadAllBytes(@"C:\Users\me\Desktop\test.txt"); 
      byte[] data = System.IO.File.ReadAllBytes(fileToUpload);

      // get the cookies from the Claims based authentication and add it to the cookie container that we will then pass to the request 
      CookieCollection cookies = null; // TODO context.GetAuthenticatedCookies(webUrl, 200, 200);
      CookieContainer cookieContainer = new CookieContainer();
      cookieContainer.Add(cookies);

      // make a standard PUT request 
      System.Net.ServicePointManager.Expect100Continue = false;
      HttpWebRequest request = HttpWebRequest.Create(urlToSaveAs) as HttpWebRequest;
      request.Method = "PUT";
      request.Accept = "*/*";
      request.ContentType = "multipart/form-data; charset=utf-8";
      request.CookieContainer = cookieContainer;
      request.AllowAutoRedirect = false;
      request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
      request.Headers.Add("Accept-Language", "en-us");
      request.Headers.Add("Translate", "F");
      request.Headers.Add("Cache-Control", "no-cache");
      request.ContentLength = data.Length;
      request.ReadWriteTimeout = timeoutInMilliseconds;
      request.Timeout = timeoutInMilliseconds;


      using (System.IO.Stream req = request.GetRequestStream()) {
        req.ReadTimeout = timeoutInMilliseconds;
        req.WriteTimeout = timeoutInMilliseconds;
        req.Write(data, 0, data.Length);
      }

      // get the response back 
      HttpWebResponse response = null;
      try {
        response = (HttpWebResponse)request.GetResponse();
        System.IO.Stream res = response.GetResponseStream();
        using (System.IO.StreamReader rdr = new System.IO.StreamReader(res)) {
          string rawResponse = rdr.ReadToEnd();

        }
      } catch (Exception ex) {
        throw ex;
      } finally {
        if (response != null) {
          response.Close();
        }
      }

      // NOTE: the file that was uploaded is still checked out and must be checked in before it will be available to others. 
      // The method includes a checkin command. If the method below is removed for some reason, 
      // a checkin method call should be added here so that the file will be available to all (that have access). 
      // NOTE: The method calls add keyValues passed in as well. These would need to be done also if the method is removed. 
      UriBuilder urlBuilder = new UriBuilder(urlToSaveAs);
      string serverRelativeUrlToSaveAs = urlBuilder.Path;

      //TODO
      //ChangeCreatedModifiedInfo(webUrl, serverRelativeUrlToSaveAs, createdDate, modifiedDate, keyValues);
    }

#endregion

    #region TODO copy and update site columns and field values

    private static void updateInPlace(ClientContext context, List list, Guid originalListColumnID, string newInternalName, string groupName) {
        var web = context.Web;
        var listFields = list.Fields;
        context.Load(web);
        var rootWeb = context.Site.RootWeb;
        context.Load(rootWeb);
        context.Load(list);
        context.Load(listFields);
        FieldCollection webFields = web.Fields;
        FieldCollection siteFields = web.AvailableFields;
        context.Load(webFields);
        context.Load(siteFields);
        context.ExecuteQuery();

        //Guid originalListColumnID// = (Guid)selectedItem.Key; KeyValuePair<Guid, string> selectedItem
       
        var originalListColumn = listFields.GetById(originalListColumnID);
        context.Load(originalListColumn);
        context.ExecuteQuery();


        var tempSiteColumn = GetColumnWithID(siteFields, originalListColumn.Id);
        if (tempSiteColumn != null) {
          //MessageBox.Show("This Field is already a Site Column. It is tied to Column " + tempSiteCOlumn.Title);
          return;
        }

        //string newInternalName = textBoxNewInternalName.Text;
        if (string.IsNullOrEmpty(newInternalName))
          tempSiteColumn = GetColumnWithIntenalName(siteFields, originalListColumn.InternalName);
        else
          tempSiteColumn = GetColumnWithIntenalName(siteFields, newInternalName);
        if (tempSiteColumn != null) {
          //MessageBox.Show("There is already a Site Field with the Intenal name " + tempSiteCOlumn.InternalName + " it is titled" + tempSiteCOlumn.Title + ". You must specify a Different  Internal name");
          return;
        }

        //string groupName = textBoxGrroupName.Text;
        // create the new Site COlumn
        XElement newSiteColumnSchema = XElement.Parse(originalListColumn.SchemaXml, LoadOptions.None);
        newSiteColumnSchema.Attributes("ColName").Remove();
        newSiteColumnSchema.Attributes("RowOrdinal").Remove();
        newSiteColumnSchema.Attributes("Version").Remove(); // needed for lookups
        newSiteColumnSchema.SetAttributeValue("Group", groupName);
        newSiteColumnSchema.SetAttributeValue("SourceID", String.Format("{{{0}}}", web.Id)); // Set the SourecID to be the ID og the current web

        var newFieldSchemaXML = newSiteColumnSchema.ToString();
        Field newSiteColumn = null;
        newSiteColumn = web.Fields.AddFieldAsXml(newFieldSchemaXML, true, AddFieldOptions.DefaultValue);//the simplest way to create a field is to specify a bit of XML that defines the field, and pass that XML to the AddFieldAsXml method. There is a Add method that you can use to create a field, but instead of taking a FieldCreationInformation object, it takes another Field object as a parameter that it uses as a prototype for the field to be created. This is useful in some scenarios. see  http://msdn.microsoft.com/en-us/library/ee857094(v=office.14).aspx#SP2010ClientOM_Creating_Populating_List
        web.Update();
        context.ExecuteQuery();
    }

    private static Field GetColumnWithID(FieldCollection fields, Guid Id) {
      foreach (Field siteField in fields) {
        if (siteField.Id == Id) {
          return siteField;
        }
      }
      return null;
    }
    private static Field GetColumnWithIntenalName(FieldCollection fields, String InternalName) {
      foreach (Field siteField in fields) {
        if (siteField.InternalName == InternalName) {
          return siteField;
        }
      }
      return null;
    }

    private static void UpdateByCopy(ClientContext context, List list, Guid originalListColumnID, string newInternalName, string groupName) {

        var web = context.Web;
        context.Load(list);

        var listFields = list.Fields;
        context.Load(listFields);

        var siteColumns = web.AvailableFields;
        context.Load(siteColumns);

        context.ExecuteQuery();

        //KeyValuePair<Guid, string> selectedItem = (KeyValuePair<Guid, string>)lbFields.SelectedItem;
        //Guid originalListColumnID = (Guid)selectedItem.Key;

        var originalListColumn = listFields.GetById(originalListColumnID);
        context.Load(originalListColumn);
        context.ExecuteQuery();

        var tempSiteCOlumn = GetColumnWithID(siteColumns, originalListColumn.Id);
        if (tempSiteCOlumn != null) {
          //MessageBox.Show("This Field is already a Site Column. It is tied to Column " + tempSiteCOlumn.Title);
          return;
        }

        if (string.IsNullOrEmpty(newInternalName))
          tempSiteCOlumn = GetColumnWithIntenalName(siteColumns, originalListColumn.InternalName);
        else
          tempSiteCOlumn = GetColumnWithIntenalName(siteColumns, newInternalName);
        if (tempSiteCOlumn != null) {
          //MessageBox.Show("There is already a Site Field with the Intenal name " + tempSiteCOlumn.InternalName + " it is titled" + tempSiteCOlumn.Title + ". You must specify a Different  Internal name");
          return;
        }



        // create the new Site COlumn
        //AddMessage("Creating a new Site Column named " + (string.IsNullOrEmpty(textBoxNewInternalName.Text) ? originalListColumn.InternalName : textBoxNewInternalName.Text));
        XElement newSiteColumnSchema = XElement.Parse(originalListColumn.SchemaXml, LoadOptions.None);
        newSiteColumnSchema.Attributes("ID").Remove();
        newSiteColumnSchema.Attributes("Source").Remove();
        newSiteColumnSchema.Attributes("ColName").Remove();
        newSiteColumnSchema.Attributes("RowOrdinal").Remove();
        newSiteColumnSchema.Attributes("StaticName").Remove();
        if (!string.IsNullOrEmpty(newInternalName)) {
          newSiteColumnSchema.SetAttributeValue("Name", newInternalName);
        }
        if (!string.IsNullOrEmpty(newInternalName)) {
          newSiteColumnSchema.SetAttributeValue("DisplayName", newInternalName);
        }

        var newFieldSchemaXML = newSiteColumnSchema.ToString();
        var newSiteColumn = web.Fields.AddFieldAsXml(newFieldSchemaXML, true, AddFieldOptions.DefaultValue);//the simplest way to create a field is to specify a bit of XML that defines the field, and pass that XML to the AddFieldAsXml method. There is a Add method that you can use to create a field, but instead of taking a FieldCreationInformation object, it takes another Field object as a parameter that it uses as a prototype for the field to be created. This is useful in some scenarios. see  http://msdn.microsoft.com/en-us/library/ee857094(v=office.14).aspx#SP2010ClientOM_Creating_Populating_List


        // create a temporary list column
        var tempColumnName = "_ _ _ TEMP _ _ _ " + originalListColumn.Title;
        //AddMessage("Creating a temporary list Column named " + tempColumnName);

        XElement templistColumnSchema = XElement.Parse(originalListColumn.SchemaXml, LoadOptions.None);
        templistColumnSchema.Attributes("ID").Remove();
        templistColumnSchema.Attributes("Source").Remove();
        templistColumnSchema.Attributes("ColName").Remove();
        templistColumnSchema.Attributes("RowOrdinal").Remove();
        templistColumnSchema.Attributes("StaticName").Remove();
        templistColumnSchema.SetAttributeValue("Name", tempColumnName);
        templistColumnSchema.SetAttributeValue("DisplayName", tempColumnName);
        var templistColumnSchemaXML = templistColumnSchema.ToString();
        var templistColumn = list.Fields.AddFieldAsXml(templistColumnSchemaXML, true, AddFieldOptions.DefaultValue);//the simplest way to create a field is to specify a bit of XML that defines the field, and pass that XML to the AddFieldAsXml method. There is a Add method that you can use to create a field, but instead of taking a FieldCreationInformation object, it takes another Field object as a parameter that it uses as a prototype for the field to be created. This is useful in some scenarios. see  http://msdn.microsoft.com/en-us/library/ee857094(v=office.14).aspx#SP2010ClientOM_Creating_Populating_List
        context.Load(templistColumn);

        // copy the Original list column to the temporary list column
        context.ExecuteQuery();

        //AddMessage("copying Original Column to the temprorary column");
        CopyFieldValue(context, list, originalListColumn, templistColumn);

        // remove the original column
        //AddMessage("Removing the  Original Columnfrom the list");

        list.Fields.GetById(originalListColumnID).DeleteObject();
        context.ExecuteQuery();

        // Add he new site column to the list.
        //AddMessage("Adding the New Site Column to the list");

        var newSiteColumnInList = list.Fields.Add(newSiteColumn);
        context.Load(newSiteColumnInList);
        context.ExecuteQuery();


        // copy the temp column to the new column
        //AddMessage("Copying the Temporary list column to the New Site Column");

        CopyFieldValue(context, list, templistColumn, newSiteColumnInList);
        list.Fields.GetById(templistColumn.Id).DeleteObject();
        context.ExecuteQuery();

        //AddMessage("All Done... ");
    }

    private static void CopyFieldValue(ClientContext context, List list, Field SourceColumn, Field DesitinationColumn) {
      // update 200 rows at a time. Value must be les than WebApplication.ClientCallableSettings.MaxObjectPaths  which defaults to 256
      var camlQuery1 = new CamlQuery();
      var loopCount = 0;
      ListItemCollectionPosition itemPosition = null;
      do {
        camlQuery1.ViewXml = "<View><ViewFields><FieldRef Name='" + SourceColumn.InternalName + "' /><FieldRef Name='" + DesitinationColumn.InternalName + "' /><FieldRef Name='Title' /><FieldRef Name='CheckoutUser' /></ViewFields><RowLimit>200</RowLimit></View>";
        camlQuery1.ListItemCollectionPosition = itemPosition;
        var listItems = list.GetItems(camlQuery1);
        context.Load(listItems);
        context.ExecuteQuery();
        itemPosition = listItems.ListItemCollectionPosition;
        loopCount++;
        //AddMessage("Updateing Rows " + (loopCount * 200 - 199).ToString() + " to " + (loopCount * 200 - 200 + listItems.Count).ToString());
        foreach (var listitem in listItems) {
          if (listitem["CheckoutUser"] != null) {
            var message = listitem["Title"] + " Was not updated. It is checed out by " + listitem["CheckoutUser"];
            //AddMessage(message);

          } else {
            listitem[DesitinationColumn.InternalName] = listitem[SourceColumn.InternalName];
            listitem.Update();
          }
        }

      } while (itemPosition != null);

    }

    #endregion

    public static List<User> GetUsersInGroup(ClientContext clientContext, string groupName) {
      GroupCollection collGroup = clientContext.Web.SiteGroups;
#if !DOTNET_V35
      Group oGroup = collGroup.GetByName(groupName);
#else
      Group oGroup = collGroup.Where(g => g.Title == groupName).FirstOrDefault();
#endif
      UserCollection collUser = oGroup.Users;
      clientContext.Load(collUser);
      clientContext.ExecuteQuery();

      List<User> users = new List<User>();
      foreach (User user in collUser) {
        users.Add(user);
      }
      return users;
    }

  }

  public enum ClientAuthenticationType {
    Unknown,
    SPOCredentials,
    SPOCustomCookie,
    SharePointNTLMUserPass,
    SharePointNTLMCurrentUser,
    SharePointClaims,
    SharePointForms
  }

}
