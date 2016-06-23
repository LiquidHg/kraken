using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client {

  public enum FieldType {

    /// <summary>
    /// Not used.
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// Specifies positive or negative integer values. Corresponds to the Integer field type that is specified on the Field element. 
    /// </summary>
    Integer = 1,

    /// <summary>
    /// Specifies a single line of text. Corresponds to the SPFieldText class and to the Text field type that is specified on the Field element.
    /// </summary>
    Text = 2,

    /// <summary>
    /// Specifies a field that can contain multiple lines of text. Corresponds to the SPFieldMultiLineText class and to the Note field type that is specified on the Field element.
    /// </summary>
    Note = 3,

    /// <summary>
    /// Specifies date and time values. Corresponds to the SPFieldDateTime class and to the DateTime field type that is specified on the Field element. 
    /// </summary>
    DateTime = 4,

    /// <summary>
    /// Specifies an integer used for internal ID fields. Corresponds to the Counter field type that is specified on the Field element.
    /// </summary>
    Counter = 5,

    /// <summary>
    /// Specifies a predetermined set of values that can be used to enter data into the field. Corresponds to the SPFieldChoice class and to the Choice field type that is specified on the Field element.
    /// </summary>
    Choice = 6,
    
    /// <summary>
    /// Specifies references to values in other lists. Corresponds to the SPFieldLookup class and to the Lookup field type that is specified on the Field element.
    /// </summary>
    Lookup = 7,
    
    /// <summary>
    /// Specifies Boolean values that are stored in the database as 1 or 0. Corresponds to the SPFieldBoolean class and to the Boolean field type that is specified on the Field element.
    /// </summary>
    Boolean = 8,

    /// <summary>
    /// Specifies floating point numbers. Corresponds to the SPFieldNumber class and to the Number field type that is specified on the Field element. 
    /// </summary>
    Number = 9,

    /// <summary>
    /// Specifies currency values formatted based on a specific locale. Corresponds to the SPFieldCurrency class and to the Currency field type that is specified on the Field element. 
    /// </summary>
    Currency = 10,

    /// <summary>
    /// Specifies hyperlinks. Corresponds to the SPFieldUrl class and to the URL field type that is specified on the Field element. 
    /// </summary>
    URL = 11,

    /// <summary>
    /// Specifies an abstract field type that depends on other fields for its content and definition. Corresponds to the SPFieldComputed class and to the Computed field type that is specified on the Field element.
    /// </summary>
    Computed = 12,
    
    /// <summary>
    /// Specifies a field that is used in the creation and display of threaded Web discussions. Corresponds to the Threading field type that is specified on the Field element.
    /// </summary>
    Threading = 13,

    /// <summary>
    /// Specifies GUIDs. Corresponds to the Guid field type that is specified on the Field element.
    /// </summary>
    Guid = 14,

    /// <summary>
    /// Specifies multiple values per list item. Corresponds to the SPFieldMultiChoice class and to the MultiChoice field type that is specified on the Field element.
    /// </summary>
    MultiChoice = 15, 

    /// <summary>
    /// Specifies a Choice field for a data sheet. Corresponds to the SPFieldRatingScale class and to the GridChoice field type that is specified on the Field element.
    /// </summary>
    GridChoice = 16,
    
    /// <summary>
    /// Specifies calculated values. Corresponds to the SPFieldCalculated class and to the Calculated field type that is specified on the Field element.
    /// </summary>
    Calculated = 17,

    /// <summary>
    /// Specifies files. Corresponds to the SPFieldFile class and to the File field type that is specified on the Field element.
    /// </summary>
    File = 18,

    /// <summary>
    /// Specifies attachments. Corresponds to the SPFieldAttachments class and to the Attachments field type that is specified on the Field element. 
    /// </summary>
    Attachments = 19,

    /// <summary>
    /// Specifies users of a SharePoint site. Corresponds to the SPFieldUser class and to the User field type that is specified on the Field element.
    /// </summary>
    User = 20,
 Recurrence Specifies a field that is used in calendars for recurring events and abstract field type that, like computed fields, depends on other fields for its content and definition. Corresponds to the SPFieldRecurrence class and to the Recurrence field type that is specified on the Field element. Value = 21. 
 CrossProjectLink Specifies a link between projects in a Meetings Workspace site. Corresponds to the SPFieldCrossProjectLink class and to the CrossProjectLink field type that is specified on the Field element. Value = 22. 
 ModStat Specifies Content Approval status. Corresponds to the SPFieldModStat class and to the ModStat field type that is specified on the Field element. Value = 23. 
 Error Specifies errors. Value = 24. 
 ContentTypeId Specifies a content type ID. Corresponds to the ContentTypeId field type that is specified on the Field element. Value = 25. 
 PageSeparator Inserts a page break in a survey list. Corresponds to the SPFieldPageSeparator class and to the PageSeparator field type that is specified on the Field element. Value = 26. 
 ThreadIndex Specifies the ID that indicates the relative position of a message within a conversation thread. Corresponds to the ThreadIndex field type that is specified on the Field element. Value = 27. 
 WorkflowStatus Specifies workflow status. Corresponds to the SPFieldWorkflowStatus class and to the WorkflowStatus field type that is specified on the Field element. Value = 28. 
 AllDayEvent Specifies an all day event. Corresponds to the SPFieldAllDayEvent class and to the AllDayEvent field type that is specified on the Field element. Value = 29. 
 WorkflowEventType Specifies a workflow event type. Corresponds to the WorkflowEventType field type that is specified on the Field element. Value = 30. 
 Geolocation  
 OutcomeChoice  
 MaxItems Specifies the maximum number of items. Value = 31. 
  
  }
}
