
namespace Kraken {

    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Configuration;
    using System.Data;
    using System.Reflection;
    using System.Runtime.Remoting;
    using System.Security;
    using System.Security.Policy;
    using System.Text;

    /// <summary>
    /// For now this is a static class. It is not thread safe.
    /// </summary>
    public class AppDomainManager { // TODO implement IList or some other kind of collection interface

        private static HybridDictionary _appDomainsStatic = new HybridDictionary();

        private bool _useStaticList;
        private HybridDictionary _appDomains;

        private HybridDictionary AppDomainList {
            get {
                if (_useStaticList)
                    return _appDomainsStatic;
                else
                    return _appDomains;
            }
        }

        public AppDomainManager(bool useStaticList) {
            _useStaticList = useStaticList;
            if (!useStaticList)
                _appDomains = new HybridDictionary();
        }

        public AppDomainInstance Create(string name, Type type) {
            if (AppDomainList.Contains(name))
                throw new ArgumentOutOfRangeException("name", string.Format("Name must be unqiue. There is already an app domain in the collection by the name '{0}'.", name));
            AppDomainInstance instance = new AppDomainInstance(name, type);
            AppDomainList.Add(name, instance);
            return instance;
        }

        public AppDomainInstance Add(AppDomainInstance instance) {
            string name = instance.Name;
            if (AppDomainList.Contains(name))
                throw new ArgumentOutOfRangeException("name", string.Format("Name must be unqiue. There is already an app domain in the collection by the name '{0}'.", name));
            AppDomainList.Add(name, instance);
            return instance;
        }

        public AppDomainInstance this[string name] {
            get {
                if (AppDomainList.Contains(name))
                    return (AppDomainInstance)AppDomainList[name];
                return null;
            }
        }

        public bool Remove(string name) {
            AppDomainInstance instance = this[name];
            if (instance == null)
                return false;
            instance.Dispose();
            instance = null;
            AppDomainList.Remove(name);
            return true;
        }

    } // AppDomainManager

    /// <summary>
    /// This class is used to create a single app domain that has a handle to a single object
    /// </summary>
    public class AppDomainInstance : IDisposable {

        protected AppDomain _appDomain;

        protected ObjectHandle _handle;
        public ObjectHandle Handle {
            get { return _handle; }
        }

        private string _name;
        public string Name {
            get { return _name; }
        }

        public string ApplicationBase {
            get { return _appDomain.SetupInformation.ApplicationBase; }
        }
        public string PrivateBinPathProbe {
            get { return _appDomain.SetupInformation.PrivateBinPathProbe; }
        }
        public string PrivateBinPath {
            get { return _appDomain.SetupInformation.PrivateBinPath; }
        }

        public AppDomainInstance(string name, Type type) {
            _name = name;
            AppDomainSetup appDomainSetup = AppDomain.CurrentDomain.SetupInformation;
            Evidence evidence = AppDomain.CurrentDomain.Evidence;
            _appDomain = AppDomain.CreateDomain(name, evidence, appDomainSetup);
            _handle = _appDomain.CreateInstance(Assembly.GetExecutingAssembly().GetName().FullName, type.FullName);
        }

        #region IDisposable Members

        public void Dispose() {
            if (_appDomain != null) {
                AppDomain.Unload(_appDomain);
                _appDomain = null;
            }
            if (_handle != null)
                _handle = null;
        }

        #endregion

    } // AppDomainInstance

} // namespace