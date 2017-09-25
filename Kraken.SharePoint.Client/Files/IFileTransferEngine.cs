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
        void Upload(Folder targetFolder, ITransferTimeEstimator estimator);

        void Upload(File targetFile, ITransferTimeEstimator estimator);

        void Download(File file, ITransferTimeEstimator estimator);

        void Download(Folder folder, ITransferTimeEstimator estimator);

    }
}
