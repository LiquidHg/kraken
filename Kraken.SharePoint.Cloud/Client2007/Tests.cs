/*
namespace Behemoth.SharePoint.Client {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class Tests {

        [TestMethod]
        public static void GetWebsTest() {
            string webUrl = "http://spdev";
            WebRepository webRep = new WebRepository();
            var webs = webRep.GetSubWebs(webUrl);
            Assert.IsTrue(webs.Count > 0, string.Format("There were no subwebs in '{0}'", webUrl));
            foreach (Web web in webs) {
                Console.WriteLine(string.Format("Web with ID='{0}' and Title='{1}' at Url='{2}'.", web.ID, web.Title, web.Url));
            }
        }

    }
}
*/