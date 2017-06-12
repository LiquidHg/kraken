
namespace Kraken.IO {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Globalization;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Text;

	/// <summary>
	/// This class imports a CSV file and makes it available as a DataTable
	/// as well as other formats.
	/// </summary>
	public class CsvReader : IDisposable {

		#region Private Properties and Constants

		// These have some kind of public accessor, usi it instead if possible
		private string _name;
		//private string _filePath;
		private int _fields;
		private TextReader _reader;
		private ArrayList _values;
		private ArrayList _columns;
		private DataTable _table;
		private char _quoteChar;

		// these don't have public access at all
		/// <summary>
		/// possible values ',', ';', '\t', '|'
		/// </summary>
		private char _colDelim;
		private int _pos;
		private int _used;
		private char[] _buffer;
		// /// <summary>
		// /// Assumes end of record delimiter is {CR}{LF}, {CR}, or {LF}
		// /// Possible values are {CR}{LF}, {CR}, {LF}, ';', ',', '\t'
		// /// </summary>
		// char _recDelim;

		private const int defaultBufferSize = 4096;
		private const int EOF = 0xffff;

		#endregion

		#region Constructor / Destructor

		public CsvReader(string location) : this(new Uri(location)) { }
		public CsvReader(Uri location)
			: this(location, string.Empty, defaultBufferSize) { }
		public CsvReader(Uri location, string proxy)
			: this(location, proxy, defaultBufferSize) { }
		public CsvReader(Stream stream)
			: this(stream, defaultBufferSize) { }
		public CsvReader(TextReader reader)
			: this(reader, defaultBufferSize) { }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="location">the location of the .csv file</param>
		/// <param name="stream">the location of the .csv file</param>
		/// <param name="reader">the location of the .csv file</param>
		/// <param name="proxy"></param>
		/// <param name="bufferSize">size in bytes of the buffer</param>
		public CsvReader(Uri location, string proxy, int bufferSize) {  // 
			if (location.IsFile) {
				string filePath = location.LocalPath;
				if (!File.Exists(filePath))
					throw new IOException("File does not exist: " + filePath);
				_reader = new StreamReader(filePath, true);
			} else {
				WebRequest wr = WebRequest.Create(location);
				if (!string.IsNullOrEmpty(proxy))
					wr.Proxy = new WebProxy(proxy);
				wr.Credentials = CredentialCache.DefaultCredentials;
				Stream stm = wr.GetResponse().GetResponseStream();
				_reader = new StreamReader(stm, true);
			}
			_buffer = new char[bufferSize];
			_values = new ArrayList();
		}

		public CsvReader(Stream stream, int bufferSize) {
			_reader = new StreamReader(stream, true);
			_buffer = new char[bufferSize];
			_values = new ArrayList();
		}
		public CsvReader(TextReader reader, int bufferSize) {
			_reader = reader;
			_buffer = new char[bufferSize];
			_values = new ArrayList();
		}

		~CsvReader() {
			Dispose(false);
		}


		/*
		public CsvReader(string name, string filePath) {
			_name = name;
			_filePath = filePath;
			if (!File.Exists(_filePath))
				throw new IOException("File does not exist: " + _filePath);

		 * // open the file
			_reader = File.OpenText(_filePath);
		  
			ImportFromCsvStream();
		}
		public CsvReader(string name, StreamReader stream) {
			_name = name;
			_reader = stream;

		 ImportFromCsvStream();
		}
		 */

		#endregion

		#region Read Only Public Properties

		public ArrayList ColumnNames {
			get {
				if (_columns == null)
					_columns = new ArrayList();
				return _columns;
			}
		}
		public TextReader Reader {
			get { return _reader; }
		}
		public char QuoteChar {
			get { return _quoteChar; }
		}
		public int FieldCount {
			get { return _fields; }
		}
		public string this[int index] {
			get { return GetValue(index); }
		}
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1306:SetLocaleForDataTypes")]
		public DataTable Table {
			get {
				if (_table == null) {
					if (string.IsNullOrEmpty(this.Name)) {
						_table = new DataTable();
						this.Name = _table.TableName;
					} else {
						_table = new DataTable(this.Name);
					}
					_table.Locale = CultureInfo.InvariantCulture; // TODO is this right?
				}
				return _table;
			}
		}

		#endregion
		#region Settable Public Properties

		public char Delimiter {
			get { return _colDelim; }
			set { _colDelim = value; }
		}
		public string Name {
			get { return _name; }
			set {
				_name = value;
				if (this.Table != null)
					Table.TableName = value;
			}
		}

		#endregion

		private string GetValue(int index) {
			if (index >= FieldCount)
				throw new ArgumentOutOfRangeException("index", "index (" + index + ") must be less than FieldCount, which is " + FieldCount);
			return ((StringBuilder)_values[index]).ToString();
		}

		private StringBuilder AddField() {
			if (_fields == _values.Count) {
				_values.Add(new StringBuilder());
			}
			StringBuilder sb = (StringBuilder)_values[_fields++];
			sb.Length = 0;
			return sb;
		}

		private char ReadChar() {
			if (_pos == _used) {
				_pos = 0;
				_used = _reader.Read(_buffer, 0, _buffer.Length);
			}
			if (_pos == _used) {
				return (char)0;
			}
			return _buffer[_pos++];
		}


