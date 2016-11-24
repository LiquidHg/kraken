using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Kraken.Core.Security.Certificates;
using System.Security.Cryptography.X509Certificates;
using System.Web;

namespace Kraken.Tests
{
    [TestClass]
    public class CertificateUtilTests
    {
        [TestMethod]
        public void GetCertificate_ReturnsTrue()
        {
            Assert.IsNotNull(CertificateUtil.GetCertificate(StoreName.My, StoreLocation.LocalMachine, "a5 f3 ac c3 1f e5 eb 59 b1 4a be c1 38 ad 8b 00 51 00 b5 85"));
            Assert.IsNotNull(CertificateUtil.GetCertificate(StoreName.My, StoreLocation.LocalMachine, "A5F3ACC31FE5EB59B14ABEC138AD8B005100B585"));
            Assert.IsNotNull(CertificateUtil.GetCertificate(StoreName.My, StoreLocation.LocalMachine, "CN=127.0.0.1, O=TESTING ONLY, OU=Windows Azure DevFabric"));
            Assert.IsNotNull(CertificateUtil.GetCertificate(StoreName.My, StoreLocation.LocalMachine, "CN=colossusconsulting-GUARDIAN-CA, DC=colossusconsulting, DC=com"));
        }

        [TestMethod]
        public void Temp()
        {
            var url = "https://dmitryf04:82/_trust";
            Console.WriteLine(HttpUtility.UrlEncode(url));
        }
    }
}
