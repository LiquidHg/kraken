
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly Interop.DSOFile
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = ( 01 00 07 44 53 4F 46 69 6C 65 00 00 )             // ...DSOFile..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 38 39 36 38 31 34 35 2D 63 66 30 30   // ..$58968145-cf00
                                                                                                  2D 34 33 34 31 2D 39 39 35 66 2D 32 65 65 30 39   // -4341-995f-2ee09
                                                                                                  33 66 36 61 62 61 33 00 00 )                      // 3f6aba3..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibVersionAttribute::.ctor(int32,
                                                                                                int32) = ( 01 00 02 00 00 00 00 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 2:0:0:0
}
.module DSOFile.dll
// MVID: {19D26591-7B4D-4170-8982-CB46D84BA641}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01BD0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi sealed DSOFile.dsoFilePropertyType
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype DSOFile.dsoFilePropertyType dsoPropertyTypeUnknown = int32(0x00000000)
  .field public static literal valuetype DSOFile.dsoFilePropertyType dsoPropertyTypeString = int32(0x00000001)
  .field public static literal valuetype DSOFile.dsoFilePropertyType dsoPropertyTypeLong = int32(0x00000002)
  .field public static literal valuetype DSOFile.dsoFilePropertyType dsoPropertyTypeDouble = int32(0x00000003)
  .field public static literal valuetype DSOFile.dsoFilePropertyType dsoPropertyTypeBool = int32(0x00000004)
  .field public static literal valuetype DSOFile.dsoFilePropertyType dsoPropertyTypeDate = int32(0x00000005)
} // end of class DSOFile.dsoFilePropertyType

.class public auto ansi sealed DSOFile.dsoFileOpenOptions
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype DSOFile.dsoFileOpenOptions dsoOptionDefault = int32(0x00000000)
  .field public static literal valuetype DSOFile.dsoFileOpenOptions dsoOptionOnlyOpenOLEFiles = int32(0x00000001)
  .field public static literal valuetype DSOFile.dsoFileOpenOptions dsoOptionOpenReadOnlyIfNoWriteAccess = int32(0x00000002)
  .field public static literal valuetype DSOFile.dsoFileOpenOptions dsoOptionDontAutoCreate = int32(0x00000004)
  .field public static literal valuetype DSOFile.dsoFileOpenOptions dsoOptionUseMBCStringsForNewSets = int32(0x00000008)
} // end of class DSOFile.dsoFileOpenOptions

.class interface public abstract auto ansi import DSOFile.CustomProperty
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 05 56 61 6C 75 65 00 00 )                   // ...Value..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 38 39 36 38 31 34 35 2D 43 46 30 33   // ..$58968145-CF03
                                                                                                  2D 34 33 34 31 2D 39 39 35 46 2D 32 45 45 30 39   // -4341-995F-2EE09
                                                                                                  33 46 36 41 42 41 33 00 00 )                      // 3F6ABA3..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(int16) = ( 01 00 C0 10 00 00 ) 
  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Name() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 04 00 00 00 ) 
  } // end of method CustomProperty::get_Name

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype DSOFile.dsoFilePropertyType 
          get_Type() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 04 00 00 00 ) 
  } // end of method CustomProperty::get_Type

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( struct) 
          get_Value() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
  } // end of method CustomProperty::get_Value

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Value([in] object&  marshal( struct) pvValue) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
  } // end of method CustomProperty::set_Value

  .method public hidebysig newslot abstract virtual 
          instance void  Remove() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 04 00 00 00 ) 
  } // end of method CustomProperty::Remove

  .property string Name()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 04 00 00 00 ) 
    .get instance string DSOFile.CustomProperty::get_Name()
  } // end of property CustomProperty::Name
  .property valuetype DSOFile.dsoFilePropertyType
          Type()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 04 00 00 00 ) 
    .get instance valuetype DSOFile.dsoFilePropertyType DSOFile.CustomProperty::get_Type()
  } // end of property CustomProperty::Type
  .property object Value()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    .set instance void DSOFile.CustomProperty::set_Value(object&)
    .get instance object DSOFile.CustomProperty::get_Value()
  } // end of property CustomProperty::Value
} // end of class DSOFile.CustomProperty

