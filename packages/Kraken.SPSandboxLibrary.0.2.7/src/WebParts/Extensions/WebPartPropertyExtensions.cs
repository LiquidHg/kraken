namespace Kraken.SharePoint.WebParts {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Reflection;
  using System.Web.UI;
  using System.Web.UI.WebControls;

  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Utilities;
  using Microsoft.SharePoint.WebPartPages;

  using aspwp = System.Web.UI.WebControls.WebParts;
  using spwp = Microsoft.SharePoint.WebPartPages;

  /// <summary>
  /// This class includes extension methods useful for linking web parts to the tool pane.
  /// </summary>
  public static class WebPartPropertyExtensions {

    /// <summary> 
    /// Sets a property value of an object by reflection. 
    /// </summary> 
    /// <param name="target">Target object.</param> 
    /// <param name="name">Name of the property.</param> 
    /// <param name="value">The value to set.</param> 
    /// <returns>The property value.</returns> 
    public static void SetWebPartProperty(this aspwp.WebPart target, string name, object value) {
      try {
        System.Reflection.PropertyInfo pi = target.GetType().GetProperty(name);
        pi.SetValue(target, value, null);
      } catch (Exception ex) {
        throw new Exception(string.Format("Could not set value of web part property '{0}'.", name), ex);
      }
    }

    public static object GetWebPartProperty(this aspwp.WebPart target, string name) {
      try {
        System.Reflection.PropertyInfo pi = target.GetType().GetProperty(name);
        object value = pi.GetValue(target, null);
        return value;
      } catch (Exception ex) {
        throw new Exception(string.Format("Could not get value of web part property '{0}'.", name), ex);
      }
    }

    public static Type GetWebPartPropertyType(this aspwp.WebPart target, string name) {
      try {
        System.Reflection.PropertyInfo pi = target.GetType().GetProperty(name);
        return pi.PropertyType;
      } catch (Exception ex) {
        throw new Exception(string.Format("Could not determine type of web part property '{0}'. Exception was: {1}", name, ex.Message), ex);
      }
    }


    /// <summary> 
    /// Sets a property value of an object by reflection. 
    /// </summary> 
    /// <param name="target">Target object.</param> 
    /// <param name="name">Name of the property.</param> 
    /// <param name="value">The value to set.</param> 
    /// <returns>The property value.</returns> 
    public static void SetWebPartProperty(this spwp.WebPart target, string name, object value) {
      try {
        System.Reflection.PropertyInfo pi = target.GetType().GetProperty(name);
        pi.SetValue(target, value, null);
      } catch (Exception ex) {
        throw new Exception(string.Format("Could not set value of web part property '{0}'.", name), ex);
      }
    }

    public static object GetWebPartProperty(this spwp.WebPart target, string name) {
      try {
        System.Reflection.PropertyInfo pi = target.GetType().GetProperty(name);
        object value = pi.GetValue(target, null);
        return value;
      } catch (Exception ex) {
        throw new Exception(string.Format("Could not get value of web part property '{0}'.", name), ex);
      }
    }

    public static Type GetWebPartPropertyType(this spwp.WebPart target, string name) {
      try {
        System.Reflection.PropertyInfo pi = target.GetType().GetProperty(name);
        return pi.PropertyType;
      } catch (Exception ex) {
        throw new Exception(string.Format("Could not determine type of web part property '{0}'. Exception was: {1}", name, ex.Message), ex);
      }
    }

  }

}
