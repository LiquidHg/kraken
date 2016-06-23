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
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Diagnostics;
  using Microsoft.SharePoint.Administration;
  using Microsoft.Win32;
  using System.Diagnostics.CodeAnalysis;

  /// <summary>
  /// Implements a logging service that uses "Kraken" as the product name.
  /// </summary>
  /// <remarks>
  /// Thanks to http://blog.mastykarz.nl/logging-uls-sharepoint-2010/ for the example!
  /// </remarks>
  public class KrakenLoggingService : SPDiagnosticsServiceBase {

    // TODO implement methods and administration pages to allow us to persist these providers in the farm and manage them through Central Admin.

    /// <summary>
    /// Create a new instance of the logging service on the local farm with the default service name (DEFAULT_SERVICE_NAME).
    /// </summary>
    public KrakenLoggingService() : base() { }
    // was: Creates a new instance of KrakenLoggingService without any provided service name or famr.
    // TODO decide if alternative constructor would be a good idea.

    /// <summary>
    /// Create a new instance of the logging service on the local farm with the supplied name.
    /// </summary>
    /// <remarks>
    /// A better way to do this with more fault tolerant behavior would be to call static method CreateNew.
    /// </remarks>
    /// <param name="name">If empty, uses the default service name (DEFAULT_SERVICE_NAME).</param>
    public KrakenLoggingService(string name) : this(name, null) { }

    /// <summary>
    /// Create a new instance of the logging service with the provided name on the supplied farm.
    /// </summary>
    /// <remarks>
    /// A better way to do this with more fault tolerant behavior would be to call static method CreateNew.
    /// </remarks>
    /// <param name="name">If empty, uses the default service name (DEFAULT_SERVICE_NAME).</param>
    /// <param name="createDetachedLog">If true, the log will not be attached to Config DB in any way</param>
    /// <param name="farm">If null, it will be detached from the farm config DB. Usually you should pass SPFarm.Local.</param>
    public KrakenLoggingService(string name, SPFarm farm, LoggingCategories defaultCategory/* = LoggingCategories.None*/)
      : base(string.IsNullOrEmpty(name) ? DEFAULT_SERVICE_NAME : name, (farm == null) ? SPFarm.Local : farm) {
#pragma warning disable 618 // This is okay to use in this case, because we're using it to set behavior, not to store
      if (defaultCategory != LoggingCategories.None)
        this.DefaultCategory = defaultCategory;
#pragma warning restore 618
    }

#pragma warning disable 618 // This is okay to use in this case, because we're using it to set behavior, not to store
    public KrakenLoggingService(string name, SPFarm farm) : this(name, farm, LoggingCategories.None) { }
#pragma warning restore 618

    public const string ULS_EXCEPTION_TAG = "***EXCEPTION***";
    public const string DEFAULT_SERVICE_NAME = "Kraken Logging Service";
    private const int DEFAULT_ID_TAG = 0;
    private const string ERR_DEV_TIP = "You might be running in a context that does not have suficient permissions to create or read the service; running elevated was already tried, so you should try creating your own service instance. ";
    private const LoggingCategories DEFAULT_CATEGORY = LoggingCategories.KrakenUnknown;
    private const bool FAULT_TOLERANT_NO_CATEGORY = true;

    // TODO implement a collection of providers and allow different modules to register their service in it and switch the current provider.

    #region Properties

    [Persisted]
    private string assemblyName;
    public string AssemblyName {
      get {
        try {
          if (string.IsNullOrEmpty(assemblyName)) {
            assemblyName = System.Reflection.Assembly.GetCallingAssembly().FullName;
            int pos = assemblyName.IndexOf(',');
            if (pos >= 0)
              assemblyName = assemblyName.Substring(0, pos);
          }
        } catch {
          return ULS_EXCEPTION_TAG;
        }
        return assemblyName; // why are we caching this??
      }
    }

    // reserved for future use
    public string ExeName {
      get {
        return string.Empty;
      }
    }


#pragma warning disable 618 // This is okay to use in this case; we trap for errors later on.
    [Persisted]
    private LoggingCategories defaultCategory = LoggingCategories.None; // has to be set to DEFAULT_CATEGORY or some other option later.
#pragma warning restore 618
    public LoggingCategories DefaultCategory {
      get {
#pragma warning disable 618 // This is okay to use in this case; we're trapping for an error.
        if (defaultCategory == LoggingCategories.None) {
          if (FAULT_TOLERANT_NO_CATEGORY)
            return DEFAULT_CATEGORY;
          throw new NotSupportedException("You may not call on this provider using default overloads when a DefaultCategory is None. Specify DefaultCategory before using method that use the default.");
        }
#pragma warning restore 618
        return defaultCategory;
      }
      set {
#pragma warning disable 618 // This is okay to use in this case; we're trapping for an error.
        if (value == LoggingCategories.None)
          throw new NotSupportedException("You may set DefaultCategory to None; specify anoter value.");
#pragma warning restore 618
        defaultCategory = value;
      }
    }

    private LoggingCategoryProvider categoryProvider;
    public LoggingCategoryProvider CategoryProvider {
      get {
        if (categoryProvider == null)
          categoryProvider = LoggingCategoryProvider.DefaultCategoryProvider;
        return categoryProvider;
      }
      set { categoryProvider = value; }
    }

    #endregion

    public bool IsErrorState { get; protected set; }

    protected override IEnumerable<SPDiagnosticsArea> ProvideAreas() {
      return CategoryProvider.Areas;
    }

    #region Static Methods

    private static KrakenLoggingService currentProvider;
    /// <summary>
    /// Developers can use this method to work with a current logging provider for this thread.
    /// If a current provider has not been specified, the default provider will be used.
    /// </summary>
    public static KrakenLoggingService Current {
      get {
        if (currentProvider == null)
          currentProvider = Default;
        return currentProvider;
      }
      set { currentProvider = value; }
    }

    private static KrakenLoggingService defaultProvider;
    /// <summary>
    /// Gets the "Kraken Logging Service" service from the local farm.
    /// If a local copy exists in the singleton it will be used instead.
    /// Otherwise, this methid goes back to the config DB, and attempts
    /// to create a new instance if one is not retreived from the DB.
    /// </summary>
    public static KrakenLoggingService Default {
      get {
        if (defaultProvider == null)
          defaultProvider = Local;
        return defaultProvider;
      }
    }

    /// <summary>
    /// Gets the "Kraken Logging Service" service from the local farm.
    /// This method will always go back to the config DB, and attempts
    /// to create a new instance if one is not retreived from the DB.
    /// </summary>
    public static KrakenLoggingService Local {
      get {
        // TODO fix things so that when Kraken deploys globally, it creates the default service in persisted object store
        KrakenLoggingService kls = null;
        try {
          //kls = SPDiagnosticsServiceBase.GetLocal<KrakenLoggingService>();
          kls = new KrakenLoggingService();
        } catch (Exception ex) {
          WriteToBackupLog("Could not do 'SPDiagnosticsServiceBase.GetLocal'. Where will I get KrakenLoggingService.Local?", ex, true);
        }
        // fail safe method 1
        if (kls == null) {
          try {
            SPSecurity.RunWithElevatedPrivileges(delegate() {
              //kls = SPDiagnosticsServiceBase.GetLocal<KrakenLoggingService>();
              kls = new KrakenLoggingService();
              if (kls != null)
                kls.Write("RECOVERY: It's OK. Don't panic. I used elevation Fail-safe.", TraceSeverity.Unexpected, EventSeverity.ErrorCritical, LoggingCategories.KrakenLogging);
            });
          } catch (Exception ex) {
            WriteToBackupLog("Could not do 'SPDiagnosticsServiceBase.GetLocal' even when elevated.", ex, true);
          }
        }
        // fail safe method 2
        if (kls == null) {
          kls = GetFailSafeLog();
          if (kls != null)
            kls.Write("RECOVERY: It's OK. Don't panic. I used GetFailSafeLog Fail-safe.", TraceSeverity.Unexpected, EventSeverity.ErrorCritical, LoggingCategories.KrakenLogging);
        }
        return kls;
      }
    }

    /// <summary>
    /// Creates an instance of the logging service with the default service name.
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// Use this method in preference to the object's constructor if you want to
    /// provide additional fail-safes and automatic security elevation.
    /// </remarks>
    public static KrakenLoggingService CreateNew() {
      return CreateNew(DEFAULT_SERVICE_NAME, DEFAULT_CATEGORY, false);
    }
    public static KrakenLoggingService CreateNew(LoggingCategories defaultCategory) {
      return CreateNew(DEFAULT_SERVICE_NAME, defaultCategory, false);
    }
    public static KrakenLoggingService CreateNew(string serviceName) {
#pragma warning disable 618 // This is okay to use in this case; we want to specify the category later on - don't forget!
      return CreateNew(serviceName, LoggingCategories.None, false);
#pragma warning restore 618
    }
    public static KrakenLoggingService CreateNew(string serviceName, LoggingCategories defaultCategory/* = LoggingCategories.None*/) {
      return CreateNew(serviceName, defaultCategory, false);
    }
    public static KrakenLoggingService CreateNew(string serviceName, bool saveToConfigDatabase) {
#pragma warning disable 618 // This is okay to use in this case; we want to specify the category later on - don't forget!
      return CreateNew(serviceName, LoggingCategories.None, saveToConfigDatabase);
#pragma warning restore 618
    }
    /// <summary>
    /// This static function performs similar functionality to the constructor,
    /// but with additional attempts for diagnostic logging and elevation.
    /// </summary>
    /// <param name="serviceName">Name of the new service. Must be unique in the persisted object config store</param>
    /// <param name="defaultCategory">
    /// Provide a default logging category.
    /// (Use 'LoggingCategories.None' if you do not want specify one at this time.)
    /// </param>
    /// <param name="saveToConfigDatabase">
    /// NOT YET IMPLEMENTED
    /// In future this will save to teh config db when true.
    /// </param>
    /// <returns></returns>
    /// <remarks>
    /// Use this method in preference to the object's constructor if you want to
    /// provide additional fail-safes and automatic security elevation.
    /// </remarks>
    public static KrakenLoggingService CreateNew(string serviceName, LoggingCategories defaultCategory, bool saveToConfigDatabase) {
      if (string.IsNullOrEmpty(serviceName))
        throw new ArgumentNullException("name");
      KrakenLoggingService uls = null;
      try {
        // There are some contexts, such as within a claims provider, where the running user
        // lacks sufficient permissions to get/set persisted config objects in SharePoint, which
        // is a bit of a rare chicken-and-egg problem that we solve here by running elevated.
        // This seems to especially happen before the logged in user is authenticated.
        SPSecurity.RunWithElevatedPrivileges(delegate() {
          //uls = ReadFromConfigDB(serviceName, true);
          uls = SPFarm.Local.Services.GetValue<KrakenLoggingService>(serviceName);
        });
      } catch (Exception ex) {
        if (uls == null) {
          string msg = string.Format("Instance of ULS logging service with name='{0}' could not be read from the configuration database. We will create a temp object. " + ERR_DEV_TIP, serviceName);
          WriteToBackupLog(msg, ex, true);
        }
      } finally {
        if (uls != null) {
          if (saveToConfigDatabase) {
            throw new NotImplementedException("We don't have ability to save to config db yet");
            // TODO we should provide a way to save to configuration object store
          }
        } else {
          // creates an instance not assocaiated with the config database at all
          uls = GetFailSafeLog(serviceName, defaultCategory);
          //string msg = string.Format("Could not create new instance of ULS logging service with name='{0}'. " + ERR_DEV_TIP, serviceName);
          //WriteToBackupLog(msg, ex);
        }
      }
      if (uls == null) {
        WriteToBackupLog("Construction of new object as fail-safe failed.", null, true);
      }
      return uls;
    }

    #endregion

    public static KrakenLoggingService GetFailSafeLog() {
      return GetFailSafeLog(DEFAULT_SERVICE_NAME, LoggingCategories.KrakenUnknown);
    }
    public static KrakenLoggingService GetFailSafeLog(string serviceName) {
      return GetFailSafeLog(serviceName, LoggingCategories.KrakenUnknown);
    }
    public static KrakenLoggingService GetFailSafeLog(string serviceName, LoggingCategories defaultCategory) {
      if (string.IsNullOrEmpty(serviceName))
        serviceName = DEFAULT_SERVICE_NAME;
      KrakenLoggingService failSafe = null;
      try {
        // this one isn't tied to the configuration DB
        failSafe = new KrakenLoggingService(serviceName, null, defaultCategory);
      } catch (Exception ex) {
        // This is important, because if this happens to be true
        // it will spawn a cycle of evil recursion!
        // The potential for circular logic is strong with this one - yes!
        string message = "Failed to create a fail-safe log.";
        WriteToBackupLog(message, ex, false, true);
      }
      return failSafe;
    }

    private static void WriteToBackupLog(string message, Exception ex, bool tryGetFailSafeLog) {
      WriteToBackupLog(message, ex, true);
    }
    private static void WriteToBackupLog(string message, Exception ex, bool tryGetFailSafeLog, bool failCatastrophically) {
      if (defaultProvider == null && tryGetFailSafeLog)
        defaultProvider = GetFailSafeLog();

      if (defaultProvider != null) {
        try {
          defaultProvider.Write("Unable to write to ULS log! ", TraceSeverity.Unexpected, EventSeverity.ErrorCritical, LoggingCategories.KrakenLogging);
          failCatastrophically = false;
        } catch { }
        SPDiagnosticsCategory category = LoggingCategoryProvider.DefaultCategoryProvider.GetCategory(LoggingCategories.KrakenUnknown);
        if (string.IsNullOrEmpty(message)) {
          try {
            defaultProvider.Write(message, TraceSeverity.Unexpected, EventSeverity.ErrorCritical, category); // LoggingCategories.KrakenLogging
            failCatastrophically = false;
          } catch { }
        }
        if (ex != null) {
          try {
            defaultProvider.Write(ex);
            failCatastrophically = false;
          } catch { }
        }
      }

      // TODO log me someplace!
      if (failCatastrophically) {
        // nothing else to do but fail miserably here
        FailCatastrophically(message, ex);
      } else {
        // well fuck, now what?
      }
    }

    private static void FailCatastrophically(string message, Exception ex) {
      throw (ex != null) ? new Exception(message, ex) : new Exception(message);
    }

    #region Log Writing Logic

    // TODO what ever happened to 'correlation' and 'assembly', are they automatic now
    // no it turns out we need a special class to handle these. Work being done in Unsafe folder 

    public void Write(string message) {
      SPDiagnosticsCategory category = CategoryProvider.GetCategory(this.DefaultCategory);
      Write(message, category.DefaultTraceSeverity, category.DefaultEventSeverity, category);
    }
    public void Write(string message, TraceSeverity traceLevel, EventSeverity eventLevel) {
      SPDiagnosticsCategory category = CategoryProvider.GetCategory(this.DefaultCategory);
      Write(message, traceLevel, eventLevel, category);
    }
    public void Write(string message, TraceSeverity traceLevel, EventSeverity eventLevel, LoggingCategories catFromEnum) {
      SPDiagnosticsCategory category = CategoryProvider.GetCategory(catFromEnum);
      Write(message, traceLevel, eventLevel, category);
    }
    public void Write(string message, TraceSeverity traceLevel, EventSeverity eventLevel, string categoryName, string areaName) {
      bool isCustom;
      SPDiagnosticsCategory category = CategoryProvider.GetCategory(categoryName, areaName, out isCustom);
      Write(message, traceLevel, eventLevel, category);
    }
    /// <summary>
    /// Write to the ULS log, and if fails, write to trace logs
    /// </summary>
    public bool Write(string message, TraceSeverity traceLevel, EventSeverity eventLevel, SPDiagnosticsCategory category) {
      //_Write(message, category, traceLevel, eventLevel);
      try {
        WriteEvent(DEFAULT_ID_TAG, category, eventLevel, message, new object[] { });
        return true;
      } catch (Exception ex) {
        try {
          WriteTrace(DEFAULT_ID_TAG, category, TraceSeverity.Unexpected, "Unable to write to event log! " + ex.Message);
          WriteTrace(DEFAULT_ID_TAG, category, traceLevel, message);
        } catch {
          // a spectacular fail! what do we do now??
          // but we are at such a low level in code, we don't dare blow up!
        }
        IsErrorState = true;
        return false;
      }
    }

    public void WriteStack(Type type, string methodName, bool isExit) {
      WriteStack(type, methodName, isExit, this.DefaultCategory);
    }
    public void WriteStack(Type type, string methodName, bool isExit, LoggingCategories cat) {
      Write(string.Format("{0} {1}::{2}", isExit ? "Leaving" : "Entering", type.FullName, methodName), TraceSeverity.Verbose, EventSeverity.Verbose, cat);
    }

    public void Write(Exception ex) {
      SPDiagnosticsCategory category = CategoryProvider.GetCategory(this.DefaultCategory);
      Write(ex, category);
    }
    public void Write(Exception ex, SPDiagnosticsCategory category) {
      string message = BuildErrorMessage(ex);
      TraceSeverity traceLevel = TraceSeverity.Unexpected;
      EventSeverity eventLevel = EventSeverity.Error;
      Write(message, traceLevel, eventLevel, category);
    }
    public void Write(Exception ex, LoggingCategories catFromEnum) {
      string message = BuildErrorMessage(ex);
      SPDiagnosticsCategory category = CategoryProvider.GetCategory(catFromEnum);
      TraceSeverity traceLevel = TraceSeverity.Unexpected;
      EventSeverity eventLevel = EventSeverity.Error;
      Write(message, traceLevel, eventLevel, category);
    }
    public void Write(Exception ex, string categoryName, string areaName) {
      string message = BuildErrorMessage(ex);
      TraceSeverity traceLevel = TraceSeverity.Unexpected;
      EventSeverity eventLevel = EventSeverity.Error;
      bool isCustom;
      SPDiagnosticsCategory category = CategoryProvider.GetCategory(categoryName, areaName, out isCustom);
      Write(message, traceLevel, eventLevel, category);
    }

    protected string BuildErrorMessage(Exception ex) {
      return BuildErrorMessage(ex, true);
    }
    protected string BuildErrorMessage(Exception ex, bool usePadHeader) {
      if (ex == null)
        return string.Empty;
      string pad = usePadHeader ? ULS_EXCEPTION_TAG + " " : string.Empty;
      string exType = ex.GetType().FullName;
      string exTypeAndSrc = string.IsNullOrEmpty(ex.Source) ? exType : string.Format("{0} in {1}", exType, ex.Source);
      string errorText = string.Format("{0}{1}: {2} {3}", pad, exTypeAndSrc, ex.Message, ex.StackTrace);
      if (ex.InnerException != null)
        errorText += "  INNER EXCEPTION -->  " + BuildErrorMessage(ex.InnerException, false);
      return errorText;
    }

    #endregion

    #region Register/Unregister (Event Log Registry Keys)

    public void Log(object sender, LoggingEventArgs e) {
      if (e.Exception != null)
        Write(e.Exception, e.Category);
      else
        Write(e.Message, e.TraceLevel, e.EventLevel, e.Category);
    }

    private const string EventLogApplicationRegistryKeyPath = @"SYSTEM\CurrentControlSet\services\eventlog\Application";

    private static void Register(SPServer server) {
      RegistryKey eventLogKey = GetEventLogKey(server);
      if (eventLogKey == null)
        return;

      IList<SPDiagnosticsArea> areas = LoggingCategoryProvider.DefaultCategoryProvider.Areas;
      if (areas == null)
        throw new ArgumentNullException("areas", "You must provide a LoggingCategoryProvider object with Areas collection defined.");
      KrakenLoggingService.Default.Write("Attempting to Register [" + areas.Count.ToString() + "] Diagnostic Logging Areas",
              TraceSeverity.Verbose, EventSeverity.Verbose);
      foreach (SPDiagnosticsArea area in areas) {
        KrakenLoggingService.Default.Write("Attempting to Register Diagnostic Logging Area [" + area.Name + "]",
            TraceSeverity.Verbose, EventSeverity.Verbose);
        RegistryKey loggingServiceKey = null;
        try {
          loggingServiceKey = eventLogKey.OpenSubKey(area.Name);
        } catch (System.IO.IOException) { }
        if (loggingServiceKey == null) {
          loggingServiceKey = eventLogKey.CreateSubKey(area.Name,
                               RegistryKeyPermissionCheck.ReadWriteSubTree);
          loggingServiceKey.SetValue("EventMessageFile",
            @"C:\Windows\Microsoft.NET\Framework\v2.0.50727\EventLogMessages.dll",
            RegistryValueKind.String);
        }
      }
    }
    public static void Register() {
      SPSecurity.RunWithElevatedPrivileges(delegate() {
        SPFarm farm = SPFarm.Local;
        if (farm == null)
          return;

        KrakenLoggingService service = KrakenLoggingService.Local;
        if (service == null) {
          service = new KrakenLoggingService();
          service.Update();
          if (service.Status != SPObjectStatus.Online)
            service.Provision();
        }

        foreach (SPServer server in farm.Servers) {
          Register(server);
        }
      });
    }

    public static void Unregister() {
      SPSecurity.RunWithElevatedPrivileges(delegate() {
        SPFarm farm = SPFarm.Local;
        if (farm == null)
          return;

        KrakenLoggingService service = KrakenLoggingService.Local;
        if (service != null)
          service.Delete();
        foreach (SPServer server in farm.Servers) {
          RegistryKey eventLogKey = GetEventLogKey(server);
          if (eventLogKey == null)
            continue;

          IList<SPDiagnosticsArea> areas = LoggingCategoryProvider.DefaultCategoryProvider.Areas;
          foreach (SPDiagnosticsArea area in areas) {
            RegistryKey loggingServiceKey = null;
            try {
              loggingServiceKey = eventLogKey.OpenSubKey(area.Name);
            } catch (System.IO.IOException) { }
            if (loggingServiceKey != null)
              eventLogKey.DeleteSubKey(area.Name);
          }
        }
      });
    }

    private static RegistryKey GetEventLogKey(SPServer server) {
      Default.Write(
        string.Format("Attempting to connect to registry at server address = {0}", server.Address),
        TraceSeverity.Medium, EventSeverity.Information, LoggingCategories.KrakenAlerts
      );
      RegistryKey baseKey = null, eventLogKey = null;
      try {
        baseKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, server.Address);
        if (baseKey == null)
          return null;
        eventLogKey = baseKey.OpenSubKey(EventLogApplicationRegistryKeyPath, true);
        if (eventLogKey == null)
          return null;
      } catch (System.IO.IOException ex) {
        Default.Write(ex, LoggingCategories.KrakenAlerts);
        return null;
      }
      return eventLogKey;
    }

    #endregion

  } // class
} // namespace
