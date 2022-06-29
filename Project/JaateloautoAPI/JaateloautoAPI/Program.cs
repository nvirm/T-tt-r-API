using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JaateloautoAPI.Helpers;

namespace JaateloautoAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var jHelper = new JaateloHelper();
            VRoutes.Maintenance = true;
            VRoutes.InitialRunDone = false;
            CreateHostBuilder(args).Build().Run();
            
            //
            //if (init == "OK")
            //{var init = Task.Run(async () => await jHelper.getBaseInfo()).Result; //Wait for initial run to build data (Locks application while running)
            //    CreateHostBuilder(args).Build().Run();
            //}
            //else
            //{
            //    Console.WriteLine("KILLING SESSION");
            //    System.Diagnostics.Process.GetCurrentProcess().Kill();
            //}
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
