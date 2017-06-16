
namespace Kraken {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Value to be used in enumeraations as their string equvalent, useful for 
    /// when you want a string value that is different than its variable name.
    /// </summary>
    public class EnumStringValueAttribute : System.Attribute {

        private string _value;

        public EnumStringValueAttribute(string value) {
            _value = value;
        }

        public string Value {
            get { return _value; }
        }

    } // class

} // namespace
