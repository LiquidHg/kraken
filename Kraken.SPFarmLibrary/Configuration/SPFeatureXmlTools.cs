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

namespace Kraken.SharePoint.Configuration {

  using System;
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.Linq;
  using System.Text;
  using System.Xml;
  using System.Xml.Linq;

  using Microsoft.SharePoint.Utilities;

  using Kraken.Xml.Linq;

  public class SPFeatureXmlTools {

    //internal const string MOSS_NAMESPACE = "http://schemas.microsoft.com/sharepoint/";
    //internal const string MOSS_SOAP_NAMESPACE = "http://schemas.microsoft.com/sharepoint/soap/";


    public static string GetTemplateFilePath(string filePath) {
      string fn =
#if DOTNET_V35
      SPUtility.GetGenericSetupPath(@"TEMPLATE\")
#else
      SPUtility.GetVersionedGenericSetupPath(@"TEMPLATE\", SPFeatureExtensions.FarmVersion)
#endif
       + filePath;
      return fn;
    }
    public static string GetFeatureFilePath(string featureAndFileName) {
      string fn =
        SPFeatureExtensions.GetFeatureFilePath(featureAndFileName);
      return fn;
    }

    [Obsolete("If possible use System.Xml.Linq and XElement queries instead.")]
    [SuppressMessage("Microsoft.Design", "CA1041:ProvideObsoleteAttributeMessage", Justification = "This overloaded method has also been marked as obsolete.")]
    public static XmlDocument GetConfigFile(string featureAndFileName) {
      string fn = GetFeatureFilePath(featureAndFileName);
      XmlDocument doc = new XmlDocument();
      doc.Load(fn);
      return doc.DocumentElement.CreateCleanXmlDocument();
    }

    public static XElement XGetConfigFile(string featureAndFileName) {
      string fn = GetFeatureFilePath(featureAndFileName);
      XElement x = XElement.Load(fn);
      return x;
    }

    /*
    public static string GetFeatureFilePath(SPFeatureDefinition featureDefinition, string featureFile) {
        string sharepointFeaturesDir = GetSharePointFeaturesDirectory();
        string filePath = String.Empty;
        if (featureDefinition != null && !String.IsNullOrEmpty(sharepointFeaturesDir)) {
            string featureName = featureDefinition.DisplayName;
            string featureDir = Path.Combine(sharepointFeaturesDir, featureName);
            filePath = Path.Combine(featureDir, featureFile); // "SiteProvisioning.xml"
        }
        return filePath;
    }

    public static string GetFeatureFilePath(string featureFile, SPFeatureDefinition featureDefinition) {
        string sharepointFeaturesDir = GetSharePointFeaturesDirectory();
        string filePath = String.Empty;
        if (featureDefinition != null && !String.IsNullOrEmpty(sharepointFeaturesDir)) {
            string featureName = featureDefinition.DisplayName;
            string featureDir = Path.Combine(sharepointFeaturesDir, featureName);
            filePath = Path.Combine(featureDir, featureFile); // "SiteProvisioning.xml"
        }
        return filePath;
    }

    public static string GetSharePointTemplateDirectory() {
        string propertyName = @"SOFTWARE\Microsoft\Shared Tools\Web Server Extensions\12.0";
        string name = "Location";
        string dir = String.Empty;
        try {
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(propertyName);
            string valString = regKey.GetValue(name) as string;
            regKey.Close();
            dir = Path.Combine(valString, @"template");
        } catch (SecurityException) {
            dir = String.Empty;
        } catch (ArgumentNullException) {
            dir = String.Empty;
        } catch (ArgumentException) {
            dir = String.Empty;
        } catch (ObjectDisposedException) {
            dir = String.Empty;
        } catch (IOException) {
            dir = String.Empty;
        } catch (UnauthorizedAccessException) {
            dir = String.Empty;
        }
        return dir;
    }

    internal static string GetSharePointFeaturesDirectory() {
        string templateDir = GetSharePointTemplateDirectory();
        if (string.IsNullOrEmpty(templateDir))
            return templateDir;
        templateDir = Path.Combine(templateDir, @"features");
        return templateDir;
    }
     */

  } // class

} // namespace
