// ----------------------------------------------------------------------------
// dotNet Development Tools. (c)2003-2008 Thomas Carpe. Some Rights Reserved.
// Contact me at: www.thomascarpe.com or dotnet@thomascarpe.com.
// Summary and specific terms of copyright are described in CopyrightTerms.txt file.
// ----------------------------------------------------------------------------

namespace Kraken.IO {

    using System;
    using System.IO;
    using System.Text;

	/// <summary>
	/// Summary description for TextMemoryStream.
	/// </summary>
	public class TextMemoryStream : MemoryStream {

		public TextMemoryStream() : base() {}

		public string Read() {
			// Set the position to the beginning of the stream.
			Seek(0, SeekOrigin.Begin);
			// Read the first 20 bytes from the stream.
			byte[] byteArray = new byte[this.Length];
			int intCount = this.Read(byteArray, 0, 255);
			// Read the remaining bytes, byte by byte.
			while(intCount < this.Length) {
				byteArray[intCount++] = System.Convert.ToByte(this.ReadByte());
			}
			// Decode the byte array into a char array 
			//UnicodeEncoding objEncoding = new UnicodeEncoding();
			ASCIIEncoding objEncoding = new ASCIIEncoding();

			char[] charArray = new char[objEncoding.GetCharCount(byteArray, 0, intCount)];
			objEncoding.GetDecoder().GetChars(byteArray, 0, intCount, charArray, 0);
			string objString = new String(charArray);
			return objString;
		}

	} // class TextMemoryStream

} // namespace
