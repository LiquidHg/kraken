
namespace Kraken {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    // thank you nice people at http://geekswithblogs.net/cbreisch/archive/2005/08/04/49123.aspx
    public static class StringEnum<E> {

        public static string Value(E e) {
            EnumStringValueAttribute[] attrs =
            typeof(E).GetField(e.ToString()).GetCustomAttributes(
                typeof(EnumStringValueAttribute), false
            ) as EnumStringValueAttribute[];
            if (attrs.Length > 0)
                return attrs[0].Value;
            else
                return Enum.GetName(e.GetType(), e);
        }

        public static E Parse(string value) {
            return (E)Enum.Parse(typeof(E), value, false);
        }

    }

}