.class interface public abstract auto ansi import DSOFile.CustomProperties
       implements [mscorlib]System.Collections.IEnumerable
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 38 39 36 38 31 34 35 2D 43 46 30 34   // ..$58968145-CF04
                                                                                                  2D 34 33 34 31 2D 39 39 35 46 2D 32 45 45 30 39   // -4341-995F-2EE09
                                                                                                  33 46 36 41 42 41 33 00 00 )                      // 3F6ABA3..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(int16) = ( 01 00 C0 10 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_Count() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 03 00 00 00 ) 
  } // end of method CustomProperties::get_Count

  .method public hidebysig newslot abstract virtual 
          instance class DSOFile.CustomProperty 
          marshal( interface ) 
          Add([in] string  marshal( bstr) sPropName,
              [in] object&  marshal( struct) Value) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 03 00 00 00 ) 
  } // end of method CustomProperties::Add

  .method public hidebysig newslot specialname abstract virtual 
          instance class DSOFile.CustomProperty 
          marshal( interface ) 
          get_Item([in] object  marshal( struct) Index) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
  } // end of method CustomProperties::get_Item

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerator 
          marshal( custom ("System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler, CustomMarshalers, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a","")) 
          GetEnumerator() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibFuncAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 FC FF FF FF 00 00 ) 
  } // end of method CustomProperties::GetEnumerator

  .property int32 Count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 03 00 00 00 ) 
    .get instance int32 DSOFile.CustomProperties::get_Count()
  } // end of property CustomProperties::Count
  .property class DSOFile.CustomProperty Item(object)
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    .get instance class DSOFile.CustomProperty DSOFile.CustomProperties::get_Item(object)
  } // end of property CustomProperties::Item
} // end of class DSOFile.CustomProperties

