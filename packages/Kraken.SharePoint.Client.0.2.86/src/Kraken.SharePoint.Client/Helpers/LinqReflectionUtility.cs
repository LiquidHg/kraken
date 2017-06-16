using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kraken.SharePoint.Client.Helpers {
  public class LinqReflectionUtility {

    // requires object instance, but you can skip specifying T
    public static string GetPropertyName<T>(Expression<Func<T>> exp) {
      return (((MemberExpression)(exp.Body)).Member).Name;
    }

    // requires explicit specification of both object type and property type
    public static string GetPropertyName<TObject, TResult>(Expression<Func<TObject, TResult>> exp) {
      // extract property name
      return (((MemberExpression)(exp.Body)).Member).Name;
    }

    // requires explicit specification of object type
    public static string GetPropertyName<TObject>(Expression<Func<TObject, object>> exp) {
      var body = exp.Body;
      var convertExpression = body as UnaryExpression;
      if (convertExpression != null) {
        if (convertExpression.NodeType != ExpressionType.Convert) {
          throw new ArgumentException("Invalid property expression.", "exp");
        }
        body = convertExpression.Operand;
      }
      return ((MemberExpression)body).Member.Name;
    }
  }
}
