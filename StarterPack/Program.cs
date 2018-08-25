using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarterKit.Models;
using StarterKit.Repository;
using System;
using System.Reflection;
using System.Threading;

namespace StarterKit
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);
            using (var serviceScope = host.Services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
                context.Database.Migrate();

                var config = serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();
                var logger = serviceScope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                DbSeeder dbSeeder = new DbSeeder(logger, config);
                dbSeeder.SeedAsync(serviceScope.ServiceProvider,
                    serviceScope.ServiceProvider.GetService<UserManager<ApplicationUser>>(),
                    serviceScope.ServiceProvider.GetService<RoleManager<IdentityRole>>(),
                    CancellationToken.None).GetAwaiter().GetResult();
            }

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, config) =>
            {
                var env = ctx.HostingEnvironment;
                if (env.IsDevelopment())
                {
                    config.AddUserSecrets<Startup>();
                }
                var builtConfig = config.Build();

                var keyVaultConfigBuilder = new ConfigurationBuilder();

                keyVaultConfigBuilder.AddAzureKeyVault(
                    builtConfig["MSI_ENDPOINT"],
                    builtConfig["MSI_CLIENT"],
                    builtConfig["MSI_SECRET"]);
                var keyVaultConfig = keyVaultConfigBuilder.Build();
                config.AddConfiguration(keyVaultConfig);

                //else
                //{
                //    var builtConfig = config.Build();

                //    var keyVaultEndpoint = GetKeyVaultEndpoint();
                //    if (!string.IsNullOrEmpty(keyVaultEndpoint))
                //    {
                //        var azureServiceTokenProvider = new AzureServiceTokenProvider();
                //        var keyVaultClient = new KeyVaultClient(
                //            new KeyVaultClient.AuthenticationCallback(
                //                azureServiceTokenProvider.KeyVaultTokenCallback));
                //        config.AddAzureKeyVault(
                //            keyVaultEndpoint, keyVaultClient, new DefaultKeyVaultSecretManager());
                //    }
                //}
            })
            .UseStartup<Startup>()
          //.UseUrls("http://0.0.0.0:5000")
            .Build();
        //private static string GetKeyVaultEndpoint() => "https://starterpackvault.vault.azure.net/";
    }
}