
namespace Kraken.SharePoint.ContentTypes {

    using System;
    using System.Collections.Generic;
    using System.Text;

    using Kraken.Configuration;
  using Kraken.SharePoint.Configuration;
    using Microsoft.SharePoint;

    /// <summary>
    /// Properties for the ContentTypeUpdate Feature and Receiver. ContentTypeUpdate is 
    /// an extension of a standard Content Type or Site Column definition/elements.
    /// </summary>
    public class ContentTypeUpdateProperties : SPFeaturePropertyReaderBase {

        public ContentTypeUpdateProperties(SPFeatureReceiverProperties props) : base(props) { }
        public ContentTypeUpdateProperties(SPFeature feature) : base(feature) { }

#if LegacyXmlContentTypeFunctions
        /// <summary>
        /// WARNING: Setting this property to TRUE will cause the ContentTypeUpdateReceiver
        /// to use legacy class SPContentTypeFeatureTools which is obsolete.
        /// </summary>
        [StrongTypeConfigEntryAttribute(true)]
        public bool UseXmlLinqMethods {
            get;
            set;
        }
#else
        [Obsolete("This method has no effect in the version, and should not be used without enabling compiler option 'LegacyXmlContentTypeFunctions'.")]
        public bool UseXmlLinqMethods {
            get { return false; }
        }
#endif

        [StrongTypeConfigEntryAttribute(true)]
        public bool CreateSiteColumnsByWebService {
            get;
            set;
        }
        [StrongTypeConfigEntryAttribute(true)]
        public bool UpdateContentTypesByWebService {
            get;
            set;
        }
        [StrongTypeConfigEntryAttribute(true)]
        public string SiteColumnElementFilePath {
            get;
            set;
        }
        [StrongTypeConfigEntryAttribute(true)]
        public string ContentTypeElementFilePath {
            get;
            set;
        }

        [StrongTypeConfigEntryAttribute(true)]
        public bool EnableRefreshListContentTypes {
            get;
            set;
        }

        private bool _listContentTypeUseTimerJob = true;
        [StrongTypeConfigEntryAttribute("RefreshListContentTypes_UseTimerJob", false)]
        public bool ListContentTypeUseTimerJob {
            get { return _listContentTypeUseTimerJob; }
            set { _listContentTypeUseTimerJob = value; }
        }
        private bool _listContentTypeForceUpdate = false;
        [StrongTypeConfigEntryAttribute("RefreshListContentTypes_ForceUpdate", false)]
        public bool ListContentTypeForceUpdate {
            get { return _listContentTypeForceUpdate; }
            set { _listContentTypeForceUpdate = value; }
        }
        private bool _listContentTypeRecurseSubWebs = false;
        [StrongTypeConfigEntryAttribute("RefreshListContentTypes_RecurseSubWebs", false)]
        public bool ListContentTypeRecurseSubWebs {
            get { return _listContentTypeRecurseSubWebs; }
            set { _listContentTypeRecurseSubWebs = value; }
        }

    } // class

} // namespace
