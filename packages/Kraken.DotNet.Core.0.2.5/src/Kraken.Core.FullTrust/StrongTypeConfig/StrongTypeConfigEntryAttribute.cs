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
  Strong Type Config Library by Thomas Carpe and Charlie Hill
  Copyright (C)2006, 2008 Thomas Carpe and Charlie Hill. Some Rights Reserved.
  Contact: dotnet@Kraken.com, chill@chillweb.net
 
  The classes in this file were written jointly and are the mutual property of both authors.
  They are licensed for use under the Creative Commons license. Rights reserved include
  "Share and Share Alike", and "Attribution". You may use this code for commercial purposes
  and derivative works, provided that you maintain this copyright notice.
*/
#define IncludeType

namespace Kraken.Configuration {

    using System;

	[Flags]
	public enum ConfigFlags {
		None,
		DateOnly,
		CaseInsensitive
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public class StrongTypeConfigEntryAttribute : Attribute {

		#region Cosnturctor

		public StrongTypeConfigEntryAttribute() { }

		public StrongTypeConfigEntryAttribute(string key) {
			this.Key = key;
    }
		public StrongTypeConfigEntryAttribute(bool required) {
			this.Required = required;
		}
    public StrongTypeConfigEntryAttribute(ConfigFlags flags) {
			this.Flags = flags;
		}

		public StrongTypeConfigEntryAttribute(string key, bool required) {
			this.Key = key; 
      this.Required = required;
    }
		public StrongTypeConfigEntryAttribute(string key, bool required, ConfigFlags flags) {
			this.Key = key;
      this.Required = required;
      this.Flags = flags;
		}

#if IncludeType
    public StrongTypeConfigEntryAttribute(Type type) : this() {
      this.Type = type;
    }
    public StrongTypeConfigEntryAttribute(bool required, Type type) : this(required) {
      this.Type = type;
    }
    public StrongTypeConfigEntryAttribute(string key, bool required, Type type) : this(key, required) {
      this.Type = type;
    }
#endif

		#endregion

		#region Configuration

		private string _key = string.Empty;
    public string Key {
      get{ return _key; }
      set{ _key = value; _isKeyDefined = true; }
    }

		private bool _required = true;
    public  bool Required {
      get { return _required; }
      set { _required = value; _isRequiredDefined = true; }
    }

    private bool _isRequiredDefined = false;
    public bool IsRequiredDefined {
      get { return _isRequiredDefined; }
    }

		private bool _isKeyDefined = false;
    public bool IsKeyDefined {
      get { return _isKeyDefined; }
    }

		private ConfigFlags _flags = ConfigFlags.None;
    public ConfigFlags Flags {
      get { return _flags; }
      set { _flags = value; }
    }

#if IncludeType
    private Type _type;
    /// <summary>
    /// This is needed to support some derived classes that rely on explicitly setting the type.
    /// However, it may be possible to remove it in the near future. So, if you aren't using Type
    /// now, please don't start. :-) TC 3/15/2007
    /// </summary>
    public Type Type {
      get { return _type; }
      set { _type = value; }
    }
#endif

		#endregion

	} // class
} // namespace
