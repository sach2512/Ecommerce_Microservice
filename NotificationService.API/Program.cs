
using Consul;
using Ecommerce.Common.ServiceDiscovery.Configuration;
using Ecommerce.Common.ServiceDiscovery.Registrations;
using Ecommerce.Common.ServiceDiscovery.Resolver;

namespace NotificationService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.Configure<ConsulConfig>(builder.Configuration.GetSection("Consul"));
            builder.Services.AddSingleton<IconsulServiceResolver, consulServiceResolver>();
            builder.Services.AddHostedService<ConsulRegistrationHostedService>();
            builder.Services.AddSingleton<IConsulClient>(sp =>
            {
                return new ConsulClient(c =>
                {
                    c.Address = new Uri("http://localhost:8500");
                });
            });

            var app = builder.Build();
            app.MapGet("/health", () =>
            {
                return ("ok");
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
