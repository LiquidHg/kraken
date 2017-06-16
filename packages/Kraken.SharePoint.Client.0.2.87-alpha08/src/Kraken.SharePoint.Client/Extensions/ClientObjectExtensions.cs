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

    // There is something like this in OfficeDevPnp called EnsureProperties. Ours is better! ;-)
    // How so? chaining, tracing, and query optimizations, for starters!
    #region EnsureProperty

    /// <summary>
    /// Load one or more properties of a client object and call execute query
    /// so we can use them, but only if a call to the server is really needed. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="clientObject"></param>
    /// <param name="trace"></param>
    /// <param name="propertyName"></param>
    /// <param name="loadedProperties">An array of properties that were actually loaded</param>
    /// <returns></returns>
    public static T EnsureProperty<T>(this T clientObject, ITrace trace, string propertyName, out string[] loadedProperties) where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      return clientObject.EnsureProperty(trace, new string[] { propertyName }, out loadedProperties);
    }
    /// <summary>
    /// Load one or more properties of a client object and call execute query
    /// so we can use them, but only if a call to the server is really needed. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="clientObject"></param>
    /// <param name="propertyName"></param>
    /// <param name="trace"></param>
    /// <returns></returns>
    public static T EnsureProperty<T>(this T clientObject, ITrace trace, string propertyName) where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      string[] loadedProperties; // ve vill bury you! ;-)
      return clientObject.EnsureProperty(trace, propertyName, out loadedProperties);
    }

    /// <summary>
    /// Load one or more properties of a client object and call execute query
    /// so we can use them, but only if a call to the server is really needed. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="clientObject"></param>
    /// <param name="trace"></param>
    /// <param name="propertyNames"></param>
    /// <param name="loadedProperties">An array of properties that were actually loaded</param>
    /// <returns></returns>
    public static T EnsureProperty<T>(this T clientObject, ITrace trace, string[] propertyNames, out string[] loadedProperties) where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)clientObject.Context;
      loadedProperties = context.LoadIfRequired(clientObject, propertyNames/* , ExecuteQueryFrequency.Once */, false, trace);
      if (loadedProperties.Length > 0)
        context.ExecuteQueryIfNeeded();
      return clientObject;
    }

    /// <summary>
    /// Load one or more properties of a client object and call execute query
    /// so we can use them, but only if a call to the server is really needed. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="clientObject"></param>
    /// <param name="trace"></param>
    /// <param name="propertyNames"></param>
    /// <returns></returns>
    public static T EnsureProperty<T>(this T clientObject, ITrace trace, string[] propertyNames) where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      string[] loadedProperties; // ve vill bury you! ;-)
      return clientObject.EnsureProperty(trace, propertyNames, out loadedProperties);
    }
    /// <summary>
    /// Provides so you don't *have* to provide a trace parameter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="clientObject"></param>
    /// <param name="propertyNames"></param>
    /// <returns></returns>
    public static T EnsureProperty<T>(this T clientObject, string[] propertyNames) where T : ClientObject {
      return clientObject.EnsureProperty(NullTrace.Default, propertyNames);
    }

    /// <summary>
    /// Load one or more properties of a client object and call execute query
    /// so we can use them, but only if a call to the server is really needed. 
    /// If no properties are passed, this function does nothing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="clientObject"></param>
    /// <param name="trace"></param>
    /// <param name="force">If true, will call Load and Execute Query, event when no properties are assigned</param>
    /// <param name="propertyExpressions"></param>
    /// <returns></returns>
    public static T EnsureProperty<T>(this T clientObject, ITrace trace, params Expression<Func<T, object>>[] propertyExpressions) where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)clientObject.Context;
      if (propertyExpressions == null || propertyExpressions.Length == 0) {
        // not really sure it makes sense to be ensuring an item with no properties
        // this is now handled by overload, if you really want to "force the issue"
      } else {
        context.LoadIfRequired(clientObject, trace, false, propertyExpressions);
        context.ExecuteQueryIfNeeded();
      }
      return clientObject;
    }

    /// <summary>
    /// Provides so you don't *have* to provide a trace parameter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="clientObject"></param>
    /// <param name="propertyExpressions"></param>
    /// <returns></returns>
    public static T EnsureProperty<T>(this T clientObject, params Expression<Func<T, object>>[] propertyExpressions) where T : ClientObject {
      return clientObject.EnsureProperty(NullTrace.Default, propertyExpressions);
    }

    /// <summary>
    /// Load one or more properties of a client object and call execute query
    /// so we can use them, but only if a call to the server is really needed. 
    /// If no properties are passed, this function does nothing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="clientObject"></param>
    /// <param name="trace"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    public static T EnsureProperty<T>(this T clientObject, ITrace trace, bool force) where T : ClientObject {
      if (trace == null) trace = NullTrace.Default;
      ClientContext context = (ClientContext)clientObject.Context;
      if (force) {
        trace.TraceVerbose("Forced load of client object");
        // TODO is there a way to optimize this???
        context.Load(clientObject);
        context.ExecuteQueryIfNeeded();
      }
      return clientObject;
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


  } // class

}
