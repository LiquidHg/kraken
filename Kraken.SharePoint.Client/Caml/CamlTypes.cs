using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Client.Caml {

  /// <summary>
  /// An enumeration of value types for CAML.
  /// <seealso cref="http://blog.halan.se/post/Undocumented-CAML-Field-Element-types.aspx"/>
  /// <seealso cref="https://msdn.microsoft.com/en-us/library/ms437580.aspx"/>
  /// </summary>
  public enum CamlFieldValueType {

    /// <summary>
    /// Specifies an all day event. 
    /// </summary>
    AllDayEvent,

    /// <summary>
    /// Contains attachments. 
    /// </summary>
    Attachments,

    /// <summary>
    /// Contains Boolean values that are stored in the database as 1 or 0. 
    /// </summary>
    Boolean,

    /// <summary>
    /// Contains calculated values. 
    /// </summary>
    Calculated,

    /// <summary>
    /// Specifies a predetermined set of values that can be used to enter data into the field. 
    /// Known to work well.
    /// </summary>
    Choice,

    /// <summary>
    /// Specifies an abstract field type that depends on other fields for its content and definition.
    /// </summary>
    Computed,

    /// <summary>
    /// Contains a content type ID.
    /// </summary>
    ContentTypeId,

    /// <summary>
    /// Contains an integer used for internal ID fields.
    /// </summary>
    Counter,

    /// <summary>
    /// Specifies a link between projects in a Meetings Workspace site. 
    /// </summary>
    CrossProjectLink,

    /// <summary>
    /// Contains currency values formatted based on a specific locale. 
    /// </summary>
    Currency,

    /// <summary>
    /// Contains date value without time. 
    /// </summary>
    Date,

    /// <summary>
    /// Contains date and time values. 
    /// Known to work well.
    /// </summary>
    DateTime,

    /// <summary>
    /// Contains errors.
    /// </summary>
    Error,

    /// <summary>
    /// Contains files.
    /// </summary>
    File,

    /// <summary>
    /// Specifies a Choice field for a data sheet.
    /// </summary>
    GridChoice,

    /// <summary>
    /// Contains GUIDs.
    /// </summary>
    Guid,

    /// <summary>
    /// Contains positive or negative integer values.
    /// Known to work well.
    /// </summary>
    Integer,

    /// <summary>
    /// Contains references to values in other lists. 
    /// Known to work well.
    /// </summary>
    Lookup,

    /// <summary>
    /// Contains references to values in other lists. 
    /// </summary>
    LookupMulti,

    /// <summary>
    /// Contains the maximum number of items.
    /// </summary>
    MaxItems,

    /// <summary>
    /// Specifies Content Approval status.
    /// </summary>
    ModStat,

    /// <summary>
    /// Contains multiple values per list item.
    /// Known to work well.
    /// </summary>
    MultiChoice,

    /// <summary>
    /// A Note field that emulates a field containing multiple values. 
    /// For an example of a multicolumn field type, see Custom Field 
    /// Type Definition. For information on multicolumn fields, see 
    /// Custom Multicolumn Field Classes.
    /// </summary>
    MultiColumn,

    /// <summary>
    /// Specifies a field that can contain multiple lines of text.
    /// </summary>
    Note,

    /// <summary>
    /// Contains floating point numbers.
    /// Known to work well.
    /// </summary>
    Number,

    /// <summary>
    /// Inserts a page break in a survey list.
    /// </summary>
    PageSeparator,

    /// <summary>
    /// Specifies a field used in calendars for recurring events and, like computed fields, an abstract field type that depends on other fields for its content and definition.
    /// </summary>
    Recurrence,

    /// <summary>
    /// Contains a single line of text. 
    /// Known to work well.
    /// </summary>
    Text,

    /// <summary>
    /// Contains the ID that indicates the relative position of a message within a conversation thread. 
    /// </summary>
    ThreadIndex,

    /// <summary>
    /// Specifies a field that is used in the creation and display of threaded Web discussions. 
    /// </summary>
    Threading,

    /// <summary>
    /// Contains hyperlinks.
    /// </summary>
    URL,

    /// <summary>
    /// Specifies users of a SharePoint site.Same as "Person or Group" in STS.
    /// </summary>
    User,

    /// <summary>
    /// A User field that can contain more than one value.
    /// </summary>
    UserMulti,

    /// <summary>
    /// Specifies a workflow event type.
    /// </summary>
    WorkflowEventType,

    /// <summary>
    /// Specifies workflow status. 
    /// </summary>
    WorkflowStatus

  }
}
