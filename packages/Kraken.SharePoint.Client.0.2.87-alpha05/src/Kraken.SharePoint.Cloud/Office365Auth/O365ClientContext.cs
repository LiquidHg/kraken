using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using Microsoft.SharePoint.Client;

namespace Kraken.SharePoint.Cloud.Authentication {

  /// <summary>
  /// </summary>
  /// <remarks>
  /// Attribution: This module is based on the work of Wictor Wilén and Steve
  /// Peschka, without whom all of us would still be banging rocks together 
  /// trying to make our stuff claims compatible.
  /// Wictor Wilén - SharePoint MCA, MCSM, MCM and MVP
  /// http://www.wictorwilen.se/Post/How-to-do-active-authentication-to-Office-365-and-SharePoint-Online.aspx
  /// Share-n-dipity
  /// http://blogs.technet.com/b/speschka/
  /// </remarks>
  public class O365ClientContext {

    private MsOnlineClaimsHelper _helper = null;

    /// <summary>
    /// Use this cookie container for Web Reference calls outside of the ClientContext in this object.
    /// Once authenticated, this container has all the necessary authentication cookies to bang against SharePoint as the user
    /// passed to the constructor method of the parent object without the need for the ClientContext (such as for Web Reference
    /// (svc calls),  ClientOM in Java- or Ecma-script, or AJAX).
    /// </summary>
    public System.Net.CookieContainer CookieContainer { get { return _helper.CookieContainer; } }

    private ClientContext _ctx = null;
    public ClientContext Context { get { return _ctx; } }

    private Uri _absoluteWebUrl = null;
    public Uri AbsoluteURL { get { return _absoluteWebUrl; } }

    private SecureString _pass = null;
    public string UserPass {
      get {
        if (_pass == null)
          return null;
        else
          return ConvertToUnsecureString(_pass);

      }
    }
    private string _user = null;
    public string UserName { get { return _user; } }

    public O365ClientContext(Uri MSOnlineSiteUri, string userName, SecureString userPass) {
      {
        string url = MSOnlineSiteUri.ToString();
        if (!url.EndsWith("/"))
          url += "/";
        if (!url.ToLowerInvariant().StartsWith("https://"))
          throw new ArgumentException("Url MUST start with 'https://'", "MSOnlineSiteUrl");
        MSOnlineSiteUri = new Uri(url);
      }

      if (string.IsNullOrEmpty(userName.Trim()))
        throw new ArgumentNullException("userName", "userName cannot be empty, white space, or null");
      if (userPass == null || userPass.Length < 1)
        throw new ArgumentNullException("userPass", "userPass cannot be empty or null");

      _helper = new MsOnlineClaimsHelper(MSOnlineSiteUri, userName, ConvertToUnsecureString(userPass));
      _ctx = new ClientContext(MSOnlineSiteUri);
      _ctx.ExecutingWebRequest += new EventHandler<Microsoft.SharePoint.Client.WebRequestEventArgs>(this.ctx_ExecutingWebRequest);
      _absoluteWebUrl = MSOnlineSiteUri;
      _ctx.Load(_ctx.Web);
      _ctx.ExecuteQuery();
      _user = userName;
      _pass = userPass;
    }

    private void ctx_ExecutingWebRequest(object sender, Microsoft.SharePoint.Client.WebRequestEventArgs e) {
      e.WebRequestExecutor.WebRequest.CookieContainer = _helper.CookieContainer;
    }

    public struct MetaData {
      public object Value;
      public string InternalFieldName;
      public MetaData(object value, string internalFieldName) {
        Value = value;
        InternalFieldName = internalFieldName;
      }
    }


    public int SaveDocumentOnline(String relativeWebUrl,
                                            String docLibraryTitle,
                                            System.IO.FileStream file,
                                            bool overwrite,
                                            out Exception error_obj) {
      Exception problem = null;
      byte[] stuff = new byte[file.Length];
      file.Read(stuff, 0, (int)file.Length);
      System.IO.FileInfo fi = new System.IO.FileInfo(file.Name);
      string fName = fi.FullName.Replace(fi.DirectoryName + '\\', "");
      int success = SaveDocumentOnline(relativeWebUrl, docLibraryTitle, fName, stuff, null, null, overwrite, out problem);
      error_obj = problem;
      return success;
    }


