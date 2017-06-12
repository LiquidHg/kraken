namespace Microsoft.SharePoint.Client {

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Linq.Expressions;

  using Kraken.SharePoint.Client;
  using Kraken.SharePoint.Client.Connections;

  public static class KrakenClientContextExtensions
    {
        public static void ExecuteQueryIfNeeded(this ClientRuntimeContext context)
        {
            if (context.HasPendingRequest)
                context.ExecuteQuery();
        }

        public static bool IsNull(this ClientObject clientObject)
        {
            //check object
            if (clientObject == null)
            {
                //client object is null, so yes, we're null (we can't even check the server object null property)
                return true;
            }
            else if (!clientObject.ServerObjectIsNull.HasValue)
            {
                //server object null property is itself null, so no, we're not null
                return false;
            }
            else
            {
                //server object null check has a value, so that determines if we're null
                return clientObject.ServerObjectIsNull.Value;
            }
        }

        // USE clientObject.EnsureProperty instead
        /*
          public static void Init<T>(this ClientContext clientContext, T clientObject, params Expression<Func<T, object>>[] retrievals) where T : ClientObject
          {
              clientContext.Load(clientObject, retrievals);
              clientContext.ExecuteQuery();
          }

          public static void Init<T>(this ClientRuntimeContext clientContext, T clientObject, params Expression<Func<T, object>>[] retrievals) where T : ClientObject
          {
              clientContext.Load(clientObject, retrievals);
              clientContext.ExecuteQuery();
          }
         */

        public static bool IsSPO(this WebContextManager ctxm)
        {
            var authType = ctxm.Credentials.AuthType;
            return authType.Equals(ClientAuthenticationType.SPOCredentials) || authType.Equals(ClientAuthenticationType.SPOCustomCookie);
        }

        public static bool IsSP2013AndUp(this ClientRuntimeContext context) {
          if (context == null)
            throw new ArgumentNullException("context");
          return ((ClientContext)context).IsSP2013AndUp();
        }
        public static bool IsSP2013AndUp(this ClientContext context) {
          if (context == null)
            throw new ArgumentNullException("context");
          Version ver = null;
          try {
            ver = context.ServerVersion;
          } catch (PropertyOrFieldNotInitializedException) {
            // only do when necessary
            context.ExecuteQuery();
            ver = context.ServerVersion;
          }
          return (ver.Major >= 15);
        }
    }
}
