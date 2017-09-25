namespace Kraken.SharePoint.Client.Caml {

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

  using Microsoft.SharePoint.Client;

  /// <summary>
  /// Provides the properties needed to create a simple
  /// field to value compare operation, to ease creation 
  /// of Caml queries.
  /// </summary>
  [System.Reflection.Obfuscation(Exclude = true)]
  public class CamlFieldToValueMatchOptions : CamlMatchOptions {

    public CamlFieldToValueMatchOptions() {}
    public CamlFieldToValueMatchOptions(Hashtable ht) : this() {
      SetProperties(ht);
    }
    public override bool SetProperty(string propertyName, object value) {
      // remove leading - from powershell-like operators
      if (propertyName.StartsWith("-"))
        propertyName = propertyName.Substring(1);
      // strip match prefix
      if (propertyName.StartsWith(MatchRulePrefix))
        propertyName = propertyName.Substring(MatchRulePrefix.Length);
      // code too cumbersome
      //if (string.Equals(propertyName, "", StringComparison.InvariantCultureIgnoreCase)) {}
      switch (propertyName.ToLower()) {
        /*
        case "fieldname":
          this.FieldName = value.ToString();
          return true;
        case "fieldtype":
          this.FieldType = value.ToString();
          return true;
        case "fieldvalue":
          this.FieldValue = value.ToString();
          return true;
        */
        case "operator":
          if (value is CAML.Operator)
            this.Operator = (CAML.Operator)value;
          else
            this.Operator = ParseOperator(value.ToString());
          return true;
        default:
          return base.SetProperty(propertyName, value);
      }
    }

    public bool HasValidProperties {
      get {
        if (string.IsNullOrEmpty(this.FieldName))
          return false;
        if (this.Operator == CAML.Operator.And
          || this.Operator == CAML.Operator.Or)
          return false;
        if (string.IsNullOrEmpty(this.FieldValue)
          && this.Operator != CAML.Operator.IsNotNull
          && this.Operator != CAML.Operator.IsNull)
          return false;
        if (!string.IsNullOrEmpty(this.FieldValue)
          && (this.Operator == CAML.Operator.IsNotNull
          || this.Operator == CAML.Operator.IsNull))
          return false;
        return true;
      }
    }

    /// <summary>
    /// Attempt to take any of a series of well known
    /// string representations and convert them to CAML 
    /// Operator. Known shorthands include &lt;, &gt;, 
    /// &lt;=, &gt;=, ==, !=, &lt;&gt;, and a few other rare varients.
    /// </summary>
    /// <param name="op"></param>
    public CAML.Operator ParseOperator(string op) {
      // remove leading - from powershell-like operators
      if (op.StartsWith("-"))
        op = op.Substring(1);
      // allow for shorthand conversions
      switch (op) {
        case "<":
          op = "Lt";
          break;
        case ">":
          op = "Gt";
          break;
        case ">=":
        case "=>":
          op = "Geq";
          break;
        case "<=":
        case "=<":
          op = "Leq";
          break;
        case "===":
        case "==":
        case "": // prevents the parser from running qhen its not necessary
          op = "Eq";
          break;
        case "!=":
        case "!==":
        case "<>":
          op = "Neq";
          break;
      }
      CAML.Operator res;
      if (Enum.TryParse<CAML.Operator>(op, out res)) {
        if (res == CAML.Operator.And
          || res == CAML.Operator.Or)
          throw new ArgumentOutOfRangeException("operator", op, "And or Or operators are not valid in this context. ");
        return res;
      } else
        throw new ArgumentOutOfRangeException("operator", op, "Provided value is not a valid CAML operator. Valid operators include: BeginsWith, Contains, Eq, Geq, Gt, IsNotNull, IsNull, Leq, Like, Lt, Neq");
    }

    [System.Reflection.Obfuscation(Exclude = true)]
    public string FieldName { get; set; }

    [System.Reflection.Obfuscation(Exclude = true)]
    public string FieldValue { get; set; }

    [System.Reflection.Obfuscation(Exclude = true)]
    public string FieldType { get; set; } = "Text";
    public CamlFieldValueType? Type {
      get {
        CamlFieldValueType result;
        if (Enum.TryParse<CamlFieldValueType>(this.FieldType, out result))
          return result;
        return null;
      }
      set {
        if (value != null)
          this.FieldType = value.ToString();
      }
    }

    [System.Reflection.Obfuscation(Exclude = true)]
    public CAML.Operator Operator { get; set; } = CAML.Operator.Eq;

    public virtual string ToCamlOp() {
      return CAML.GetOperator(this.Operator, CAML.FieldRef(this.FieldName), CAML.Value(this.FieldType, this.FieldValue));
    }

    public override string ToCamlWhere() {
      return CAML.Where(
        this.Operator,
        this.FieldName,
        this.FieldType,
        this.FieldValue
      );
    }

    /// <summary>
    /// Performs a CAML-like search of values that are
    /// already held in memory.
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public List<ListItem> SimpleMatch(IEnumerable<ListItem> items) {
      List<ListItem> foundItems = new List<ListItem>();
      if (string.IsNullOrEmpty(this.FieldName))
        return foundItems;
      foundItems = (from i in items
                    where i[this.FieldName] != null
                    && SimpleCompare(i[this.FieldName].ToString(), this.FieldValue)
                    select i).ToList();
      return foundItems;
    }

    private bool SimpleCompare(string val1, string val2) {
      switch (this.Operator) {
        // unary ops - ignore val2
        case CAML.Operator.IsNull:
          return string.IsNullOrEmpty(val1);
        case CAML.Operator.IsNotNull:
          return !string.IsNullOrEmpty(val1);
        // binary ops
        case CAML.Operator.Eq:
          return val1 == val2;
        case CAML.Operator.Neq:
          return val1 != val2;
        case CAML.Operator.Contains:
          return val1.Contains(val2);
        case CAML.Operator.BeginsWith:
          return val1.StartsWith(val2);
        default:
          throw new NotImplementedException(string.Format("The specified comparison operator is not implemented in this method. Operator={0}", this.Operator.ToString()));
      }
      // TODO need a better understanding of how this is interpreted in SharePoint
      /*
      case CAML.Operator.Like:
      // TODO comparisons that require knowledge of the data type
      */
      /*
      case CAML.Operator.Leq:
      case CAML.Operator.Lt:
      case CAML.Operator.Geq:
      case CAML.Operator.Gt:
      */
      // TODO these would be difficult, we'd need a more complex type than val1 and val2 can hold
      /*
      case CAML.Operator.And:
      case CAML.Operator.Or:
      */
    }

  }
}
