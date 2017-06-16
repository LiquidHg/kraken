
namespace Kraken.SharePoint.Client.Legacy {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class Web {

        public Web(string webUrl) {
            Url = webUrl;
        }

        /*
    private void Test() {
        SPWeb web;
        web.ID
    }
                */

        public Guid ID { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public Uri Uri {
            get { return new Uri(Url); } 
        }

    }

}
