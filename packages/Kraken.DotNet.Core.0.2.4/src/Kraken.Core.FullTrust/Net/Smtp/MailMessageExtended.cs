
namespace Kraken.Net.Smtp {

    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Configuration;
    using System.Globalization;
    using System.Data;
    using System.IO;
    //using System.Web.Mail;
    using System.Net.Mail;
    using System.Runtime.Serialization;

    using Kraken;
    //using Kraken.Diagnostics;
    using Kraken.Diagnostics.Logging;

	/// <summary>
	/// An extension of System.Web.Mail.MailMessage (or System.Net.Mail.MailMessage in
	/// .NET 2.0) that provides some additional features for merge lists and logging,
	/// as well as some logic to better expose errors that occur in the CDONTS COM object.
	/// </summary>
	public class MailMessageExtended : System.Net.Mail.MailMessage {

		public MailMessageExtended() : base() {}

		/// <summary>
		/// Creates the object and reads some properties from the application
		/// configuration file. 
		/// </summary>
		/// <param name="strConfigPrefix">The prefix string is prepended to each key in AppSettings,
		/// in order to allow for multiple mail profiles in the same configuration file. Use a blank
		/// string if you are not implementing this feature.</param>
		public MailMessageExtended(string configPrefix) : base() {
			ReadPropertiesFromConfig(configPrefix);
		}

		#region Private Properties

		private bool verboseLogging;
		private LoggingMemory _log;
		private string _smtpServer;
		private string _templateBodyText;
		private string _templateSubject;
		private string _templateBodyFileName;
		// TODO instead of using a name value collection how about using a DataRow?
		private IDictionary _mergeNameValuePairs;
		private DataRow _mergeFieldRow;
		private SmtpClient _client;
		private bool isReadyToSendAgain = true;

		#endregion

		#region Public Properties

		/// <summary>
		/// Used to specify a host for this mail message when using SendEnhanced.
		/// (If empty, uses the default host for the SmtpClient or SmtpMail object.)
		/// </summary>
		public string SmtpServer {
			get { return _smtpServer; }
			set { _smtpServer = value; }
		}

		/// <summary>
		/// When true, logs all sucessful email sneds, otherwise logs only failures.
		/// This setting persists across multiple instances of the class, but only within a single thread.
		/// </summary>
		public bool VerboseLogging {
			get { return verboseLogging; }
			set {
				verboseLogging = value;
				if (_log != null)
					_log.Level = verboseLogging ? LogDetailLevel.Info : LogDetailLevel.Warning;
			}
		}

		/// <summary>
		/// Use this object to browse the results of multiple successive Send operations.
		/// This object persists across multiple instances of the class, but only within a single thread.
		/// </summary>
		public LoggingMemory Log {
			get {
				if (_log == null)
					_log = new LoggingMemory(this.GetType().Name, this.VerboseLogging ? LogDetailLevel.Info : LogDetailLevel.Warning);
				return _log;
			}
		}

		public string TemplateBodyText {
			get { return _templateBodyText; }
			set { _templateBodyText = value; }
		}

		public string TemplateSubject {
			get { return _templateSubject; }
			set { _templateSubject = value; }
		}

		public string TemplateBodyFileName {
			get { return _templateBodyFileName; }
			set { _templateBodyFileName = value; }
		}

		/// <summary>
		/// Use this dictionary to store add custom merge fields in code
		/// without all the muss and fuss of creating a full blown data table.
		/// Any items that match those in MergeFieldRow will override them.
		/// </summary>
		public IDictionary MergeFieldDictionary {
			get {
				if (_mergeNameValuePairs == null)
					_mergeNameValuePairs = new HybridDictionary();
				return _mergeNameValuePairs; }
		}

		/// <summary>
		/// You can bind this fieldName to a DataRow to set up merge fields.
		/// This can be used in conjunction with MergeFieldDictionary (which
		/// will take precedence for any items set in it).
		/// </summary>
		public DataRow MergeFieldRow {
			get { return _mergeFieldRow; }
			set { _mergeFieldRow = value; }
		}

		/// <summary>
		/// Provides a shorthand mechanism to assign all the template properties at once
		/// this is generally used to load items for different templates from the config file.
		/// </summary>
		public MailMessageTemplateData TemplateData {
			get {
				MailMessageTemplateData data = new MailMessageTemplateData(TemplateSubject, TemplateBodyText, TemplateBodyFileName);
				return data;
			}
			set {
				if (value != null) {
					TemplateSubject = value.Subject;
					TemplateBodyText = value.BodyText;
					TemplateBodyFileName = value.BodyFile;
				}
			}
		}

		public SmtpClient Client {
			get {
				if (_client == null)
					_client = CreateClient();
				return _client;
			}
			set {
				_client = value;
			}
		}

		public bool IsReadyToSend {
			get {
				return isReadyToSendAgain;
			}
		}

