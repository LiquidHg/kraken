using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using wp = System.Web.UI.WebControls.WebParts;

using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;

using Kraken.SharePoint.WebParts.Cloud;

namespace Kraken.SharePoint.WebParts {

  [ToolboxItemAttribute(false)]
  public class FlexBaseDotNetWebPart : SandboxWebPart {

    /// <summary>
    /// Developers should override this method and return true when a web part runs as sandbox code
    /// </summary>
    public override bool IsSandboxWebPart {
      get {
        return false;
      }
    }

    /// <summary>
    /// Note this method will not work on sandboxed solutions.
    /// </summary>
    /// <param name="runElevated"></param>
    public override void DoSaveProperties(bool runElevated) {
      base.DoSaveProperties(runElevated);
      SPSecurity.RunWithElevatedPrivileges(delegate() {
        base.SetPersonalizationDirty();
      });
    }

  }

}
