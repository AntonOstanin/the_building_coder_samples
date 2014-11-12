﻿#region Header
//
// JtPairPicker.cs - helper class to pick a pair of elements
//
// Copyright (C) 2014 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Pick a pair of elements of a specific type.
  /// If exactly two exist in the entire model,
  /// take them. If there are less than two, give
  /// up. If elements have been preselected, use
  /// those. Otherwise, prompt for interactive
  /// picking.
  /// </summary>
  class JtPairPicker<T> where T : Element
  {
    UIDocument _uidoc;
    Document _doc;
    List<T> _a;

    /// <summary>
    /// Allow selection of elements of type T only.
    /// </summary>
    class ElementsOfClassSelectionFilter<T2> : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return e is T2;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return true;
      }
    }

    public JtPairPicker( UIDocument uidoc )
    {
      _uidoc = uidoc;
      _doc = _uidoc.Document;
    }

    public IList<T> Selected
    {
      get
      {
        return _a;
      }
    }

    public Result Pick()
    {
      // Select all T elements in the entire model.

      _a = new List<T>(
        new FilteredElementCollector( _doc )
          .OfClass( typeof( T ) )
          .ToElements()
          .Cast<T>() );

      int n = _a.Count;

      // If there are less than two, 
      // there is nothing we can do.

      if( 2 > n )
      {
        return Result.Failed;
      }

      // If there are exactly two, pick those.

      if( 2 == n )
      {
        return Result.Succeeded;
      }

      // There are more than two to choose from.
      // Check for a pre-selection.

      _a.Clear();

      Selection sel = _uidoc.Selection;

      ICollection<ElementId> ids
        = sel.GetElementIds();

      n = ids.Count;

      Debug.Print( "{0} pre-selected elements.", n );

      // If two or more T elements were pre-
      // selected, use the first two encountered.

      if( 1 < n )
      {
        foreach( ElementId id in ids )
        {
          T e = _doc.GetElement( id ) as T;

          Debug.Assert( null != e,
            "only elements of type T can be picked" );

          _a.Add( e );

          if( 2 == _a.Count )
          {
            Debug.Print( "Found two pre-selected "
              + "elements of desired type and "
              + "ignoring everything else." );

            break;
          }
        }
      }

      // None or less than two elements were pre-
      // selected, so prompt for an interactive 
      // post-selection instead.

      if( 2 != _a.Count )
      {
        _a.Clear();

        // Select first element.

        try
        {
          Reference r = sel.PickObject(
            ObjectType.Element,
            new ElementsOfClassSelectionFilter<T>(),
            "Please pick first element." );

          _a.Add( _doc.GetElement( r.ElementId )
            as T );
        }
        catch( Autodesk.Revit.Exceptions
          .OperationCanceledException )
        {
          return Result.Cancelled;
        }

        // Select second element.

        try
        {
          Reference r = sel.PickObject(
            ObjectType.Element,
            new ElementsOfClassSelectionFilter<T>(),
            "Please pick second element." );

          _a.Add( _doc.GetElement( r.ElementId )
            as T );
        }
        catch( Autodesk.Revit.Exceptions
          .OperationCanceledException )
        {
          return Result.Cancelled;
        }
      }
      return Result.Succeeded;
    }
  }
}