/*
  Project Kraken: SPARK for Microsoft SharePoint 2010
  Copyright (C) 2003-2011 Thomas Carpe. <http://www.ThomasCarpe.com/>
  Maintained by: <http://www.LiquidMercurySolutions.com/>

  This file is part of SPARK: SharePoint Application Resource Kit.
  SPARK projects are distributed via CodePlex: <http://www.codeplex.com/spark/>

  You may use this code for commercial purposes and derivative works, 
  provided that you maintain all copyright notices.

  SPARK is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version. You should have received a copy of
  the GNU General Public License along with SPARK.  If not, see
  <http://www.gnu.org/licenses/>.

  SPARK is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.
  
  We worked hard on all SPARK code, and we don't make any profit from
  sharing it with the world. Please do us a favor amd give us credit
  where credit is due, by leaving this notice unchanged. We all stand
  on the backs of giants. Wherever we have used someone else's code or
  blog article as the basis of our work, we have provided references
  to our source.
*/

namespace Kraken.SharePoint.Events {

  using System;
  using System.Security.Permissions;
  using Microsoft.SharePoint;
  using Microsoft.SharePoint.Security;
  using Microsoft.SharePoint.Utilities;
  using Microsoft.SharePoint.Workflow;

  using Kraken.SharePoint;

  /// <summary>
  /// List Item Events
  /// </summary>
  public class CopyFieldValueListItemEventReceiver : SPItemEventReceiver {

    public string FieldSource { get; set; } //= "CategoryTitle";
    public string FieldTarget { get; set; } //= "DocCategoryName";

    /// <summary>
    /// An item is being added.
    /// </summary>
    public override void ItemAdding(SPItemEventProperties properties) {
      SyncFieldValue(properties.ListItem);
      base.ItemAdding(properties);
    }

    /// <summary>
    /// An item is being updated.
    /// </summary>
    public override void ItemUpdating(SPItemEventProperties properties) {
      SyncFieldValue(properties.ListItem);
      base.ItemUpdating(properties);
    }

    protected virtual void SyncFieldValue(SPListItem item) {
      base.EventFiringEnabled = false;
      string status;
      bool didRead = item.TryGetValue<string>(FieldSource, out status);
      if (didRead) {
        item[FieldTarget] = status;
        item.SystemUpdate(false);
      }
      base.EventFiringEnabled = true;
    }

  }
}
