
namespace Kraken.IO {

    using System;
    using System.IO;
    using System.Text;

	/// <summary>
	/// Summary description for QuickLogWriter.
	/// </summary>
	public class QuickLogWriter {

		private string _logPath;

		public QuickLogWriter(string logPath) {
			_logPath = logPath;
		}

		/// <summary>
		/// sLogFormat used to create log files format :
		/// dd/mm/yyyy hh:mm:ss AM/PM ==> Log Message
		/// </summary>
		/// <returns></returns>
		private string GetLogPrefix() {
			string sLogFormat = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString()+" ==> ";
			return sLogFormat;
		}

		/// <summary>
		/// this variable used to create log filename format "
		/// for example filename : ErrorLogYYYYMMDD
		/// </summary>
		/// <returns></returns>
		public string GetLogDate() {
			string sYear    = DateTime.Now.Year.ToString();
			string sMonth    = DateTime.Now.Month.ToString();
			string sDay    = DateTime.Now.Day.ToString();
			string sErrorTime = sYear + sMonth + sDay;
			return sErrorTime;
		}

		public void Write(string sErrMsg) {
			string sErrorTime = GetLogDate();
			string sLogFormat = GetLogPrefix();
			StreamWriter sw = new StreamWriter(this._logPath + sErrorTime + ".log", true);
			sw.WriteLine(sLogFormat + sErrMsg);
			sw.Flush();
			sw.Close();
		}

	}
}
