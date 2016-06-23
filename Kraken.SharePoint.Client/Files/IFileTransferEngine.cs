using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint.Client;

namespace Kraken.SharePoint.Client.Files
{
    public interface IFileTransferEngine
    {
        void Upload(Folder targetFolder);

        void Upload(File targetFile);

        void Download(File file);

        void Download(Folder folder);

    }
}
