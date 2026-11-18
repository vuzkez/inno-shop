using Inno_Shop.Products.Infrastructure;

namespace Inno_Shop.Products.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection")!);

            builder.Services.AddControllers();
            builder.Services.AddProblemDetails();
            builder.Services.AddHealthChecks();
            builder.Services.AddEndpointsApiExplorer();

            var app = builder.Build();

            app.UseStatusCodePages();

            app.MapHealthChecks("/health");
            app.MapControllers();

            app.Run();
        }
    }
}
