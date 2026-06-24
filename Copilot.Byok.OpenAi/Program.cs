using Copilot.Byok.OpenAi.Handlers;
using Copilot.Byok.OpenAi.Middlewares;
using Copilot.Byok.OpenAi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
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
                    .AddOptions<OpenAiOptions>()
                    .Bind(builder.Configuration.GetSection(nameof(OpenAi)));

                builder.Services
                    .AddMemoryCache()
                    .AddHttpForwarder()
                    .AddHttpClient()
                    .AddCopilotByokOpenAi()
                    .AddAuthorization()
                    .AddSingleton<RecyclableMemoryStreamManager>()
                    .PostConfigure<OpenAiOptions>(options => options.Initialize())
                    .ConfigureHttpJsonOptions(jsonOptions =>
                    {
                        jsonOptions.SerializerOptions.TypeInfoResolverChain.Insert(0, JsonContext.Default);
                    });

                builder.Services
                    .AddAuthentication("ApiKey")
                    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKey", null);

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
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseMiddleware<ModelConfigMiddleware>();

                app.MapGet("/", () => "Copilot.Byok.OpenAi is running.");
                var v1 = app.MapGroup("/v1").RequireAuthorization(p => p.RequireAuthenticatedUser());
                v1.MapGet("/models", ModelHandler.GetAll);
                v1.MapGet("/models/{**id}", ModelHandler.GetOne);
                v1.MapPost("/chat/completions", ChatHandler.HandleAsync);
                
                app.Run();
            }
        }
    }
}
