using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PublicApi
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                var logger = loggerFactory.CreateLogger("RequestHandling");

                logger.LogInformation("Handling request");

                string backendPort = null;
                string backendHost = null;
                try
                {
                    using (var c = new Consul.ConsulClient())
                    {
                        var result = await c.Agent.Services();

                        if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var agentService = result.Response["backend"];
                            backendPort = agentService.Port.ToString();
                            backendHost = agentService.Address;
                            logger.LogInformation($"Found host {backendHost} and port {backendPort} for backend");
                        }
                        else
                        {
                            await WriteFailure(context, logger, $"Failed to get OK response from Consul: {result.StatusCode.ToString()}");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    await WriteFailure(context, logger, $"Failed to get host and port from Consul: {ex.ToString()}");
                    return;
                }
                
                using (var c = new System.Net.Http.HttpClient())
                {
                    if (backendHost == Environment.MachineName) backendHost = "localhost";
                    c.BaseAddress = new Uri($"http://{backendHost}:{backendPort}");

                    logger.LogInformation($"Calling backend at {c.BaseAddress}");

                    try
                    {
                        string serviceResponse = await c.GetStringAsync("");

                        logger.LogInformation($"Backend call completed with '{serviceResponse}'");

                        await context.Response.WriteAsync($"Hello {serviceResponse}!");
                    }
                    catch (Exception ex)
                    {
                        await WriteFailure(context, logger, $"Failed to call backend: {ex.ToString()}");
                    }
                }      
            });
        }

        private async Task WriteFailure(HttpContext context, ILogger logger, string message)
        {
            logger.LogError(message);
            context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync(message);
        }
    }
}