		/// <summary>
		/// Called when ReadChar returns a quote, so that whatever is inside
		/// those quotes can be fully read without moving on to a new fieldName.
		/// </summary>
		/// <param name="ch">The character that triggered the quote, (usually " or ')</param>
		/// <returns>When it is done, the next character on the stream is returned to continue processing</returns>
		private char ReadInsideQuotes(char ch, ref StringBuilder sb) {
			bool done = false;
			_quoteChar = ch;
			char c = ReadChar();
			while (!done && c != 0) {
				while (c != 0 && c != _quoteChar) { // scan literal.
					sb.Append(c);
					c = ReadChar();
				}
				if (c == _quoteChar) {
					done = true;
					char next = ReadChar(); // consume end quote
					if (next == _quoteChar) {
						// it was an escaped quote sequence "" inside the literal
						// so append a single " and consume the second end quote.
						done = false;
						sb.Append(next);
						c = ReadChar();
					} else if (_colDelim != 0 && next != _colDelim && next != 0 && !IsLineBreakCharacter(next)) {
						// it was an un-escaped quote embedded inside a string literal
						// in this case the quote is probably just part of the text so ignore it.
						done = false;
						sb.Append(c);
						sb.Append(next);
						c = ReadChar();
					} else {
						c = next;
					}
				} // if (c == _quoteChar)
			} // while (!done && c != 0)
			return c;
		}

		/// <summary>
		/// Reads a record on a single line.
		/// When you are done, use this[index] to get the values
		/// </summary>
		/// <returns></returns>
		public bool Read() {
			return ReadLine();
		}
		/// <summary>
		/// Reads a record on a single line.
		/// When you are done, use this[index] to get the values
		/// </summary>
		/// <returns></returns>
		public bool ReadLine() {
			_fields = 0;

			char ch = ReadChar();
			if (ch == 0)
				return false;
			// cycle through any spaces and linefeeds
			while (ch != 0 && IsLineBreakCharacter(ch) || ch == ' ')
				ch = ReadChar();
			if (ch == 0)
				return false;
			// break for any linefeed character
			while (ch != 0 && !IsLineBreakCharacter(ch)) {
				StringBuilder sb = AddField();
				// if quoted sequence begins
				if (IsQuoteCharacter(ch)) {
					ch = ReadInsideQuotes(ch, ref sb);
				} else { // not quoted character
					// scan number, date, time, float, etc.
					while (ch != 0 && !IsLineBreakCharacter(ch)) {
						if (IsEndOfFieldCharacter(ch))
							break;
						sb.Append(ch);
						ch = ReadChar();
					}
				}
				if (IsEndOfFieldCharacter(ch)) {
					_colDelim = ch; // all future fields must use the same delimiter
					ch = ReadChar();
					if (IsLineBreakCharacter(ch)) {
						sb = AddField(); // blank fieldName.
					}
				}
			}
			// based on what is in the _values collection, create a DataRow
			// and drop it into the table, first set up column names if necessary
			if (!CopyColumnNames()) { // 
				CopyDataRow();
			}
			// TODO change table behavior so that column names are optional
			return true;
		}

		/// <summary>
		/// true if a character represents the end of a fieldName
		/// when _colDelim is '\0' that we use the default of
		/// any of ',' ';' tab or '|' 
		/// </summary>
		/// <param name="ch">character to test</param>
		/// <returns></returns>
		private bool IsEndOfFieldCharacter(char ch) {
			return (
				ch == _colDelim ||
				(_colDelim == '\0' && (ch == ',' || ch == ';' || ch == '\t' || ch == '|'))
			);
		}
		private static bool IsLineBreakCharacter(char ch) {
			 return (ch == '\n' || ch == '\r');
		}
		private static bool IsQuoteCharacter(char ch) {
			return (ch == '\'' || ch == '"');
		}

		public void Close() {
			_reader.Close();
		}

		// note that these methods are not yet fully interoperable with Read()
		#region Methods provided for DataTable interoperability

		/// <summary>
		/// Explicitly reads the curreent line of text data and copies it into
		/// the ColumnNames collection for later use. This can only be done once,
		/// as copying over existing columns will have no effect.
		/// </summary>
		public bool ReadColumnNames() {
			bool result = this.ReadLine();
			if (result)
				result = CopyColumnNames();
			return result;
		}

		/// <summary>
		/// Copies the current row into the ColumnNames collection for later use
		/// </summary>
		/// <returns>true if data was read, otherwise false</returns>
		private bool CopyColumnNames() {
			// If column names were already provided then we just skip this row
			if (this.ColumnNames != null && this.ColumnNames.Count > 0)
				return false;
			for (int i = 0; i < this.FieldCount; i++) {
				string name = this[i];
				ColumnNames.Add(name);
				// TODO there is no real way to know they are strings
				Table.Columns.Add(name, typeof(System.Object));
			}
			return true;
		}

		/// <summary>
		/// Copies the current row into a new DataRow in the table
		/// </summary>
		/// <returns>true if data was read, otherwise false</returns>
		private bool CopyDataRow() {
			if (Table.Columns.Count <= 0)
				return false;
			//DataRow row = this.Table.NewRow();
			object[] newItems = new object[this.FieldCount];
			for (int i = 0; i < this.FieldCount; i++) {
				newItems[i] = _values[i];
			}
			Table.Rows.Add(newItems);
			return true;
		}

		/// <summary>
		/// Reads the entire stream, one line at a time. When finished
		/// use the Table property to access the data.
		/// </summary>
		public void ImportToTable() {
			bool boolDone = false;
			do {
				try {
					boolDone = !this.ReadLine();
				} catch (System.IO.EndOfStreamException) {
					boolDone = true;
				}
			} while (!boolDone);
			// after we are done, the table should be "ready to go"
		}

		#endregion

		#region IDisposable Members

		private bool _isDisposed;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		public void Dispose(bool explicitlyCalled) {
			if (!_isDisposed) {
				if (explicitlyCalled) {
					// no effect today! :-P
				}
				this._reader = null;
				this._table = null; // don't call dispose since some other object may have picked up the table
				_isDisposed = true;
			}
		}

		#endregion

	} // class

} // namespace
