using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.SharePoint.Client;
using System.Linq.Expressions;
using System.Net;

namespace Kraken.SharePoint.Client.Helpers
{
    public static class Utils
    {
        public static string MakeFullUrl(ClientContext context, string serverRelativeUrl)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (serverRelativeUrl == null)
            {
                throw new ArgumentNullException("serverRelativeUrl");
            }
            if (!serverRelativeUrl.StartsWith("/"))
            {
                throw new ArgumentOutOfRangeException("serverRelativeUrl");
            }
            Uri baseUri = new Uri(context.Url);
            baseUri = new Uri(baseUri, serverRelativeUrl);
            return baseUri.AbsoluteUri;
        }

        public static string CombineUrl(string baseUrlPath, string additionalNodes)
        {
            if (baseUrlPath == null)
            {
                throw new ArgumentNullException("baseUrlPath");
            }
            if (baseUrlPath.Length <= 0)
            {
                return additionalNodes;
            }
            if (additionalNodes == null)
            {
                throw new ArgumentNullException("additionalNodes");
            }
            if (additionalNodes.Length <= 0)
            {
                return baseUrlPath;
            }
            bool flag = baseUrlPath.EndsWith("/");
            bool flag2 = additionalNodes.StartsWith("/");
            if (flag && flag2)
            {
                return baseUrlPath + additionalNodes.Substring(1);
            }
            if ((!flag && flag2) || (flag && !flag2))
            {
                return baseUrlPath + additionalNodes;
            }
            return baseUrlPath + "/" + additionalNodes;
        }

        public static bool TryResolveClientContext(Uri requestUri, out ClientContext context, ICredentials credentials)
        {
            context = null;
            var baseUrl = requestUri.GetLeftPart(UriPartial.Authority);
            for (int i = requestUri.Segments.Length; i >= 0; i--)
            {
#if !DOTNET_V35
                var path = string.Join(string.Empty, requestUri.Segments.Take(i));
#else
                var path = string.Join(string.Empty, requestUri.Segments.Take(i).ToArray());
#endif
                string url = string.Format("{0}{1}", baseUrl, path);
                try
                {
                    context = new ClientContext(url);
                    if (credentials != null)
                        context.Credentials = credentials;
                    context.ExecuteQuery();
                    return true;
                }
                catch (Exception ex) { }
            }
            return false;
        }

        public static bool EqualsIgnoreCase(this string s1, string s2)
        {
            return s1.Equals(s2, StringComparison.InvariantCultureIgnoreCase);
        }

        public static void UpsertElement<TKey, TValue>(this IDictionary<TKey, TValue> coll, TKey key, TValue value)
        {
            if (coll.ContainsKey(key))
            {
                coll[key] = value;
            }
            else
            {
                coll.Add(key, value);
            }
        }

        public static void UpsertElementList<TKey, TValue>(this IDictionary<TKey, List<TValue>> coll, TKey key, TValue value)
        {
            List<TValue> v;
            if (coll.TryGetValue(key, out v))
            {
                v.Add(value);
            }
            else
            {
                coll.Add(key, new List<TValue> { value });
            }
        }

        public static void AddRange<T, S>(this Dictionary<T, S> source, Dictionary<T, S> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Collection is null");
            }

            foreach (var item in collection)
            {
                if (!source.ContainsKey(item.Key))
                {
                    source.Add(item.Key, item.Value);
                }
                else
                {
                    // handle duplicate key issue here
                }
            }
        }

        public static T ParseValue<T>(object value, T defaultValue)
        {
            if (value == null)
                return defaultValue;

            var t = typeof(T);
            if (t.IsEnum)
            {
                try
                {
                    return (T)Enum.Parse(typeof(T), value.ToString(), true);
                }
                catch
                {
                    return defaultValue;
                }
            }

            if (!(value is IConvertible))
                return (T)value;

            try
            {
                return (T)Convert.ChangeType(value, t);
            }
            catch (Exception ex)
            {
                return defaultValue;
            }
        }

        public static object ParseValue(object value, Type type)
        {
            if (value == null)
                return GetDefault(type);

            if (type.IsEnum)
            {
                try
                {
                    return Enum.Parse(type, value.ToString(), true);
                }
                catch
                {
                    return GetDefault(type);
                }
            }

            if (!(value is IConvertible))
                return value;

            try
            {
                return Convert.ChangeType(value, type);
            }
            catch (Exception ex)
            {
                return GetDefault(type);
            }
        }

        public static object GetDefault(Type type)
        {
            object output = null;

            if (type.IsValueType)
            {
                output = Activator.CreateInstance(type);
            }

            return output;
        }

        public static string GetArrayAsString(IEnumerable<object> objects, string del = "|")
        {
            if (objects == null)
                return null;
            try
            {
                string[] array = objects.Cast<string>().ToArray();
                return string.Join(del, array);
            }
            catch (Exception)
            {

            }
            return null;
        }

        public static object GetArrayFromString(string value, string del = "|")
        {
            if (string.IsNullOrEmpty(value))
                return null;
            if (value.Contains(del))
            {
                return value.Split(del.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            return value;
        }
    }




}
