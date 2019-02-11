using Autofac;
using Calendar.API.BasicAuth;
using Calendar.API.Core;
using Calendar.API.Core.Exceptions;
using Calendar.API.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Serilog;

namespace Calendar.API
{
    public class Startup
    {
        private static readonly ILogger Logger = Log.ForContext<Startup>();

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="configuration">Instance of configuration to load</param>
        /// <param name="env">Instance of hosting environment to load</param>
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        private IConfiguration Configuration { get; }

        private IHostingEnvironment HostingEnvironment { get; }

        /// <summary>
        /// Handler for configuring .net core DI
        /// </summary>
        /// <param name="services">The instance of DI services</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddAntiforgery(opts => opts.Cookie.Name = "AntiForgery.CalendarAPI");

            var azureBlobStorage = Configuration.GetConnectionString("AzureBlobStorage") as string;
            if (string.IsNullOrWhiteSpace(azureBlobStorage))
            {
                throw new ConfigurationErrorsException("AzureBlobStorage connection string is not set in the app.settings.");
            }

            var dpContainerName = Configuration["DataProtectionContainerName"] as string;
            if (string.IsNullOrWhiteSpace(dpContainerName))
            {
                throw new ConfigurationErrorsException("DataProtectionContainerName is not set in the app.settings.");
            }

            var blobClient = CloudStorageAccount.Parse(azureBlobStorage).CreateCloudBlobClient();
            services.AddDataProtection()
                .PersistKeysToAzureBlobStorage(blobClient.GetContainerReference(dpContainerName), "DataProtectionKeys")
                .SetApplicationName("Evolve Calendar API");

            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddVersionedApiExplorer(options =>
            {
                //Will format the version as "'v'major[.minor][-status]"
                options.GroupNameFormat = "'v'VVVV";
                options.SubstituteApiVersionInUrl = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
            });

            services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
            });

            services.AddSwaggerGen(options =>
            {
                var provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerDoc(description.GroupName, Program.CreateInfoForApiVersion(description));
                }
                options.OperationFilter<SwaggerDefaultValues>();
                options.IncludeXmlComments($"{Program.WorkingDirectory}/Calendar.API.xml", true);
                options.IncludeXmlComments($"{Program.WorkingDirectory}/Calendar.API.Core.xml", true);
            });

            Logger.Debug("Startup -> Configure AspNetCore Services: COMPLETE");
        }

        /// <summary>
        /// Handle for Autofac DI and configuration
        /// </summary>
        /// <param name="builder">The autofac container builder</param>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new CalendarApiCoreModule(Configuration));

            Logger.Debug("Startup -> AutoFac Registration: COMPLETE");
        }

        /// <summary>
        /// .Net Core Pipeline configuration
        /// </summary>
        /// <param name="app">Instance of the application configuration</param>
        /// <param name="env">Instance of the hosting configuration</param>
        /// <param name="provider">Instance of the API Versioning configuration</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Xss-Protection", "1");
                context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                await next();
            });

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                }
            });

            app.UseMiddleware<BasicAuthMiddleware>(Configuration);

            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseMvc();
            app.UseMiddleware<SerilogMiddleware>();

            Logger.Debug("Startup -> Configure AspNetCore: COMPLETE");
        }
    }
}
