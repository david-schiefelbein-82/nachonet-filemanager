using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Nachonet.Common.Web;
using Nachonet.Common.Web.ActiveDirectory;
using Nachonet.Common.Web.ActiveDirectory.Config;
using Nachonet.Common.Web.AppLocal;
using Nachonet.Common.Web.AppLocal.Config;
using Nachonet.Common.Web.Configuration;
using Nachonet.Common.Web.Oidc;
using Nachonet.Common.Web.Oidc.Config;
using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Data;
using Serilog;
using System.Text;

namespace Nachonet.FileManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            OidcKeyCache oidcKeyCache;

            try
            {
                oidcKeyCache = OidcKeyCache.Load(OidcKeyCache.DefaultConfigName);
            }
            catch
            {
                oidcKeyCache = new OidcKeyCache(OidcKeyCache.DefaultConfigName);
            }

            var configManager = new ConfigManager();
            builder.Services.AddSingleton<IConfigManager>(configManager);
            builder.Services.AddSingleton<FolderManager>();
            builder.Services.AddSingleton<ClipboardManager>();
            builder.Services.AddSingleton<FileContentManager>();
            builder.Services.AddSingleton<DownloadManager>();
            builder.Services.AddSingleton<UploadManager>();
            builder.Services.AddSingleton<UserManager>();
            builder.Services.AddSingleton<ILocalUserManager, LocalUserManager>();
            builder.Services.AddTransient(x => configManager.WebServer);
            builder.Services.AddTransient<IOidcConfig>(x => configManager.WebServer.Oidc);
            builder.Services.AddSingleton<IOidcKeyCache>(oidcKeyCache);
            builder.Services.AddSingleton<IOidcClient, OidcClient>();
            builder.Services.AddSingleton<IJwtSecurityTokenProvider, JwtSecurityTokenProvider>();
            builder.Services.AddTransient<IActiveDirectoryConfig>(x => configManager.WebServer.ActiveDirectory);
            builder.Services.AddTransient<IActiveDirectoryUserAuthenticator, ActiveDirectoryUserAuthenticator>();
            builder.Services.AddTransient<IAppLocalUserAuthenticator, AppLocalUserAuthenticator>();
            builder.Services.AddTransient<IAuthorizationConfig>(x => configManager.Authorization);
            builder.Services.AddTransient<IAppLocalConfig>(x => configManager.AppLocal);
            builder.Services.AddTransient<IJwtTokenAuthenticationConfig>(x => configManager.WebServer.JwtTokenAuthentication);

            var key = configManager.WebServer.JwtTokenAuthentication.GetTokenSigningKey();
            builder.Services.AddAuthentication(auth =>
            {
                auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
             .AddJwtBearer(token =>
             {
                 token.RequireHttpsMetadata = false;
                 token.SaveToken = true;
                 token.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuerSigningKey = true,
                     IssuerSigningKey = new SymmetricSecurityKey(key),
                     ValidateIssuer = true,
                     ValidIssuer = configManager.WebServer.JwtTokenAuthentication.WebSiteDomain,
                     ValidateAudience = true,
                     ValidAudience = configManager.WebServer.JwtTokenAuthentication.WebSiteDomain,
                     RequireExpirationTime = true,
                     ValidateLifetime = true,
                     ClockSkew = TimeSpan.Zero
                 };
             });

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = configManager.WebServer.SessionIdleTimeout;
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var loggingConfigPath = Path.Combine("Config", "logging.json");
            var loggingConfig = new ConfigurationBuilder()
               .AddJsonFile(loggingConfigPath, optional: false, reloadOnChange: true).Build();

            builder.Host.UseSerilog((ctx, lc) => lc
                .ReadFrom.Configuration(loggingConfig));

            var app = builder.Build();

            var logger = app.Services.GetService<ILogger<Program>>();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if (!string.IsNullOrWhiteSpace(configManager.WebServer.PathBase))
            {
                app.UsePathBase(new PathString(configManager.WebServer.PathBase));
            }

            logger?.LogInformation("FileManager Starting {config}", configManager);
            
            if (configManager != null && configManager.WebServer.UseHttpsRedirection)
            {
                app.UseHttpsRedirection();
            }

            app.UseStaticFiles();

            app.UseSession();
            app.UseRouting();

            app.Use(async (context, next) =>
            {
                //if (!string.IsNullOrWhiteSpace(configManager?.WebServer.PathBase))
                //    context.Request.PathBase = configManager.WebServer.PathBase;

                var jwtToken = context.Session.GetString("jwtToken");
                if (!string.IsNullOrEmpty(jwtToken))
                {
                    context.Request.Headers.Authorization = "Bearer " + jwtToken;
                }
                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}