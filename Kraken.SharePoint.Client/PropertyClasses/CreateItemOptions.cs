using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client {

  public class CreateItemOptions : UpdateItemOptions {
    public CreateItemOptions() : base() {
      IgnoreIDField = true;
      // has no significant effect on new items, but if updating at every field it can be important
      PreserveModifiedDate = true;
      SkipTitleOnUpdate = true;
      SkipContentTypeIdOnUpdate = true;
    }

    public bool IgnoreIDField { get; set; }

  }
}
