/*-------------------------------------------------------------------------------------
Class AutoSortArrayList, using C#
Written by Peter L. Blum (www.PeterBlum.com)
Free to distribute and reuse. Release: June 10, 2002

AutoSortArrayList fills a gap in the various collection classes offered within the
Microsoft.NET framework. It maintains a list that is always sorted. The list does not
use a key to identify each instance.
The .NET framework offers similar classes, each with shortcomings.
* ArrayList - Can sort any type of object but cannot automatically add instances into
    the correct sorted position.
* SortedList - Can keep any type of object in a sorted order. However, it demands a key
    for every instance. If you don't manage your objects by name, this won't work.
* StringsCollection - Can keep a list of strings without the requirement of a key.
    However, it can't keep them sorted.
AutoSortArrayList is based on ArrayList and overrides any method that dictates order,
such as Add(), Insert(), and AddRange(). It overrides methods that lookup data so
that a binary search is used for speed. It overrides methods that allow the user to
control the location of inserted data and throws InvalidOperationExceptions.

AutoSortArrayList can be used with any class that you want to keep ordered. It determines
order with an object that implements the IComparer interface. If you are collecting primative
types like int and double, the default IComparer methodology is used. If you are collecting
strings and need case insensitivity, .NET provides System.Collections.CaseInsensitiveComparer.
For any other class, you will have two choices:
* implement a IComparer class that determines ordering. This is especially useful when storing
  a heterogeneous list of objects.
* implement the IComparable interface on the actual classes being stored in the list. This adds
  the method ComparesTo to your classes. If you use this, the default IComparer methodology
  will work.

Here is an overview of the key properties of AutoSortArrayList:
* xAutoSortB - when true, Add() will insert in a sorted order. Defaults to true.
* xIComparer - use this to supply a custom IComparer. Defaults to null.
* xDuplicatesAllowedB - when true, Add() will permit duplicate instances (as determined
  by BinarySearch() finding an exact match in the list. Defaults to false.
There are several constructors, designed to override these property's defaults.

For additional documentation, see MSDN for the ArrayList members.

--------------------------------------------------------------------------------------*/
namespace Kraken.Collections {

    using System;
    using System.Collections;

// ----------- CLASS AutoSortArrayList ------------------------------------------------
/// <remarks>
/// AutoSortArrayList extends the ArrayList to manage a list that is always sorted.
/// For details, see the header of this file.
/// </remarks>
   public class AutoSortArrayList : ArrayList
   {
// ----------- PROPERTIES -------------------------------------------------------------
/// <summary>
/// xAutoSortB indicates if auto sorting is on.
/// When true, Add() and AddRange() will insert the object in the position provided by BinarySearch.
/// A number of methods will raise InvalidOperationException when true: Insert(), InsertRange(), and Reverse().
/// When false, all methods follow use their ancestor's functionality.
/// If set to false and you add items, when you set it back to true, Add() and AddRange() can
/// incorrectly position data as a binary search is used to position data. So if switching from false
/// to true, call Sort().
/// </summary>
      public bool xAutoSortB
      {
         get { return fAutoSortB; }
         set { fAutoSortB = value; }
      }
      protected bool fAutoSortB = true;

/// <summary>
/// xIComparer provides an IComparer interface for use with Sort() and BinarySearch().
/// By default, these methods use the IComparer methods of the objects you insert into the list.
/// For primitive values, this works well. For strings, you may consider System.Collections.CaseInsensitiveComparer.
/// But for custom objects, you'll need to help with an IComparer.
/// Defaults to null.
/// </summary>
      public IComparer xIComparer
      {
         get { return fIComparer; }
         set { fIComparer = value; }
      }
      protected IComparer fIComparer = null;

/// <summary>
/// xDuplicatesAllowedB determines if a duplicate is allowed into the list.
/// This only applies with xAutoSortB is true.
/// If Add() detects a duplicate when this is false, an InvalidOperationException is thrown.
/// Defaults to false.
/// </summary>
      public bool xDuplicatesAllowedB
      {
         get { return fDuplicatesAllowedB; }
         set { fDuplicatesAllowedB = value; }
      }
      protected bool fDuplicatesAllowedB = false;

// ----------- CONSTRUCTORS -------------------------------------------------------------
      public AutoSortArrayList()
      {
      }  // constructor

