using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using System.Reflection;
using Module = Autofac.Module;
using Serilog;
using Microsoft.Extensions.Configuration;
using Calendar.API.Core.Exceptions;
using Calendar.API.Core.Database;
using NPoco;
using Calendar.API.Core.Database.Interfaces;
using Microsoft.Azure.ServiceBus;
using System.Net.Http;

namespace Calendar.API.Core
{
    public class CalendarApiCoreModule : Module
    {
        private static readonly ILogger Logger = Log.ForContext<CalendarApiCoreModule>();
        private IConfiguration Configuration { get; set; }

        public CalendarApiCoreModule() { }

        public CalendarApiCoreModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void Load(ContainerBuilder builder)
        {
            //setup the database connection
            var calendarConnectionString = Configuration.GetConnectionString("CalendarDatabase");
            if (string.IsNullOrEmpty(calendarConnectionString))
            {
                throw new ConfigurationErrorsException("CalendarDatabase connection string is not set in appsettings.json.");
            }

            builder.Register(c => new CalendarDatabase(calendarConnectionString, DatabaseType.SqlServer2012, System.Data.SqlClient.SqlClientFactory.Instance, Configuration))
               .As<ICalendarDatabase>()
               .InstancePerDependency();

            //setup the Azure Service Bus queue client
            var azureServiceBusConnectionString = Configuration.GetConnectionString("AzureServiceBus");
            if (string.IsNullOrEmpty(azureServiceBusConnectionString))
            {
                throw new ConfigurationErrorsException("Azure Service Bus connection string is not set in appsettings.json");
            }

            var azureServiceBusQueue = Configuration.GetSection("AzureServiceBusQueue").Value;

            builder.Register(b => new QueueClient(azureServiceBusConnectionString, azureServiceBusQueue))
                .As<IQueueClient>()
                .SingleInstance();

            //setup the HttpClient
            builder.Register(c => new HttpClient())
                .As<HttpClient>()
                .SingleInstance();

            ////register repositories
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(t => t.Name.EndsWith("Repository"))
                .AsImplementedInterfaces();

            //register the services
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(t => t.Name.EndsWith("Service"))
                .AsImplementedInterfaces();

            Logger.Debug("Startup -> AutoFac CalendarApiCoreModule Module Registration: COMPLETE");
        }
    }
}
