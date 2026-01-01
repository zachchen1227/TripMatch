using Lab1224_Identity.Services;

namespace TripMatch
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //一個服務只負責一種責任


            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddIdentityInfrastructure(builder.Configuration);
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<TestingService>();
            // Swagger 與 授權
            builder.Services.AddAuthorization();
            builder.Services.AddEndpointsApiExplorer();

            // --- 建立應用程式 ---
            var app = builder.Build();
            // --- 3. 中間件配置 ---
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseHttpsRedirection();
            app.UseDefaultFiles(); // 支援 wwwroot/signup.html 等靜態檔案

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            // --- 4. 路由映射 ---
            // A. 映射你封裝好的 API 端點
            app.MapAuthEndpoints();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
