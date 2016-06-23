/*
  Project Kraken: SPARK for Microsoft SharePoint 2010
  Copyright (C) 2003-2011 Thomas Carpe. <http://www.ThomasCarpe.com/>
  Maintained by: <http://www.LiquidMercurySolutions.com/>

  This file is part of SPARK: SharePoint Application Resource Kit.
  SPARK projects are distributed via CodePlex: <http://www.codeplex.com/spark/>

  You may use this code for commercial purposes and derivative works, 
  provided that you maintain all copyright notices.

  SPARK is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version. You should have received a copy of
  the GNU General Public License along with SPARK.  If not, see
  <http://www.gnu.org/licenses/>.

  SPARK is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.
  
  We worked hard on all SPARK code, and we don't make any profit from
  sharing it with the world. Please do us a favor amd give us credit
  where credit is due, by leaving this notice unchanged. We all stand
  on the backs of giants. Wherever we have used someone else's code or
  blog article as the basis of our work, we have provided references
  to our source.
*/

namespace Kraken.SharePoint.Logging {

  using System;
  //using System.Collections.Generic;
  //using System.Linq;
  //using System.Text;

  /// <summary>
  /// D020 is the hex equivalent of 53280
  /// </summary>
  public enum LoggingCategories : uint {
    [Obsolete("This (0) is just here to prevent a compiler warning, DO NOT USE IT *EVAR*!")]
    None = 0,
    KrakenFeatures = 0xD0200001,
    KrakenLogging = 0xD0200002,
    KrakenWebParts = 0xD0200003,
    KrakenBranding = 0xD0200004,
    KrakenSiteColumns = 0xD0200005,
    KrakenContentTypes = 0xD0200006,
    KrakenFieldTypes = 0xD0200007,
    KrakenSecurity = 0xD0200008,
    KrakenTimerJobs = 0xD0200009,
    KrakenUtilities = 0xD020000a,
    KrakenBlogs = 0xD020000b,
    KrakenConfiguration = 0xD020000c,
    KrakenAlerts = 0xD020000d,
    KrakenTagging = 0xD020000e,
    KrakenClaims = 0xD020000f,
    KrakenProfiles = 0xD0200010,
    KrakenUnknown = 0xD020ffff,

    CustomError = 0xD0210001,
    CustomWarning = 0xD0210002,
    CustomHigh = 0xD0210003,
    CustomMedium = 0xD0210004,
    CustomVerbose = 0xD0210005,
    CustomUnknown = 0xD021ffff
  }

  public enum LoggingAreas : uint {
    [Obsolete("This is just here to prevent a compiler warning, DO NOT USE IT *EVAR*!")]
    None = 0,
    Kraken = 0xD0200000,
    Custom = 0xD0210000
  }

}
