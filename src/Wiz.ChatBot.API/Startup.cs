using AutoMapper;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Net.Http.Headers;
using NSwag;
using NSwag.SwaggerGeneration.Processors.Security;
using Polly;
using System;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using Wiz.ChatBot.API.Extensions;
using Wiz.ChatBot.API.Filters;
using Wiz.ChatBot.API.Middlewares;
using Wiz.ChatBot.API.Services;
using Wiz.ChatBot.API.Services.Interfaces;
using Wiz.ChatBot.API.Settings;
using Wiz.ChatBot.API.Swagger;
using Wiz.ChatBot.Domain.Interfaces.Identity;
using Wiz.ChatBot.Domain.Interfaces.Notifications;
using Wiz.ChatBot.Domain.Interfaces.Services;
using Wiz.ChatBot.Domain.Interfaces.UoW;
using Wiz.ChatBot.Domain.Notifications;
using Wiz.ChatBot.Infra.Context;
using Wiz.ChatBot.Infra.Identity;
using Wiz.ChatBot.Infra.Services;
using Wiz.ChatBot.Infra.UoW;

[assembly: ApiConventionType(typeof(MyApiConventions))]
namespace Wiz.ChatBot.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.AddControllers();
            services.AddMvc(options =>
            {
                options.Filters.Add<DomainNotificationFilter>();
                options.EnableEndpointRouting = false;
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.IgnoreNullValues = true;
            });

            services.Configure<GzipCompressionProviderOptions>(x => x.Level = CompressionLevel.Optimal);
            services.AddResponseCompression(x =>
            {
                x.Providers.Add<GzipCompressionProvider>();
            });

            services.AddHttpClient<IViaCEPService, ViaCEPService>((s, c) =>
            {
                c.BaseAddress = new Uri(Configuration["API:ViaCEP"]);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.OrResult(response =>
                    !response.IsSuccessStatusCode)
              .WaitAndRetryAsync(3, retry =>
                   TimeSpan.FromSeconds(Math.Pow(2, retry)) +
                   TimeSpan.FromMilliseconds(new Random().Next(0, 100))))
              .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.CircuitBreakerAsync(
                   handledEventsAllowedBeforeBreaking: 3,
                   durationOfBreak: TimeSpan.FromSeconds(30)
            ));

            if (PlatformServices.Default.Application.ApplicationName != "testhost")
            {
                var healthCheck = services.AddHealthChecksUI().AddHealthChecks();

                healthCheck.AddSqlServer(Configuration["ConnectionStrings:CustomerDB"]);

                if (WebHostEnvironment.IsProduction())
                {
                    healthCheck.AddAzureKeyVault(options =>
                    {
                        options.UseKeyVaultUrl($"{Configuration["Azure:KeyVaultUrl"]}");
                    }, name: "azure-key-vault");
                }
                    
                healthCheck.AddApplicationInsightsPublisher();
            }

            if (!WebHostEnvironment.IsProduction())
            {
                services.AddSwaggerDocument(document =>
                {
                    document.DocumentName = "v1";
                    document.Version = "v1";
                    document.Title = "ChatBot API";
                    document.Description = "API de ChatBot";
                    document.OperationProcessors.Add(new OperationSecurityScopeProcessor("JWT"));
                    document.AddSecurity("JWT", Enumerable.Empty<string>(), new SwaggerSecurityScheme
                    {
                        Type = SwaggerSecuritySchemeType.ApiKey,
                        Name = HeaderNames.Authorization,
                        Description = "Token de autenticação via SSO",
                        In = SwaggerSecurityApiKeyLocation.Header
                    });
                });
            }

            services.AddAutoMapper(typeof(Startup));
            services.AddHttpContextAccessor();

            RegisterServices(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<ApplicationInsightsSettings> options)
        {
            if (!env.IsProduction())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseRouting();
            app.UseHttpsRedirection();
            app.UseResponseCompression();

            if (PlatformServices.Default.Application.ApplicationName != "testhost")
            {
                app.UseHealthChecks("/health", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                }).UseHealthChecksUI(setup =>
                {
                    setup.UIPath = "/health-ui";
                });
            }

            if (!env.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUi3();
            }

            app.UseAuthorization();
            app.UseAuthentication();
            app.UseLogMiddleware();

            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = new ErrorHandlerMiddleware(options, env).Invoke
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void RegisterServices(IServiceCollection services)
        {
            services.Configure<ApplicationInsightsSettings>(Configuration.GetSection("ApplicationInsights"));

            #region Service

            services.AddTransient<IFuncionarioService, FuncionarioService>();

            #endregion

            #region Domain

            services.AddScoped<IDomainNotification, DomainNotification>();

            #endregion

            #region Infra

            services.AddDbContext<EntityContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("CustomerDB")));
            services.AddScoped<DapperContext>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IIdentityService, IdentityService>();

            services.AddHttpClient<ICandidatoService, CandidatoService>((s, c) =>
            {
                var httpContext = s.GetService<IHttpContextAccessor>().HttpContext;
                c.BaseAddress = new Uri(Configuration["Wiz:urlCandidato"]);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.OrResult(response =>
                   !response.IsSuccessStatusCode)
              .WaitAndRetryAsync(3, retry =>
                   TimeSpan.FromSeconds(Math.Pow(2, retry)) +
                   TimeSpan.FromMilliseconds(new Random().Next(0, 100))))
             .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.CircuitBreakerAsync(
                  handledEventsAllowedBeforeBreaking: 3,
                  durationOfBreak: TimeSpan.FromSeconds(30)
           ));

            services.AddHttpClient<IFuncionarioWizService, FuncionarioWizService>((s, c) =>
            {
                var httpContext = s.GetService<IHttpContextAccessor>().HttpContext;
                c.BaseAddress = new Uri(Configuration["Wiz:urlFuncionario"]);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                c.DefaultRequestHeaders.Add("x-tenant", "4e6cc3b091d941fa88a98991bd3cd41f");
            }).AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.OrResult(response =>
                   !response.IsSuccessStatusCode)
              .WaitAndRetryAsync(3, retry =>
                   TimeSpan.FromSeconds(Math.Pow(2, retry)) +
                   TimeSpan.FromMilliseconds(new Random().Next(0, 100))))
             .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.CircuitBreakerAsync(
                  handledEventsAllowedBeforeBreaking: 3,
                  durationOfBreak: TimeSpan.FromSeconds(30)
           ));

            services.AddHttpClient<IEquipeGestorWizService, EquipeGestorWizService>((s, c) =>
            {
                var httpContext = s.GetService<IHttpContextAccessor>().HttpContext;
                c.BaseAddress = new Uri(Configuration["Wiz:urlEquipeGestor"]);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                c.DefaultRequestHeaders.Add("x-tenant", "4e6cc3b091d941fa88a98991bd3cd41f");
            }).AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.OrResult(response =>
                   !response.IsSuccessStatusCode)
              .WaitAndRetryAsync(3, retry =>
                   TimeSpan.FromSeconds(Math.Pow(2, retry)) +
                   TimeSpan.FromMilliseconds(new Random().Next(0, 100))))
             .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.CircuitBreakerAsync(
                  handledEventsAllowedBeforeBreaking: 3,
                  durationOfBreak: TimeSpan.FromSeconds(30)
           ));

            #endregion
        }
    }
}
