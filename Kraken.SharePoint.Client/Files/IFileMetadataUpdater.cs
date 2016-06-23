using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Microsoft.SharePoint.Client;

namespace Kraken.SharePoint.Client.Files {

    public interface IFileMetadataUpdater
    {
        void UpdateListItem(File file);

        void UpdateFileProperties(ListItem item);
    }

}
