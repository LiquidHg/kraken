using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Logging {
  public class LoggingProperties {

    public const bool FAULT_TOLERANT_NO_CATEGORY = true;
    public const LoggingCategories DEFAULT_CATEGORY = LoggingCategories.KrakenUnknown;
    public const string DEFAULT_SERVICE_NAME = "Kraken Logging Service";

    /// <summary>
    /// When true, treat the creation operation as if it is a fall
    /// back mechanism. Defaults to false and will be automatically 
    /// set to true when the first attempt to create KLS fails.
    /// </summary>
    public bool IsRecovery { get; set; } = false;

    /// <summary>
    /// The name of the service to load. Set this to empty string 
    /// when you want to create an instance not attached to the 
    /// configuration database.
    /// </summary>
    public string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

    /// <summary>
    /// When true, instance should never be saved to
    /// the configuration database.
    /// </summary>
    public bool DoNotPersistToConfigDb { get; set; } = false;

    /// <summary>
    /// Specify that logging service should be created
    /// using elevated privileges. Usually this is not needed,
    /// but in certain cases it should be done from the start.
    /// </summary>
    public bool CreateElevated { get; set; } = false;

#pragma warning disable 618 // This is okay to use in this case; we trap for errors later on.
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

  }

}
