using System;
using System.Net.Http;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NuciWeb.HTTP;

using NuciCraftInfo.Components;
using NuciCraftInfo.Endpoints;

namespace NuciCraftInfo
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            int cacheDurationInMinutes = builder.Configuration.GetValue<int>(
                "NuciCraftApi:CacheDurationInMinutes",
                30
            );

            if (cacheDurationInMinutes < 30)
            {
                throw new ArgumentOutOfRangeException(
                    "NuciCraftApi:CacheDurationInMinutes",
                    "NuciCraft API cache duration must be at least 30 minutes."
                );
            }

            builder.Services.AddRazorComponents();
            builder.Services.AddOutputCache(options => options.AddPolicy(
                NuciCraftEndpoints.CachePolicyName,
                policy => policy.Expire(TimeSpan.FromMinutes(cacheDurationInMinutes))
            ));
            builder.Services.AddSingleton<HttpClient>(services =>
            {
                HttpClient client = HttpClientCreator.Create();
                client.BaseAddress = new Uri(
                    builder.Configuration.GetValue<string>("NuciCraftApi:BaseUrl") ?? string.Empty
                );
                client.Timeout = TimeSpan.FromSeconds(30);

                return client;
            });

            WebApplication app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();
            app.UseOutputCache();

            app.MapNuciCraftEndpoints();
            app.MapStaticAssets();
            app.MapRazorComponents<App>();

            app.Run();
        }
    }
}
