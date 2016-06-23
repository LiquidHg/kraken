using newt = Newtonsoft.Json;
using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace Kraken.Net.WebApi {

	//[System.Security.SecurityCritical()]
	public class RestSharpJsonNetSerializer : ISerializer {
		public RestSharpJsonNetSerializer() {
			ContentType = "application/json";
			ReplaceTypeNames = new Dictionary<string, string>();
		}

		private static newt.JsonSerializer js;
		public static newt.JsonSerializer NewtonSerializer {
			get {
				if (js == null) {
					js = new newt.JsonSerializer() {
						DateFormatHandling = newt.DateFormatHandling.MicrosoftDateFormat,
						TypeNameHandling = newt.TypeNameHandling.Objects,
						StringEscapeHandling = StringEscapeHandling.EscapeHtml,
						NullValueHandling = NullValueHandling.Ignore
					};
				}
				return js;
			}
		}

		public string Serialize(object obj) {
			string s = string.Empty;
			try {
				s = ToJson(obj, this.ReplaceTypeNames); // JsonConvert.SerializeObject(obj);
			} catch (Exception ex) {
				// TODO Log
				// we're getting weird security errors here
			}
				return s;
		}

		public static string ToJson(object o, Dictionary<string,string>replaceTypeNames = null) {
			//JsonTextWriter writer = new JsonTextWriter();
			StringBuilder sb = new StringBuilder();
			using (StringWriter sw = new StringWriter(sb)) {
				using (JsonWriter jw = new JsonTextWriter(sw)) {
					jw.Formatting = Formatting.Indented;
					NewtonSerializer.Serialize(jw, o);
					//jw.Flush();
					jw.Close();
				}
				sw.Close();
			}
			string s = sb.ToString();
			if (replaceTypeNames != null && replaceTypeNames.Count > 0) {
				// TODO there is probably a more efficient way to do this string replacement
				foreach (string replace in replaceTypeNames.Keys) {
					string replaceWith = replaceTypeNames[replace];
					s = s.Replace(replace, replaceWith);
				}
			}
			return s;
		}

		public string RootElement { get; set; }

		public string Namespace { get; set; }

		public string DateFormat { get; set; }

		public string ContentType { get; set; }

		/// <summary>
		/// Support renaming types needed in case of obfuscation or DLL linking
		/// </summary>
		public Dictionary<string, string> ReplaceTypeNames { get; set; }

	}
}
