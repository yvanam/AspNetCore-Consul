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
                var logger = loggerFactory.CreateLogger("Consul");
                string backendPort = null;
                try
                {
                    using (var c = new Consul.ConsulClient())
                    {
                        var result = await c.KV.Get("backend_port");
                        backendPort = System.Text.Encoding.UTF8.GetString(result.Response.Value);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to get service port from Consul: {ex.ToString()}");
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(ex.ToString());
                    return;
                }
                
                using (var c = new System.Net.Http.HttpClient())
                {
                    c.BaseAddress = new Uri($"http://localhost:{backendPort}");
                    string serviceResponse = await c.GetStringAsync("");

                    await context.Response.WriteAsync($"Hello {serviceResponse}!");
                }

                   
            });
        }
    }
}
