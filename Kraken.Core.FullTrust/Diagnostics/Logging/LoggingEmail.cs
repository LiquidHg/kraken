
namespace Kraken.Diagnostics.Logging {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Mail;
    using System.Globalization;
    using System.Text;

    using Kraken.Net.Smtp;

	public class LoggingEmail : LoggingBase {

		public LoggingEmail(string name) : base(name) { }
		public LoggingEmail(string name, LogDetailLevel infoLevel) : base(name, infoLevel) { }

		// TODO since message is exposed there is really no good reason to have these
		private string fromAddy;
		private string toAddy;
		private MailMessageExtended _message;
	
		/// <summary>
		/// Allows customization of mail message settings and templates
		/// </summary>
		public MailMessageExtended Message {
			get { return _message; }
		}
		public string To {
			get { return toAddy; }
			set { toAddy = value; }
		}
		public string From {
			get { return fromAddy; }
			set { fromAddy = value; }
		}

		public override bool CanOpen {
			get { return false; }
		}
		public override bool IsOpen {
			get { throw new NotImplementedException("The method or operation is not implemented."); }
		}

		public override void Initialize() {
			if (_message == null)
				_message = new MailMessageExtended();
		}
		public override void Open() {
			throw new NotImplementedException("The method or operation is not implemented.");
		}
		public override void Close() {
			throw new NotImplementedException("The method or operation is not implemented.");
		}

		private static string BuildBody(bool includeCategory, bool includeException) {
			StringBuilder errorMessage = new StringBuilder();
			errorMessage.AppendLine("Date: %DATE%");
			errorMessage.AppendLine("Time: %TIME%");
			errorMessage.AppendLine("Level: %LEVEL%");
			if (includeCategory)
				errorMessage.AppendLine("Category: %CATEGORY%");
			errorMessage.AppendLine("Details: ");
			errorMessage.AppendLine("  %DETAILS%");
			if (includeException) {
				errorMessage.AppendLine("Exception Data: ");
				errorMessage.AppendLine("  Source: %EXSOURCE%");
				errorMessage.AppendLine("  Message: %EXMESSAGE%");
				errorMessage.AppendLine("  Stack Trace: %EXSTACK%");
			}
			return errorMessage.ToString();
		}

		public override void Write(LogDetailLevel infoLevel, string text) {
			Write(infoLevel, text, string.Empty);
		}
		public override void Write(LogDetailLevel infoLevel, string text, string category) {
			LogItem info = new LogItem(infoLevel, text, category, null);
			Write(info);
		}
		public override void Write(LogItem info) {
			if (!this.IsLevelThresholdReached(info.Level))
				return;
			DateTime now = DateTime.Now;

			// create notification message
			_message.TemplateSubject = "System Notification: %NAME%";
			// TODO get the mail template from a file
			_message.TemplateBodyText = BuildBody(!string.IsNullOrEmpty(info.Category), (info.RelatedException != null));
			_message.MergeFieldDictionary.Add("NAME", Name);
			_message.MergeFieldDictionary.Add("DATE", now.ToShortDateString());
			_message.MergeFieldDictionary.Add("TIME", now.ToShortTimeString());
			_message.MergeFieldDictionary.Add("LEVEL", info.Level.ToString());
			_message.MergeFieldDictionary.Add("CATEGORY", info.Category);
			_message.MergeFieldDictionary.Add("DETAILS", info.Text);
			if (info.RelatedException != null) {
				//_message.MergeFieldDictionary.Add("EXXML", info.RelatedException);
				_message.MergeFieldDictionary.Add("EXMESSAGE", info.RelatedException.Message);
				_message.MergeFieldDictionary.Add("EXSTACK", info.RelatedException.StackTrace);
				_message.MergeFieldDictionary.Add("EXSOURCE", info.RelatedException.Source);
			}
			_message.From = new MailAddress(this.From);
			_message.To.Add(this.To);
			// TODO add CC capability

			// send mail
			try {
				_message.SendEnhanced();
			} catch (SmtpExtendedException) {
				// nothing to do here. Go to the log if you want to know if it worked or not :-)
			}

		}

	} // class

} // namepace