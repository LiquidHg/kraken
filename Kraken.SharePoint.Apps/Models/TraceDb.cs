using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using Kraken.SharePoint.Apps;

namespace Kraken.Apps.Models {

    public partial class TraceDb : DbContext
    {

      public TraceDb()
        : base("TraceDbConnection") {
          Database.SetInitializer(new MigrateDatabaseToLatestVersion<TraceDb, Kraken.Apps.Migrations.Configuration>());
        ((IObjectContextAdapter)this).ObjectContext.ObjectMaterialized +=
              (sender, e) => DateTimeKindAttribute.Apply(e.Entity);
      }
      public virtual DbSet<Application> Applications { get; set; }
      public virtual DbSet<LogEntry> Log { get; set; }
        public virtual DbSet<ServerError> ServerErrors { get; set; }
        public virtual DbSet<ClientError> ClientErrors { get; set; }
        //public virtual DbSet<TimeTrack> TimeTracks { get; set; }
        internal static TraceDb Create()
        {
            return new TraceDb();
        }
    }

    public partial class Application {

      [Key]
      public virtual Guid Id { get; set; }

      [MaxLength(255)]
      public virtual string Name { get; set; }
    }

      public partial class LogEntry {

        [Key]
        public virtual Guid Id { get; set; }

        [DateTimeKind(DateTimeKind.Utc)]
        public DateTime Time { get; set; }

        public virtual System.Diagnostics.TraceLevel Level { get; set; }

        [Column(TypeName = "ntext")]
        public virtual string Message { get; set; }

        /// <summary>
        /// Used to track a user/browser session
        /// </summary>
        public virtual string Session { get; set; }

        /// <summary>
        /// Tracks which app the trace message originates from
        /// </summary>
        public virtual Application Application { get; set; }

        /// <summary>
        /// Used to group related trace log activities together
        /// Sometimes links to a specific task in SharePoint
        /// </summary>
        public virtual Guid CorrelationId { get; set; }

      }

    public class ClientError
    {
        [Key]
        public virtual Guid Id { get; set; }

        [DateTimeKind(DateTimeKind.Utc)]
        public DateTime Time { get; set; }

        public virtual int Line { get; set; }
        public virtual int Column { get; set; }

        [MaxLength(255)]
        public virtual string File { get; set; }

        [Column(TypeName = "ntext")]
        public virtual string CallStack { get; set; }

        /// <summary>
        /// Tracks which app the trace message originates from
        /// </summary>
        public virtual Application Application { get; set; }

        /// <summary>
        /// Used to group related trace log activities together
        /// Sometimes links to a specific task in SharePoint
        /// </summary>
        public virtual Guid CorrelationId { get; set; }

    }

    public class ServerError
    {
        [Key]
        public virtual Guid Id { get; set; }

        [Column(TypeName = "ntext")]
        public virtual string TypeName { get; set; }

        [Column(TypeName = "ntext")]
        public virtual string Request { get; set; }

        [Column(TypeName = "ntext")]
        public virtual string Message { get; set; }
        
        [Column(TypeName = "ntext")]
        public virtual string FullMessage { get; set; }

        [Column(TypeName = "ntext")]
        public virtual string StackTrace { get; set; }

        [Index]
        [DateTimeKind(DateTimeKind.Utc)]
        public virtual DateTime Time { get; set; }

        [MaxLength(64)]
        public virtual string AzureBlobAttachmentId { get; set; }

        [MaxLength(64)]
        public virtual string AzureBlobContainerId { get; set; }

        /// <summary>
        /// Tracks which app the trace message originates from
        /// </summary>
        public virtual Application Application { get; set; }

        /// <summary>
        /// Used to group related trace log activities together
        /// Sometimes links to a specific task in SharePoint
        /// </summary>
        public virtual Guid CorrelationId { get; set; }

    }

}