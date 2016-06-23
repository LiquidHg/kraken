using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client {
  public enum FieldTypeAlias {
    InvalidFieldType,
    Boolean,
    Calculated,
    Currency,
    Money,
    MoneyPenny, // "I love you, Mr. Bond!"
    DateTime,
    Date,
    FriendlyDate,
    Choice,
    MultiChoice,
    Note,
    Percent,
    Number,
    Decimal,
    Lookup,
    LookupMulti,
    URL,
    Text,
    TextBox,
    MultilineText,
    RichText,
    Computed,
    User,
    UserMulti,
    PageSeparator,
    TaxonomyFieldType,
    TaxonomyFieldTypeMulti,
    Group,
    Groups,
    Person,
    People,
    PeopleAndGroups,
    PersonOrGroup,
    ChoiceWithOther,
    MultiChoiceWithOther,
    Formula
  }
}
