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
  Parser by Thomas Carpe and Charlie Hill
  Copyright (C)2006, 2008 Thomas Carpe and Charlie Hill. Some Rights Reserved.
  Contact: dotnet@Kraken.com, chill@chillweb.net
 
  The classes in this file were written jointly and are the mutual property of both authors.
  They are licensed for use under the Creative Commons license. Rights reserved include
  "Share and Share Alike", and "Attribution". You may use this code for commercial purposes
  and derivative works, provided that you maintain this copyright notice.
*/

namespace Kraken {

    using System;
    using System.Collections.Specialized;
    using System.Collections.Generic;
    using System.Reflection;

    [Flags]
    public enum ParseFlags {
        Simple = 1,
        Invoke = 2
    }

    /// <summary>
    /// This static class implements a universal Parse method that can convert a string
    /// into any specified type which implements a Parse method. For performance reasons,
    /// simple parsing of known .NET types and reflection based parsing cna be called
    /// either togehter or seperately.
    /// </summary>
    public class Parser {

        private const string ParseMethodName = "Parse";

        private Parser() { }

        /// <summary>
        /// A method similar to TryParse in .NET 2.0, but with more options about 
        /// what types can be parsed. This method does throw and handle exceptions, 
        /// so it may not perform as well as one that uses type-checking.
        /// </summary>
        /// <param name="value">The string that contains the value you want to parse</param>
        /// <param name="targetType">The expected type of the object represented by 'value'</param>
        /// <param name="flags">set flags to specify the types of parsing that should be tried</param>
        /// <param name="result">The parsed result as an output parameter</param>
        /// <returns>A boolean, true if parse was successful</returns>
        public static bool TryParse(string value, Type targetType, ParseFlags flags, out object result) {
            result = null;
            try {
                result = Parse(value, targetType, flags);
                return (result != null);
            } catch {
                return false;
            }
        }

        /// <summary>
        /// Attempts multiple types of string parsing. You can specify the types of parsing
        /// using the flags parameter, which you may want ot do for performance reasons.
        /// </summary>
        /// <param name="value">The string that contains the value you want to parse</param>
        /// <param name="targetType">The expected type of the object represented by 'value'</param>
        /// <param name="flags">set flags to specify the types of parsing that should be tried</param>
        /// <param name="throwException">True if you want invoke based parse to throw an excpetion when the type does not implement a Parse method</param></param>
        /// <returns>The parsed result or null if not successful</returns>
        public static object Parse(string value, Type targetType, ParseFlags flags) {
            return Parse(value, targetType, flags, true);
        }
        public static object Parse(string value, Type targetType, ParseFlags flags, bool throwException) {
            object o = null;
            if (((int)flags & (int)ParseFlags.Simple) > 0)
                o = Parse_Simple(value, targetType);
            if (o == null && ((int)flags & (int)ParseFlags.Invoke) > 0)
                o = Parse_Invoke(value, targetType, true);
            return o;
        }

        private static object Parse_Simple(string value, Type targetType) {
            switch (targetType.FullName) {
                // Text
                case "System.Char": return System.Char.Parse(value);
                case "System.String": return value;
                // Numeric
                case "System.Boolean": return System.Boolean.Parse(value);
                case "System.Decimal": return System.Decimal.Parse(value);
                case "System.Double": return System.Double.Parse(value);
                case "System.Int16": return System.Int16.Parse(value);
                case "System.Int32": return System.Int32.Parse(value);
                case "System.Int64": return System.Int64.Parse(value);
                case "System.SByte": return System.SByte.Parse(value);
                case "System.Single": return System.Single.Parse(value);
                case "System.UInt16": return System.UInt16.Parse(value);
                case "System.UInt32": return System.UInt32.Parse(value);
                case "System.UInt64": return System.UInt64.Parse(value);
                // Guid
                case "System.Guid": return new Guid(value);
                // Date / Time
                case "System.DateTime": return System.DateTime.Parse(value);
                default:
                    if (targetType.IsEnum) { // Parse Enumerations
                        // implements a layer to parse enums with string values in Attributes
#if DOTNET_V35
                      throw new NotSupportedException("Sorry ParseStringEnum is not supported in .NET 3.5");
#else
                      return ParseStringEnum(targetType, value, true);
                      // tried this before, its too complex to use in this case because of the generics
                        //Enum myEnum = StringEnum<targetType>.Parse(value);
                        //return myEnum;
#endif
                    } else
                        return null;
            } // switch
        }

#if !DOTNET_V35
        public static object ParseStringEnum(Type targetType, string value, bool ignoreCase) {
            foreach (Enum e in Enum.GetValues(targetType)) {
                string currentValueFieldName = e.ToString();
                EnumStringValueAttribute[] attrs =
                    targetType.GetField(currentValueFieldName).GetCustomAttributes(
                        typeof(EnumStringValueAttribute), false
                    ) as EnumStringValueAttribute[];
                if (attrs.Length > 0) {
                    string testValue = attrs[0].Value;
                    if (string.Compare(testValue, value, ignoreCase) == 0)
                        return e; // if this gives us any typing trouble we could use parse to get the strong type because we have that value too
                }
                // else just keep going
            }
            // if we get through all possible values without a match, just parse it like any other enum
            return Enum.Parse(targetType, value, ignoreCase);
        }
#endif

