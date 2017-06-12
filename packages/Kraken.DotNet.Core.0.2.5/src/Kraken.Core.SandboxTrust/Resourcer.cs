
namespace Kraken.Resources {

    using System;
    using System.Collections.Specialized;
    using System.Resources;
    using System.Reflection;
    using System.Text;

	/// <summary>
	/// This class provides static methods for quickly instantiating resource files
	/// such as those created by Lutz Roeder's Reosurcer.net. 
	/// </summary>
	public class Resourcer {

    public Resourcer() { }

    private static HybridDictionary _mgrList = new HybridDictionary(); // <ResourceManager>
    /// <summary>
    /// All resource amangers created by this class are stored for caching purposes.
    /// This method is not thread safe, so when running in a different thread you will
    /// get a seperate instance of this list and seperate instances of its ResourceManagers.
    /// </summary>
    public static HybridDictionary Instances {
      get { return _mgrList; }
    }

    /// <summary>
    /// This will resolve the resourceName (if unqualified) using the namespace of the
    /// provided type. For assemblies you must use the fully qualified name. It will search 
    /// for the resource within the provided (or provided type's) assembly.) The results are 
    /// cahced so that it can be read from a hash list on subsequent calls.
    /// 
    /// Use like:
    ///   GetResourceManager(typeof(Registration), "Constellation.CCG.FOIT.MiscCoal.AppResources");
    ///   or GetResourceManager(typeof(Registration), "AppResources");
    /// </summary>
    /// <param name="assembly">The assembly that contains the resouce</param>
    /// <param name="assemblyType">A type in the same assembly as the resouce</param>
    /// <param name="resourceName">Qualified or unqualified name of the resource class</param>
    /// <param name="resourceFullName">Fully qualified name of the resource class</param>
    /// <returns>A resource manager that can be used to read the resource</returns>
    public static ResourceManager GetResourceManager(Type assemblyType, string resourceName) {
      string resName = BuildTypeName(assemblyType, resourceName);
      return GetResourceManager(assemblyType.Assembly, resName);
    }

    public static ResourceManager GetResourceManager(Assembly assembly, string resourceFullName) {
      if (!_mgrList.Contains(resourceFullName))
        _mgrList[resourceFullName] = new ResourceManager(resourceFullName, assembly);
      return (ResourceManager)_mgrList[resourceFullName];
    }


    /// <summary>
    /// If an unqualified resource name, it will attempt to prefix it with the namespace
    /// of the provided type. If fully qualified, it will just return the string you give it.
    /// </summary>
    /// <param name="assemblyType"></param>
    /// <param name="resourceName"></param>
    /// <returns></returns>
    internal static string BuildTypeName(Type assemblyType, string resourceName) {
      if ( resourceName.IndexOf(".") >= 0 )
        return resourceName;
      string fullName = assemblyType.FullName;
      int lastDotPos = fullName.LastIndexOf(".") + 1;
      fullName = fullName.Substring(0, lastDotPos) + resourceName;
      return fullName;
    }

    /// <summary>
    /// Reads a file embedded in a RESX file (a la Resourcer.NET) and returns it
    /// as a string. This uses UTF8 encoding, so if the file was saved as unencoded 
    /// text (not a byte array) then you can skip this can just use GetObject, but
    /// you can call this function and it will still work.
    /// </summary>
    /// <param name="resources"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static string GetStringFromResourceFile(ResourceManager resources, string fileName) {
      object o = resources.GetObject(fileName);
      if (o == null)
        return string.Empty;
      if (o.GetType() == typeof(string))
        return (string)o;

      // type check to make sure it is byte[]
      if (o.GetType() == typeof(byte[]))
        throw new NotSupportedException(
          string.Format("The specified resource named '{0}' is not a byte[] or string. Type '{1}' is not supported by this function.",
          fileName,
          o.GetType().FullName
        ));
      byte[] txtData = o as byte[];

      UTF8Encoding encoding = new UTF8Encoding();
      return encoding.GetString(txtData);

      /*
        // this method also works - nice and clean, so why did I do it the other way? :-)
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("AnAppADay.CommandLineCSharp.ConsoleApp.Header.txt")) {
          using (StreamReader read = new StreamReader(s)) {
            header = read.ReadToEnd();
          }
        }
      */
    }

	} // class

} // namespace
