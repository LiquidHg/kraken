
namespace Kraken.SharePoint.Client.Legacy {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class ContentType {

        public string Name { get; set; }
        public string ID { get; set; }
        public string Description { get; set; }
        public string Group { get; set; }
        public string NewDocumentControl { get; set; }
        public string Scope { get; set; }
        public int Version { get; set; }
        public bool RequireClientRenderingOnNew { get; set; }

        // TODO what about its parent CT?

    }
}
