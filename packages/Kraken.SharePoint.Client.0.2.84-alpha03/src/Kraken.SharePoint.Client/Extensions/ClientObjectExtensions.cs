namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Linq.Expressions;
  using System.Reflection;
  using System.Text;

  using Kraken.SharePoint.Client;
  using Kraken.Tracing;

  public static class KrakenClientObjectExtensions {

    /// <summary>
    /// Load one or more properties of a client object and call execute query
    /// so we can use them, but only if a call to the server is really needed. 
    /// This replaces ClientContext extension Init().
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="clientObject"></param>
    /// <param name="propertyName"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    public static string[] EnsureProperty<T>(this T clientObject, ITrace trace, string propertyName) where T : ClientObject {
      return clientObject.EnsureProperty<T>(trace, new string[] { propertyName });
    }
    public static string[] EnsureProperty<T>(this T clientObject, ITrace trace, string[] propertyNames) where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)clientObject.Context;
      return context.LoadProperties(clientObject, propertyNames, ExecuteQueryFrequency.Once, false, trace);
    }
    public static void EnsureProperty<T>(this T clientObject, ITrace trace, params Expression<Func<T, object>>[] propertyExpressions) where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)clientObject.Context;
      // fix for value does not fall within the expected range
      if (propertyExpressions != null)
        context.Load(clientObject, propertyExpressions);
      else
        context.Load(clientObject);
      context.ExecuteQueryIfNeeded();
    }

		/// <summary>
		/// Determines whether a given client object property has been loaded.
		/// Usage: Syntax similar to Load e.g. context.IsLoaded(object, o => o.Property);
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="clientObject"></param>
		/// <param name="property"></param>
		/// <returns>Return true is the property is available or instantiated.</returns>
		public static bool IsLoaded<T>(this T clientObject, Expression<Func<T, object>> property) where T : ClientObject {
      if (clientObject == null)
        return false;
      MemberExpression expression = null;
			if (property as LambdaExpression != null) {
				// here we hope that we get a member, but if it is wrapped in Convert we'll get an error instead
				expression = (MemberExpression)property.Body;
			} else if (property as MemberExpression != null) {
				expression = (MemberExpression)property.Body;
			}
			string propertyName = expression.Member.Name;
			Type propertyType = property.Body.Type;
			return clientObject.IsLoaded(propertyName, propertyType);
		}
		public static bool IsLoaded<T>(this T clientObject, PropertyInfo property) where T : ClientObject {
      if (clientObject == null)
        return false;
      string propertyName = property.Name;
			Type propertyType = property.PropertyType;
			return clientObject.IsLoaded(propertyName, propertyType);
		}
		public static bool IsLoaded<T>(this T clientObject, string propertyName, Type propertyType) where T : ClientObject {
      if (clientObject == null)
        return false;
			bool isCollection = typeof(ClientObjectCollection).IsAssignableFrom(propertyType);
			return isCollection ? clientObject.IsObjectPropertyInstantiated(propertyName) : clientObject.IsPropertyAvailable(propertyName);
		}

    public static bool LoadFromProperty(this ClientRuntimeContext context, ClientObject clientObject, PropertyInfo property, bool force = false, ITrace trace = null) {
      Type genericType = clientObject.GetType();
      MethodInfo method = typeof(KrakenClientObjectExtensions).GetMethod("Load", BindingFlags.Public | BindingFlags.Static); //, null, new Type[] { typeof(PropertyInfo) }, null);
      method = method.MakeGenericMethod(genericType);
      object result = method.Invoke(null, new object[] { context, clientObject, property, force, trace });
      return (bool)result;
    }

		public static bool Load<T>(this ClientRuntimeContext context, T clientObject, PropertyInfo property, bool force = false, ITrace trace = null) 
      where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      if (property == null)
        throw new ArgumentNullException("property");
      bool genericMatchesClientObject = (typeof(T) == clientObject.GetType());
      // This can happen when the generic type is ClientObject instead of the correect type
      // much easier and more reliable to fix it here than to do it twice below, though the 
      // other method below was working correctly.
      if (!genericMatchesClientObject) {
        return context.LoadFromProperty(clientObject, property, force, trace);
      }

      Type clientObjectType = clientObject.GetType();
      if (clientObjectType.GetProperty(property.Name) == null) //if (property.DeclaringType != typeof(T))
        throw new ArgumentException(string.Format("Specified property '{0}' must be a member of type '{1}'.", property.Name, clientObjectType.Name), "property");

      if (clientObject == null)
        return false;

			/*
			// creates a dynamic expressed to load the property by its name using reflection
			Expression<Func<T, object>> exp = (Expression<Func<T, object>>)System.Linq.Dynamic.DynamicExpression.CreateMemberExpression<T, object>(property, true);
			// early versions used this, but we got Convert more often than desired, so it was refined
			Expression<Func<T, object>> exp = (Expression<Func<T, object>>)System.Linq.Dynamic.DynamicExpression.ParseLambda<T, object>("o => o." + propName, pt);
			 */
      Expression<Func<T, object>> exp1 = null;
      Expression exp2 = null;
      if (genericMatchesClientObject) {
        exp1 = System.Linq.Dynamic.DynamicExpression.CreateParamterExpression<T, object>(property);
      } else {
        // eventually we gave up on utility and decided to roll the expression by hand
        exp2 = System.Linq.Dynamic.DynamicExpression.CreateParamterExpressionFromPropertyType(property, true);
      }

			if (force || !clientObject.IsLoaded(property)) {
				// collections must be handled slightly differently
				/*
				bool isCollection = typeof(ClientObjectCollection).IsAssignableFrom(property.PropertyType);
				if (isCollection) {
					var propVal = typeof(T).GetProperty(property.Name).GetValue(clientObject);
					context.Load(propVal);
				} else {
				 */

        if (exp1 != null)
					context.Load(clientObject, exp1);
        else {
          //context.Load(clientObject, exp2);
          Type funcType = typeof(Func<,>).MakeGenericType(clientObjectType, typeof(object));
          Type expType = typeof(Expression<>).MakeGenericType(funcType);
          Type expArrayType = expType.MakeArrayType();
          var arr = Array.CreateInstance(expType, 1);
          arr.SetValue(exp2, 0);
          // context.Load is the most annoying generic type evar!
          // there can be only ONE!
          MethodInfo loadMethod = typeof(ClientRuntimeContext).GetMethod("Load", BindingFlags.Public | BindingFlags.Instance);
          loadMethod = loadMethod.MakeGenericMethod(clientObjectType); // expArrayType
          loadMethod.Invoke(context, new object[] { 
            clientObject, arr
          });
        }
        return true;
				//}
      } else {
        return false;
      }
		}

    private static List<string> _unsupportedPropertyNames = null;
    private static List<string> UnsupportedPropertyNames {
      get {
        if (_unsupportedPropertyNames == null)
          _unsupportedPropertyNames = new string[] {
            "Context",
            "Path",
            "Tag",
            "ObjectVersion",
            "ServerObjectIsNull",
            "TypedObject"
          }.ToList();
        return _unsupportedPropertyNames;
      }
    }

		/// <summary>
		/// Loads all the public properties of a client object that have not been
		/// loaded previously, and optionally runs context.ExecuteQuery.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context"></param>
		/// <param name="clientObject"></param>
		/// <param name="executeQyery"></param>
    public static string[] LoadAllProperties<T>(this ClientRuntimeContext context, T clientObject, ExecuteQueryFrequency executeQyery = ExecuteQueryFrequency.Once, bool throwOnFail = false, ITrace trace = null) 
      where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      List<string> propertiesLoaded = new List<string>();
			List<string> propertiesSkipped = new List<string>();
			// get only the public properties of the object instance
      Type type = clientObject.GetType();
      foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				try {
          //if (!clientObject.IsLoaded(property)) // now done in Load<T>
          if (UnsupportedPropertyNames.Contains(property.Name) || property.PropertyType == typeof(ClientRuntimeContext)) {
            propertiesSkipped.Add(property.Name);
            trace.TraceVerbose("Skipped property '{0}' because it is in the known list of unsupport properties. ", property.Name);
          } else if (context.Load<T>(clientObject, property)) {
              propertiesLoaded.Add(property.Name);
              trace.TraceVerbose("Loaded property '{0}'. ", property.Name);
              if (executeQyery == ExecuteQueryFrequency.EveryItem)
                context.ExecuteQuery(); // IfNeeded
          } else {
            propertiesSkipped.Add(property.Name);
            trace.TraceVerbose("Skipped property '{0}' because it has already been loaded. ", property.Name);
          }
        } catch (Exception ex) {
          string addMsg = (ex.InnerException != null) ? "InnerException = " + ex.InnerException.Message : string.Empty;
          trace.TraceWarning("Skipped property '{0}' due to error. Exception = {1} {2}", property.Name, ex.Message, addMsg);
          propertiesSkipped.Add(property.Name);
					if (throwOnFail)
						throw ex;
				}
			}
      if (executeQyery == ExecuteQueryFrequency.Once && propertiesLoaded.Count > 0)
        context.ExecuteQuery(); // IfNeeded
			return propertiesLoaded.ToArray();
		}
		public static string[] LoadProperties<T>(this ClientRuntimeContext context, T clientObject, string[] propertyNames, ExecuteQueryFrequency executeQyery = ExecuteQueryFrequency.Once, bool throwOnFail = false, ITrace trace = null) 
      where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      if (clientObject == null) {
        trace.TraceWarning("Caller passed a null ClientObject of type '{0}'; the developer should learn to write better code. ", typeof(T).FullName);
        return new string[] { };
      }
      List<string> propertiesLoaded = new List<string>();
			List<string> propertiesSkipped = new List<string>();
      Type type = clientObject.GetType();
      foreach (string propertyName in propertyNames) {
				try {
          PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
          //if (!clientObject.IsLoaded(property)) // now done in Load<T>
          if (property == null) {
            propertiesSkipped.Add(propertyName);
            trace.TraceVerbose("Skipped property '{0}' because reflection could not retrieve it. ", propertyName);
          } else if (UnsupportedPropertyNames.Contains(property.Name) || property.PropertyType == typeof(ClientRuntimeContext)) {
            propertiesSkipped.Add(property.Name);
            trace.TraceVerbose("Skipped property '{0}' because it is in the known list of unsupport properties. ", property.Name);
          } else if (context.Load<T>(clientObject, property)) {
              propertiesLoaded.Add(property.Name);
              trace.TraceVerbose("Loaded property '{0}'. ", property.Name);
              if (executeQyery == ExecuteQueryFrequency.EveryItem)
                context.ExecuteQuery();
          } else {
            propertiesSkipped.Add(propertyName);
            trace.TraceVerbose("Skipped property '{0}' because it has already been loaded. ", property.Name);
          }
				} catch (Exception ex) {
          string addMsg = (ex.InnerException != null) ? "InnerException = " + ex.InnerException.Message : string.Empty;
          trace.TraceWarning("Skipped property '{0}' due to error. Exception = {1} {2}", propertyName, ex.Message, addMsg);
          propertiesSkipped.Add(propertyName);
					if (throwOnFail)
						throw ex;
				}
			}
      if (executeQyery == ExecuteQueryFrequency.Once && propertiesLoaded.Count > 0)
        context.ExecuteQuery(); // IfNeeded
			return propertiesLoaded.ToArray();
		}

	}

}
