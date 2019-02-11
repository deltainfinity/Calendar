using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NetTools;
using Serilog;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Calendar.API.BasicAuth
{
    public class BasicAuthMiddleware
    {
        private ILogger Logger = Log.ForContext<BasicAuthMiddleware>();

        private RequestDelegate Next { get; set; }

        private IConfiguration Configuration { get; set; }

        public BasicAuthMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            Next = next;
            Configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            string authHeader = context.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic"))
            {
                var encodedCredentials = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                var credentials = CreateCredentials(encodedCredentials);

                if (IsAuthorized(credentials) && IsValidIpAddress(context.Request.HttpContext.Connection.RemoteIpAddress))
                {
                    await Next.Invoke(context);
                    return;
                }
            }

            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }

        private Credentials CreateCredentials(string base64Credentials)
        {
            byte[] data = Convert.FromBase64String(base64Credentials);
            var decodedCredentials = Encoding.UTF8.GetString(data);
            var splitCredentials = decodedCredentials.Split(":", StringSplitOptions.RemoveEmptyEntries);
            var creds = new Credentials
            {
                Username = splitCredentials[0],
                Passcode = splitCredentials[1]
            };
            return creds;
        }

        private bool IsAuthorized(Credentials credentials)
        {
            //get the username and password from configuration
            var username = Configuration.GetValue<string>("BasicAuth:Username");
            if (string.IsNullOrEmpty(username))
            {
                throw new Exception("Username not found in appsettings.json");
            }
            var passcode = Configuration.GetValue<string>("BasicAuth:Passcode");
            if (string.IsNullOrEmpty(passcode))
            {
                throw new Exception("Passcode not found in appsettings.json");
            }

            var validCreds = credentials.Username.Equals(username) && credentials.Passcode.Equals(passcode);
            if (validCreds)
            {
                return true;
            }

            Logger.Error($"API Credentials failed Username = {credentials.Username}; API Key = {credentials.Passcode}.");
            return false;
        }

        private bool IsValidIpAddress(IPAddress clientIp)
        {
            var systemValidIpRange = Configuration.GetValue<string>("SystemValidIpRange");
            var clientIpV4 = clientIp.MapToIPv4();

            foreach (var masterIp in systemValidIpRange.Split(','))
            {
                var range = IPAddressRange.Parse(masterIp);
                if (range.Contains(clientIpV4))
                {
                    return true;
                }
            }

            Logger.Error($"Incoming connection from an unsafe IP {clientIp.ToString()}. IP connections must come from {systemValidIpRange}.");
            return false;
        }
    }
}