    public int SaveDocumentOnline(String relativeWebUrl,
                                            String docLibraryTitle,
                                            string filenameNoPath,
                                            byte[] fileContents,
                                            bool overwrite,
                                            out Exception error_obj) {
      Exception problem;
      int success = SaveDocumentOnline(relativeWebUrl, docLibraryTitle, filenameNoPath, fileContents, null, null, overwrite, out problem);
      error_obj = problem;
      return success;
    }

    public int SaveDocumentOnline(String relativeWebUrl,
                                            String docLibraryTitle,
                                            System.IO.FileStream file,
                                            List<MetaData> metaData,
                                            String contentType,
                                            bool overwrite,
                                            out Exception error_obj) {
      Exception problem = null;
      byte[] stuff = new byte[file.Length];
      file.Read(stuff, 0, (int)file.Length);
      System.IO.FileInfo fi = new System.IO.FileInfo(file.Name);
      string fName = fi.FullName.Replace(fi.DirectoryName + '\\', "");
      int success = SaveDocumentOnline(relativeWebUrl, docLibraryTitle, fName, stuff, metaData, contentType, overwrite, out problem);
      error_obj = problem;
      return success;
    }

    public int SaveDocumentOnline(String relativeWebUrl,
                                        String docLibraryTitle,
                                        string filenameNoPath,
                                        byte[] fileContents,
                                        List<MetaData> metaData,
                                        String contentType,
                                        bool overwrite,
                                        out Exception error_obj) {
      error_obj = null;
      try {
        //get the site colleciton
        Web theWeb = _ctx.Web;

        //get the document library folder
        Folder theFolder = theWeb.GetFolderByServerRelativeUrl(relativeWebUrl + docLibraryTitle);

        _ctx.ExecuteQuery();

        //populate information about the new file
        FileCreationInformation fci = new FileCreationInformation();
        fci.Url = filenameNoPath;
        fci.Content = fileContents;
        fci.Overwrite = overwrite;

        //load the file collection for the documents in the library
        FileCollection theFiles = theFolder.Files;
        _ctx.Load(theFiles);
        _ctx.ExecuteQuery();

        //add this file to the file collection
        Microsoft.SharePoint.Client.File newFile = theFiles.Add(fci);
        _ctx.Load(newFile);
        _ctx.ExecuteQuery();

        ListItem item = newFile.ListItemAllFields;

        if (!string.IsNullOrEmpty(contentType.Trim())) {
          //get a reference to the list
          List list = theWeb.Lists.GetByTitle(docLibraryTitle);
          _ctx.Load(list);
          ContentTypeCollection listContentTypes = list.ContentTypes;
          //load content type information for content types associated with the doc library
          _ctx.Load(listContentTypes, types => types.Include
            (type => type.Id, type => type.Name, type => type.Parent));
          //LINQ query to load content type
          var result = _ctx.LoadQuery(listContentTypes.Where(c => c.Name == contentType));
          _ctx.ExecuteQuery();

          ContentType targetDocumentContentType = result.FirstOrDefault();
          //get content ID from "Tax Certificate"
          string contentTypeID = targetDocumentContentType.Id.ToString();


          //set content type
          item["ContentTypeId"] = contentTypeID;
        }

        if (metaData != null) // && metaData.Count > 0)
                {
          foreach (MetaData md in metaData) {
            item[md.InternalFieldName] = md.Value;
          }
        }

        item.Update();
        _ctx.Load(item);
        _ctx.ExecuteQuery();

        return item.Id;
      } catch (Exception ex) {
        error_obj = ex;
        return -1;
        //note need to log this
      }
    }

    // TODO get Tom's marshalling lib from 2005.

    /// <summary>
    /// REPLACE THIS WITH A BETTER MARSHALLING ROUTINE!!!!
    /// </summary>
    /// <param name="securePassword"></param>
    /// <returns></returns>
    private static string ConvertToUnsecureString(SecureString securePassword) {
      // taken from:
      // http://blogs.msdn.com/b/fpintos/archive/2009/06/12/how-to-properly-convert-securestring-to-string.aspx
      if (securePassword == null)
        throw new ArgumentNullException("securePassword");

      IntPtr unmanagedString = IntPtr.Zero;
      try {
        unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
        return Marshal.PtrToStringUni(unmanagedString);
      } finally {
        Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
      }
    }
  }
}
