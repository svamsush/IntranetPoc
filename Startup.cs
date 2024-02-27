using Autofac.Extensions.DependencyInjection;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;
using IntranetPortal.Entities;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Autofac.Core;
using System.Configuration;

namespace IntranetPortal.API
{
    public static class Startup
    {
        private static WebApplicationBuilder? builder;
        private static IServiceCollection? services;
        public static void Start(WebApplicationBuilder _builder)
        {
            builder = _builder;
            services = builder.Services;
            AddDbContext();
            InjectDependenciesThroughAutoFac();
            //AddIdentity();


            //AddJwtTokenConfig();
            AddADAuth();



            AddSwagger();
            //AddAutoMapper();
            AddSerilog();
            //InitializeConstants();
            //UpdateMaxRequestSize();
            //AddQuartz();
        }

        private static void AddSwagger()
        {
            builder!.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "SVAM Api",
                    Version = "V1",
                    Description = "SVAM Api",
                    Contact = new OpenApiContact
                    {
                        Name = "SVAM Api"
                    },

                });
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme."
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                if (xmlFile != null)
                {
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                }
                
            });
        }
        private static void InjectDependenciesThroughAutoFac()
        {
            builder!.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(conBuilder =>
            {
                 conBuilder.RegisterAssemblyTypes(Assembly.Load("IntranetPortal.IService"),
                 Assembly.Load("IntranetPortal.Service"))
                .Where(t => t.Name.EndsWith("Service", StringComparison.OrdinalIgnoreCase))
                .PublicOnly()
                .AsImplementedInterfaces()
                .PreserveExistingDefaults()
                .InstancePerLifetimeScope();


                conBuilder.RegisterAssemblyTypes(Assembly.Load("IntranetPortal.IRepository"),
                Assembly.Load("IntranetPortal.Repository"))
                .Where(t => t.Name.EndsWith("Repository", StringComparison.OrdinalIgnoreCase))
                .PublicOnly()
                .AsImplementedInterfaces()
                .PreserveExistingDefaults()
                .InstancePerLifetimeScope();

                conBuilder.RegisterType<IPContext>().As<DbContext>().InstancePerLifetimeScope();

            });
        }
        //private static void AddIdentity()
        //{
        //    builder!.Services.AddIdentity<IdentityUser, IdentityRole>(o =>
        //    {
        //        o.Password.RequireDigit = false;
        //        o.Password.RequireLowercase = false;
        //        o.Password.RequireUppercase = false;
        //        o.Password.RequireNonAlphanumeric = false;
        //        o.User.RequireUniqueEmail = false;
        //        o.SignIn.RequireConfirmedEmail = false;
        //    })
        //.AddEntityFrameworkStores<IPContext>()
        //.AddDefaultTokenProviders();
        //}
        private static void AddJwtTokenConfig()
        {
            var jwtConfig = builder!.Configuration.GetSection("JwtConfig");
            var secretKey = jwtConfig["SecretKey"];
            var issuer = jwtConfig["Issuer"];
            var audience = jwtConfig["Audience"];
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? ""))
            };

            builder.Services.AddSingleton(tokenValidationParameters);

            builder.Services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = "oidc";
            })
            //.AddOpenIdConnect()
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = tokenValidationParameters;
            })
            .AddMicrosoftIdentityWebApi(builder!.Configuration);
            
        }

        private static void AddADAuth()
        {
            services!.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddMicrosoftIdentityWebApi(builder!.Configuration);

           // .AddMicrosoftIdentityWebAppAuthentication(builder!.Configuration.GetSection("AzureAd"));
        }
        
        private static void AddDbContext()
        {
            var identityConnString = builder!.Configuration.GetConnectionString("Default");
            builder.Services.AddDbContext<IPContext>(options =>
                                                                options.UseSqlServer(identityConnString, sqlOptions =>
                                                                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null)));
        }
        private static void AddSerilog()
        {
            Log.Logger = new LoggerConfiguration().CreateBootstrapLogger();
            builder!.Host.UseSerilog((ctx, lc) => lc
            .ReadFrom.Configuration(ctx.Configuration));
        }

        //private static void AddAutoMapper()
        //{
        //    builder.Services.AddAutoMapper(typeof(Startup));
        //}
    }
}
