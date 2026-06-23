using Copilot.Byok.OpenAi.Handlers;
using Copilot.Byok.OpenAi.Middlewares;
using Copilot.Byok.OpenAi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using ServiceSelf;

namespace Copilot.Byok.OpenAi
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            if (Service.UseServiceSelf(args))
            {
                var builder = WebApplication.CreateSlimBuilder(args);

                builder.Services
                    .AddOptions<ModelOptions>()
                    .Bind(builder.Configuration);

                builder.Services
                    .AddMemoryCache()
                    .AddHttpForwarder()
                    .AddHttpClient()
                    .AddCopilotByokOpenAi()
                    .PostConfigure<ModelOptions>(options => options.Initialize())
                    .ConfigureHttpJsonOptions(jsonOptions =>
                    {
                        jsonOptions.SerializerOptions.TypeInfoResolverChain.Insert(0, JsonContext.Default);
                    });

                builder.Logging.ClearProviders();
                builder.Host.UseServiceSelf();
                builder.Host.UseSerilog((context, logging) =>
                {
                    var template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}]{NewLine}{SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";
                    logging.ReadFrom.Configuration(context.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(outputTemplate: template);
                }, writeToProviders: true);

                builder.WebHost.UseKestrelHttpsConfiguration();
                builder.WebHost.ConfigureKestrel(kestrel =>
                {
                    var certService = kestrel.ApplicationServices.GetRequiredService<CertService>();
                    certService.CreateCaCertIfNotExists();
                    certService.InstallCaCertIfWindows();

                    kestrel.ListenLocalhost(443, http => http.UseHttps(https =>
                    {
                        https.ServerCertificateSelector = (context, sni) => certService.GetOrCreateServerCert(sni);
                    }));
                });

                var app = builder.Build();
                app.UseMiddleware<ModelMiddleware>();

                app.MapGet("/", ModelHandler.Get);
                app.MapGet("/v1/models", ModelHandler.GetAll);
                app.MapGet("/v1/models/{**model}", ModelHandler.GetOne);
                app.Map("/v1/chat/completions", ChatHandler.HandleAsync);

                app.Run();
            }
        }
    }
}
