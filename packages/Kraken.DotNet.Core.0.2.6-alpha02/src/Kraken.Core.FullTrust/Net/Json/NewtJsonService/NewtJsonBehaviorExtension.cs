/* There's no .net 3.5 equivalent for System.Net.Http.Formatting
 * at least that we can find so far, so for now folks in order 
 * versions will just have to do without.
 */
#if !DOTNET_V35
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Configuration;
using System.Web;

namespace Kraken.Net.WebApi {
	public class NewtJsonBehaviorExtension : BehaviorExtensionElement {
		public override Type BehaviorType {
			get { return typeof(NewtJsonBehavior); }
		}

		protected override object CreateBehavior() {
			return new NewtJsonBehavior();
		}
	}
}
#endif