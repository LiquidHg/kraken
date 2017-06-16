using Kraken.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Reflection {

  /// <summary>
  /// Stores source-to-destination property mappings.
  /// Keys should correspond to the Source and Values 
  /// is a matching property in Target.
  /// </summary>
  public class PropertyMap<TSource, TTarget> 
    : Dictionary<string, string>
    where TSource : class
    where TTarget : class
    {

    public PropertyMap() { }
    public PropertyMap(TSource source, TTarget target) : this() {
      this.Target = target;
      this.Source = source;
      EnsureSourceAndTarget();
    }

    public ITrace Trace { get; set; } = NullTrace.Default;

    public TTarget Target { get; set; }
    public TSource Source { get; set; }

    private string[] _targetPropertyNames;
    private string[] _sourcePropertyNames;

    //public delegate bool ShouldIncludePropertyHandler(PropertyInfo);

    public Func<PropertyInfo, bool> IncludeSourceWhen { get; set; } = DefaultShouldInclude;

    public Func<PropertyInfo, bool> IncludeTargetWhen { get; set; } = DefaultShouldInclude;

    private static Func<PropertyInfo, bool> DefaultShouldInclude = delegate (PropertyInfo pi) { return true; };

    PropertyMapBehavior Behavior { get; set; } = PropertyMapBehavior.NoMappingUseDefault;

    Dictionary<string, string>.KeyCollection SourceProperties {
      get { return Keys; }
    }

    public void EnsureSourceAndTarget() {
      Trace.Enter(MethodBase.GetCurrentMethod());
      if (this.Target == null)
        throw new ArgumentNullException("this.Target");
      if (this.Source == null)
        throw new ArgumentNullException("this.Source");
      Trace.TraceVerbose("Checking Target");
      if (this.Target.GetType() == typeof(Hashtable)) {
        Trace.TraceVerbose("Target is Hashtable");
        Hashtable htt = this.Target as Hashtable;
        _targetPropertyNames = htt.KeysAsArray();
      } else {
        Trace.TraceVerbose("Target is Object");
        PropertyInfo[] targetProperties;
        targetProperties = this.Target.GetType().GetProperties();
        _targetPropertyNames = targetProperties
          .Where(this.IncludeTargetWhen)
          .Select(p => p.Name)
          .ToArray();
      }
      Trace.TraceVerbose("Target Property Names:");
      Trace.Depth++;
      foreach (string n in _targetPropertyNames) {
        Trace.TraceVerbose(n);
      }
      Trace.Depth--;

      Trace.TraceVerbose("Checking Source");
      if (this.Source.GetType() == typeof(Hashtable)) {
        Trace.TraceVerbose("Source is Hashtable");
        Hashtable hts = this.Source as Hashtable;
        _sourcePropertyNames = hts.KeysAsArray();
      } else {
        Trace.TraceVerbose("Source is Object");
        PropertyInfo[] sourceProperties;
        sourceProperties = this.Source.GetType().GetProperties();
        _sourcePropertyNames = sourceProperties
          .Where(this.IncludeSourceWhen)
          .Select(p => p.Name)
          .ToArray();
      }
      Trace.TraceVerbose("Source Property Names:");
      Trace.Depth++;
      foreach (string n in _sourcePropertyNames) {
        Trace.TraceVerbose(n);
      }
      Trace.Depth--;
      Trace.Exit(MethodBase.GetCurrentMethod());
    }

    public string MapToName(string sourcePropertyName, bool throwOnNotFound = true) {
      Trace.Enter(MethodBase.GetCurrentMethod(), "sourcePropertyName='{0}'", sourcePropertyName);
      if (sourcePropertyName.IsEmpty())
        throw new ArgumentNullException("sourcePropertyName");
      if (!this._sourcePropertyNames.Contains(sourcePropertyName)) {
        throw new IndexOutOfRangeException(string.Format("Index '{0}' does not exist in Source properties/keys", sourcePropertyName));
      }
      string trueName =
        (this.Behavior == PropertyMapBehavior.NoMappingUseDefault)
        ? sourcePropertyName : string.Empty;
      if (this.ContainsKey(sourcePropertyName))
        trueName = this[sourcePropertyName];
      if (trueName.IsEmpty())
        return string.Empty;
      if (!this._targetPropertyNames.Contains(trueName)) {
        throw new IndexOutOfRangeException(string.Format("Index '{0}' does not exist in Source properties/keys", trueName));
      }
      Trace.Exit(MethodBase.GetCurrentMethod(), " trueName='{0}'", trueName);
      return trueName;
      /*
      Hashtable hts = this.Source as Hashtable;
      if (hts != null && !hts.ContainsKey(sourcePropertyName)) {
        if (throwOnNotFound)
          throw new IndexOutOfRangeException(string.Format("Key '{0}' does not exist in Source Hashtable", sourcePropertyName));
      } else {}
      Hashtable htt = this.Target as Hashtable;
      if (htt != null && !htt.ContainsKey(trueName)) {
        if (throwOnNotFound)
          throw new IndexOutOfRangeException(string.Format("Key '{0}' does not exist in Target Hashtable", trueName));
      }
      */
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourcePropertyName">Name of the property in the source object</param>
    /// <returns></returns>
    public PropertyInfo MapToProperty(string sourcePropertyName) {
      Trace.Enter(MethodBase.GetCurrentMethod(), "sourcePropertyName='{0}'", sourcePropertyName);
      EnsureSourceAndTarget();
      Hashtable htt = this.Target as Hashtable;
      if (htt != null)
        throw new NotSupportedException("Use MapToName to get the key instead.");
      string trueName = MapToName(sourcePropertyName);
      PropertyInfo pi = this.Target.GetType().GetProperty(trueName);
      Trace.Exit(MethodBase.GetCurrentMethod());
      return pi;
    }

    private static ReflectionOperationResult SetValue(object target, string propertyOrKeyName, object value, bool isTarget, ITrace trace = null) {
      trace = trace.Ensure();
      trace.Enter(MethodBase.GetCurrentMethod(), "propertyOrKeyName='{0}'", propertyOrKeyName);
      ReflectionOperationResult result = new ReflectionOperationResult(target) { };
      Hashtable ht = target as Hashtable;
      if (ht != null) {
        if (ht.ContainsKey(propertyOrKeyName)) {
          ht[propertyOrKeyName] = value;
        } else {
          result.Message = string.Format("{0} hash table does not contain a key '{1}'.", isTarget ? "Target" : "Source", propertyOrKeyName);
        }
      } else {
        result = target.ImportProperty(propertyOrKeyName, value);
        /*
        PropertyInfo pi = target.GetType().GetProperty(propertyOrKeyName);
        if (pi != null) {
          result.TargetProperty = pi;
          pi.SetValue(target, value);
        } else {
          result.Message = string.Format("{0} object does not contain a property '{1}'.", isTarget ? "Target" : "Source", propertyOrKeyName);
        }
        */
      }
      trace.TraceVerbose("result.Success = {0}", result.Success);
      if (!result.Message.IsEmpty())
        trace.TraceVerbose("result.Message = {0}", result.Message);
      trace.Exit(MethodBase.GetCurrentMethod());
      return result;
    }

    private static object GetValue(object target, string propertyOrKeyName, bool isTarget, out ReflectionOperationResult result, ITrace trace = null) {
      trace = trace.Ensure();
      trace.Enter(MethodBase.GetCurrentMethod(), "propertyOrKeyName='{0}'", propertyOrKeyName);
      result = new ReflectionOperationResult(target) { };
      Hashtable ht = target as Hashtable;
      object sourceValue = null;
      if (ht != null) {
        if (ht.ContainsKey(propertyOrKeyName)) {
          sourceValue = ht[propertyOrKeyName];
          result.Target = sourceValue;
          result.TargetType = (result.Target == null) ? typeof(object) : result.Target.GetType();
          result.Success = true;
        } else {
          result.Message = string.Format("{0} hash table does not contain a key '{1}'.", isTarget ? "Target" : "Source", propertyOrKeyName);
        }
      } else {
        PropertyInfo pi = target.GetType().GetProperty(propertyOrKeyName);
        if (pi != null) {
          result.TargetProperty = pi;
          sourceValue = pi.GetValue(target);
          result.Target = sourceValue;
          result.TargetType = (result.Target == null) ? typeof(object) : result.Target.GetType();
          result.Success = true;
        } else {
          result.Message = string.Format("{0} object does not contain a property '{1}'.", isTarget ? "Target" : "Source", propertyOrKeyName);
        }
      }
      trace.TraceVerbose("result.Success = {0}", result.Success);
      if (!result.Message.IsEmpty())
        trace.TraceVerbose("result.Message = {0}", result.Message);
      trace.Exit(MethodBase.GetCurrentMethod());
      return sourceValue;
    }

    public ReflectionOperationResult CopyMappedValue(string sourcePropertyName) {
      Trace.Enter(MethodBase.GetCurrentMethod(), "sourcePropertyName='{0}'", sourcePropertyName);
      EnsureSourceAndTarget();
      ReflectionOperationResult result;
      /*
      if (IncludeSourceWhen != null
        && !IncludeSourceWhen(sourcePropertyName, this.Source)) {
        result = new ReflectionOperationResult();
        result.Message = string.Format("Skipped '{0}' because it didn't meet the rule for IncludeSourceWhen.", sourcePropertyName);
        return result;
      }
      */

      object sourceValue = GetValue(this.Source, sourcePropertyName, false, out result, this.Trace);
      if (!result.Success)
        return result;
      string trueName = MapToName(sourcePropertyName);
      if (trueName.IsEmpty()) {
        result.Message = string.Format("No mapped property for {0}. Nothing done.", sourcePropertyName);
        return result;
      }

      /*
      if (IncludeTargetWhen != null
        && !IncludeTargetWhen(sourcePropertyName, this.Target)) {
        result = new ReflectionOperationResult();
        result.Message = string.Format("Skipped '{0}' because it didn't meet the rule for IncludeTargetWhen.", sourcePropertyName);
        return result;
      }
      */

      result = SetValue(this.Target, trueName, sourceValue, true, this.Trace);
      Trace.Exit(MethodBase.GetCurrentMethod());
      return result;
    }
    public IEnumerable<ReflectionOperationResult> CopyMappedValues() {
      Trace.Enter(MethodBase.GetCurrentMethod());
      EnsureSourceAndTarget();
      List<ReflectionOperationResult> results = new List<ReflectionOperationResult>();
      foreach (string name in _sourcePropertyNames) {
        ReflectionOperationResult result = CopyMappedValue(name);
        results.Add(result);
      }
      Trace.Exit(MethodBase.GetCurrentMethod());
      return results;
    }

  }
  public enum PropertyMapBehavior {
    NoMappingUseDefault,
    NoMappingDoNothing
  }

}
