namespace Kraken.SharePoint.Client {

  using System;

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
