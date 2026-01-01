using Lab1224_Identity.Data;
using Lab1224_Identity.Models;
using Lab1224_Identity.Models.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lab1224_Identity.Services
{
    public class Infrastructure
    {
    }
    public static class InfrastructureService
    {
        public static IServiceCollection AddIdentityInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<SendGridSettings>(configuration.GetSection("checkemail"));


            // Add services to the container.
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            //擴充方法
            services.AddIdentityServices(configuration);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddIdentityApiEndpoints<ApplicationUser>() // 這行會幫你註冊 UserManager
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddTransient<IEmailSender<ApplicationUser>, EmailSender>();




            services.ConfigureApplicationCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
            });

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                // 1. 定義 Bearer 方案
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "請輸入 JWT Token。格式為: Bearer {你的Token}"
                });
                var securityRequirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {

                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
           Array.Empty<string>()
        }
    };
                options.AddSecurityRequirement(securityRequirement);
     

        });
            services.AddTransient<IEmailSender<ApplicationUser>, EmailSender>(); // External
            return services;
        }
    }
}
