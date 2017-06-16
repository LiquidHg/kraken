using System;

namespace Kraken.SharePoint.Client.Helpers.FPRPC
{
    /// <summary>
    /// FrontPage RPC exception. 
    /// </summary>
    /// <remarks>Currently this serves as a simple wrapper for an ApplicationException</remarks>
    [Serializable]
    public class FrontPageRPCException : ApplicationException
    {
        public string Url;

        public FrontPageRPCException()
        {
        }

        public FrontPageRPCException(string message)
            : base(message)
        {
        }

        public FrontPageRPCException(string message, string url)
            : base(message)
        {
            Url = url;
        }

        public FrontPageRPCException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public FrontPageRPCException(string message, string url, Exception innerException)
            : base(message, innerException)
        {
            Url = url;
        }
    }
}