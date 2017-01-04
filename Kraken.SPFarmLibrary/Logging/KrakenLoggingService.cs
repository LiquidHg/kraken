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
  using Tracing;
  using System.Diagnostics;

  /// <summary>
  /// Implements a logging service that uses "Kraken" as the product name.
  /// </summary>
  /// <remarks>
  /// Thanks to http://blog.mastykarz.nl/logging-uls-sharepoint-2010/ for the example!
  /// </remarks>
  public class KrakenLoggingService : SPDiagnosticsServiceBase, ITrace {

    // TODO implement methods and administration pages to allow us to persist these providers in the farm and manage them through Central Admin.

    // TODO decide if alternative constructor would be a good idea.
    // NOOOOO! Don't do this!! StackOverflows ABOUND!!!
    // A better way to do this with more fault tolerant behavior would be to call static method CreateNew.

    #region Constructors

    /// <summary>
    /// Creates a new instance of KrakenLoggingService without any provided service name or famr.
    /// </summary>
    public KrakenLoggingService() : base() {
      DoNotPersistToConfigDb = true;
      InitHandlers();
    }

    /// <summary>
    /// Creates a new instance of KrakenLoggingService without any provided service name or famr.
    /// Allows you the specify the default logging category
    /// </summary>
#pragma warning disable 618 // This is okay to use in this case, because we're using it to set behavior, not to store
    public KrakenLoggingService(LoggingCategories defaultCategory/* = LoggingCategories.None*/) : this() {
      if (defaultCategory != LoggingCategories.None)
        this.DefaultCategory = defaultCategory;
    }
#pragma warning restore 618

    /*
    public KrakenLoggingService(LoggingProperties props) {
    }
    */

    /// <summary>
    /// Create a new instance of the logging service with the provided name on the supplied farm.
    /// </summary>
    /// <param name="name">If empty, uses the default service name (DEFAULT_SERVICE_NAME).</param>
    /// <param name="farm">If null, it will be detached from the farm config DB. Usually you should pass SPFarm.Local.</param>
    /// <param name="defaultCategory">Default is none, please specify a category otherwise you'd have to do it every time you log.</param>
#pragma warning disable 618 // This is okay to use in this case, because we're using it to set behavior, not to store
    public KrakenLoggingService(string name, LoggingCategories defaultCategory = LoggingCategories.None, SPFarm farm = null)
      : base(string.IsNullOrEmpty(name) ? LoggingProperties.DEFAULT_SERVICE_NAME : name, farm ?? SPFarm.Local) {
      if (defaultCategory != LoggingCategories.None)
        this.DefaultCategory = defaultCategory;
      InitHandlers();
    }
    public KrakenLoggingService(string name, SPFarm farm)
      : this(string.IsNullOrEmpty(name) ? LoggingProperties.DEFAULT_SERVICE_NAME : name, LoggingCategories.None, farm ?? SPFarm.Local) {
    }
#pragma warning restore 618

    #endregion

    #region Properties

    public const string ULS_EXCEPTION_TAG = "***EXCEPTION***";
    //public const string DEFAULT_SERVICE_NAME = "Kraken Logging Service";
    private const int DEFAULT_ID_TAG = 0;
    private const string ERR_DEV_TIP = "You might be running in a context that does not have suficient permissions to create or read the service; running elevated was already tried, so you should try creating your own service instance. ";
    //private const LoggingCategories DEFAULT_CATEGORY = LoggingCategories.KrakenUnknown;
    //private const bool FAULT_TOLERANT_NO_CATEGORY = true;

    protected bool DoNotPersistToConfigDb = false;

    public bool IsErrorState { get; protected set; }

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
          if (LoggingProperties.FAULT_TOLERANT_NO_CATEGORY)
            return LoggingProperties.DEFAULT_CATEGORY;
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


    protected override IEnumerable<SPDiagnosticsArea> ProvideAreas() {
      return CategoryProvider.Areas;
    }

    #region Static Methods

    // TODO implement a collection of providers and allow different modules to register their service in it and switch the current provider.

    #region Current, Default, and Local

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
    /// Otherwise, this method calls Local, which goes back to the config 
    /// DB, and attempts to create a new instance if one is not retreived 
    /// from the DB, using fail safe methods.
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
    /// to create a new instance if one is not retreived from the DB
    /// using fail-safe methods.
    /// </summary>
    public static KrakenLoggingService Local {
      get {
        KrakenLoggingService kls = TryGetLocal();
        return kls;
      }
    }


    /// <summary>
    /// Returns either the Default service, if it already exists
    /// or a new KLS instance loaded through elevation.
    /// </summary>
    private static KrakenLoggingService FailSafeLogService {
      get {
        return defaultProvider ?? TryNewService(
          new LoggingProperties() {
            ServiceName = string.Empty,
            DefaultCategory = LoggingCategories.KrakenLogging,
            CreateElevated = false,
            IsRecovery = false
          }
        );
      }
    }

    #endregion

    public override void Update() {
      if (this.DoNotPersistToConfigDb
        || string.IsNullOrEmpty(this.Name)
        || this.Id == Guid.Empty)
        return;
      DeleteDuplicateService(this);
      base.Update();
    }

    /// <summary>
    /// Check for a duplicate object here and delete it if necessary
    /// should prevent SPDuplicateObjectException on base.Update()
    /// </summary>
    private void DeleteDuplicateService(KrakenLoggingService compareTo) {
    }
    private static void DeleteDuplicateService(SPPersistedObject parent, string name, Guid id) {
      KrakenLoggingService existing = null;
      try {
        existing = parent.GetChild<KrakenLoggingService>(name);
      } catch { }
      if (existing != null && existing.Id != id) {
        string msg = string.Format("Logging service with name '{0}' exists in configuration database with ID = '{1}'; to Update current logging service by same name with Id = '{2}' requires that the existing object will be deleted."
          , existing.Name
          , existing.Id
          , id
        );
        WriteToFailSafeLog(msg, null);
        existing.Delete();
      }
    }

    private static bool SuppressLocalFailSafe { get; set; } = false;
    private static bool SuppressBackupLog { get; set; } = false;

    protected enum LocalFailReason {
      None,
      FileNotFound,
      DuplicateDbEntry,
      AccessDeniedOnUpdate,
      OtherException
    }
    protected static LocalFailReason GetLocalFailedReason { get; set; } = LocalFailReason.None;

    #region Fault Tolerant Service Creators

    /*
    /// <param name="defaultCategory">The default logging category to use for this instance.</param>
    /// <param name="isRecovery">When true, show ULS message to indicate that log recovery worked.</param>
    /// <param name="elevate">Defaault to true. Uses elevation to access the objects.</param>
     */
    /// <summary>
    /// Creates a new instance of logging service that is not tied to the farm
    /// configuration DB in any way. Optionally, uses elevation to access the objects.
    /// </summary>
    /// <remarks>
    /// This is the most fault tolerant method to create a logging class, but
    /// may lack certain features, such as log filtering.
    /// </remarks>
    /// <returns>The newly created logging service</returns>
    private static KrakenLoggingService TryNewService(LoggingProperties props = null) {
      //LoggingCategories defaultCategory = DEFAULT_CATEGORY, bool isRecovery = true, bool elevate = true
      if (props == null) {
        props = new LoggingProperties() {
          ServiceName = LoggingProperties.DEFAULT_SERVICE_NAME, // string.Empty,
          DefaultCategory = LoggingProperties.DEFAULT_CATEGORY,
          CreateElevated = false
        };
      }
      /*
      LoggingCategories defaultCategory = props.DefaultCategory;
      bool isRecovery = props.IsRecovery;
      bool elevate = props.CreateElevated;
      */
      KrakenLoggingService kls = null;
      try {
        if (string.IsNullOrEmpty(props.ServiceName)) {
          if (props.CreateElevated) {
            SPSecurity.RunWithElevatedPrivileges(delegate () {
              kls = new KrakenLoggingService(props.DefaultCategory);
            });
          } else {
            kls = new KrakenLoggingService(props.DefaultCategory);
          }
        } else {
          if (props.CreateElevated) {
            SPSecurity.RunWithElevatedPrivileges(delegate () {
              kls = new KrakenLoggingService(props.ServiceName, props.DefaultCategory);
            });
          } else {
            kls = new KrakenLoggingService(props.ServiceName, props.DefaultCategory);
          }
        }
        if (kls != null && props.IsRecovery) // isRecovery
          kls.Write("LOG RECOVERY: It's OK. Don't panic. I used TryNewService Fail-safe.", TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenLogging);
      } catch (Exception ex) {
        // This is important, because if this happens to be true
        // it will spawn a cycle of evil recursion!
        // The potential for circular logic is strong with this one - yes!
        WriteToFailSafeLog("KLS failed in 'TryNewService'.", ex);
      }
      if (kls == null)
        props.IsRecovery = true;
      return kls;
    }

    /*
    /// <summary>
    /// Tries to create the service with a name and default category
    /// that will load it into SPFarm.Local and the configuration DB.
    /// Optionally, uses elevation to access the objects.
    /// </summary>
    /// <param name="serviceName">Service name to try and load/create in config db</param>
    /// <param name="defaultCategory">The default logging category to use for this instance.</param>
    /// <param name="isRecovery">When true, show ULS message to indicate that log recovery worked.</param>
    /// <param name="elevate">Defaault to false. Uses elevation to access the objects.</param>
    /// <remarks>
    /// Unlike TryGetNewService, this method tries to read from the
    /// configuration database, which may require some permissions from
    /// the user or via elevation.
    /// </remarks>
    /// <returns>The existing or newly created logging service</returns>
    public static KrakenLoggingService TryNewServiceNamed(LoggingProperties props = null) {
      //string serviceName = DEFAULT_SERVICE_NAME, LoggingCategories defaultCategory = LoggingCategories.KrakenUnknown, bool isRecovery = true, bool elevate = false
      if (props == null) {
        props = new LoggingProperties() {
          ServiceName = LoggingProperties.DEFAULT_SERVICE_NAME,
          DefaultCategory = LoggingCategories.KrakenUnknown, // LoggingProperties.DEFAULT_CATEGORY,
          CreateElevated = true
        };
      }
      string serviceName = props.ServiceName;
      if (string.IsNullOrEmpty(serviceName))
        serviceName = LoggingProperties.DEFAULT_SERVICE_NAME;
      LoggingCategories defaultCategory = props.DefaultCategory;
      bool isRecovery = props.IsRecovery;
      bool elevate = props.CreateElevated;

      KrakenLoggingService kls = null;
      try {
        if (elevate) {
          SPSecurity.RunWithElevatedPrivileges(delegate () {
            kls = new KrakenLoggingService(serviceName, defaultCategory);
          });
        } else {
          kls = new KrakenLoggingService(serviceName, defaultCategory);
        }
        if (kls != null && isRecovery)
          kls.Write("LOG RECOVERY: It's OK. Don't panic. I used TryNewServiceNamed fail-safe.", TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenLogging);
      } catch (Exception ex) {
        // This is important, because if this happens to be true
        // it will spawn a cycle of evil recursion!
        // The potential for circular logic is strong with this one - yes!
        WriteToFailSafeLog("KLS kailed in 'TryNewServiceNamed'.", ex);
      }
      if (kls == null)
        props.IsRecovery = true;
      return kls;
    }
    */

    /// <summary>
    /// Tries to get the logging service from SPFarm.Local.Services
    /// </summary>
    /// <param name="serviceName">Service name to try and load/create in config db</param>
    /// <param name="elevate">Elevate permissions while reading the config DB</param>
    /// <returns>The existing or newly created logging service</returns>
    private static KrakenLoggingService TryGetService(
      LoggingProperties props = null /* string serviceName, bool elevate = true */
    ) {
      if (props == null) {
        props = new LoggingProperties() {
          //ServiceName = LoggingProperties.DEFAULT_SERVICE_NAME,
          //DefaultCategory = LoggingCategories.KrakenUnknown, // LoggingProperties.DEFAULT_CATEGORY,
          CreateElevated = true
        };
      }
      FailSafeLogService.Entering(System.Reflection.MethodBase.GetCurrentMethod(), string.Format("ServiceName={0}; Elevated={1}", props.ServiceName, props.CreateElevated));
      //string serviceName = DEFAULT_SERVICE_NAME, LoggingCategories defaultCategory = LoggingCategories.KrakenUnknown, bool isRecovery = true, bool elevate = false
      if (string.IsNullOrEmpty(props.ServiceName))
        throw new ArgumentNullException("props.ServiceName");
      /*
      string serviceName = props.ServiceName;
      LoggingCategories defaultCategory = props.DefaultCategory;
      bool isRecovery = props.IsRecovery;
      bool elevate = props.CreateElevated;
      */
      KrakenLoggingService kls = null;
      try {
        if (props.CreateElevated) {
          // There are some contexts, such as within a claims provider, where the running user
          // lacks sufficient permissions to get/set persisted config objects in SharePoint, which
          // is a bit of a rare chicken-and-egg problem that we solve here by running elevated.
          // This seems to especially happen before the logged in user is authenticated.
          SPSecurity.RunWithElevatedPrivileges(delegate () {
            //uls = ReadFromConfigDB(serviceName, true);
            kls = SPFarm.Local.Services.GetValue<KrakenLoggingService>(props.ServiceName);
          });
        } else {
          kls = SPFarm.Local.Services.GetValue<KrakenLoggingService>(props.ServiceName);
          if (kls == null)
            WriteToFailSafeLog(
                string.Format("Attempt to get service name = {0} from SPFarm.Local.Services returned null. This is normal (enough) assuming it doesn't really exist.", props.ServiceName)
                , null, false);
        }
      } catch (Exception ex) {
        string msg = string.Format("Instance of ULS logging service with name='{0}' could not be read from the configuration database. We will attempt to create a temporary service isntead. {1}", props.ServiceName, ERR_DEV_TIP);
        WriteToFailSafeLog(msg, ex);
      }
#pragma warning disable 618
      if (kls != null && props.DefaultCategory != LoggingCategories.None)
        kls.DefaultCategory = props.DefaultCategory;
#pragma warning restore 618
      FailSafeLogService.Leaving(System.Reflection.MethodBase.GetCurrentMethod());
      if (kls == null)
        props.IsRecovery = true;
      return kls;
    }

    /// <summary>
    /// This is the primary method usually tried first.
    /// Underlying methods implement fail-safe methods.
    /// </summary>
    /// <param name="elevate">Defaault to false. Uses elevation to access the objects.</param>
    /// <returns>The existing or newly created logging service</returns>
    /// <remarks>
    /// Uses SPDiagnosticsServiceBase.GetLocal to get the logging service.
    /// </remarks>
    private static KrakenLoggingService TryGetLocal(
      LoggingProperties props = null /* bool elevated = false */
    ) {
      if (props == null) props = new LoggingProperties(); // { CreateElevated = false };
      KrakenLoggingService kls = null;

      if (props.CreateElevated) {
        SPSecurity.RunWithElevatedPrivileges(delegate () {
          kls = TryGetLocalInternals();
        });
      } else {
        kls = TryGetLocalInternals();
      }
      if (SuppressLocalFailSafe)
        return kls;

      if (kls == null)
        props.IsRecovery = true;

      if (kls == null && !props.CreateElevated) {
        SPSecurity.RunWithElevatedPrivileges(delegate () {
          kls = TryGetLocalInternals(); // fail safe method 1
        });
        if (kls != null)
          kls.Write("LOG RECOVERY: It's OK. Don't panic. I used TryGetLocal elevation Fail-safe.", TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenLogging);
      }
      if (kls == null) {
        kls = TryNewService(props); // fail safe method 2
      }
      if (kls == null && !string.IsNullOrEmpty(props.ServiceName)) {
        props.ServiceName = string.Empty;
        kls = TryNewService(props); // fail safe method 3
      }
      if (kls == null)
        WriteToFailSafeLog("TryGetLocal failed completely.", null);
      return kls;
    }

    // was: <param name="retry"></param>
    /// <summary>
    /// Internals to get local service which account for common
    /// problems that are well known to the SharePoint community.
    /// </summary>
    /// <returns></returns>
    private static KrakenLoggingService TryGetLocalInternals() { /*bool retry = true*/
      KrakenLoggingService kls = null;
      bool logException = false;
      try {
        kls = SPDiagnosticsServiceBase.GetLocal<KrakenLoggingService>();
      } catch (System.IO.FileNotFoundException fnfEx) {
        // this can happen if there is a serialization or version 
        // trouble reading the object in the config database. In which 
        // case, we want to delete the existing object and replace it. 
        // However, we may not be able to do this at any time.
        GetLocalFailedReason = LocalFailReason.FileNotFound;
        WriteToFailSafeLog("KLS failed in 'TryGetLocal' with FileNotFound error looking for assembly. Was an incorrect version deployed?", fnfEx);
      } catch (SPDuplicateObjectException dupEx) {
        // https://social.msdn.microsoft.com/Forums/sharepoint/en-US/b92d3969-544d-45e8-a5b9-8ec12d16fcb3/spdiagnosticsservicebasegetlocal-causes-exception-during-spfeaturereceiverfeatureinstalled?forum=sharepointdevelopmentprevious
        // This one seems to happen always on PowerShell commands
        // but not when the same code is run using Central Admin.
        // We'll work around it the best we can.
        GetLocalFailedReason = LocalFailReason.DuplicateDbEntry;
        WriteToFailSafeLog("KLS failed in 'TryGetLocal' with SPDuplicateObjectException error. This is expected when running from PowerShell. We'll try SPFarm.Local.GetChild instead. ", null, false);
        if (logException)
          WriteToFailSafeLog("The following exception can be safely ignored.", dupEx, false);
        kls = SPFarm.Local.GetChild<KrakenLoggingService>(LoggingProperties.DEFAULT_SERVICE_NAME);
        /*
        if (retry) {
          DeleteDuplicateService(SPFarm.Local, KrakenLoggingService.DEFAULT_SERVICE_NAME, Guid.Empty);
          // shouldn't throw another error, but prevent recursion anyway
          kls = TryGetLocalInternals(false);
        }
        */
      } catch (System.Security.SecurityException secEx) {
        // This happens when user (or lack thereof) really does not
        // have permission to update Configuration Database.
        // We can try elevation, or we can cop-out and just run detached.
        GetLocalFailedReason = LocalFailReason.AccessDeniedOnUpdate;
        WriteToFailSafeLog("KLS failed in 'TryGetLocal' with SecurityException error on Update. We'll try SPFarm.Local.GetChild instead. ", null, false);
        if (logException)
          WriteToFailSafeLog("The following exception can be safely ignored.", secEx, false);
        kls = SPFarm.Local.GetChild<KrakenLoggingService>(LoggingProperties.DEFAULT_SERVICE_NAME);
      } catch (Exception ex) {
        if (GetLocalFailedReason == LocalFailReason.None)
          GetLocalFailedReason = LocalFailReason.OtherException;
        WriteToFailSafeLog("KLS failed in 'TryGetLocal'. KrakenLoggingService.Local will use fall-back methods to maintain ability to write ULS logs.", ex);
      }
      if (kls != null)
        GetLocalFailedReason = LocalFailReason.None;
      return kls;
    }

    #endregion

    /// <summary>
    /// This static function performs similar functionality to the constructor,
    /// but with additional attempts for diagnostic logging and elevation.
    /// </summary>
    /// <param name="serviceName">
    /// Name of the new service. Must be unique in the persisted object config store; defaults to DEFAULT_SERVICE_NAME
    /// </param>
    /// <param name="defaultCategory">
    /// Provide a default logging category.
    /// (Use 'LoggingCategories.None' (default) if you do not want specify one at this time.)
    /// </param>
    /// <param name="saveToConfigDatabase">
    /// NOT YET IMPLEMENTED; default is false
    /// In future this will save to the config db when true.
    /// </param>
    /// <returns></returns>
    /// <remarks>
    /// Use this method in preference to the object's constructor if you want to
    /// provide additional fail-safes and automatic security elevation.
    /// </remarks>
    public static KrakenLoggingService CreateNew(
      LoggingProperties props = null /* string serviceName = DEFAULT_SERVICE_NAME, LoggingCategories defaultCategory = DEFAULT_CATEGORY, bool elevate = false, bool saveToConfigDatabase = false */
    ) {
      if (props == null) {
        props = new LoggingProperties() {
          ServiceName = string.Empty,
          CreateElevated = false,
          DoNotPersistToConfigDb = true
        };
      }
      string serviceName = props.ServiceName;

      /*
      if (string.IsNullOrEmpty(serviceName))
        throw new ArgumentNullException("serviceName");
      */
      // This has been modified now to be much more similar
      // to the way Local is loaded
      KrakenLoggingService kls = null;
#pragma warning disable 618
      if (props.DefaultCategory == LoggingCategories.None)
        props.DefaultCategory = LoggingProperties.DEFAULT_CATEGORY;
#pragma warning restore 618
      if (!string.IsNullOrEmpty(props.ServiceName))
        kls = TryGetService(props);
      if (kls == null) {
        kls = TryNewService(props);
      }
      if (kls == null && !props.CreateElevated) {
        props.CreateElevated = true;
        kls = TryNewService(props);
      }
      // when there is no service name provided, this is all we can do anyway
      if (kls == null && !string.IsNullOrEmpty(props.ServiceName)) {
        props.ServiceName = string.Empty;
        kls = TryNewService(props); // fail safe method 3
      }

      if (kls != null && !props.DoNotPersistToConfigDb)
        kls.SaveToConfigurationDatabase();
      if (kls == null && Default != null) {
        // In certain cases where processes such as claim providers
        // run outside a context where the user has the right to create
        // logging service, you will errors can happen here.
        kls = defaultProvider;
        if (kls != null)
          kls.Write("CreateNew failed to create ULS log service; falling back on Default ULS logging service. ", TraceSeverity.Monitorable, EventSeverity.Warning);
      }
      if (kls == null) {
        kls = FailSafeLogService;
        WriteToFailSafeLog(
          string.Format("All fail-safes for CreateNew service = '{0}' failed. Returning unattached (fail-safe) service.", serviceName)
          , null, false);
      }
      if (kls == null)
        WriteToFailSafeLog("CreateNew failed completely.", null);
      return kls;
    }
    /*
    public static KrakenLoggingService CreateNew(LoggingCategories defaultCategory = DEFAULT_CATEGORY, bool saveToConfigDatabase = false) {
      return CreateNew(DEFAULT_SERVICE_NAME, defaultCategory, saveToConfigDatabase);
    }
    */

    #region Logging Fail Safe

    private static void WriteToFailSafeLog(string message, Exception ex, bool severe = true) {
      if (SuppressBackupLog)
        return;
      // never allow commands to use backup log while they are in the backup process
      bool oldValue = SuppressBackupLog; SuppressBackupLog = true;
      // made false because it doesn't seem to be making life better
      bool failCatastrophically = false; // true;
      // if we're able to write anything we'll fail gracefully and set this to false
      // we call on defaultProvider instead of Default to prevent infinite recursion
      KrakenLoggingService kls = FailSafeLogService;
      if (kls != null) {
        try {
          //SPDiagnosticsCategory category = LoggingCategoryProvider.DefaultCategoryProvider.GetCategory(LoggingCategories.KrakenUnknown);
          if (severe) {
            if (!string.IsNullOrEmpty(message))
              kls.Write(message, TraceSeverity.Unexpected, EventSeverity.ErrorCritical, LoggingCategories.KrakenLogging); // category
          } else {
            if (!string.IsNullOrEmpty(message))
              kls.Write(message, TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenLogging); // category
          }
          if (ex != null && !string.IsNullOrEmpty(ex.Message))
            kls.Write(ex);
          failCatastrophically = false;
        } catch { }
      }
      SuppressBackupLog = oldValue;
      if (failCatastrophically) {
        // TODO log me someplace!
        // nothing else to do but fail miserably here
        throw (ex != null) ? new Exception(message, ex) : new Exception(message);
      }
    }

    #endregion

    #endregion

    internal void SaveToConfigurationDatabase() {
      if (this.DoNotPersistToConfigDb)
        return;
      // TODO we should provide a way to save to configuration object store
      Write("KLS: SaveToConfigurationDatabase is not implemented.", TraceSeverity.Monitorable, EventSeverity.Information, LoggingCategories.KrakenLogging);
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
      Write(string.Format("{0} {1}::{2}", isExit ? "Exiting" : "Entering", type.FullName, methodName), TraceSeverity.Verbose, EventSeverity.Verbose, cat);
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

    #region Logging Event listener

    public void Log(object sender, LoggingEventArgs e) {
      if (e.Exception != null)
        Write(e.Exception, e.Category);
      else
        Write(e.Message, e.TraceLevel, e.EventLevel, e.Category);
    }

    #endregion
    #region ITrace interface

    private void InitHandlers() {
      Handler = (level, msg) => {
        if (level == TraceLevel.Warning) {
          TraceWarning(msg);
        }
        if (level == TraceLevel.Error) {
          TraceError(msg);
        }
        if (level == TraceLevel.Info) {
          TraceInfo(msg);
        }
        if (level == TraceLevel.Verbose) {
          TraceVerbose(msg);
        }
      };
    }

    public int Depth { get; set; } = 0;

    public Action<TraceLevel, string> Handler { get; set; }

    private TraceLevel Convert(TraceSeverity severity) {
      switch (severity) {
        case TraceSeverity.Unexpected:
          return TraceLevel.Error;
        case TraceSeverity.High:
          return TraceLevel.Warning;
        case TraceSeverity.Medium:
        case TraceSeverity.Monitorable:
          return TraceLevel.Info;
        default: // Verbose VerboseEx None
          return TraceLevel.Verbose;
      }
    }

    private TraceSeverity Convert(TraceLevel level, out EventSeverity eSev) {
      switch (level) {
        case TraceLevel.Error:
          eSev = EventSeverity.Error;
          return TraceSeverity.Unexpected;
        case TraceLevel.Warning:
          eSev = EventSeverity.Warning;
          return TraceSeverity.High;
        case TraceLevel.Info:
          eSev = EventSeverity.Information;
          return TraceSeverity.Medium;
        case TraceLevel.Verbose:
          eSev = EventSeverity.Verbose;
          return TraceSeverity.Verbose;
        default:
          eSev = EventSeverity.Verbose;
          return TraceSeverity.VerboseEx;
      }
    }

    public TraceLevel Level { get; set; }

    public bool SilenceErrors { get; set; }
    public bool SilenceWarnings { get; set; }

    public void Trace(TraceLevel level, string format, params object[] args) {
      EventSeverity eSev;
      TraceSeverity tSev = Convert(level, out eSev);
      string indent = string.Empty;
      for (int i = 1; i < this.Depth; i++)
        indent += "  ";
      format = indent + string.Format(format, args);
      Write(format, tSev, eSev);
    }

    public void TraceInfo(string format, params object[] args) {
      Trace(TraceLevel.Info, format, args);
    }

    public void TraceError(string format, params object[] args) {
      Trace(TraceLevel.Error, format, args);
    }

    public void TraceError(Exception ex) {
      Write(ex);
    }

    public void TraceWarning(string format, params object[] args) {
      Trace(TraceLevel.Warning, format, args);
    }

    public void TraceVerbose(string format, params object[] args) {
      Trace(TraceLevel.Verbose, format, args);
    }

    public void TraceObject(object obj) {
      TraceInfo("OBJECT: {0}", obj.ToString());
      //throw new NotImplementedException();
    }

    #endregion

    #region Register/Unregister (Event Log Registry Keys)

    /*
     * The following Register and Unregister methods are typically 
     * called by feature receivers; they check for the service instance
     * that's read by Local property and they make changes to the registry
     * to support configuration of logging categories. For calls to local,
     * most fail-safes are temporary disabled. All methods handle exceptions
     * so there should be no need to have exception handling in the caller.
     */

    private const string EventLogMessageFile = @"C:\Windows\Microsoft.NET\Framework\v2.0.50727\EventLogMessages.dll";
    private const string EventLogApplicationRegistryKeyPath = @"SYSTEM\CurrentControlSet\services\eventlog\Application";

    /// <summary>
    /// Loads or provisions the KLS logging service.
    /// Writes the registry keys for logging categories
    /// into each farm server.
    /// </summary>
    /// <returns>KLS service read by Local or created (and saved to db) by this call.</returns>
    public static KrakenLoggingService Register() {
      KrakenLoggingService service = null;
      KrakenLoggingService kls = KrakenLoggingService.Default;
      kls.Entering(System.Reflection.MethodBase.GetCurrentMethod());
      try {
        kls.TraceVerbose("Elevating");
        SPSecurity.RunWithElevatedPrivileges(delegate () {
          kls.TraceVerbose("Getting local farm");
          SPFarm farm = SPFarm.Local;
          if (farm == null)
            throw new ArgumentNullException("SPFarm.Local");

          kls.TraceVerbose("Getting local KLS service");
          // fail safe methods are now baked into Local
          bool originalValue = SuppressLocalFailSafe;
          SuppressLocalFailSafe = true;
          service = KrakenLoggingService.Local;
          SuppressLocalFailSafe = originalValue;
          if (GetLocalFailedReason == LocalFailReason.FileNotFound) {
            kls.TraceVerbose("Detected a potention version conflict in the configuration database");
            // TODO we may want to delete the original object now too
          }

          if (service == null) {
            kls.TraceVerbose("Service not found. Creating new service.");
            // This one has no name or ID... maybe that's not what we want to do anymore
            service = new KrakenLoggingService();
            kls.TraceVerbose("Saving to database (calling Update)");
            service.Update();
            if (service.Status != SPObjectStatus.Online) {
              kls.TraceVerbose("Service not Online. Provisioning.");
              service.Provision();
            } else {
              kls.TraceVerbose("Saving to database (calling Update)");
              service.Update();
            }
            kls.TraceVerbose("Done.");
          }
          kls.TraceVerbose("Registering logging areas on each server");
          foreach (SPServer server in farm.Servers) {
            Register(server);
          }
          kls.TraceVerbose("Done registering. Leaving Elevated.");
        });
      } catch (Exception ex) {
        WriteToFailSafeLog("Unexpected error in Register", ex);
      } finally {
        kls.Leaving(System.Reflection.MethodBase.GetCurrentMethod());
      }
      return service;
    }

    private static void Register(SPServer server) {
      KrakenLoggingService kls = KrakenLoggingService.Default;
      if (server.Role == SPServerRole.Invalid) {
        kls.EnteringPreCheck(System.Reflection.MethodBase.GetCurrentMethod()
          , string.Format("Server name = '{0}' has farm role = 'Invalid' and will be skipped. For SQL database and SMTP/mail servers, this is normal.", server.Name));
        return;
      }
      kls.Entering(System.Reflection.MethodBase.GetCurrentMethod());
      try {
        RegistryKey eventLogKey = GetEventLogKey(server);
        if (eventLogKey == null)
          return;

        IList<SPDiagnosticsArea> areas = LoggingCategoryProvider.DefaultCategoryProvider.Areas;
        if (areas == null)
          throw new ArgumentNullException("areas", "You must provide a LoggingCategoryProvider object with Areas collection defined.");
        kls.TraceVerbose("Attempting to Register {0} Diagnostic Logging Areas", areas.Count);
        foreach (SPDiagnosticsArea area in areas) {
          kls.TraceVerbose("Attempting to Register Diagnostic Logging Area '{0}'", area.Name);
          RegistryKey loggingServiceKey = null;
          try {
            loggingServiceKey = eventLogKey.OpenSubKey(area.Name);
          } catch (System.IO.IOException) { }
          if (loggingServiceKey == null) {
            loggingServiceKey = eventLogKey.CreateSubKey(area.Name, RegistryKeyPermissionCheck.ReadWriteSubTree);
            loggingServiceKey.SetValue("EventMessageFile", EventLogMessageFile, RegistryValueKind.String);
          }
        }
      } catch (Exception ex) {
        WriteToFailSafeLog(string.Format("Unexpected error in Register for server = {0}", server.Name), ex);
      } finally {
        kls.Leaving(System.Reflection.MethodBase.GetCurrentMethod());
      }
    }

    public static bool Unregister() {
      KrakenLoggingService kls = KrakenLoggingService.Default;
      kls.Entering(System.Reflection.MethodBase.GetCurrentMethod());
      bool success = false;
      try {
        kls.TraceVerbose("Elevating");
        SPSecurity.RunWithElevatedPrivileges(delegate () {
          kls.TraceVerbose("Getting local farm");
          SPFarm farm = SPFarm.Local;
          if (farm == null)
            throw new ArgumentNullException("SPFarm.Local");

          kls.TraceVerbose("Un-registering logging areas on each server");
          foreach (SPServer server in farm.Servers) {
            Unregister(server);
          }
          kls.TraceVerbose("Done un-registering.");

          kls.TraceVerbose("Getting local KLS service");
          // fail safe methods are now baked into Local
          bool originalValue = SuppressLocalFailSafe;
          SuppressLocalFailSafe = true;
          KrakenLoggingService service = KrakenLoggingService.Local;
          SuppressLocalFailSafe = originalValue;
          if (GetLocalFailedReason == LocalFailReason.FileNotFound) {
            kls.TraceVerbose("Detected a potention version conflict in the configuration database, but we are deleting it anyway.");
            // we're deleting it anyway
          }

          kls.TraceVerbose("Deleting KLS service");
          if (service != null) {
            try {
              service.Delete();
              kls.TraceVerbose("Deleted.");
              success = true;
            } catch (NullReferenceException) {
              kls.TraceVerbose("Failed to Delete with NullReferenceException. It's probably already gone.");
            }
          }
          kls.TraceVerbose("Leaving Elevated.");
        });
      } catch (Exception ex) {
        WriteToFailSafeLog("Unexpected error in Unregister", ex);
      } finally {
        kls.Leaving(System.Reflection.MethodBase.GetCurrentMethod());
      }
      return success;
    }

    private static void Unregister(SPServer server) {
      KrakenLoggingService kls = KrakenLoggingService.Default;
      kls.Entering(System.Reflection.MethodBase.GetCurrentMethod());
      try {
        RegistryKey eventLogKey = GetEventLogKey(server);
        if (eventLogKey == null)
          return;
        IList<SPDiagnosticsArea> areas = LoggingCategoryProvider.DefaultCategoryProvider.Areas;
        foreach (SPDiagnosticsArea area in areas) {
          RegistryKey loggingServiceKey = null;
          try {
            loggingServiceKey = eventLogKey.OpenSubKey(area.Name);
          } catch (System.IO.IOException) { }
          if (loggingServiceKey != null)
            eventLogKey.DeleteSubKey(area.Name);
        }
      } catch (Exception ex) {
        WriteToFailSafeLog(string.Format("Unexpected error in Unregister for server = {0}", server.Name), ex);
      } finally {
        kls.Leaving(System.Reflection.MethodBase.GetCurrentMethod());
      }
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
      } catch (System.IO.IOException) {
        // This is normal for SMTP and SQL servers
        string msg = string.Format("Couldn't open registry key {0} on server = {1}. This is normal for servers such as SQL and SMTP which may be part of the farm but are not SharePoint servers, and in such cases this warning may be ignored.", EventLogApplicationRegistryKeyPath, server.Name);
        Default.Write(msg, TraceSeverity.Monitorable, EventSeverity.Warning);
        //Default.Write(ex, LoggingCategories.KrakenAlerts);
        return null;
      }
      return eventLogKey;
    }

    #endregion

  } // class
} // namespace
