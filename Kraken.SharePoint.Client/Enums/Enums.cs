namespace Microsoft.SharePoint.Client {

  using System;

  public enum UpdateItemResult {
    NoResult = 0,
    UpdateOK = 1,
    UpdatePartialFail = 2,
    UpdateFail = 3
  }

  /// <summary>
  /// Used to specify methodolgy to use for item searches
  /// such as queries queries against a list.
  /// </summary>
  [System.Reflection.Obfuscation(Exclude = true, ApplyToMembers = false)]
  public enum ListItemFindMethod {

    /// <summary>
    /// Perform a master query, then use simple in-memory 
    /// logic to filter items based on the individual rules.
    /// </summary>
    OneQuerySimpleMatch,

    /// <summary>
    /// Ignores any provided master query and uses only the rules
    /// of a rule set to to find matches. Results should be a union 
    /// of multiple queries.
    /// </summary>
    MultiQueryMatch,

    // TODO best not to reveal until actually used
    /*
    /// <summary>
    /// Experimental: leverage SharePoint search service to find
    /// items across multiple lists, libraries, webs, and sites.
    /// </summary>
    SearchService,

    /// <summary>
    /// Experimental: leverage SharePoint content query service to find
    /// items across multiple lists, libraries, webs.
    /// </summary>
    ContentQueryService
    */

  }

  public enum ListItemUrlType {
    FileRefUrl,
    DocIdUrl,
    DisplayFormUrl,
    EditFormUrl
  }

  [Flags]
  public enum WorkflowEvents {
    ItemAdded,
    ItemUpdated,
    WorkflowStart
  }

  [Flags]
  public enum FindMethod {
    None,
    InternalName,
    DisplayName,
    Id,
    Any = InternalName | DisplayName | Id
    //AutoInternalDisplay,
    //AutoDisplayInternal
  }

  [Flags]
  public enum FieldFindMethod {
    None,
    InternalName,
    DisplayName,
    StaticName,
    Id,
    Any = InternalName | DisplayName | StaticName | Id
    //AutoStaticInternal,
    //AutoInternalStatic,
    //AutoStaticInternalDisplay,
    //AutoDisplayInternalStatic
  }

  public enum ModerationStatusType {
			Approved = 0,
			Denied = 1,
			Pending = 2,
			Draft = 3,
			Scheduled = 4
		}

  /// <summary>
  /// Determine how often ExecuteQuery should be called
  /// for functions that do so.
  /// </summary>
    public enum ExecuteQueryFrequency {
      Skip,
      Once,
      EveryItem
    }

		public enum UploadMethod {
			None = 0,
			CSOM = 1,
			Direct = 2,
			DirectClone = 3
		}

		[Flags]
    public enum HashAlgorithmType {
      None = 0,
      CRC32 = 1,
      MD5 = 2
    }
    /*
		public enum FileIntegrityMethods {
			None = 0,
			CRC32 = 1,
			MD5 = 2
		}*/

		public enum HashCompareType {
			None = 0,
			HashAndName = 1,
			OnlyHash = 2,
			NoHash = 3
		}

		public enum FieldUpdateCondition {
			UpdateAlways = 0,
			UpdateIfEmpty = 1
		}

}
