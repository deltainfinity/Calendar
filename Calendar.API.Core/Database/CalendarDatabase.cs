using Calendar.API.Core.Database.Interfaces;
using Microsoft.Extensions.Configuration;
using NPoco;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Calendar.API.Core.Database
{
    class CalendarDatabase : NPoco.Database, ICalendarDatabase
    {
        private static readonly ILogger Logger = Log.ForContext<CalendarDatabase>();
        private IConfiguration Configuration { get; set; }

        public CalendarDatabase(DbConnection connection) : base(connection)
        { }

        public CalendarDatabase(DbConnection connection, DatabaseType dbType)
            : base(connection, dbType)
        { }

        public CalendarDatabase(DbConnection connection, DatabaseType dbType, IsolationLevel? isolationLevel)
            : base(connection, dbType, isolationLevel)
        { }

        public CalendarDatabase(DbConnection connection, DatabaseType dbType, IsolationLevel? isolationLevel, bool enableAutoSelect)
            : base(connection, dbType, isolationLevel, enableAutoSelect)
        { }

        public CalendarDatabase(string connectionString, DatabaseType databaseType, DbProviderFactory provider)
            : base(connectionString, databaseType, provider)
        { }

        public CalendarDatabase(string connectionString, DatabaseType databaseType, DbProviderFactory provider, IsolationLevel? isolationLevel = null, bool enableAutoSelect = true)
            : base(connectionString, databaseType, provider, isolationLevel, enableAutoSelect)
        { }

        public CalendarDatabase(string connectionString, DatabaseType databaseType, DbProviderFactory provider, IConfiguration configuration)
            : base(connectionString, databaseType, provider)
        {
            Configuration = configuration;
        }

        protected override void OnExecutingCommand(DbCommand cmd)
        {
            var dumpSQL = Configuration["Logging::DumpSQL"] != null ? Convert.ToBoolean(Configuration["Logging::DumpSQL"]) : false;
            if (dumpSQL)
            {
                Logger.Debug(FormatCommand(cmd));
            }

            base.OnExecutingCommand(cmd);
        }

        protected override void OnException(Exception e)
        {
            Logger.Error(e, $"NPOCO SQL Exception: {e.Message}.{Environment.NewLine}Last Executing SQL:{Environment.NewLine}{Environment.NewLine}{LastSQL}");
            if (e.InnerException != null) Logger.Error(e.InnerException, e.InnerException.Message);
            e.Data["LastSQL"] = LastSQL;
            base.OnException(e);
        }
    }
}