		#endregion

		#region Helper Methods

		private SmtpExtendedException WrapSendException(string infoMessage, Exception ex) {
			return WrapSendException(infoMessage, ex, true);
		}
		/// <summary>
		/// Wrap an exception in a SmtpExtendedException with detailed text
		/// and the ability to catch it in the caller so that they can be supressed.
		/// </summary>
		/// <param name="infoMessage">Text to include after the error to identify the cause (usually attempted email to-addy etc.)</param>
		/// <param name="ex">The inner excetption to wrap around</param>
		/// <param name="drillIntoCDONTS">If true will inclide buried CDONTS exception messages in the main exception message text. (This seems to be useful even in .NET 2.0!)</param>
		/// <returns></returns>
		private SmtpExtendedException WrapSendException(string infoMessage, Exception ex, bool drillIntoCDONTS) {
			string errorMessage = "Call to Send() threw exception for " + infoMessage;
			// in reality because .NET 1.x/2 Smtp was just a wrapper for CDONTS
			// the errors aren't really meaningful until you try to drill into them
			// (Bubbling the messages up makes debugging a whole lot faster.)
			// This still seems somewhat true in .NET 2.0 as well
			if (drillIntoCDONTS) {
				Exception CDONTSDrillDown = null;
				if (ex.InnerException != null)
					if (ex.InnerException.InnerException != null)
						CDONTSDrillDown = ex.InnerException.InnerException;
					else
						CDONTSDrillDown = ex.InnerException;
				if (CDONTSDrillDown != null) {
					errorMessage += " InnerException: " + CDONTSDrillDown.Message;
				}
			}
			SmtpExtendedException throwEx = new SmtpExtendedException(errorMessage, ex);
			LogItem item = new LogItem(LogDetailLevel.Error, errorMessage, string.Empty, throwEx);
			Log.Write(item);
			return throwEx;
		}

		private void ReadPropertiesFromConfig(string strConfigPrefix) {
      throw new NotSupportedException("This function is obsolete. You must set properties in your own implementation.");
      /*
			SmtpServer = ConfigTools.GetAppSetting(strConfigPrefix + "SMTPServer");
			From = new MailAddress(ConfigTools.GetAppSetting(strConfigPrefix + "TemplateFromAddy"));
			string formatKey = strConfigPrefix + "DefaultMailFormat";
			string strFormat = ConfigTools.GetAppSetting(formatKey).ToLower(CultureInfo.InvariantCulture);
			switch(strFormat) {
				case "text":
					this.IsBodyHtml = false;
					break;
				case "html":
					this.IsBodyHtml = true;
					break;
				default:
					throw new SmtpExtendedException(string.Format(
						CultureInfo.CurrentCulture,
						"Unrecognized value \"{0}\" for application setting key \"{1}\"",
						strFormat, formatKey
					));
			}
			//BodyEncoding = System.Text.Encoding.Unicode;
			TemplateSubject = ConfigTools.GetAppSetting(strConfigPrefix + "TemplateSubject");
			//if (StringTools.IsEmpty(TemplateBodyFileName))
			TemplateBodyText = ConfigTools.GetAppSetting(strConfigPrefix + "TemplateBodyText");
			TemplateBodyFileName = ConfigTools.GetAppSetting(strConfigPrefix + "TemplateBodyFileName");
       */
		}

		/// <summary>
		/// This function reads the body text from a file or config string and formats it appropriately.
		/// </summary>
		private void ReadAndFormatBodyText() {
			if (!string.IsNullOrEmpty(TemplateBodyFileName)) {
				if (!File.Exists(TemplateBodyFileName))
					throw new IOException("Template body file does not exist: " + TemplateBodyFileName);
				TextReader objBodyStream = File.OpenText(TemplateBodyFileName);
				TemplateBodyText = objBodyStream.ReadToEnd();
			} else {
        if (!string.IsNullOrEmpty(TemplateBodyText))
				  TemplateBodyText = StringTools.ReplaceEscapeSequences(TemplateBodyText);
			}
		}

		private void Merge() {
			if (!string.IsNullOrEmpty(TemplateSubject)) {
				Subject = TemplateSubject;
				Subject = this.Merge(Subject);
			}
			if (!string.IsNullOrEmpty(TemplateBodyText)) {
				Body = TemplateBodyText;
				Body = this.Merge(Body);
			}
		}

		/// <summary>
		/// A merge based on a DataTable or Dictionary.
		/// Replaces merge fieldName tags in the format %FIELDNAME% with
		/// the data in the corresponding row/column or item.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		private string Merge(string text) {
			if (this.MergeFieldDictionary != null && this.MergeFieldDictionary.Count > 0) {
				text = text.MergeFields(this.MergeFieldDictionary);
			}
			if (this.MergeFieldRow != null) {
        text = text.MergeFields(this.MergeFieldRow);
			}
			return text;
		}

