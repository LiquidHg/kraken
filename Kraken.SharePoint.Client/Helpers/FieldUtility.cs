using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;

namespace Kraken.SharePoint.Client {

  public class FieldUtility {

    public static string Convert(FieldTypeAlias ft) {
      string ftText = Enum.GetName(typeof(FieldTypeAlias), ft);
      Enum t = FieldUtility.ValidateFieldType(ftText, true, false);
      if (t != null)
        return ftText;
      switch (ft) {
        case FieldTypeAlias.Formula:
          return FieldType.Calculated.ToString();
        case FieldTypeAlias.MoneyPenny: //haha - an omage!
          return FieldType.Currency.ToString();
        case FieldTypeAlias.Money:
          return FieldType.Currency.ToString();
        case FieldTypeAlias.Percent:
          return FieldType.Number.ToString();
        case FieldTypeAlias.MultilineText:
        case FieldTypeAlias.TextBox:
          return FieldType.Note.ToString();
        case FieldTypeAlias.RichText:
          return FieldType.Note.ToString();
        case FieldTypeAlias.Date:
        case FieldTypeAlias.FriendlyDate:
          return FieldType.DateTime.ToString();
        // TODO Verify that there is no such thing as groups only??
        /*
        case FieldTypeAlias.Group:
          this.Type = FieldType.User.ToString();
          break;
        case FieldTypeAlias.Groups:
          this.Type = FieldTypeExtended.UserMulti.ToString();
          break;
         */
        case FieldTypeAlias.Lookup:
        case FieldTypeAlias.LookupMulti:
          return FieldType.Lookup.ToString();
        case FieldTypeAlias.Person:
          return FieldType.User.ToString();
        case FieldTypeAlias.People:
          return FieldTypeExtended.UserMulti.ToString();
        case FieldTypeAlias.PersonOrGroup:
          return FieldType.User.ToString();
        case FieldTypeAlias.PeopleAndGroups:
          return FieldTypeExtended.UserMulti.ToString();
        case FieldTypeAlias.ChoiceWithOther:
          return FieldType.Choice.ToString();
        case FieldTypeAlias.MultiChoiceWithOther:
          return FieldType.MultiChoice.ToString();
      }
      throw new NotSupportedException("Specified type is invalid.");
    }


    public static bool IsMultiSelectFieldType(string type) {
      if (type.Equals(FieldTypeExtended.TaxonomyFieldTypeMulti.ToString(), StringComparison.InvariantCultureIgnoreCase))
        return true;
      if (type.Equals(FieldTypeExtended.UserMulti.ToString(), StringComparison.InvariantCultureIgnoreCase))
        return true;
      if (type.EndsWith(FieldType.MultiChoice.ToString(), StringComparison.InvariantCultureIgnoreCase))
        return true;
      // TODO implement Lookup with muyltiple option here
      // TODO implement BCS with muyltiple option here
      return false;
    }

    public static bool IsFieldType(string type, Enum testType) {
      if (testType.GetType() == typeof(FieldType))
        return IsFieldType(type, (FieldType)testType);
      if (testType.GetType() == typeof(FieldTypeExtended))
        return IsFieldType(type, (FieldTypeExtended)testType);
      return false;
    }
    public static bool IsFieldType(string type, FieldType testType) {
      return (type.Equals(testType.ToString(), StringComparison.InvariantCultureIgnoreCase));
    }
    public static bool IsFieldType(string type, FieldTypeExtended testType) {
      return (type.Equals(testType.ToString(), StringComparison.InvariantCultureIgnoreCase));
    }

    public static bool IsLookupFieldType(string type) {
      // TODO does User count as a lookup field??
      return (type.StartsWith(FieldType.Lookup.ToString(), StringComparison.InvariantCultureIgnoreCase));
    }
    public static bool IsChoiceFieldType(string type) {
      return (type.EndsWith(FieldType.Choice.ToString(), StringComparison.InvariantCultureIgnoreCase));
    }
    public static bool IsTaxonomyFieldType(string type) {
      return (type.StartsWith(FieldTypeExtended.TaxonomyFieldType.ToString(), StringComparison.InvariantCultureIgnoreCase));
    }

