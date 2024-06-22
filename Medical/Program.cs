using Medical.Extensions;
using Medical.Helper;
using Medical.Repositories;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Threading.Tasks;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Medical API", Version = "v1" });

            // Configure Swagger to use JWT Bearer authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer"
            });

            // Configure Swagger to use JWT Bearer authentication
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
        });

        builder.Services.AddContextsServices(builder.Configuration);
        builder.Services.AddScoped<ApplicationUserRepo>();
        builder.Services.AddScoped<PatientMedicalRepo>();

        var app = builder.Build();

        await app.AddAppConfig();

        // Enable middleware to serve generated Swagger as a JSON endpoint.
        app.UseSwagger();

        // Specify the Swagger UI endpoint and configure authorization
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Medical API V1");
            c.RoutePrefix = string.Empty; // This sets Swagger UI to be the default page
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.DefaultModelsExpandDepth(-1);
            c.InjectStylesheet("/swagger-ui/custom.css");
            c.InjectJavascript("/swagger-ui/custom.js");
            c.DocumentTitle = "Medical API Documentation";
            c.DefaultModelExpandDepth(0);
            c.EnableFilter();
            c.DocExpansion(DocExpansion.None);
            c.EnableValidator();

            // Add JWT bearer token input manually in Swagger UI
            c.OAuthUseBasicAuthenticationWithAccessCodeGrant();
        });

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.Use(async (context, next) =>
        {
            await next();

            if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
            {
                context.Request.Path = "/Home/NotFound";
                await next();
            }
        });

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        // Enable authentication
        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Ads}/{id?}");

        app.Run();
    }
}
