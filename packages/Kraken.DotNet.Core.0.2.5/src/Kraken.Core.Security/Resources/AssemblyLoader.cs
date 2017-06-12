using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Kraken.Security.Resources {
	public static class AssemblyLoader {

		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public static Dictionary<string, string> Names = new Dictionary<string, string>();

		public static List<Assembly> SearchAssemblies = new List<Assembly>();

		//public static bool ThrowOnNotFound = true;

		public static byte[] GetAssemblyFromResource(string resName) {
			foreach (Assembly asm in SearchAssemblies) {
				byte[] buff = GetAssemblyFromResource(asm, resName);
				if (buff != null)
					return buff;
			}
			return null;
		}

		public static byte[] GetAssemblyFromResource(Assembly asm, string resName) {
			if (asm == null)
				throw new ArgumentNullException("asm");
			byte[] buff = null;
			string dllName = resName + ".dll";
			foreach (string res in asm.GetManifestResourceNames()) {
				if (res.EndsWith(dllName)) {
					Stream s = asm.GetManifestResourceStream(res);
					buff = new byte[s.Length];
					s.Read(buff, 0, buff.Length);
					return buff;
				}
			}
#if DEBUG
			Trace.TraceWarning("AssemblyLoader - Assembly not found in resources: " + dllName);
#endif
			return null;
		}

		[System.Reflection.Obfuscation(Exclude = false)]
		public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
			try {
				string name = string.Empty; string fullName = string.Empty; 
				//string version = string.Empty; string culture = string.Empty; string token = string.Empty;
				// if the name contains a comma, its a fully qualified name
				// for now we only care about its short name
				// TODO can we do a strong name integrity check here?
				if (name.Contains(",")) {
					AssemblyName asmName = new AssemblyName(args.Name);
					name = asmName.Name;
				} else {
					name = args.Name;
				}
				if (!Names.Keys.Contains(name)) {
#if DEBUG
					Trace.TraceWarning("AssemblyLoader - Assembly not listed in names: " + args.Name);
#endif
					return null;
				}
				byte[] raw = GetAssemblyFromResource((!string.IsNullOrEmpty(Names[name])) ? Names[name] : name);
				if (raw != null)
					return Assembly.Load(raw);
				//if (ThrowOnNotFound)
				//	throw new FileNotFoundException(dllName);
			} catch (Exception ex) {
				Log.Error(ex);
#if DEBUG
				Trace.TraceError("AssemblyLoader - " + ex.Message);
#endif
			}
			return null;
		}














	}
}
