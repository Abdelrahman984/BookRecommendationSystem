using BrainHope.Services.DTO.Email;
using CleanArchitecture.DataAccess;
using CleanArchitecture.DataAccess.Contexts;
using CleanArchitecture.DataAccess.Models;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Services;
using CleanArchitecture.Services.Interfaces;
using CleanArchitecture.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.ML.OnnxRuntime;
using StackExchange.Redis;
using Utilites;

namespace CleanArchitecture.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var config = builder.Configuration;

            // Inject external DI
            builder.Services
                .AddDataAccessServices(config)
                .AddServiceLayer()
                .AddApiLayer(config);

            // ------------------[ ADDITIONS FOR RECOMMENDATION ENGINE ]------------------
            // Register your new book and recommendation services
            builder.Services.AddScoped<IBookService, BookService>();
            builder.Services.AddScoped<IRecommendationService, RecommendationService>();

            // Register the ONNX InferenceSession as a singleton for performance.
            // This ensures the model is loaded into memory only once.
            var onnxModelPath = Path.Combine(builder.Environment.ContentRootPath, "OnnxModels", "t_book_recommender.onnx");
            builder.Services.AddSingleton(new InferenceSession(onnxModelPath));
            // -------------------------------------------------------------------------

            // Identity config
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            #region JWT

            #region Cores

            // 🔹 Enable CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
            #endregion

            // ✅ Add infrastructure layer (JWT, Google)
            builder.Services.AddInfrastructure(builder.Configuration);
            #endregion

            #region Email
            var Configure = builder.Configuration;
            var emailconfig = Configure.GetSection("EmailConfiguration").Get<EmailConfiguration>();
            builder.Services.AddSingleton(emailconfig);
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.Configure<IdentityOptions>(opts => opts.SignIn.RequireConfirmedEmail = true);
            #endregion

            builder.Services.AddAuthorization();

            // Redis
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisConnection = config.GetSection("Redis")["ConnectionString"];
                return ConnectionMultiplexer.Connect(redisConnection);
            });

            builder.Services.AddSignalR();

            builder.Services.AddScoped<IDataSeedingService, DataSeedingService>();
            builder.Services.AddScoped<IBookService, BookService>();
            builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
            var app = builder.Build();

            // Redis ImageHelper Config
            var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
            var env = app.Services.GetRequiredService<IWebHostEnvironment>();
            ImageHelper.Configure(accessor, env);

            // Middleware
            // if (app.Environment.IsDevelopment())
            // {
                app.UseSwagger();
                app.UseSwaggerUI();
            // }

            app.UseStaticFiles();
            app.UseCors("AllowAll");
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