      public AutoSortArrayList(bool pAutoSortB)
      {
         fAutoSortB = pAutoSortB;
      }  // constructor

      public AutoSortArrayList(bool pAutoSortB, IComparer pIComparer)
      {
         fAutoSortB = pAutoSortB;
         fIComparer = pIComparer;
      }  // constructor

      public AutoSortArrayList(IComparer pIComparer)
      {
         fIComparer = pIComparer;
      }  // constructor

      public AutoSortArrayList(bool pAutoSortB, IComparer pIComparer, bool pDuplicatesAllowedB)
      {
         fAutoSortB = pAutoSortB;
         fIComparer = pIComparer;
         fDuplicatesAllowedB = pDuplicatesAllowedB;
      }  // constructor

// ----------- METHODS -------------------------------------------------------------
/// <summary>
/// Add inserts an item into the collection.
/// It adds to the end if xAutoSortB is false.
/// Otherwise, it adds in the sorted order as determined by BinarySearch(xIComparer).
/// Returns the position inserted into the list.
/// Will throw the exception InvalidOperationException if called when xDuplicatesAllowedB is false
/// and you add a duplicate instance.
/// </summary>
      public override int Add(object pValue)
      {
         if (xAutoSortB)
         {
            int vIndex = -1;
            if (Count == 0)
               return base.Add(pValue);
            else
            {
               vIndex = BinarySearch(pValue, xIComparer);
               if (vIndex < 0)   // not found. vIndex is the bitwise complement of the position to insert
               {
                  vIndex = ~vIndex;
                  if (vIndex >= Count)
                     return base.Add(pValue);
                  else
                  {
                     base.Insert(vIndex, pValue);
                     return vIndex;
                  }
               }
               else  if (xDuplicatesAllowedB) // already have one
               {
                  base.Insert(vIndex, pValue);
                  return vIndex;
               }
               else
                  throw new InvalidOperationException("The instance is a duplicate of one already in the list.");
            }
         }
         else
            return base.Add(pValue);
      }  // Add()

//--------------------------------------------------------------------------
/// <summary>
/// AddRange is overridden to enforce sorting when xAutoSortB is true.
/// </summary>
      public override void AddRange(ICollection pCollection)
      {
         if (xAutoSortB)
         {
            // following the documented rules of ArrayList.AddRange:
            // If the new Count (the current Count plus the size of the collection) will be greater than Capacity,
            // the capacity of the list is either doubled or increased to the new Count, whichever is greater.
            if (Count + pCollection.Count > Capacity)
               if (Capacity * 2 > Count + pCollection.Count)
                  Capacity = Capacity * 2;
               else
                  Capacity = Count + pCollection.Count;

            foreach (object vValue in pCollection)
               Add(vValue);  // this will keep it sorted
         }
         else
            base.AddRange(pCollection);
      }  // AddRange()

//--------------------------------------------------------------------------
/// <summary>
/// Contains is overridden to use BinarySearch when xAutoSort is true.
/// </summary>
      public override bool Contains(object pItem)
      {
         if (xAutoSortB)
            return BinarySearch(pItem, xIComparer) >= 0;
         else
            return base.Contains(pItem);
      }  // Contains()

//--------------------------------------------------------------------------
/// <summary>
/// BinarySearch is overridden to enforce the xIComparer property.
/// </summary>
      public override int BinarySearch(object pItem)
      {
         if (xAutoSortB)
            return BinarySearch(pItem, xIComparer);
         else
            return base.BinarySearch(pItem);
      }  // BinarySearch()

//--------------------------------------------------------------------------
/// <summary>
/// IndexOf is overridden to optimize the search using BinarySearch when xAutoSortB is true.
/// </summary>
      public override int IndexOf(object pValue)
      {
         if (xAutoSortB)
         {
            int vIndex = BinarySearch(pValue, xIComparer);
            if (vIndex >= 0)  // exact match
               return vIndex;
            else
               return -1;
         }
         else
            return base.IndexOf(pValue);
      }  // IndexOf()

//--------------------------------------------------------------------------
/// <summary>
/// IndexOf is overridden to optimize the search using BinarySearch when xAutoSortB is true.
/// </summary>
      public override int IndexOf(object pValue, int pStartIndex)
      {
         if (xAutoSortB)
         {
            int vIndex = BinarySearch(pStartIndex, Count - pStartIndex, pValue, xIComparer);
            if (vIndex >= 0)  // exact match
               return vIndex;
            else
               return -1;
         }
         else
            return base.IndexOf(pValue, pStartIndex);
      }  // IndexOf()

//--------------------------------------------------------------------------
/// <summary>
/// IndexOf is overridden to optimize the search using BinarySearch when xAutoSortB is true.
/// </summary>
      public override int IndexOf(object pValue, int pStartIndex, int pCount)
      {
         if (xAutoSortB)
         {
            int vIndex = BinarySearch(pStartIndex, pCount, pValue, xIComparer);
            if (vIndex >= 0)  // exact match
               return vIndex;
            else
               return -1;
         }
         else
            return base.IndexOf(pValue, pStartIndex, pCount);
      }  // IndexOf()

//--------------------------------------------------------------------------
/// <summary>
/// Insert is overridden to prevent inserting when xAutoSortB is true.
/// You should use Add() instead.
/// Will throw the exception InvalidOperationException if called when xAutoSortB is true.
/// </summary>
      public override void Insert(int pIndex, object pValue)
      {
         if (xAutoSortB)
            throw new InvalidOperationException("Cannot insert into a sorted AutoSortArrayList. Use the Add method instead.");
         else
            base.Insert(pIndex, pValue);
      }  // Insert()

//--------------------------------------------------------------------------
/// <summary>
/// InsertRange is overridden to prevent inserting when xAutoSortB is true.
/// You should use AddRange instead.
/// Will throw the exception InvalidOperationException if called when xAutoSortB is true.
/// </summary>
      public override void InsertRange(int pIndex, ICollection pCollection)
      {
         if (xAutoSortB)
            throw new InvalidOperationException("Cannot insert into a sorted AutoSortArrayList. Use the AddRange method instead.");
         else
            base.InsertRange(pIndex, pCollection);
      }  // InsertRange()

//--------------------------------------------------------------------------
/// <summary>
/// Sort is overridden to enforce the xIComparer property
/// </summary>
      public override void Sort()
      {
         if (xAutoSortB)
            Sort(xIComparer);
         else
            base.Sort();
      }  // Sort()

//--------------------------------------------------------------------------
/// <summary>
/// Reverse is overridden to prevent reversing when xAutoSortB is true.
/// You should supply an IComparer that reverses the sort and call Sort().
/// Will throw the exception InvalidOperationException if called when xAutoSortB is true.
/// </summary>
      public override void Reverse()
      {
         if (xAutoSortB)
            throw new InvalidOperationException("Cannot reverse into a sorted AutoSortArrayList. Use an xIComparer whose sort is reversed and call Sort().");
         else
            base.Reverse();
      }  // Reverse()

//--------------------------------------------------------------------------
/// <summary>
/// Reverse is overridden to prevent reversing when xAutoSortB is true.
/// You should supply an IComparer that reverses the sort and call Sort().
/// Will throw the exception InvalidOperationException if called when xAutoSortB is true.
/// </summary>
      public override void Reverse(int pIndex, int pCount)
      {
         if (xAutoSortB)
            throw new InvalidOperationException("Cannot reverse into a sorted AutoSortArrayList. Use an xIComparer whose sort is reversed and call Sort().");
         else
            base.Reverse(pIndex, pCount);
      }  // Reverse()

//--------------------------------------------------------------------------
/// <summary>
/// SetRange is overridden to prevent its use when xAutoSortB is true.
/// You must delete the original items in the range and call AddRange.
/// This could have been done automatically here but that changes the definition
/// of SetRange where order is implied.
/// Will throw the exception InvalidOperationException if called when xAutoSortB is true.
/// </summary>
      public override void SetRange(int pIndex, ICollection pCollection)
      {
         if (xAutoSortB)
            throw new InvalidOperationException("Cannot use SetRange on a sorted AutoSortArrayList. Use RemoveRange then AddRange.");
         else
            base.SetRange(pIndex, pCollection);
      }  // SetRange()


   }  // class AutoSortArrayList()

}  // end namespace