.class interface public abstract auto ansi import DSOFile.SummaryProperties
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(int16) = ( 01 00 C0 10 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 38 39 36 38 31 34 35 2D 43 46 30 32   // ..$58968145-CF02
                                                                                                  2D 34 33 34 31 2D 39 39 35 46 2D 32 45 45 30 39   // -4341-995F-2EE09
                                                                                                  33 46 36 41 42 41 33 00 00 )                      // 3F6ABA3..
  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Title() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_Title

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Title([in] string  marshal( bstr) pbstrTitle) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 02 00 00 00 ) 
  } // end of method SummaryProperties::set_Title

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Subject() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_Subject

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Subject([in] string  marshal( bstr) pbstrSubject) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 02 00 00 00 ) 
  } // end of method SummaryProperties::set_Subject

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Author() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_Author

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Author([in] string  marshal( bstr) pbstrAuthor) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 02 00 00 00 ) 
  } // end of method SummaryProperties::set_Author

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Keywords() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_Keywords

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Keywords([in] string  marshal( bstr) pbstrKeywords) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 02 00 00 00 ) 
  } // end of method SummaryProperties::set_Keywords

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Comments() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_Comments

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Comments([in] string  marshal( bstr) pbstrComments) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 02 00 00 00 ) 
  } // end of method SummaryProperties::set_Comments

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Template() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_Template

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_LastSavedBy() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_LastSavedBy

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_LastSavedBy([in] string  marshal( bstr) pbstrLastSavedBy) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 02 00 00 00 ) 
  } // end of method SummaryProperties::set_LastSavedBy

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_RevisionNumber() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_RevisionNumber

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_TotalEditTime() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_TotalEditTime

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( struct) 
          get_DateLastPrinted() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_DateLastPrinted

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( struct) 
          get_DateCreated() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_DateCreated

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( struct) 
          get_DateLastSaved() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_DateLastSaved

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_PageCount() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_PageCount

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_WordCount() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_WordCount

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_CharacterCount() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0F 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_CharacterCount

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( struct) 
          get_Thumbnail() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 10 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_Thumbnail

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_ApplicationName() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 11 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_ApplicationName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_DocumentSecurity() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibFuncAttribute::.ctor(int16) = ( 01 00 40 00 00 00 )                               // ..@...
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 12 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_DocumentSecurity

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Category() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 13 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_Category

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Category([in] string  marshal( bstr) pbstrCategory) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 13 00 02 00 00 00 ) 
  } // end of method SummaryProperties::set_Category

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_PresentationFormat() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 14 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_PresentationFormat

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_ByteCount() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 15 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_ByteCount

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_LineCount() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 16 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_LineCount

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_ParagraphCount() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 17 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_ParagraphCount

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_SlideCount() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 18 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_SlideCount

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_NoteCount() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 19 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_NoteCount

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_HiddenSlideCount() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1A 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_HiddenSlideCount

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_MultimediaClipCount() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1B 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_MultimediaClipCount

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Manager() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1C 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_Manager

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Manager([in] string  marshal( bstr) pbstrManager) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1C 00 02 00 00 00 ) 
  } // end of method SummaryProperties::set_Manager

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Company() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1D 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_Company

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Company([in] string  marshal( bstr) pbstrCompany) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1D 00 02 00 00 00 ) 
  } // end of method SummaryProperties::set_Company

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_CharacterCountWithSpaces() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1E 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_CharacterCountWithSpaces

  .method public hidebysig newslot specialname abstract virtual 
          instance bool  get_SharedDocument() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1F 00 02 00 00 00 ) 
  } // end of method SummaryProperties::get_SharedDocument

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Version() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 20 00 02 00 00 00 )                         // .. .....
  } // end of method SummaryProperties::get_Version

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( struct) 
          get_DigitalSignature() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 21 00 02 00 00 00 )                         // ..!.....
    .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibFuncAttribute::.ctor(int16) = ( 01 00 40 00 00 00 )                               // ..@...
  } // end of method SummaryProperties::get_DigitalSignature

  .property string Title()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_Title()
    .set instance void DSOFile.SummaryProperties::set_Title(string)
  } // end of property SummaryProperties::Title
  .property string Subject()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_Subject()
    .set instance void DSOFile.SummaryProperties::set_Subject(string)
  } // end of property SummaryProperties::Subject
  .property string Author()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_Author()
    .set instance void DSOFile.SummaryProperties::set_Author(string)
  } // end of property SummaryProperties::Author
  .property string Keywords()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_Keywords()
    .set instance void DSOFile.SummaryProperties::set_Keywords(string)
  } // end of property SummaryProperties::Keywords
  .property string Comments()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_Comments()
    .set instance void DSOFile.SummaryProperties::set_Comments(string)
  } // end of property SummaryProperties::Comments
  .property string Template()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_Template()
  } // end of property SummaryProperties::Template
  .property string LastSavedBy()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_LastSavedBy()
    .set instance void DSOFile.SummaryProperties::set_LastSavedBy(string)
  } // end of property SummaryProperties::LastSavedBy
  .property string RevisionNumber()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_RevisionNumber()
  } // end of property SummaryProperties::RevisionNumber
  .property int32 TotalEditTime()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_TotalEditTime()
  } // end of property SummaryProperties::TotalEditTime
  .property object DateLastPrinted()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 02 00 00 00 ) 
    .get instance object DSOFile.SummaryProperties::get_DateLastPrinted()
  } // end of property SummaryProperties::DateLastPrinted
  .property object DateCreated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 02 00 00 00 ) 
    .get instance object DSOFile.SummaryProperties::get_DateCreated()
  } // end of property SummaryProperties::DateCreated
  .property object DateLastSaved()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 02 00 00 00 ) 
    .get instance object DSOFile.SummaryProperties::get_DateLastSaved()
  } // end of property SummaryProperties::DateLastSaved
  .property int32 PageCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_PageCount()
  } // end of property SummaryProperties::PageCount
  .property int32 WordCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_WordCount()
  } // end of property SummaryProperties::WordCount
  .property int32 CharacterCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0F 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_CharacterCount()
  } // end of property SummaryProperties::CharacterCount
  .property object Thumbnail()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 10 00 02 00 00 00 ) 
    .get instance object DSOFile.SummaryProperties::get_Thumbnail()
  } // end of property SummaryProperties::Thumbnail
  .property string ApplicationName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 11 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_ApplicationName()
  } // end of property SummaryProperties::ApplicationName
  .property int32 DocumentSecurity()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 12 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_DocumentSecurity()
  } // end of property SummaryProperties::DocumentSecurity
  .property string Category()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 13 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_Category()
    .set instance void DSOFile.SummaryProperties::set_Category(string)
  } // end of property SummaryProperties::Category
  .property string PresentationFormat()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 14 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_PresentationFormat()
  } // end of property SummaryProperties::PresentationFormat
  .property int32 ByteCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 15 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_ByteCount()
  } // end of property SummaryProperties::ByteCount
  .property int32 LineCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 16 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_LineCount()
  } // end of property SummaryProperties::LineCount
  .property int32 ParagraphCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 17 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_ParagraphCount()
  } // end of property SummaryProperties::ParagraphCount
  .property int32 SlideCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 18 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_SlideCount()
  } // end of property SummaryProperties::SlideCount
  .property int32 NoteCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 19 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_NoteCount()
  } // end of property SummaryProperties::NoteCount
  .property int32 HiddenSlideCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1A 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_HiddenSlideCount()
  } // end of property SummaryProperties::HiddenSlideCount
  .property int32 MultimediaClipCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1B 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_MultimediaClipCount()
  } // end of property SummaryProperties::MultimediaClipCount
  .property string Manager()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1C 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_Manager()
    .set instance void DSOFile.SummaryProperties::set_Manager(string)
  } // end of property SummaryProperties::Manager
  .property string Company()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1D 00 02 00 00 00 ) 
    .get instance string DSOFile.SummaryProperties::get_Company()
    .set instance void DSOFile.SummaryProperties::set_Company(string)
  } // end of property SummaryProperties::Company
  .property int32 CharacterCountWithSpaces()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1E 00 02 00 00 00 ) 
    .get instance int32 DSOFile.SummaryProperties::get_CharacterCountWithSpaces()
  } // end of property SummaryProperties::CharacterCountWithSpaces
  .property bool SharedDocument()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1F 00 02 00 00 00 ) 
    .get instance bool DSOFile.SummaryProperties::get_SharedDocument()
  } // end of property SummaryProperties::SharedDocument
  .property string Version()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 20 00 02 00 00 00 )                         // .. .....
    .get instance string DSOFile.SummaryProperties::get_Version()
  } // end of property SummaryProperties::Version
  .property object DigitalSignature()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 21 00 02 00 00 00 )                         // ..!.....
    .get instance object DSOFile.SummaryProperties::get_DigitalSignature()
  } // end of property SummaryProperties::DigitalSignature
} // end of class DSOFile.SummaryProperties

