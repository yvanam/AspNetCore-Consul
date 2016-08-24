using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backend
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

            RegisterService(app, loggerFactory); //Fire and forget

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Fred");
            });
        }

        public async void RegisterService(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("Consul");
            try
            {
                logger.LogInformation("Registering service with Consul");
                var addresses = app.ServerFeatures.Get<IServerAddressesFeature>();
                var serviceUri = new Uri(addresses.Addresses.Single());
                logger.LogInformation($"Service URI is {serviceUri.ToString()}");
                using (var c = new Consul.ConsulClient())
                {
                    var writeResult = await c.Agent.ServiceRegister(
                        new Consul.AgentServiceRegistration()
                        {
                            Name = "backend",
                            Port = serviceUri.Port,
                            Address = Environment.MachineName
                        });

                    logger.LogInformation($"Completed registration with Consul with response code {writeResult.StatusCode.ToString()}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error registering service with Consul: {ex.ToString()}");
            }
            
        }
    }
}
