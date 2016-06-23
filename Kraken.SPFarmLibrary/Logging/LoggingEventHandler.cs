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

namespace Kraken.SharePoint.Logging {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    //using Microsoft.SharePoint.Diagnostics;
    using Microsoft.SharePoint.Administration;

  public class LoggingEventArgs : EventArgs {

      private LoggingCategoryProvider categoryProvider;
      public LoggingCategoryProvider CategoryProvider {
        get {
          if (categoryProvider == null)
            categoryProvider = LoggingCategoryProvider.DefaultCategoryProvider;
          return categoryProvider;
        }
        set { categoryProvider = value; }
      }

        public LoggingEventArgs(string category, string area, Exception ex)
        : this(category, area, ex, TraceSeverity.Unexpected, EventSeverity.Error) {
        }
        public LoggingEventArgs(string categoryName, string areaName, Exception ex, TraceSeverity traceLevel, EventSeverity eventLevel) {
          category = CategoryProvider.GetCategory(categoryName, areaName, traceLevel, eventLevel, out isCustomCategory);
          Exception = ex;
          TraceLevel = traceLevel;
          EventLevel = eventLevel;
        }
        public LoggingEventArgs(string categoryName, string areaName, string message, TraceSeverity traceLevel, EventSeverity eventLevel) {
          category = CategoryProvider.GetCategory(categoryName, areaName, traceLevel, eventLevel, out isCustomCategory);
          Message = message;
          TraceLevel = traceLevel;
          EventLevel = eventLevel;
        }

        public LoggingEventArgs(SPDiagnosticsCategory category, Exception ex)
          : this(category, ex, TraceSeverity.Unexpected, EventSeverity.Error) {
        }
        public LoggingEventArgs(SPDiagnosticsCategory category, Exception ex, TraceSeverity traceLevel, EventSeverity eventLevel) {
          this.category = category;
          Exception = ex;
          TraceLevel = traceLevel;
          EventLevel = eventLevel;
        }
        public LoggingEventArgs(SPDiagnosticsCategory category, string message, TraceSeverity traceLevel, EventSeverity eventLevel) {
          this.category = category;
          Message = message;
          TraceLevel = traceLevel;
          EventLevel = eventLevel;
        }
        public LoggingEventArgs(LoggingCategories cat, Exception ex, TraceSeverity traceLevel, EventSeverity eventLevel) :
          this(LoggingCategoryProvider.DefaultCategoryProvider.GetCategory(cat), ex, traceLevel, eventLevel) {
        }
        public LoggingEventArgs(LoggingCategories cat, string message, TraceSeverity traceLevel, EventSeverity eventLevel) :
          this(LoggingCategoryProvider.DefaultCategoryProvider.GetCategory(cat), message, traceLevel, eventLevel) {
        }

        public Exception Exception {
            get;
            set;
        }
        public string Message {
            get;
            set;
        }

        private SPDiagnosticsCategory category;
        public SPDiagnosticsCategory Category {
          get { return category; }
          set {
            category = value;
            // is custom is false            
          }
        }

        private bool isCustomCategory = false;
        /// <summary>
        /// True when the category was created for jsut htis single logging event.
        /// For performance reasons, its bad to create the category over and over again.
        /// </summary>
        public bool IsCustomCategory {
          get { return isCustomCategory; }
        }

        public TraceSeverity TraceLevel {
            get;
            set;
        }
        public EventSeverity EventLevel {
          get;
          set;
        }

    } // LoggingEventArgs 


    [System.Diagnostics.CodeAnalysis.SuppressMessage ("Microsoft.Design", "CA1003:UseGenericEventHandlerInstances",         Justification = "This delegate type is used all over the place and can't easily be renamed at this time.")]
    public delegate void LoggingEventHandler(object sender, LoggingEventArgs e);

} // namespace