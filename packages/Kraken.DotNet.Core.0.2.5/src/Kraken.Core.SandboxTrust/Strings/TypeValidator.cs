/*
  This file is part of SPARK: SharePoint Application Resource Kit.
  The project is distributed via CodePlex: http://www.codeplex.com/spark/
  Copyright (C) 2003-2009 by Thomas Carpe. http://www.ThomasCarpe.com/

  SPARK is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  SPARK is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with SPARK.  If not, see <http://www.gnu.org/licenses/>.
*/
/*
  DotNet Tools by Thomas Carpe
  TypeValidator by Thomas Carpe and Charlie Hill
  Copyright (C)2006, 2008 Thomas Carpe and Charlie Hill. Some Rights Reserved.
  Contact: dotnet@Kraken.com, chill@chillweb.net
 
  The classes in this file were written jointly and are the mutual property of both authors.
  They are licensed for use under the Creative Commons license. Rights reserved include
  "Share and Share Alike", and "Attribution". You may use this code for commercial purposes
  and derivative works, provided that you maintain this copyright notice.
*/
namespace Kraken {

    using System;
    using System.Text.RegularExpressions;

  /// <summary>
  /// This class provides basic functionality for testing strings to see if they are
  /// of a particular type. In some cases, you may wish to use these methods as opposed
  /// to exception handling around a Parse method if there is a lot of looping that might
  /// negatively impact performance.
  /// </summary>
	public static class TypeValidator {

		private static Regex regexIsDecimal = new Regex(@"^(-|\+|)\d+(\.|)\d*$", RegexOptions.Compiled);
		private static Regex regexIsInteger = new Regex(@"^(-|\+|)\d+$", RegexOptions.Compiled);

		public static bool IsInteger(string value) {
			Match m = regexIsInteger.Match(value);
			return m.Success;
		}

		public static bool IsDecimal(string value) {
			Match m = regexIsDecimal.Match(value);
			return m.Success;
		}

		public static bool IsBoolean(string value) {
			return (value.ToUpper().Trim() == "TRUE" || value.ToUpper().Trim() == "FALSE");
		}

    /// <summary>
    /// This one is really not any faster because it uses an excepotion to test the object.
    /// However, you might want to test your objects anyway if you want to write good code. ;-)
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
		public static bool IsDateTime(string value) {
      try {
        DateTime d = DateTime.Parse(value);
        return true;
      } catch { // (Exception ex) {
        return false;
      }
    }

    /// <summary>
    /// This one is really not any faster because it uses an excepotion to test the object.
    /// However, you might want to test your objects anyway if you want to write good code. ;-)
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsGuid(string value) {
      try {
        Guid g = new Guid(value);
        return true;
      } catch { // (Exception ex) {
        return false;
      }
    }

	} // class
} // namespace
