
namespace Kraken.Diagnostics.Logging {

    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;

	public class LoggingFile : LoggingBase {

		public LoggingFile(string name) : base(name) { }
		public LoggingFile(string name, LogDetailLevel infoLevel) : base(name, infoLevel) { }

		public const string SeparatorLine = "--------------------------------------------------------------------------------"; // 80 dashes

		private string filePath;
		private StreamWriter fileStreamWriter;

		public string FilePath {
      get { return filePath; }
      set { filePath = value; }
		}

		public override void Initialize() {
			//throw new NotImplementedException("The method or operation is not implemented.");
		}

		public override void Close() {
			base.Close();
			fileStreamWriter.Close();
		}

		public override void Open() {
			base.Open();
			try {
				if (File.Exists(this.FilePath))
					fileStreamWriter = File.AppendText(this.FilePath);
				else
					fileStreamWriter = File.CreateText(this.FilePath);
			} catch (IOException) {
				fileStreamWriter = null;
				base.IsOpenProtected = false;
			}
		}

		public override bool IsOpen {
			get {
				return (base.IsOpen && fileStreamWriter != null);
			}
		}

		public void WriteHeading(string headingText) {
			if (!this.IsOpen)
				throw new InvalidOperationException("Can't write. Stream not open.");
			try {
				fileStreamWriter.WriteLine();
				fileStreamWriter.WriteLine(headingText);
				fileStreamWriter.WriteLine(SeparatorLine);
			} catch (IOException ex) {
				throw new IOException(string.Format(
					CultureInfo.CurrentCulture,
					"Could not write to file based log '{0}'"
					, FilePath), ex);
			}
		}

		public override void Write(LogDetailLevel infoLevel, string text) {
			Write(infoLevel, text, "{none}");
		}
		public override void Write(LogDetailLevel infoLevel, string text, string category) {
			LogItem info = new LogItem(infoLevel, text, category, null);
			Write(info);
		}
		public override void Write(LogItem info) {
			if (!this.IsOpen)
				throw new InvalidOperationException("Can't write. Stream not open.");
			if (!this.IsLevelThresholdReached(info.Level))
				return;
			DateTime now = DateTime.Now;
			string logLine = string.Format(
				CultureInfo.InvariantCulture,
				"Date: {0}, Time: {1}, Category: {2}, {3}",
				now.ToShortDateString(),
				now.ToShortTimeString(),
				info.Category, info.Text);
			try {
				fileStreamWriter.WriteLine(logLine);
			} catch (IOException ex) {
				throw new IOException(string.Format(
					CultureInfo.CurrentCulture,
					"Could not write to file based log '{0}'"
					, FilePath), ex);
			}
			// if there is an associated exception for the item, write it out in detail
			if (info.RelatedException != null) {
				this.WriteHeading("Exception");
				try {
					fileStreamWriter.WriteLine(info.RelatedException.Source);
					fileStreamWriter.WriteLine(info.RelatedException.Message);
					fileStreamWriter.WriteLine(info.RelatedException.StackTrace);
				} catch (IOException ex) {
					throw new IOException(string.Format(
						CultureInfo.CurrentCulture,
						"Could not write to file based log '{0}'"
						, FilePath), ex);
				}
			}

		} // if

	} // class

} // namespace