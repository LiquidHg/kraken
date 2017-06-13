using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Kraken.SharePoint.Client {

	public static class ContentTypeIdHelper {

		[System.Reflection.Obfuscation(Exclude = false)]
		public static string GetHexGuid(int digits, bool avoidDoubleZero = true) {
			// Now, this looks more like real randomization.
			Random random = new Random();

			byte[] buffer = new byte[digits / 2];
			random.NextBytes(buffer);
			string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
			if (digits % 2 != 0)
				result += random.Next(16).ToString("X");
			if (!avoidDoubleZero)
				return result;
			// check for any sequence of two digits, lets replace it with something else
			bool isPrevZero = false;
			StringBuilder newResult = new StringBuilder();
			foreach (char c in result.ToCharArray()) {
				//char c in result.ToCharArray()) {
				string replaceChar = string.Empty;
				if (c != '0') {
					// just add the next char to the string
					isPrevZero = false;
				} else {
					if (!isPrevZero) {
						// if its a zero and the previous char wasn't a zero, mark the next char for possible action
						isPrevZero = true;
					} else {
						// if its a zero and the previous char was also a zero, find a non-zero replacement
						int notZero = 0;
						while (notZero == 0) { notZero = random.Next(16); }
						replaceChar = notZero.ToString("X");
						isPrevZero = false;
					}
				}
				newResult.Append(string.IsNullOrEmpty(replaceChar) ? c.ToString() : replaceChar);
			}
			return newResult.ToString();
		}

	}
}

