using System.Collections.Generic;

namespace Kraken.SharePoint.Client.Helpers.FPRPC
{
    public class DocumentPropertyCollection :
        Dictionary<string, DocumentProperty>
    {
        public void Add(DocumentProperty prop)
        {
            if (!ContainsKey(prop.PropertyName))
            {
                Add(prop.PropertyName, prop);
            }
            else if (prop.PropertyAccess == DocumentProperty.PropertyAccessLevel.ReadWrite)
            {
                this[prop.PropertyName] = prop;
            }
        }

        public void Add(DocumentPropertyCollection coll)
        {
            foreach (DocumentProperty prop in coll.Values)
            {
                Add(prop);
            }
        }
    }
}