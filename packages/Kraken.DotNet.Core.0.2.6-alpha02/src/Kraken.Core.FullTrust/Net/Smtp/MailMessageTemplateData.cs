
namespace Kraken.Net.Smtp {

    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Data;
    using System.IO;

    using Kraken;

	/// <summary>
	/// When using many different templates for messages, this class can help to load and store
	/// the values needed to populate the MailMessageExtended object.
	/// </summary>
	public class MailMessageTemplateData {

		public MailMessageTemplateData() { }
		public MailMessageTemplateData(string configurationPrefix, bool throwException) {
			ReadConfig(configurationPrefix, throwException);
		}
		public MailMessageTemplateData(string subject, string bodyText, string bodyFile) {
			this.Subject = subject;
			this.BodyText = bodyText;
			this.BodyFile = bodyFile;
		}

		private string _subject;
		private string _bodyText;
		private string _bodyFile;

		public string Subject {
			get { return _subject; }
			set { _subject = value; }
		}
		public string BodyText {
			get { return _bodyText; }
			set { _bodyText = value; }
		}
		public string BodyFile {
			get { return _bodyFile; }
			set { _bodyFile = value; }
		}

		/// <summary>
		/// Reads the values from a specified set of AppSettings keys in the .config file
		/// </summary>
		/// <param name="strKeyPrefix"></param>
		/// <param name="boolThrowException"></param>
		public void ReadConfig(string keyPrefix, bool throwException) {
      throw new NotSupportedException("This function is obsolete and needs to be replaced.");
			/*
      Subject = ConfigTools.GetAppSetting(keyPrefix + "Subject", throwException);
			BodyText = ConfigTools.GetAppSetting(keyPrefix + "BodyText", throwException);
			BodyFile = ConfigTools.GetAppSetting(keyPrefix + "BodyFile", throwException);
       */
		}

	} // class

} // namespace Colossus.Tools.Smtp
