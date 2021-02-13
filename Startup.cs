using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MBGenerator.Services;
using MBGenerator.BackgroundTasks;

namespace MBGenerator
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            
            // adds three custom services to the dependency injection framework of MVC.
            // This is nice as it allows us to swap these out in future for other interfaces to potentially managed kafka, managed mongo or anything else 
            // we might like to try...just create a class that matches the interface and inject it under the name.

            // 1. KafkaBackgroundReciever consumes messages off of the imageResponse topic.
            // these return the mongo id of imaged computed by MBImageBuilder and stored in Mongo.
            services.AddHostedService<KafkaBackgroundReceiver>();

            // signalR service
            services.AddSignalR();
            services.AddControllersWithViews();

            // mongo interface - reads the image out of mongo and serves it back to the specific client UI via signalR.
            // not sure it needs to be a singleton to be honest.
            services.AddSingleton<IMongoDB, MongoDBInterface>();

            // Should change the name of this. Sends a request to build an image via the imageReq topic.
            services.AddSingleton<ICloudOrders, KafkaListOfCloudOrders>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            // standard HTTP pipeline configuration for ASP.NET MVC.
            // only addition by me is the inclusion of the signalR hub endpoint (ImageMarshallingHub)
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // map the endpoint for SignalR messages to the hub.
                endpoints.MapHub<ImageMarshallingHub>("/ImageMarshallingHub");

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                
            });

            Console.WriteLine("MGGenerator startup complete");
        }
    }
}
