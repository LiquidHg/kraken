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

namespace Kraken.SharePoint {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Administration;
  using Microsoft.SharePoint.Utilities;

  public static class SPFeatureExtensions {

    public static int FarmVersion {
      get {
        try {
          return SPFarm.Local.BuildVersion.Major;
        } catch {
#if DOTNET_V35
        return 14;
#else
          return 15;
#endif
        }
      }
    }

    public static string GetFeatureFilePath(this SPFeature feature) {
      return feature.Definition.GetFeatureFilePath();
    }
    public static string GetFeatureFilePath(this SPFeature feature, string fileName) {
      return feature.Definition.GetFeatureFilePath(fileName);
    }
    public static string GetFeatureFilePath(this SPFeatureDefinition featureDefinition) {
      return GetFeatureFilePath(featureDefinition.Name);
      // TODO determine if above should actually be the DisplayName as shown in old code below
    }
    public static string GetFeatureFilePath(this SPFeatureDefinition featureDefinition, string fileName) {
      return GetFeatureFilePath(featureDefinition.Name + @"\" + fileName);
      // TODO determine if above should actually be the DisplayName as shown in old code below
    }
    public static string GetFeatureFilePath(string featureName) {
      string fn =
#if DOTNET_V35
      SPUtility.GetGenericSetupPath(@"TEMPLATE\FEATURES\") + featureName;
#else
        SPUtility.GetVersionedGenericSetupPath(@"TEMPLATE\FEATURES\", FarmVersion)
#endif
       + featureName;
      return fn;
    }

#region FeatureChecker methods

    public static bool IsFeatureActivated(this SPFeatureCollection features, Guid featureId) {
      SPFeature feature = null;
      try {
        feature = features[featureId];
        return (feature != null);
      } catch {
        return false;
      }
    }

    public static Guid GetFeatureIdByName(this SPFeatureCollection features, string featureName) {
      foreach (SPFeature feature in features) {
        if (string.Equals(feature.Definition.DisplayName, featureName, StringComparison.InvariantCultureIgnoreCase)
          || string.Equals(feature.Definition.Name, featureName, StringComparison.InvariantCultureIgnoreCase)) {
          return feature.DefinitionId;
        }
      }
      return Guid.Empty;
    }

    /// <summary>
    /// Get the feature collection of the feature's parent object.
    /// This is generally useful in activating or deactivating the feature.
    /// </summary>
    /// <param name="feature"></param>
    /// <returns></returns>
    public static SPFeatureCollection GetParentFeatures(this SPFeature feature) {
      object featureParent = feature.Parent;
      SPWeb web = featureParent as SPWeb;
      if (web != null)
        return web.Features;
      SPSite site = featureParent as SPSite;
      if (site != null)
        return site.Features;
      SPWebApplication app = featureParent as SPWebApplication;
      if (app != null)
        return app.Features;
      throw new ArgumentException("The argument passed to featureParent must be type 'SPWeb', 'SPSite', or 'SPWebApplication'.", "featureParent");
    }

    public static SPFeature GetFeature(this SPWeb web, string featureName) {
      SPFeatureCollection features = web.Features;
      return GetFeature(features, featureName);
    }
    public static SPFeature GetFeature(this SPSite site, string featureName) {
      SPFeatureCollection features = site.Features;
      return GetFeature(features, featureName);
    }
    public static SPFeature GetFeature(this SPWebApplication webApp, string featureName) {
      SPFeatureCollection features = webApp.Features;
      return GetFeature(features, featureName);
    }
    /// <summary>
    /// Gets the feature by its name; throws an exception if this can't be done.
    /// </summary>
    /// <param name="web"></param>
    /// <param name="webApp"></param>
    /// <param name="features"></param>
    /// <param name="featureName"></param>
    /// <returns></returns>
    public static SPFeature GetFeature(this SPFeatureCollection features, string featureName) {
      try {
        Guid FeatureId = features.GetFeatureIdByName(featureName);
        if (FeatureId != Guid.Empty && features != null) {
          SPFeature feature = features[FeatureId];
          if (feature != null) {
            return feature;
          }
        }
        throw new Exception("Feature not found in collection.");
      } catch (Exception ex) {
        throw new Exception(string.Format("Could not get the '{0}' feature. Please check the configuration and redeploy. ", featureName), ex);
      }
    }

#endregion

  } // class

} // namespace