.class interface public abstract auto ansi import DSOFile._OleDocumentProperties
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 4E 61 6D 65 00 00 )                      // ...Name..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(int16) = ( 01 00 D0 10 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 38 39 36 38 31 34 35 2D 43 46 30 31   // ..$58968145-CF01
                                                                                                  2D 34 33 34 31 2D 39 39 35 46 2D 32 45 45 30 39   // -4341-995F-2EE09
                                                                                                  33 46 36 41 42 41 33 00 00 )                      // 3F6ABA3..
  .method public hidebysig newslot abstract virtual 
          instance void  Open([in] string  marshal( bstr) sFileName,
                              [in][opt] bool ReadOnly,
                              [in][opt] valuetype DSOFile.dsoFileOpenOptions Options) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 01 00 00 00 ) 
    .param [2] = bool(false)
    .param [3] = int32(0x00000000)
  } // end of method _OleDocumentProperties::Open

  .method public hidebysig newslot abstract virtual 
          instance void  Close([in][opt] bool SaveBeforeClose) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 01 00 00 00 ) 
    .param [1] = bool(false)
  } // end of method _OleDocumentProperties::Close

  .method public hidebysig newslot specialname abstract virtual 
          instance bool  get_IsReadOnly() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 01 00 00 00 ) 
  } // end of method _OleDocumentProperties::get_IsReadOnly

  .method public hidebysig newslot specialname abstract virtual 
          instance bool  get_IsDirty() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 01 00 00 00 ) 
  } // end of method _OleDocumentProperties::get_IsDirty

  .method public hidebysig newslot abstract virtual 
          instance void  Save() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 01 00 00 00 ) 
  } // end of method _OleDocumentProperties::Save

  .method public hidebysig newslot specialname abstract virtual 
          instance class DSOFile.SummaryProperties 
          marshal( interface ) 
          get_SummaryProperties() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 01 00 00 00 ) 
  } // end of method _OleDocumentProperties::get_SummaryProperties

  .method public hidebysig newslot specialname abstract virtual 
          instance class DSOFile.CustomProperties 
          marshal( interface ) 
          get_CustomProperties() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 01 00 00 00 ) 
  } // end of method _OleDocumentProperties::get_CustomProperties

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( idispatch ) 
          get_Icon() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 01 00 00 00 ) 
  } // end of method _OleDocumentProperties::get_Icon

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Name() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
  } // end of method _OleDocumentProperties::get_Name

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_Path() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 01 00 00 00 ) 
  } // end of method _OleDocumentProperties::get_Path

  .method public hidebysig newslot specialname abstract virtual 
          instance bool  get_IsOleFile() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 01 00 00 00 ) 
  } // end of method _OleDocumentProperties::get_IsOleFile

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_CLSID() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 01 00 00 00 ) 
  } // end of method _OleDocumentProperties::get_CLSID

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_ProgID() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 01 00 00 00 ) 
  } // end of method _OleDocumentProperties::get_ProgID

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_OleDocumentFormat() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 01 00 00 00 ) 
  } // end of method _OleDocumentProperties::get_OleDocumentFormat

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_OleDocumentType() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 01 00 00 00 ) 
  } // end of method _OleDocumentProperties::get_OleDocumentType

  .property bool IsReadOnly()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 01 00 00 00 ) 
    .get instance bool DSOFile._OleDocumentProperties::get_IsReadOnly()
  } // end of property _OleDocumentProperties::IsReadOnly
  .property bool IsDirty()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 01 00 00 00 ) 
    .get instance bool DSOFile._OleDocumentProperties::get_IsDirty()
  } // end of property _OleDocumentProperties::IsDirty
  .property class DSOFile.SummaryProperties
          SummaryProperties()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 01 00 00 00 ) 
    .get instance class DSOFile.SummaryProperties DSOFile._OleDocumentProperties::get_SummaryProperties()
  } // end of property _OleDocumentProperties::SummaryProperties
  .property class DSOFile.CustomProperties
          CustomProperties()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 01 00 00 00 ) 
    .get instance class DSOFile.CustomProperties DSOFile._OleDocumentProperties::get_CustomProperties()
  } // end of property _OleDocumentProperties::CustomProperties
  .property object Icon()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 01 00 00 00 ) 
    .get instance object DSOFile._OleDocumentProperties::get_Icon()
  } // end of property _OleDocumentProperties::Icon
  .property string Name()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    .get instance string DSOFile._OleDocumentProperties::get_Name()
  } // end of property _OleDocumentProperties::Name
  .property string Path()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 01 00 00 00 ) 
    .get instance string DSOFile._OleDocumentProperties::get_Path()
  } // end of property _OleDocumentProperties::Path
  .property bool IsOleFile()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 01 00 00 00 ) 
    .get instance bool DSOFile._OleDocumentProperties::get_IsOleFile()
  } // end of property _OleDocumentProperties::IsOleFile
  .property string CLSID()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 01 00 00 00 ) 
    .get instance string DSOFile._OleDocumentProperties::get_CLSID()
  } // end of property _OleDocumentProperties::CLSID
  .property string ProgID()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 01 00 00 00 ) 
    .get instance string DSOFile._OleDocumentProperties::get_ProgID()
  } // end of property _OleDocumentProperties::ProgID
  .property string OleDocumentFormat()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 01 00 00 00 ) 
    .get instance string DSOFile._OleDocumentProperties::get_OleDocumentFormat()
  } // end of property _OleDocumentProperties::OleDocumentFormat
  .property string OleDocumentType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 01 00 00 00 ) 
    .get instance string DSOFile._OleDocumentProperties::get_OleDocumentType()
  } // end of property _OleDocumentProperties::OleDocumentType
} // end of class DSOFile._OleDocumentProperties

