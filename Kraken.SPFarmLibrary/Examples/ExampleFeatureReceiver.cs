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

namespace Kraken.SharePoint.Examples {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint;

  using Kraken.Configuration;
  using Kraken.SharePoint.Logging;


  /// <summary>
  /// You can use this class as a basis to quickly create feature reivers with
  /// some configurability, flexibility, and robustness
  /// </summary>
  class ExampleFeatureReceiver : SPFeatureReceiver {

    private ExampleFeatureProperties typedProps;
    private ExampleFeatureProperties TypedProperties {
      get { return typedProps; }
    }

    private void EnsureProperties(SPFeatureReceiverProperties properties) {
      log.Write("Invoking EnsureProperties.");
      if (typedProps == null)
        typedProps = new ExampleFeatureProperties(properties);
      else if (typedProps.InitStatus != ConfigurationReaderStatus.Initialized)
        throw new Exception("property reader is not initialized. Can't continue.");
      log.Write("Leaving EnsureProperties.");
    }

    public override void FeatureActivated(SPFeatureReceiverProperties properties) {
      EnsureProperties(properties);
    }

    public override void FeatureDeactivating(SPFeatureReceiverProperties properties) {
      //EnsureProperties(properties);
      //throw new Exception("The method or operation is not implemented.");
    }

    public override void FeatureInstalled(SPFeatureReceiverProperties properties) {
      //EnsureProperties(properties);
      //throw new Exception("The method or operation is not implemented.");
    }

    public override void FeatureUninstalling(SPFeatureReceiverProperties properties) {
      //EnsureProperties(properties);
      //throw new Exception("The method or operation is not implemented.");
    }

    #region Logging Code Pattern v3.0

    private static KrakenLoggingService log = new KrakenLoggingService() {
      DefaultCategory = LoggingCategories.CustomVerbose
    };

    #endregion Logging Code Pattern

  }

}
