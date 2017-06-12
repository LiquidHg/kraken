namespace Kraken.SharePoint.ContentTypes {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Text;

    using Microsoft.SharePoint;
    using Microsoft.SharePoint.Administration;

    using Kraken.Configuration;
    using Kraken.SharePoint;
    using Kraken.SharePoint.Configuration;
    using Kraken.SharePoint.Logging;

    /*
    public class ContentTypeUpdateTimerJobSettings : SPPersistedObject {

        [Persisted]
        public Guid FeatureID = Guid.Empty;

        [Persisted]
        public FeatureCheckerScope FeatureScope = FeatureCheckerScope.Web;

        public ContentTypeUpdateTimerJobSettings() { }
        public ContentTypeUpdateTimerJobSettings(string name, SPPersistedObject parent, Guid id)
            : base(name, parent, id) {
        }

    } // class ContentTypeUpdateTimerJobSettings
     */

    public class ContentTypeRefreshTimerJob : SPJobDefinition {

      private KrakenLoggingService uls = KrakenLoggingService.CreateNew(LoggingCategories.KrakenContentTypes);

        [Persisted]
        public string WebUrl = string.Empty;

        [Persisted]
        public ListContentTypeRefreshEventArgs RefreshArgs = null;

        public static class Globals {

            internal static string JobName {
                get { return "BehemothListContentTypeRefresh"; }
            }
            internal static string Title {
                get { return "List Content Type Refresh Timer Job"; }
            }

            /*
            internal static string JobSettingsId {
                get { return "BehemothCTUpdateTimerJobSettings"; }
            } */

        } // class

        public ContentTypeRefreshTimerJob()
            : base() {
        }
        public ContentTypeRefreshTimerJob(SPWebApplication webApp)
            : base(Globals.JobName, webApp, null, SPJobLockType.None) {
            this.Name = Globals.JobName;
            this.Title = Globals.Title;
        }

        /*
        private ContentTypeUpdateTimerJobSettings GetSettings() {
            string settingsId = Globals.JobSettingsId;
            // Get settings for the warmup job.
            ContentTypeUpdateTimerJobSettings settings = this.GetChild<ContentTypeUpdateTimerJobSettings>(settingsId);
            return settings;
        }

        // TODO can this be made into a tool?
        private ContentTypeUpdateProperties GetProperties(SPWeb web, ContentTypeUpdateTimerJobSettings settings) {
            if (settings.FeatureID == Guid.Empty)
                throw new ArgumentNullException("Timer job settings Feature ID property was empty.", "ssettings.FeatureID");
            Guid featureId = settings.FeatureID;

            SPFeature feature = null;
            if (settings.FeatureScope == FeatureCheckerScope.Web)
                feature = web.Features[featureId];
            else if (settings.FeatureScope == FeatureCheckerScope.Site)
                feature = web.Site.Features[featureId];
            if (feature == null)
                throw new ArgumentOutOfRangeException(
                    string.Format(
                        "Feature with guid '{0}' and scope '{1}' was not found on web '{2}'.",
                        featureId,
                        web.Url,
                        settings.FeatureScope
                    ), "featureId");
            ContentTypeUpdateProperties properties = new ContentTypeUpdateProperties(feature);
            return properties;
        }
         */
        /*
        foreach (SPSite site in this.WebApplication.Sites) {
            site.RootWeb.Dispose();
            site.Dispose();
        }
        ContentTypeUpdateTimerJobSettings settings = GetSettings();
        if (settings == null)
            throw new ArgumentNullException("There was not timer job settings object.", "ssettings");
        if (string.IsNullOrEmpty(settings.WebUrl))
            throw new ArgumentNullException("Timer job settings WebUrl property was empty.", "ssettings.WebUrl");
         */
        /*
        // site feature ID is BD2C7DBA-20D9-4067-88F3-5F371545C199
        // web feature ID is B2D0CB39-79B2-4d99-B463-E3973EA28D01
        ContentTypeUpdateProperties properties = GetProperties(web, settings);
         */

        public override void Execute(Guid targetInstanceId) {
            try {
                using (SPSite site = new SPSite(this.WebUrl)) {
                    using (SPWeb web = site.OpenWeb()) {
                        DoRefreshListContentTypes(
                            web, 
                            this.RefreshArgs
                        ); // ,new LoggingEventHandler(uls.Log)
                    }
                }
            } catch (Exception ex) {
                string msg = string.Format("Timer job to update list content types failed. Exception: {0} Stack Trace: {1}", ex.Message, ex.StackTrace);
                uls.Write(msg, TraceSeverity.Unexpected, EventSeverity.Error);
                uls.Write(ex);
                throw new Exception(msg, ex); 
            }
        }

        public static void DeleteOldJobs(SPSite site) {
            // Make sure the job isn't already registered.
            foreach (SPJobDefinition job in site.WebApplication.JobDefinitions) {
                if (job.Name == Globals.JobName)
                    job.Delete();
            }
        }

        public static void CreateInstance(SPWeb web, ListContentTypeRefreshEventArgs args) {
            ContentTypeRefreshTimerJob.DeleteOldJobs(web.Site);
            ContentTypeRefreshTimerJob job = new ContentTypeRefreshTimerJob(web.Site.WebApplication);
            job.WebUrl = web.Url;
            job.RefreshArgs = args;
            job.Schedule = new SPOneTimeSchedule(DateTime.Now.AddSeconds(30));
            job.Update();
        }

      /*
        public static void DoRefreshListContentTypes(object web, ListContentTypeRefreshEventArgs args) {
            DoRefreshListContentTypes(web, args, null);
        }
       */
        public static void DoRefreshListContentTypes(object web, ListContentTypeRefreshEventArgs args) { // , LoggingEventHandler logging
            //if (logging == null)
            //    logging = new LoggingEventHandler(BehemothLoggingService.Default.Log);
            SPWeb targetWeb = web as SPWeb;
            if (targetWeb == null)
                throw new ArgumentNullException("Expecting a valid object of type SPWeb.", "web");
            // instantiated now, used later in the loop
            ContentTypePropagator cta = new ContentTypePropagator();
            //cta.Logging += logging;
            cta.RefreshListContentTypes(
                targetWeb,
                args.ContentTypeNames,
                args.UpdateFields,
                args.RemoveFields,
                args.RecurseSubWebs,
                args.ForceUpdate
            );
        }

    } // class

} // namespace