
namespace Kraken.SharePoint.ContentTypes {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text;

    using Microsoft.SharePoint;
  using Microsoft.SharePoint.Administration;
  using Microsoft.SharePoint.StsAdmin;

    using Kraken.SharePoint.Logging;

    /*
Save the following as “stsadmcommands.PropagateContentType.xml” and save it into 
“\Config” in the root of the SharePoint install folder (remember to update the 
assembly reference to whatever you compile the code into):

<?xml version="1.0" encoding="utf-8" ?>
<commands>
    <command name="propagatecontenttype"
        class="Behemoth.SharePoint.ContentTypes.PropagateContentTypeStsAdmTool,
        Behemoth.SharePoint.ARK, Version=1.?.?.?, Culture=neutral,
        PublicKeyToken="/>
</commands>
     */

    /// <summary>
    /// A custom STSAdm command for propagating site content types to lists
    /// content types.
    ///
    /// The code is provided as is, I don't take any responsibilty for
    /// any errors or data loss you might encounter.
    ///
    /// Use freely with two conditions:
    /// 1. Keep my name in there
    /// 2. Report any bugs back to http://soerennielsen.wordpress.com
    ///
    /// Enjoy
    /// Søren L. Nielsen
    /// </summary>
  public class ContentTypePropagatorStsAdmTool : ISPStsadmCommand { // LoggingEventConsumerBase

    KrakenLoggingService uls = KrakenLoggingService.CreateNew(LoggingCategories.KrakenContentTypes);

        #region Input parameters

        private bool UpdateFields {
            get; set;
        }

        private bool Verbose {
            get; set;
        }

        private bool RemoveFields {
            get; set;
        }

        private string ContentTypeName {
            get; set;
        }

        private string ProvidedUrl {
            get; set;
        }

        private bool ForceUpdate {
            get; set;
        }

        private bool RecurseSubWebs {
            get; set;
        }

        #endregion

        ///
        /// Runs the specified command. Called by STSADM.
        ///
        /// The command.
        /// The key values.
        /// The output.
        ///
        public int Run(string command, StringDictionary keyValues, out string output) {
            //Parse input
            // make sure all settings are valid
            if (!GetSettings(keyValues)) {
                Console.Out.WriteLine(GetHelpMessage(string.Empty));
                output = "Required parameters not supplied or invalid.";
            }

            SPSite siteCollection = null;
            SPWeb rootWeb = null;

            try {
                // get the site collection specified
                siteCollection = new SPSite(ProvidedUrl);
                ContentTypePropagator ctp = new ContentTypePropagator();
                //ctp.Logging += LogToConsoleAndULS;
                List<string> ctNames = new List<string>();
                ctNames.Add(ContentTypeName);
                // I am not 100% sure if this code pattern will result in logs from ctp going to the console
                //ctp.Logging += Logging; // bind logging event to console...
                ctp.RefreshListContentTypes(siteCollection, ctNames, UpdateFields, RemoveFields, RecurseSubWebs, ForceUpdate);

                output = "Operation successfully completed.";
                uls.Write(output);
                return 0;
            } catch (Exception ex) {
                output = "Unhandled error occured: " + ex.Message;
                uls.Write(output, TraceSeverity.Unexpected, EventSeverity.Error);
                uls.Write(ex);
                return -1;
            } finally {
                if (rootWeb != null)
                    rootWeb.Dispose();
                if (siteCollection != null)
                    siteCollection.Dispose();
            }
        }

    /*
        #region Logging Code Pattern v2.0

        protected void LogToConsoleAndULS(object o, LoggingEventArgs e) {
            if (e.Exception != null)
                Log(e.Exception);
            else
                Log(e.Message, e.Severity);
        }

        protected override string LOGGING_PRODUCT {
            get { return "Behemoth"; }
        }
        protected override string LOGGING_CATEGORY {
            get { return "Content Types"; }
        }

        protected override void Log(Exception ex) {
            Console.WriteLine("!!!EXCEPTION!!");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            base.Log(ex);
        }
        protected override void Log(string msg) {
            Console.WriteLine(msg);
            base.Log(msg);
        }
        protected override void Log(string msg, TraceSeverity severity) {
            Console.WriteLine(msg);
            base.Log(msg, severity);
        }

        #endregion
    */

        ///
        /// Parse the input settings
        ///
        ///
        ///
        private bool GetSettings(StringDictionary keyValues) {
            try {
                ProvidedUrl = keyValues["url"];
                //test the url
                new Uri(ProvidedUrl);

                ContentTypeName = keyValues["contenttype"];
                if (string.IsNullOrEmpty(ContentTypeName)) {
                    throw new ArgumentException("contenttype missing");
                }
                if (keyValues.ContainsKey("removefields"))
                    RemoveFields = true;
                if (keyValues.ContainsKey("verbose"))
                    Verbose = true;
                if (keyValues.ContainsKey("updatefields"))
                    UpdateFields = true;
                if (keyValues.ContainsKey("forceupdate"))
                    ForceUpdate = true;
                if (keyValues.ContainsKey("recursesubwebs"))
                    RecurseSubWebs = true;
                return true;
            } catch (Exception ex) {
                Console.Out.WriteLine("An error occuring in retrieving the"
                    + " parameters. \r\n(" + ex + ")\r\n");
                return false;
            }
        }

        ///
        /// Output help to console
        ///
        ///
        ///
        public string GetHelpMessage(string command) {
            StringBuilder helpMessage = new StringBuilder();

            // syntax
            helpMessage.AppendFormat("\tstsadm -o {0}{1}{1}", command, Environment.NewLine);
            helpMessage.Append("\t-url " + Environment.NewLine);
            helpMessage.Append("\t-contenttype " + Environment.NewLine);
            helpMessage.Append("\t[-removefields]" + Environment.NewLine);
            helpMessage.Append("\t[-updatefields]" + Environment.NewLine);
            helpMessage.Append("\t[-verbose]" + Environment.NewLine);
            helpMessage.Append("\t[-recursesubwebs]" + Environment.NewLine);
            helpMessage.Append("\t[-forceupdate]" + Environment.NewLine);

            // description
            helpMessage.AppendFormat("{0}This action will propagate a site"
                + " content type to all list content types within the "
                + "site collection.{0}Information propagated is field "
                + "addition/removal.{0}{0}", Environment.NewLine);
            helpMessage.AppendFormat("{0}Søren Nielsen (soerennielsen."
                + "wordpress.com){0}{0}", Environment.NewLine);

            return helpMessage.ToString();
        }

    } // class

} // namespace
