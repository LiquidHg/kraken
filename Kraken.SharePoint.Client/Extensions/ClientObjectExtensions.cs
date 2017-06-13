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

    #region EnsureProperty

    // There is something like this in OfficeDevPnp.
    // Ours is better! ;-)

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
      return clientObject.EnsureProperty(trace, new string[] { propertyName });
    }
    public static string[] EnsureProperty<T>(this T clientObject, ITrace trace, string[] propertyNames) where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)clientObject.Context;
      string[] propsLoaded = context.LoadIfRequired(clientObject, propertyNames/* , ExecuteQueryFrequency.Once */, false, trace);
      context.ExecuteQueryIfNeeded();
      return propsLoaded;
    }
    public static void EnsureProperty<T>(this T clientObject, ITrace trace, params Expression<Func<T, object>>[] propertyExpressions) where T : ClientObject {
      ClientContext context = (ClientContext)clientObject.Context;
      context.LoadIfRequired(clientObject, trace, false, propertyExpressions);
      context.ExecuteQueryIfNeeded();
    }

    #endregion

    #region IsLoaded

    /// <summary>
    /// Determines whether a given client object property has been loaded.
    /// Usage: Syntax similar to Load e.g. context.IsLoaded(object, o => o.Property);
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="clientObject"></param>
    /// <param name="propertyExpr"></param>
    /// <remarks>
    /// This is a bit more robust than the extension method described in 
    /// https://stackoverflow.com/questions/25680021/proper-way-to-detect-if-a-clientobject-property-is-already-retrieved-initialized
    /// </remarks>
    /// <returns>Return true is the property is available or instantiated.</returns>
    public static bool IsLoaded<T>(this T clientObject, Expression<Func<T, object>> propertyExpr) where T : ClientObject {
      if (clientObject == null)
        return false;
      var expression = propertyExpr.Body;
      Type propertyType = propertyExpr.Body.Type;
      // here we hope that we get a member, but if it is wrapped in Convert we'll get an error instead
      /*
      if (propertyExpr as LambdaExpression != null) {
        expression = (MemberExpression)propertyExpr.Body;
			} else if (propertyExpr as MemberExpression != null) {
				expression = (MemberExpression)propertyExpr.Body;
			}
      */
      string propertyName = string.Empty;
      if (expression is LambdaExpression) {
        expression = ((LambdaExpression)expression).Body;
      }
      if (expression is UnaryExpression) {
        expression = ((UnaryExpression)expression).Operand;
      }
      if (expression is MemberExpression) {
        propertyName = ((MemberExpression)expression).Member.Name;
      } else {
        throw new Exception(string.Format("Didn't know how to handle unpacked Expression of type {0}", expression.GetType().Name));
      }
			return clientObject.IsLoaded(propertyName, propertyType);
		}

    /// <summary>
    /// Return true only if all provided properties
    /// are loaded and available for use.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="clientObject"></param>
    /// <param name="properties"></param>
    /// <returns></returns>
    public static bool IsLoaded<T>(this T clientObject, params Expression<Func<T, object>>[] propertyExpressions) where T : ClientObject {
      bool allLoaded = true;
      foreach (var property in propertyExpressions) {
        allLoaded &= clientObject.IsLoaded(property);
      }
      return allLoaded;
    }

    public static bool IsLoaded(this ClientObject clientObject, PropertyInfo property) {
      if (clientObject == null)
        return false;
      string propertyName = property.Name;
			Type propertyType = property.PropertyType;
			return clientObject.IsLoaded(propertyName, propertyType);
		}

		public static bool IsLoaded(this ClientObject clientObject, string propertyName, Type propertyType) {
      if (clientObject == null)
        return false;
			bool isCollection = typeof(ClientObjectCollection).IsAssignableFrom(propertyType);
			return isCollection ? clientObject.IsObjectPropertyInstantiated(propertyName) : clientObject.IsPropertyAvailable(propertyName);
    }

    #endregion

    #region LoadIfRequired

    /// <summary>
    /// Load only those properties which have not been loaded previously
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="clientObject"></param>
    /// <param name="trace"></param>
    /// <param name="allOrNothing"></param>
    /// <param name="propertyExpressions"></param>
    public static void LoadIfRequired<T>(this ClientRuntimeContext context, T clientObject, ITrace trace, bool allOrNothing, params Expression<Func<T, object>>[] propertyExpressions) where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      // fix for value does not fall within the expected range
      if (propertyExpressions == null || propertyExpressions.Count() == 0) {
        // there is not really much point to this, without any properties but...
        //if (!clientObject.IsLoaded()) // doesn't exist
        context.Load(clientObject);
      } else {
        // TODO this could be further optimized to load only the expression not already loaded
        if (allOrNothing) {
          if (!clientObject.IsLoaded(propertyExpressions))
            context.Load(clientObject, propertyExpressions);
        } else {
          List<Expression<Func<T, object>>> filtered = new List<Expression<Func<T, object>>>();
          foreach (var propertyExpr in propertyExpressions) {
            if (!clientObject.IsLoaded(propertyExpr))
              filtered.Add(propertyExpr);
          }
          context.Load(clientObject, filtered.ToArray());
        }
      }
    }

    /// <summary>
    /// This overload uses reflection to call the correct generic Load method
    /// with the correct type. This is useful when generics fail, which they
    /// sometimes will.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="clientObject"></param>
    /// <param name="property"></param>
    /// <param name="force"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    public static bool LoadIfRequired(this ClientRuntimeContext context, ClientObject clientObject, PropertyInfo property, bool force = false, ITrace trace = null) {
      Type genericType = clientObject.GetType();
      MethodInfo method = typeof(KrakenClientObjectExtensions).GetMethod("LoadIfRequired", BindingFlags.Public | BindingFlags.Static); //, null, new Type[] { typeof(PropertyInfo) }, null);
      method = method.MakeGenericMethod(genericType);
      object result = method.Invoke(null, new object[] { context, clientObject, property, force, trace });
      return (bool)result;
    }

    /// <summary>
    /// Calls Load only when it is needed for property that have not been loaded
    /// </summary>
    /// <typeparam name="T">Type of client object</typeparam>
    /// <param name="context">Client context needed to load properties</param>
    /// <param name="clientObject">The client object for which the property will be loaded</param>
    /// <param name="property">The property that will be loaded</param>
    /// <param name="force">Will invoke Load even when IsLoaded is true</param>
    /// <param name="trace"></param>
    /// <returns></returns>
		public static bool LoadIfRequired<T>(this ClientRuntimeContext context, T clientObject, PropertyInfo property, bool force = false, ITrace trace = null) 
      where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      if (clientObject == null)
        return false; // can you load a null?? I don't think so
      if (property == null)
        throw new ArgumentNullException("property");

      // This can happen when the generic type is ClientObject instead of the correect type
      // much easier and more reliable to fix it here than to do it twice below, though the 
      // other method below was working correctly.
      bool genericMatchesClientObject = (typeof(T) == clientObject.GetType());
      if (!genericMatchesClientObject)
        return context.LoadIfRequired((ClientObject)clientObject, property, force, trace);

      Type clientObjectType = clientObject.GetType();
      if (clientObjectType.GetProperty(property.Name) == null) //if (property.DeclaringType != typeof(T))
        throw new ArgumentException(string.Format("Specified property '{0}' must be a member of type '{1}'.", property.Name, clientObjectType.Name), "property");

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

    public static string[] LoadIfRequired<T>(this ClientRuntimeContext context, T clientObject, string[] propertyNames/*, ExecuteQueryFrequency executeQyery = ExecuteQueryFrequency.Once */, bool throwOnFail = false, ITrace trace = null) where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      if (clientObject == null) {
        trace.TraceWarning("Caller passed a null ClientObject of type '{0}'; the developer should learn to write better code. ", typeof(T).FullName);
        return new string[] { };
      }
      List<string> propertiesLoaded = new List<string>();

      string[] propsToLoad = context.GetUnloadedProperties(clientObject, propertyNames, trace, false);

      Type type = clientObject.GetType();
      foreach (string propertyName in propsToLoad) {
        try {
          PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
          if (property != null) {
            if (context.LoadIfRequired<T>(clientObject, property)) {
              propertiesLoaded.Add(property.Name);
            } else {
              // document skipped here
            }
          }
        } catch (Exception ex) {
          string addMsg = (ex.InnerException != null) ? "InnerException = " + ex.InnerException.Message : string.Empty;
          trace.TraceWarning("Skipped property '{0}' due to error. Exception = {1} {2}", propertyName, ex.Message, addMsg);
          if (throwOnFail)
            throw ex;
        }
      }
      // this was commented out and moved into GetUnloadedProperties
      /*
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
        */

      // This was commented out because it should be clear to all
      // callers that LoadIfRequired only does the Load not the ExecuteQuery
      /*
      if (executeQyery == ExecuteQueryFrequency.Once && propertiesLoaded.Count > 0)
        context.ExecuteQuery(); // IfNeeded
      */
      return propertiesLoaded.ToArray();
    }

    #endregion

    /// <summary>
    /// Loads all the public properties of a client object that have not been
    /// loaded previously, and optionally runs context.ExecuteQuery.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="clientObject"></param>
    /// <param name="executeQyery"></param>
    public static string[] LoadAll<T>(this ClientRuntimeContext context, T clientObject/*, ExecuteQueryFrequency executeQyery = ExecuteQueryFrequency.Once */, bool allOrNothing = false, bool throwOnFail = false, ITrace trace = null) 
      where T : ClientObject {

      if (trace == null) trace = NullTrace.Default;
      string[] propertyNames = GetAllPropertyNames(clientObject);
      if (!allOrNothing)
        context.GetUnloadedProperties(clientObject, propertyNames, trace);
      string[] propertiesLoaded = context.LoadIfRequired(clientObject, propertyNames);
      return propertiesLoaded;
      // phased out in favor of more elegant solution
      /*
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
          } else if (context.LoadIfRequired<T>(clientObject, property)) {
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
      */
    }

    /// <summary>
    /// Loop through a list of property names and 
    /// return only those that need to be loaded
    /// </summary>
    /// <returns></returns>
    public static string[] GetUnloadedProperties<T>(this ClientRuntimeContext context, T clientObject, IEnumerable<string> propertyNames, ITrace trace = null, bool superVerbose = false)
      where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      if (clientObject == null) {
        trace.TraceWarning("Caller passed a null ClientObject of type '{0}'; the developer should learn to write better code. ", typeof(T).FullName);
        return new string[] { };
      }
      List<string> propertiesToLoad = new List<string>();
      List<string> propertiesSkipped = new List<string>();
      Type type = clientObject.GetType();
      foreach (string propertyName in propertyNames) {
        try {
          PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
          if (property == null) {
            propertiesSkipped.Add(propertyName);
            if (superVerbose)
              trace.TraceVerbose("Skipped property '{0}' because reflection could not retrieve it. ", propertyName);
          } else if (UnsupportedPropertyNames.Contains(property.Name) || property.PropertyType == typeof(ClientRuntimeContext)) {
            propertiesSkipped.Add(property.Name);
            if (superVerbose)
              trace.TraceVerbose("Skipped property '{0}' because it is in the known list of unsupport properties. ", property.Name);
          } else if (clientObject.IsLoaded(property)) {
            propertiesSkipped.Add(property.Name);
            if (superVerbose)
              trace.TraceVerbose("Skipped property '{0}' because it has already been loaded. ", property.Name);
          } else {
            propertiesToLoad.Add(property.Name);
            if (superVerbose)
              trace.TraceVerbose("Including property '{0}'. ", property.Name);
          }
        } catch (Exception ex) {
          propertiesSkipped.Add(propertyName);
          string addMsg = (ex.InnerException != null) ? "InnerException = " + ex.InnerException.Message : string.Empty;
          trace.TraceWarning("Skipped property '{0}' due to error. Exception = {1} {2}", propertyName, ex.Message, addMsg);
        }
      }
      return propertiesToLoad.ToArray();
    }


    /// <summary>
    /// Support LoadAll by gettting all the property names
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="object"></param>
    /// <returns></returns>
    private static string[] GetAllPropertyNames(object @object) {
      Type type = @object.GetType();
      List<string> allPropertyNames = new List<string>();
      foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
        allPropertyNames.Add(property.Name);
      }
      // TODO is isn't clear if we also need to get properties of the parents???
      return allPropertyNames.ToArray();
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

  } // class

}
