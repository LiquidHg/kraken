
namespace Kraken.Diagnostics.Logging {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

	/// <summary>
	/// Stores log items in an ArrayList for later use within the program itself.
	/// This is useful for logging batch actions where a short lived report of the results
	/// is required, but long term storage is not really necessary.
	/// </summary>
	public class LoggingMemory : LoggingBase {

		public LoggingMemory(string name) : base(name) { }
		public LoggingMemory(string name, LogDetailLevel infoLevel) : base(name, infoLevel) { }

		private ArrayList logItems;
		public ArrayList Items {
			get { return logItems; }
		}

		public override void Initialize() {
			logItems = new ArrayList();
		}
		public override void Close() {
			throw new NotImplementedException("The method or operation is not implemented.");
		}
		public override void Open() {
			throw new NotImplementedException("The method or operation is not implemented.");
		}
		public override bool CanOpen {
			get { return false; }
		}
		public override void Write(LogDetailLevel infoLevel, string text) {
			Write(infoLevel, text, string.Empty);
		}
		public override void Write(LogDetailLevel infoLevel, string text, string category) {
			LogItem info = new LogItem(infoLevel, text, category, null);
			Write(info);
		}
		public override void Write(LogItem info) {
			if (this.IsLevelThresholdReached(info.Level))
				Items.Add(info);
		}

	} // class


} // namespace