		#endregion

		private SmtpClient CreateClient() {
			string host = SmtpServer;
			SmtpClient client;
			if (string.IsNullOrEmpty(host))
				client = new SmtpClient();
			else
				client = new SmtpClient(host);
			client.Timeout = 5000;
			client.SendCompleted += new SendCompletedEventHandler(AsyncSendCompleted);
			// TODO support reading the credentials for the mailserver from configuration or something
			//client.Credentials = new NetworkCredential("username", "password");
			return client;
		}

		/// <summary>
		/// This overload auto-generates an SmtpClient object (or the host string in .NET 1.x)
		/// </summary>
		public void SendEnhanced() {
			SendEnhanced(this.Client);
		}

		/// <summary>
		/// Send an email using advanced templates, merge fieldName information, and logging.
		/// </summary>
		/// <param name="client">The SmtpClient object used to connect to a host</param>
		/// <param name="host">Name of the SMTP server to connect to (.NET 1.x only)</param>
		public void SendEnhanced(SmtpClient client) {
			// TODO check for isReadyToSendAgain and wait if necessary
#if Async
			while (!isReadyToSendAgain) {
				// sleep for a bit
				// check for timeout reached 
				if (isTimedOut && !isReadyToSendAgain) {
					client.SendAsyncCancel();
				}
			}
#endif

			// set the template body from a file
			ReadAndFormatBodyText();
			// set the body and subject using the template data and merge fields
			this.Merge();

      // copy SmtpServer if it was not specified for client
      if (!string.IsNullOrEmpty(this.SmtpServer) && string.IsNullOrEmpty(client.Host))
        client.Host = this.SmtpServer;

			// if success - or additional info for failures
			string successMessage = string.Format(
				CultureInfo.InvariantCulture,
				"MailMessage sent to SMTP host '{0}'. To: '{1}'",
				client.Host, To
			);
			string cc = this.CC.ToString();
			string bcc = this.Bcc.ToString();
			if (!string.IsNullOrEmpty(cc) || !string.IsNullOrEmpty(bcc)) {
				successMessage += string.Format(
					CultureInfo.InvariantCulture,
					", Cc: {0}, Bcc: {1}",
					cc, bcc
				);
			}

			// send the message
			try {
				client.Send(this);
				// log successes
				Log.Write(LogDetailLevel.Info, successMessage);
			} catch (SmtpException ex) {
				throw WrapSendException(successMessage, ex);
				/* } catch (SmtpFailedRecipientsException ex) {
					throw WrapSendException(successMessage, ex);
				} catch (SmtpFailedRecipientException ex) {
					throw WrapSendException(successMessage, ex); */
			}
		} // Send(...)

		private void AsyncSendCompleted(object source, AsyncCompletedEventArgs args) {
			string successMessage = args.UserState.ToString();
			// log successes
			Log.Write(LogDetailLevel.Info, successMessage);
			if (args.Error != null) {
				WrapSendException(successMessage, args.Error);
				// we don't throw the error here, instead since it is in the logs
				// now we can throw it later if we choose
			}
			isReadyToSendAgain = true;
		}

		/// <summary>
		/// Clears all non-templated properties in anticipation of using the
		/// instance to send successive email messages.
		/// </summary>
		public void Clear() {
//			this.From = new MailAddress("invalid@somedomain.local", "INVALID ADDRESS");
			this.To.Clear();
			this.CC.Clear();
			this.Bcc.Clear();
			this.Subject = string.Empty;
			this.Body = string.Empty;
			this.MergeFieldDictionary.Clear();
			this.MergeFieldRow = null;
			this.Attachments.Clear();
		}

		public void AttachEnhanced(string filePath) {
			// attach the selected file
			if (!string.IsNullOrEmpty(filePath)) {
				if (!File.Exists(filePath)) {
					Log.Write(LogDetailLevel.Warning, "Could not attach file to message because no file exists at path '" + filePath + "'.");
				} else {
					//ContentType = new System.Net.Mime.ContentType(); ???
					Attachment attachFile = new Attachment(filePath);
					this.Attachments.Add(attachFile);
				}
			}
		}

	} // class MailMessageExtended

	/// <summary>
	/// Use this class to catch Exceptions generated by MailMessageExtended and realted
	/// classes, so that bulk email jobs can run uninterrupted regardless of the outcome
	/// of any single attempt to send a message.
	/// </summary>
	[Serializable]
	public class SmtpExtendedException : Exception {

		public SmtpExtendedException() { }
		public SmtpExtendedException(string message) : base(message) { }
		public SmtpExtendedException(string message, Exception innerException) : base(message, innerException) { }
		protected SmtpExtendedException(SerializationInfo info, StreamingContext context) : base(info, context) { }

	} // class

} // namespace Colossus.Tools.Smtp
