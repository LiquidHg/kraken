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
namespace Kraken.Configuration {

    using System;

	public enum ConfigurationReaderStatus {
		NotIntialized,
		Initializing,
		Initialized,
		InitFailed
	}
		
	/// <summary>
	/// TODO: IStrongTypeConfig summary
	/// </summary>
	public interface IStrongTypeConfig {

		/// <summary>
		/// Loads the default configuration section: appSettings
		/// </summary>
		void Initialize(); 

		/// <summary>
		/// Loads the specified config section
		/// </summary>
		/// <param name="configSection">config section</param>
		void Initialize(string configSection);

		ConfigurationReaderStatus InitStatus { get; }

  } // interface
} // namespace
