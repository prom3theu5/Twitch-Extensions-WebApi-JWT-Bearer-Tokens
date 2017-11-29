using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Server.SocketHandlers;
using System;

namespace Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public static TokenValidationParameters TKP { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            TKP = new TokenValidationParameters()
            {
                ValidAudience = "", //Twitch Client ID
                ValidIssuer = "https://api.twitch.tv/api",
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(0)
            };

            services.AddMvc();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.MetadataAddress = "https://api.twitch.tv/api/.well-known/openid-configuration";
                options.TokenValidationParameters = TKP;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseAuthentication();
            app.UseWebSockets();

            app.Map("/clock", (endpoint)=> {
                endpoint.UseMiddleware<WebSocketClockHandler>();
            });

            app.Map("/ping", (endpoint) => {
                endpoint.UseMiddleware<WebSocketPingHandler>();
            });

            app.UseMvcWithDefaultRoute();
        }

    }
}
