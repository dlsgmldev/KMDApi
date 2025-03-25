using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NLog;
using KDMApi.DataContexts;
using KDMApi.Models.Helper;
using KDMApi.Utils;
using Swashbuckle.AspNetCore.Swagger;
using Web.Api.Core;
using Web.Api.Extensions;
using Web.Api.Infrastructure;
using Web.Api.Infrastructure.Auth;
using Web.Api.Infrastructure.Data;
using Web.Api.Infrastructure.Helpers;
using Web.Api.Infrastructure.Identity;
using Web.Api.Models.Settings;
using KDMApi.Services;
using Microsoft.AspNetCore.Http.Features;

namespace Web.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            // Don't try and load nlog config during integ tests.
            var nLogConfigPath = string.Concat(Directory.GetCurrentDirectory(), "/nlog.config");
            if (File.Exists(nLogConfigPath)) { LogManager.LoadConfiguration(string.Concat(Directory.GetCurrentDirectory(), "/nlog.config"));}
            Configuration = configuration;
            var builder = new ConfigurationBuilder()
              .SetBasePath(env.ContentRootPath)
              .AddJsonFile("appsettings.Json")
              .AddJsonFile("appsettings.Development.Json", true)
              .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            /*
 *  1. Superadmin
    2. Chief of tribe
    3. Admin
    4. Consultant
    5. Sales
    6. Regular
    7. KCA admin
    8. Sales Leader
    9. Sales Mentor
 */

            // Development Local
            //services.AddDbContext<DefaultContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Development"),
            //                    sqlServerOptions => sqlServerOptions.CommandTimeout(3600)));
            //services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Development"), b => b.MigrationsAssembly("Web.Api.Infrastructure")));
            //services.AddDbContext<AppDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Development"), b => b.MigrationsAssembly("Web.Api.Infrastructure")));
            //services.Configure<DataOptions>(options => Configuration.GetSection("DataOptionsSettingsDev").Bind(options));

            // Development Remote: pakai db kmdtestb, masuk ke folder KMDATATEST
            //                          services.AddDbContext<DefaultContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Staging"),
            //                            sqlServerOptions => sqlServerOptions.CommandTimeout(3600)));
            //                  services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Staging"), b => b.MigrationsAssembly("Web.Api.Infrastructure")));
            //          services.AddDbContext<AppDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Staging"), b => b.MigrationsAssembly("Web.Api.Infrastructure")));
            //        services.Configure<DataOptions>(options => Configuration.GetSection("DataOptionsSettingsStaging").Bind(options));

            //           // Production//
            services.AddDbContext<DefaultContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Default"),
               sqlServerOptions => sqlServerOptions.CommandTimeout(3600)));
            services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Default"), b => b.MigrationsAssembly("Web.Api.Infrastructure")));
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Default"), b => b.MigrationsAssembly("Web.Api.Infrastructure")));
            services.Configure<DataOptions>(options => Configuration.GetSection("DataOptionsSettingsProd").Bind(options));


            // Register the ConfigurationBuilder instance of AuthSettings
            var authSettings = Configuration.GetSection(nameof(AuthSettings));
            services.Configure<AuthSettings>(authSettings);

            services.AddCors(o => o.AddPolicy("QuBisaPolicy", b =>
            {
                // Development
                //b.AllowAnyOrigin()
                //.AllowAnyMethod()
                //.AllowAnyHeader();

                // Production

                b.WithOrigins(new[] { "http://*.gmlperformance.com", "https://*.gmlperformance.com", "http://*.onegml.com", "https://*.onegml.com", "http://*.kreasiciptaasia.com" })
                 .SetIsOriginAllowedToAllowWildcardSubdomains()
                 .AllowAnyHeader()
                 .AllowAnyMethod();

            }));


            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(authSettings[nameof(AuthSettings.SecretKey)]));

            // jwt wire up
            // Get options from app settings
            var jwtAppSettingOptions = Configuration.GetSection(nameof(JwtIssuerOptions));

            // Configure JwtIssuerOptions
            services.Configure<JwtIssuerOptions>(options =>
            {
                options.Issuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                options.Audience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)];
                options.SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            });

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)],

                ValidateAudience = true,
                ValidAudience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)],

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                RequireExpirationTime = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(configureOptions =>
            {
                configureOptions.ClaimsIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                configureOptions.TokenValidationParameters = tokenValidationParameters;
                configureOptions.SaveToken = true;

                configureOptions.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // api user claim policy
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiUser", policy => policy.RequireClaim(Constants.Strings.JwtClaimIdentifiers.Rol, Constants.Strings.JwtClaims.ApiAccess));
                options.AddPolicy("SuperAdminOnly", policy => policy.RequireClaim("SuperAdminOnly", "1")); 
                options.AddPolicy("CanDownloadClient", policy => policy.RequireClaim("CanDownloadClient", "1"));
                options.AddPolicy("CanUpdateCreateClient", policy => policy.RequireClaim("CanUpdateCreateClient", "1"));
                options.AddPolicy("CanReadClient", policy => policy.RequireClaim("CanReadClient", "1"));
                options.AddPolicy("CanDeleteClient", policy => policy.RequireClaim("CanDeleteClient", "0"));
                options.AddPolicy("CanDownloadArticle", policy => policy.RequireClaim("CanDownloadArticle", "1"));
                options.AddPolicy("CanUpdateCreateArticle", policy => policy.RequireClaim("CanUpdateCreateArticle", "1"));
                options.AddPolicy("CanReadArticle", policy => policy.RequireClaim("CanReadArticle", "1"));
                options.AddPolicy("CanDeleteArticle", policy => policy.RequireClaim("CanDeleteArticle", "0"));
                options.AddPolicy("CanDownloadProject", policy => policy.RequireClaim("CanDownloadProject", "1"));
                options.AddPolicy("CanUpdateCreateProject", policy => policy.RequireClaim("CanUpdateCreateProject", "1"));
                options.AddPolicy("CanReadProject", policy => policy.RequireClaim("CanReadProject", "1"));
                options.AddPolicy("CanDeleteProject", policy => policy.RequireClaim("CanDeleteProject", "0"));
            });

            // add identity
            var identityBuilder = services.AddIdentityCore<AppUser>(o =>
            {
                // configure identity options
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 6;
            });

            identityBuilder = new IdentityBuilder(identityBuilder.UserType, typeof(IdentityRole), identityBuilder.Services);
            identityBuilder.AddEntityFrameworkStores<AppIdentityDbContext>().AddDefaultTokenProviders();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1).AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>());

            services.AddAutoMapper();

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "KMD Api", Version = "v1" , Description= "KMD API for digitalization" });
                // Swagger 2.+ support
                c.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    In = "header",
                    Description = "Please insert JWT with Bearer into field",
                    Name = "Authorization",
                    Type = "apiKey"
                });

                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    { "Bearer", new string[] { } }
                });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            services.AddTransient<ActivityLogService>();
            services.AddTransient<FileService>();
            services.AddTransient<ClientService>();
            services.AddTransient<UserService>();
            services.AddTransient<CrmReportService>();
                
            services.AddSingleton<IEmailConfiguration>(Configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>());
            services.AddTransient<IEmailService, EmailService>();


            // Set up upload size
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });

            // Now register our services with Autofac container.
            var builder = new ContainerBuilder();

            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new InfrastructureModule());

            // Presenters
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).Where(t => t.Name.EndsWith("Presenter")).SingleInstance();

            builder.Populate(services);
            var container = builder.Build();
            // Create the IServiceProvider based on the container.
            return new AutofacServiceProvider(container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseExceptionHandler(
                builder =>
                {
                    builder.Run(
                        async context =>
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                            var error = context.Features.Get<IExceptionHandlerFeature>();
                            if (error != null)
                            {
                                context.Response.AddApplicationError(error.Error.Message);
                                await context.Response.WriteAsync(error.Error.Message).ConfigureAwait(false);
                            }
                        });
                });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(s => {
                s.RoutePrefix = "help";
                s.SwaggerEndpoint("../swagger/v1/swagger.json", "MySite");
                s.InjectStylesheet("../css/swagger.min.css");
            });


            //app.UseSwaggerUI(c =>
            //{
            //    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KDMApi V1");
            //});
            app.UseSwaggerUI(s => {
                s.RoutePrefix = "swagger";
                s.SwaggerEndpoint("../swagger/v1/swagger.json", "KDMApi V1");
            });
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
