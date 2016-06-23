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