.class public auto ansi import DSOFile.OleDocumentPropertiesClass
       extends [mscorlib]System.Object
       implements DSOFile._OleDocumentProperties,
                  DSOFile.OleDocumentProperties
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 4E 61 6D 65 00 00 )                      // ...Name..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 38 39 36 38 31 34 35 2D 43 46 30 35   // ..$58968145-CF05
                                                                                                  2D 34 33 34 31 2D 39 39 35 46 2D 32 45 45 30 39   // -4341-995F-2EE09
                                                                                                  33 46 36 41 42 41 33 00 00 )                      // 3F6ABA3..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(int16) = ( 01 00 02 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ClassInterfaceAttribute::.ctor(int16) = ( 01 00 00 00 00 00 ) 
  .method public specialname rtspecialname 
          instance void  .ctor() runtime managed internalcall
  {
  } // end of method OleDocumentPropertiesClass::.ctor

  .method public hidebysig newslot virtual 
          instance void  Open([in] string  marshal( bstr) sFileName,
                              [in][opt] bool ReadOnly,
                              [in][opt] valuetype DSOFile.dsoFileOpenOptions Options) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 01 00 00 00 ) 
    .param [2] = bool(false)
    .param [3] = int32(0x00000000)
    .override DSOFile._OleDocumentProperties::Open
  } // end of method OleDocumentPropertiesClass::Open

  .method public hidebysig newslot virtual 
          instance void  Close([in][opt] bool SaveBeforeClose) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 01 00 00 00 ) 
    .param [1] = bool(false)
    .override DSOFile._OleDocumentProperties::Close
  } // end of method OleDocumentPropertiesClass::Close

  .method public hidebysig newslot specialname virtual 
          instance bool  get_IsReadOnly() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 01 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::get_IsReadOnly
  } // end of method OleDocumentPropertiesClass::get_IsReadOnly

  .method public hidebysig newslot specialname virtual 
          instance bool  get_IsDirty() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 01 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::get_IsDirty
  } // end of method OleDocumentPropertiesClass::get_IsDirty

  .method public hidebysig newslot virtual 
          instance void  Save() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 01 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::Save
  } // end of method OleDocumentPropertiesClass::Save

  .method public hidebysig newslot specialname virtual 
          instance class DSOFile.SummaryProperties 
          marshal( interface ) 
          get_SummaryProperties() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 01 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::get_SummaryProperties
  } // end of method OleDocumentPropertiesClass::get_SummaryProperties

  .method public hidebysig newslot specialname virtual 
          instance class DSOFile.CustomProperties 
          marshal( interface ) 
          get_CustomProperties() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 01 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::get_CustomProperties
  } // end of method OleDocumentPropertiesClass::get_CustomProperties

  .method public hidebysig newslot specialname virtual 
          instance object 
          marshal( idispatch ) 
          get_Icon() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 01 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::get_Icon
  } // end of method OleDocumentPropertiesClass::get_Icon

  .method public hidebysig newslot specialname virtual 
          instance string 
          marshal( bstr) 
          get_Name() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::get_Name
  } // end of method OleDocumentPropertiesClass::get_Name

  .method public hidebysig newslot specialname virtual 
          instance string 
          marshal( bstr) 
          get_Path() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 01 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::get_Path
  } // end of method OleDocumentPropertiesClass::get_Path

  .method public hidebysig newslot specialname virtual 
          instance bool  get_IsOleFile() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 01 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::get_IsOleFile
  } // end of method OleDocumentPropertiesClass::get_IsOleFile

  .method public hidebysig newslot specialname virtual 
          instance string 
          marshal( bstr) 
          get_CLSID() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 01 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::get_CLSID
  } // end of method OleDocumentPropertiesClass::get_CLSID

  .method public hidebysig newslot specialname virtual 
          instance string 
          marshal( bstr) 
          get_ProgID() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 01 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::get_ProgID
  } // end of method OleDocumentPropertiesClass::get_ProgID

  .method public hidebysig newslot specialname virtual 
          instance string 
          marshal( bstr) 
          get_OleDocumentFormat() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 01 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::get_OleDocumentFormat
  } // end of method OleDocumentPropertiesClass::get_OleDocumentFormat

  .method public hidebysig newslot specialname virtual 
          instance string 
          marshal( bstr) 
          get_OleDocumentType() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 01 00 00 00 ) 
    .override DSOFile._OleDocumentProperties::get_OleDocumentType
  } // end of method OleDocumentPropertiesClass::get_OleDocumentType

  .property bool IsReadOnly()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 01 00 00 00 ) 
    .get instance bool DSOFile.OleDocumentPropertiesClass::get_IsReadOnly()
  } // end of property OleDocumentPropertiesClass::IsReadOnly
  .property bool IsDirty()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 01 00 00 00 ) 
    .get instance bool DSOFile.OleDocumentPropertiesClass::get_IsDirty()
  } // end of property OleDocumentPropertiesClass::IsDirty
  .property class DSOFile.SummaryProperties
          SummaryProperties()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 01 00 00 00 ) 
    .get instance class DSOFile.SummaryProperties DSOFile.OleDocumentPropertiesClass::get_SummaryProperties()
  } // end of property OleDocumentPropertiesClass::SummaryProperties
  .property class DSOFile.CustomProperties
          CustomProperties()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 01 00 00 00 ) 
    .get instance class DSOFile.CustomProperties DSOFile.OleDocumentPropertiesClass::get_CustomProperties()
  } // end of property OleDocumentPropertiesClass::CustomProperties
  .property object Icon()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 01 00 00 00 ) 
    .get instance object DSOFile.OleDocumentPropertiesClass::get_Icon()
  } // end of property OleDocumentPropertiesClass::Icon
  .property string Name()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    .get instance string DSOFile.OleDocumentPropertiesClass::get_Name()
  } // end of property OleDocumentPropertiesClass::Name
  .property string Path()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 01 00 00 00 ) 
    .get instance string DSOFile.OleDocumentPropertiesClass::get_Path()
  } // end of property OleDocumentPropertiesClass::Path
  .property bool IsOleFile()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 01 00 00 00 ) 
    .get instance bool DSOFile.OleDocumentPropertiesClass::get_IsOleFile()
  } // end of property OleDocumentPropertiesClass::IsOleFile
  .property string CLSID()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 01 00 00 00 ) 
    .get instance string DSOFile.OleDocumentPropertiesClass::get_CLSID()
  } // end of property OleDocumentPropertiesClass::CLSID
  .property string ProgID()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 01 00 00 00 ) 
    .get instance string DSOFile.OleDocumentPropertiesClass::get_ProgID()
  } // end of property OleDocumentPropertiesClass::ProgID
  .property string OleDocumentFormat()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 01 00 00 00 ) 
    .get instance string DSOFile.OleDocumentPropertiesClass::get_OleDocumentFormat()
  } // end of property OleDocumentPropertiesClass::OleDocumentFormat
  .property string OleDocumentType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 01 00 00 00 ) 
    .get instance string DSOFile.OleDocumentPropertiesClass::get_OleDocumentType()
  } // end of property OleDocumentPropertiesClass::OleDocumentType
} // end of class DSOFile.OleDocumentPropertiesClass

.class interface public abstract auto ansi import DSOFile.OleDocumentProperties
       implements DSOFile._OleDocumentProperties
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 38 39 36 38 31 34 35 2D 43 46 30 31   // ..$58968145-CF01
                                                                                                  2D 34 33 34 31 2D 39 39 35 46 2D 32 45 45 30 39   // -4341-995F-2EE09
                                                                                                  33 46 36 41 42 41 33 00 00 )                      // 3F6ABA3..
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 22 44 53 4F 46 69 6C 65 2E 4F 6C 65 44 6F   // .."DSOFile.OleDo
                                                                                                                          63 75 6D 65 6E 74 50 72 6F 70 65 72 74 69 65 73   // cumentProperties
                                                                                                                          43 6C 61 73 73 00 00 )                            // Class..
} // end of class DSOFile.OleDocumentProperties


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Interop.DSOFile.res
