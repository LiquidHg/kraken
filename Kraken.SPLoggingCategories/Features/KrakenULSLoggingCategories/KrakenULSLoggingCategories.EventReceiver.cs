using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Security;

using Kraken.SharePoint.Logging;
//using Kraken.Logging;

namespace Kraken.SharePoint.Features.KrakenULSLoggingCategories {
  /// <summary>
  /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
  /// </summary>
  /// <remarks>
  /// The GUID attached to this class may be used during packaging and should not be modified.
  /// </remarks>

  //ea3de771-2ff8-475f-a832-106816dccc3c
  [Guid("f680b665-a5fb-458d-9e31-0b65df1324e2")]
  public class KrakenULSLoggingCategoriesEventReceiver : SPFeatureReceiver {

    public override void FeatureActivated(SPFeatureReceiverProperties properties) {
      KrakenLoggingService.Register();
    }

    public override void FeatureDeactivating(SPFeatureReceiverProperties properties) {
      KrakenLoggingService.Unregister();
    }

    public override void FeatureInstalled(SPFeatureReceiverProperties properties) {
    }

    public override void FeatureUninstalling(SPFeatureReceiverProperties properties) {
    }

  }
}

