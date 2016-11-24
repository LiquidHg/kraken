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
      try {
        KrakenLoggingService.Register();
        KrakenLoggingService.Local.Update();
      } catch (Exception ex) {
        try {
          KrakenLoggingService.Default.Write(ex);
        } catch {
          // black hole, can't register and can't log, something's really messed up, but don't 
          // prevent installation
        }
      }
    }

    public override void FeatureDeactivating(SPFeatureReceiverProperties properties) {
      try {
        //this is actually done in Unregister
        //  KrakenLoggingService.Local.Delete();
        KrakenLoggingService.Unregister();
      } catch (Exception ex) {
        try {
          if (KrakenLoggingService.Default != null)
            KrakenLoggingService.Default.Write(ex);
        } catch {
          // black hole, can't register and can't log, something's really messed up, but don't 
          // prevent uninstallation
        }
      }
    }

    public override void FeatureInstalled(SPFeatureReceiverProperties properties) {
    }

    public override void FeatureUninstalling(SPFeatureReceiverProperties properties) {
    }

  }
}

