
namespace Kraken.SharePoint.ContentTypes {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class ListContentTypeRefreshEventArgs {

        public ListContentTypeRefreshEventArgs(List<string> names)
            : this(names, true, false, true, false, false) {
        }
        public ListContentTypeRefreshEventArgs(List<string> names, bool update, bool delete, bool recurseWebs, bool force, bool useTimerJob) {
            this.ContentTypeNames = names;
            this.UpdateFields = update;
            this.RemoveFields = delete;
            this.ForceUpdate = force;
            this.RecurseSubWebs = recurseWebs;
            this.UseTimerJob = useTimerJob;
        }

        public List<string> ContentTypeNames {
            get; set;
        }
        public bool RecurseSubWebs {
            get; set;
        }
        public bool UpdateFields {
            get; set;
        }
        public bool RemoveFields {
            get; set;
        }
        public bool ForceUpdate {
            get; set;
        }
        public bool UseTimerJob {
            get; set;
        }

    } // LoggingEventArgs 

    public delegate void ListContentTypeRefreshEventHandler(object web, ListContentTypeRefreshEventArgs e);

} // namespace