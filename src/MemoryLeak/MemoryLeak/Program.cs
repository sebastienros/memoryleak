using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Runtime;

namespace MemoryLeak
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }

    public static class GlobalGC
    {
        public static string GC = (GCSettings.IsServerGC == true) ? "Server" : "Workstation";
    }
}