    public static Enum ValidateFieldType(string type, bool allowExtendedTypes = true, bool throwOnInvalid = true) {
      FieldType ft; FieldTypeExtended eft;
#if !DOTNET_V35
      if (Enum.TryParse<FieldType>(type, out ft))
        return ft;
      if (Enum.TryParse<FieldTypeExtended>(type, out eft))
        return eft;
#else
      if (StringTools.EnumTryParse<FieldType>(type, out ft))
        return ft;
      if (StringTools.EnumTryParse<FieldTypeExtended>(type, out eft))
        return eft;
#endif
      if (throwOnInvalid)
        throw new NotSupportedException(string.Format("The field type '{0}' is not a supported field type.", type));
      return null;
    }

    public static void ValidateFieldFormat(string type, string format) {
      // streamline when we know there's nothing to pase
      if (string.IsNullOrEmpty(format)) {
        // TODO when is an empty setting NOT allowed???
        return;
      }
      ChoiceFormatType choiceFormat; UrlFieldFormatType urlFormat;
      DateTimeFormat dtFormat; DateTimeFieldFormatType dt2Format; //DateTimeFieldFriendlyFormatType dtfFormat;
      if (format.Equals("TRUE") || format.Equals("FALSE")) {
        // TODO what field types allow this???
#if !DOTNET_V35
      } else if (Enum.TryParse<ChoiceFormatType>(format, out choiceFormat)) {
#else
      } else if (StringTools.EnumTryParse<ChoiceFormatType>(format, out choiceFormat)) {
#endif
        // covers DropDown | RadioButtons
        if (!FieldUtility.IsFieldType(type, FieldType.Choice))
          throw new NotSupportedException("You should only set this property with this value if you're creating a Choice field. Set the -Type parameter first.");
#if !DOTNET_V35
      } else if (Enum.TryParse<UrlFieldFormatType>(format, out urlFormat)) {
#else
      } else if (StringTools.EnumTryParse<UrlFieldFormatType>(format, out urlFormat)) {
#endif
        // covers HyperLink | Image
        if (!FieldUtility.IsFieldType(type, FieldType.URL))
          throw new NotSupportedException("You should only set this property with this value if you're creating a Choice field. Set the -Type parameter first.");
#if !DOTNET_V35
      } else if (Enum.TryParse<DateTimeFormat>(format, out dtFormat)) {
#else
      } else if (StringTools.EnumTryParse<DateTimeFormat>(format, out dtFormat)) {
#endif
        if (!FieldUtility.IsFieldType(type, FieldType.DateTime))
          throw new NotSupportedException("You should only set this property with this value if you're creating a DateTime field. Set the -Type parameter first.");
        // covers DateOnly | DateTime | TimeOnly | EventList | ISO8601 | MonthDayOnly | MonthYearOnly
        // does not cover ISO8601Basic | ISO8601Gregorian | ISO8601BasicDateOnly
        if (format == "ISO8601Basic" || format == "ISO8601Gregorian" || format == "ISO8601BasicDateOnly") {
          // this are OK, but not listed in any enum; do they have something to do with LongDate
        } else {
#if !DOTNET_V35
          // covers LongDate but we don't know what to do with it
          if (dtFormat == DateTimeFormat.LongDate || dtFormat == DateTimeFormat.UnknownFormat)
            throw new NotSupportedException(string.Format("You should not set this property with the value {0}.", dtFormat));
#endif
        }
#if !DOTNET_V35
      } else if (Enum.TryParse<DateTimeFieldFormatType>(format, out dt2Format)) {
#else
#endif
        // This will never execute because the above enum also covers these values
        // covers DateOnly | DateTime
        if (!FieldUtility.IsFieldType(type, FieldType.DateTime))
          throw new NotSupportedException("You should only set this property with this value if you're creating a DateTime field. Set the -Type parameter first.");
      } /* else if (Enum.TryParse<DateTimeFieldFriendlyFormatType>(format, out dtfFormat)) {
          // covers Disabled | Relative | Unspecified
          if (!FieldUtility.IsFieldType(this.Type, FieldType.DateTime))
            throw new NotSupportedException("You should only set this property with this value if you're creating a DateTime field. Set the -Type parameter first.");
        */
    }

  }

}
