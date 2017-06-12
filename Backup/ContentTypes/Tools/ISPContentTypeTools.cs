using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Kraken.SharePoint.ContentTypes {

  public interface IContentTypeManager {

    // SPWeb web

    void EnsureContentTypes(string elementFeatureAndFile);
    void EnsureContentTypes(XElement elementDoc);
    string GetParentContentTypeId(XElement contentTypes, string childID);
    string XCreateContentType(XElement creatingCTypeDefinition, string parentId);
    XElement XUpdateContentType(XElement existingCTypeDefinition, XElement updatingCTypeDefinition, List<string> cTypesNeedUpdate);

  }

  public interface ISiteColumnManager {

    XElement EnsureSiteColumns(string elementFeatureAndFile);
    XElement EnsureSiteColumns(XElement elementDoc);

  }

  public enum BuildWebServiceFieldsNodeType {
    NewFields = 1,
    ExstingFields = 2,
    DeleteFields = 3
  }

}
