
namespace Kraken.SharePoint.Client.Legacy {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

  using Kraken.SharePoint.Services.Gateways;

    public class ListRepository {

      public List<List> GetLists(Uri webUrl) {
            //ListsServiceGateway gw = new ListsServiceGateway();
            List<List> listCollection = ListsServiceGateway.GetLists(webUrl);
            throw new NotImplementedException("Not implemented."); // TODO implement this function
        }
        public List GetList(string webUrl, Guid listId) {
            throw new NotImplementedException("Not implemented."); // TODO implement this function
        }
        public List<List> GetList(string webUrl, string listName) {
            throw new NotImplementedException("Not implemented."); // TODO implement this function
        }

    }
}
