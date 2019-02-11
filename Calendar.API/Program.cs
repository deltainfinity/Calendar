using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IO;
using System.Reflection;

namespace Calendar.API
{
    public class Program
    {
        private static readonly ILogger Logger = Log.ForContext<Program>();

        /// <summary>
        /// Working directory the application launched from
        /// </summary>
        public static string WorkingDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        /// <summary>
        /// .NET Configuration Service
        /// </summary>
        public static IConfiguration Configuration => new ConfigurationBuilder()
                .SetBasePath(WorkingDirectory)
                .AddJsonFile("appSettings.json", false, true)
                .AddEnvironmentVariables()
                .Build();

        public static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();

            SerilogConfig.Configure(serviceCollection, Configuration);

            Log.Verbose("Starting web host");
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

        /// <summary>
        /// Create Swashbuckle API Info for API Explorer Description
        /// </summary>
        /// <param name="description">The API Explorer Description for the API</param>
        /// <returns></returns>
        public static Info CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new Info()
            {
                Title = $"Calendar API {description.ApiVersion}",
                Version = description.ApiVersion.ToString(),
                Description = $"Evolve's Calendar API with support for Swashbuckle and API Versioning ({description.ApiVersion}).",
                Contact = new Contact() { Name = "Vant4ge Engineering", Email = "dev@vant4ge.com" },
                TermsOfService = "Proprietary",
                License = new License() { Name = "Proprietary", Url = "https://www.vant4ge.com" }
            };

            if (description.IsDeprecated)
            {
                info.Description += Environment.NewLine + "*** This API version has been deprecated ***";
            }

            return info;
        }
    }
}
