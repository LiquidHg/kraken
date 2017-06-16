// ----------------------------------------------------------------------------
// dotNet Development Tools. (c)2003-2008 Thomas Carpe. Some Rights Reserved.
// Contact me at: www.thomascarpe.com or dotnet@thomascarpe.com.
// Summary and specific terms of copyright are described in CopyrightTerms.txt file.
// ----------------------------------------------------------------------------

namespace Kraken.IO {

    using System;
    using System.IO;

	/// <summary>
  /// Summary description for StreamUtilities.
	/// </summary>
	public sealed class StreamUtilities {

		private StreamUtilities() { }

		public static byte[] ConvertStreamToByteArray(System.IO.Stream objStream) {
			byte[] binFile = new byte[objStream.Length + 1];
			int intByte = 0; long intPos = 0;
			while (intByte >= 0) { // will return -1 at EOF
				intByte = objStream.ReadByte();
				binFile[intPos++] = (byte)intByte;
			}
			return binFile;
		}

  } // class StreamUtilities

} // namespace Kraken.IO
