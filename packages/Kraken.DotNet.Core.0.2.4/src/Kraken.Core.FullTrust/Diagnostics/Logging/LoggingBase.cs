
namespace Kraken.Diagnostics.Logging {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

	public enum LogDetailLevel {
		None = 0,
		Debug = 1,
		Info = 2,
		Warning = 3,
		Error = 4,
		CriticalError = 5
	}

	public abstract class LoggingBase : IDisposable {

		#region Constructor / Destructor

		protected LoggingBase(string name) {
			logName = name;
			Initialize();
		}
		protected LoggingBase(string name, LogDetailLevel infoLevel)
			: this(name) {
			level = infoLevel;
		}
		~LoggingBase() {
			Dispose(false);
		}

		#endregion

		#region Previate Properties

		private LogDetailLevel level = LogDetailLevel.Warning;
		private string logName;
		private bool open;

		#endregion

		#region Public and Protected Properties

		public string Name {
			get { return logName; }
		}

		public LogDetailLevel Level {
			get { return level; }
			set { level = value; }
		}

		/// <summary>
		/// Indicates if the provided level should be logged.
		/// </summary>
		/// <param name="infoLevel">The level of the log item to test</param>
		/// <returns>True for loggable level, false for suppressed levels.</returns>
		protected bool IsLevelThresholdReached(LogDetailLevel infoLevel) {
			return (infoLevel >= this.Level);
		}

		/// <summary>
		/// True if the log is open and ready for writing
		/// </summary>
		public virtual bool IsOpen {
			get { return open; }
		}
		protected bool IsOpenProtected {
			get { return open; }
			set { open = value; }
		}

		#endregion

		// TODO make this output a little more real time - is there a way
		// that we can attach an event to the Write method??

		/// <summary>
		/// Open the log for writing
		/// </summary>
		public virtual void Open() {
			if (!this.CanOpen)
				throw new InvalidOperationException("Not an Openable logging class. Simply call 'Write'.");
			if (this.IsOpen)
				throw new InvalidOperationException("Already Open.");
			open = true;
		}

		/// <summary>
		/// Close the log when done
		/// </summary>
		public virtual void Close() {
			if (!this.IsOpen)
				throw new InvalidOperationException("Can't Close because class is not Open.");
			open = false;
		}

		#region Abstract methods

		/// <summary>
		/// Write a single-line entery to the log.
		/// </summary>
		/// <param name="infoLevel"></param>
		/// <param name="category"></param>
		/// <param name="text"></param>
		public abstract void Write(LogDetailLevel infoLevel, string text, string category);
		public abstract void Write(LogDetailLevel infoLevel, string text);
		public abstract void Write(LogItem info);

		/// <summary>
		/// Indicates if Open and Close methods have any effect.
		/// </summary>
		public virtual bool CanOpen {
			get { return true; }
		}

		/// <summary>
		/// Perform initial setup of the logging class so that the log can be opened and written to.
		/// </summary>
		public abstract void Initialize();

		#endregion

		#region IDisposable Members

		private bool _isDisposed;

		/// <summary>
		/// Implement IDisposable:
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Implement IDisposable: closes any open connections to logs.
		/// </summary>
		/// <param name="disposing">Has no effect at this time.</param>
		protected virtual void Dispose(bool explicitlyCalled) {
			if (!_isDisposed) {
				if (explicitlyCalled) {
					// Free other state (managed objects)
				}
				if (this.CanOpen && this.IsOpen)
					Close();
				// Destroys and dereferences all contained properties
				_isDisposed = true;
			}
		}

		#endregion

	} // class

	/// <summary>
	/// Stores all the information you would usually ever want to put into a log.
	/// In some cases this can be extended to provide additional data.
	/// </summary>
	public class LogItem {

		public LogItem() {}
		public LogItem(LogDetailLevel infoLevel, string text, string category, Exception ex) {
			this.Text = text;
			this.Category = category;
			this.Level = infoLevel;
			this.RelatedException = ex;
		}

		private string text;
		private string category;
		private LogDetailLevel infoLevel = LogDetailLevel.None;
		private Exception ex;

		public string Text {
			get { return text; }
			set { text = value; }
		}

		public string Category {
			get { return category; }
			set { category = value; }
		}

		public LogDetailLevel Level {
			get { return infoLevel; }
			set { infoLevel = value; }
		}

		public Exception RelatedException {
			get { return ex; }
			set { ex = value; }
		}

	} // class

} // namespace