        private static object Parse_Invoke(string value, Type targetType, bool throwException) {
            if (!HasParseMethod(targetType))
                if (throwException)
                    throw new NotSupportedException(targetType.FullName + " is not a supported targetType. The specified type should implement a static method named Parse with one string parameter. ");
                else
                    return null;

            // make an effort to see if it has a Parse(string value) method
            object[] args = new object[] { value }; // string
            BindingFlags flags = BindingFlags.Default | BindingFlags.InvokeMethod;
            return targetType.InvokeMember(ParseMethodName, flags, null, targetType, args);
        }

        /// <summary>
        /// Detect if the target type has a parse method that takes one string as a parameter.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool HasParseMethod(Type type) {
            foreach (MethodInfo mi in type.GetMethods(BindingFlags.Static | BindingFlags.Public)) {
                if (mi.Name == ParseMethodName && mi.GetParameters().Length == 1 && mi.GetParameters()[0].ParameterType == typeof(string)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Converts a list of delimited strings and converts it
        /// to a NameValueCollection of keys and values.
        /// </summary>
        /// <param name="args">The list of delimiter-seperated arguments</param>
        /// <param name="delimiterText">The delimiter used to determine the place to seperate keys from values. Only the first occurance has an effect.</param>
        /// <param name="stripQuotes">If true, remove quotes from the value text</param>
        /// <returns></returns>
        /// <remarks>
        /// For example, converts "sharepoint", "/Title:\"Something Awesome\"", "/Category:MOSS", "/Date:\"Today\""
        /// to {"sharepoint", ""|, {"/Title", "Something Awesome"}, {"/Category", "MOSS"}, {"/Date", "Today"}
        /// </remarks>
        public static NameValueCollection CreateParameterCollection(IList<string> args, string delimiterText, bool stripQuotes) {
            NameValueCollection values = new NameValueCollection();
            if (args == null || args.Count <= 0)
                return values;
            foreach (string arg in args) {
                bool insideQuotes = false;
                string key = string.Empty;
                string value = string.Empty;
                bool inKey = true;
                for (int i = 0; i < arg.Length; i++) {
                    if (!insideQuotes && inKey && arg.Substring(i).StartsWith(delimiterText)) {
                        i += delimiterText.Length - 1;
                        inKey = false;
                    } else {
                        if (arg[i] == '\"') {
                            insideQuotes = !insideQuotes;
                            if (stripQuotes)
                                continue; // skip quotes if this is called for
                        }
                        if (inKey) {
                            key += arg[i];
                        } else {
                            value += arg[i];
                        }
                    }
                }
                values.Add(key, value);
            }
            return values;
        }

        /// <summary>
        /// Splits a string into its various delimited parts, respecting quotation marks.
        /// </summary>
        /// <param name="qValue">The delimited string of arguments</param>
        /// <param name="splitterText">The text that represents the break point between arguments</param>
        /// <returns></returns>
        public static IList<string> SplitString(string qValue, string splitterText) {
            bool insideQuotes = false;

            IList<string> subParts = new List<string>();
            string curPart = string.Empty;
            for (int i = 0; i < qValue.Length; i++) {
                if (!insideQuotes && qValue.Substring(i).StartsWith(splitterText)) {
                    i += splitterText.Length - 1;
                    subParts.Add(curPart);
                    curPart = string.Empty;
                } else {
                    if (qValue[i] == '\"') {
                        insideQuotes = !insideQuotes;
                    }
                    // at this stage we still preserve quotes
                    curPart += qValue[i];
                }
            }
            if (!string.IsNullOrEmpty(curPart)) {
                subParts.Add(curPart);
                curPart = string.Empty; // strictly speaking not necessary because we are done
            }
            return subParts;
        }

    } // class
} // namespace
