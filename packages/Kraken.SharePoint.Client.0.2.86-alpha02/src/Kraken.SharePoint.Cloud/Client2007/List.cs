
namespace Kraken.SharePoint.Client.Legacy {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class List {

        public Guid ID { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }

        internal string parentWebUrl = string.Empty;
        internal Web parentWeb = null; // TODO use a caching mechanism in the Repository
        public Web ParentWeb {
            get {
                if (parentWeb == null) {
                    if (string.IsNullOrEmpty(parentWebUrl))
                        throw new ArgumentNullException("Internal property should not be empty or null.", "parentWebUrl");
                    parentWeb = new Web(parentWebUrl); // TODO implement a web objec tusing the repository to retreieve it
                }
                return parentWeb;
            }
        }

        public void Update() {
            throw new NotImplementedException("This method is not yet implemented.");
        }

    }

}
