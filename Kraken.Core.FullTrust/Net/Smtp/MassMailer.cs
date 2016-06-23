
namespace Kraken.Net.Smtp {

    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Net.Mail;
    using System.Net.Mime;

    using Kraken.IO;
    using Kraken.Diagnostics.Logging;
    using Kraken;

	/// <summary>
	/// Allows the sending of a large number of emails at one time.
	/// </summary>
	public class MassMailer : IDisposable {

		public MassMailer() { }
		~MassMailer() {
			Dispose(false);
		}

		private string _fieldNameEmail; // = ConfigTools.GetAppSetting("Colossus.Smtp.MassMailer.FieldName_Email");
		private string _attachmentFilePath;
		private MailMessageExtended _message;

		public string FieldNameEmailTo {
			get { return _fieldNameEmail; }
			set { _fieldNameEmail = value;  }
		}

		public string AttachmentFilePath {
			get { return _attachmentFilePath; }
			set { _attachmentFilePath = value; }
		}

		/// <summary>
		/// Note that you can override the properties of the message at any time,
		/// including before the call to SendAll.
		/// </summary>
		public MailMessageExtended Message {
			get {
				if (_message == null)
					_message = new MailMessageExtended();
				return _message;
			}
		}

		public void SendAll(string bulkListFilePath) {
			if (Message.From == null)
				throw new SmtpExtendedException(Messages.ExceptionMessageNoFromAddress);

			CsvReader importBulkList = new CsvReader(bulkListFilePath);
			importBulkList.Name = "MassMailerImportList";
			SendAll(importBulkList.Table);
		}
		public void SendAll(DataTable mergeTable) {
			if (Message.From == null)
				throw new SmtpExtendedException(Messages.ExceptionMessageNoFromAddress);
			// loop through records
			int intCount = 1;
			foreach (DataRow objRow in mergeTable.Rows) {
				string strToAddy = objRow.ItemArray[mergeTable.Columns.IndexOf(FieldNameEmailTo)].ToString();
				if (string.IsNullOrEmpty(strToAddy)) {
					LogItem info = new LogItem(
						LogDetailLevel.Warning,
						string.Format(
							CultureInfo.InvariantCulture,
							Messages.WarningMessageEmailAddressNotVali,
							intCount, strToAddy),
						"MassMailer",
						null
					);
					Message.Log.Write(info);
				} else {
					// setup mail message
					Message.To.Clear();
					try {
						Message.To.Add(strToAddy);
					} catch (FormatException ex) {
						LogItem info = new LogItem(
							LogDetailLevel.Warning,
							string.Format(
								CultureInfo.InvariantCulture,
								Messages.WarningMessageEmailAddressNotVali,
								intCount, strToAddy),
							"MassMailer",
							ex
						);
						Message.Log.Write(info);
					}
					Message.MergeFieldRow = objRow;
					// attach the selected file
					Message.AttachEnhanced(this.AttachmentFilePath);
					// send message
					if (Message.To.Count > 0) {
						try {
							Message.SendEnhanced();
						} catch (SmtpExtendedException) {
							// nothing to do here, we just collect the errors in the log and deal with them later
						}
					}
					Message.Clear();
				}
				intCount++;
			} // end loop
		} // SendAll(...)

		private sealed class Messages {
			private Messages() { }
			public const string ExceptionMessageNoFromAddress = "Can't send bulk mail without a From address. You should also ensure that other properties are populated before calling SendAll().";
			public const string WarningMessageEmailAddressNotVali = "The send to address for data row #{0} was invalid. No message was sent. Address value was '{1}'.";
		}

		#region IDisposable Members

		private bool _isDisposed;

		public void Dispose() {
			Dispose(true);
		}
		public void Dispose(bool explicitlyCalled) {
			if (!_isDisposed) {
				if (explicitlyCalled) {
					// destroy the contained message, because we asked nicely
					_message.Dispose();
					_message = null;
				}
				// mark as disposed and supress finalize in GC
				_isDisposed = true;
				GC.SuppressFinalize(this);
			}
		}

		#endregion
} // class MassMailer

} // namespace Colossus.Smtp
