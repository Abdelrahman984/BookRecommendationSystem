using CleanArchitecture.DataAccess.Contexts;
using CleanArchitecture.DataAccess.IRepository;
using CleanArchitecture.DataAccess.IUnitOfWorks;
using CleanArchitecture.DataAccess.Repsitory;
using CleanArchitecture.DataAccess.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace CleanArchitecture.DataAccess
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDataAccessServices(this IServiceCollection services, IConfiguration config)
        {

            //For Entity FrameWork
            //services.AddDbContext<ApplicationDbContext>(options =>
            //options.UseSqlServer(config.GetConnectionString("Cs")),
            //ServiceLifetime.Scoped);

            services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(
                config.GetConnectionString("Cs"),
                ServerVersion.AutoDetect(config.GetConnectionString("Cs"))
            ),
            ServiceLifetime.Scoped);


            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();

            return services;
        }
    }
}
