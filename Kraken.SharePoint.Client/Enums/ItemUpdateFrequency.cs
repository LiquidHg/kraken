using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client {

    /// <summary>
    /// Determines how often basic list item update methods will 
    /// actually call out to CSOM if they are used repeatedly.
    /// </summary>
    public enum ItemUpdateFrequency
    {
        EveryField,
        OncePerItem,
        Every10Items, // not yet implemented
        Every25Items, // not yet implemented
        Every50Items // not yet implemented
    }
}
