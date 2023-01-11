using AspNetCoreRateLimit;
using CompanyEmployees.ActionFilters;
using CompanyEmployees.Extensions;
using CompanyEmployees.Utility;
using Contracts;
using Entities.Dto;
using LoggerService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using Repository.DataShaping;
using System.IO;


namespace CompanyEmployees
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            LogManager.LoadConfiguration(string.Concat(Directory.GetCurrentDirectory(),
"/nlog.config"));
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureCors();
            services.ConfigureIISIntegration();
            services.ConfigureLoggerService();
            services.ConfigureSqlContext(Configuration);
            services.ConfigureRepositoryManager();
            services.AddAutoMapper(typeof(Startup));
            services.AddControllers(config =>
            {
                config.RespectBrowserAcceptHeader = true;
                config.ReturnHttpNotAcceptable = true; //if the client ask for unsupported media type , server will return 406 Not Acceptable
                config.CacheProfiles.Add("120SecondDuration", new CacheProfile { Duration = 120 });
            }).AddNewtonsoftJson()  // to support request body conversion to a PatchDocument
              .AddXmlDataContractSerializerFormatters() //options to enable the server to formt the XML response when the client tries negotiating for it
              .AddCustomCSVFormatter();
            services.AddCustomMediaTypes(); //Add custom media type for Hateoas
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            }); // suppress the BadRequest error when the ModelState is invalid. 
            services.AddScoped<ValidationFilterAttribute>(); //register action filter
            services.AddScoped<ValidateCompanyExistsAttribute>(); //register action filter
            services.AddScoped<ValidateEmployeeForCompanyExistsAttribute>();
            services.AddScoped<ValidateMediaTypeAttribute>();
            services.AddScoped<IDataShaper<EmployeeDto>, DataShaper<EmployeeDto>>();
            services.AddScoped<EmployeeLinks>(); //register EmployeeLinks class for Hateoas implementation
            services.AddScoped<IAuthenticationManager, AuthenticationManager>();
            services.ConfigureVersioning();
            services.ConfigureResponseCaching();
            services.ConfigureHttpCacheHeaders();
            services.AddMemoryCache(); //AspNetCoreRateLimit use memory cache to store its counters and rules
            services.ConfigureRateLimitingOptions();
            services.AddHttpContextAccessor();
            services.AddAuthentication(); //for identity
            services.ConfigureIdentity(); //for identity
            services.ConfigureJWT(Configuration); //for JWT configuration
            services.ConfigureSwagger();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerManager logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            app.ConfigureExceptionHandler(logger);
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("CorsPolicy"); //Add CORS configuration to app pipeline
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All
            });
            app.UseResponseCaching(); //add cachding to the application middleware
            app.UseHttpCacheHeaders(); //use http cache headers for cache validation
            app.UseIpRateLimiting(); //use rate limit
            app.UseRouting();
            app.UseSwagger();
            app.UseSwaggerUI(s =>
            {
                s.SwaggerEndpoint("/swagger/v1/swagger.json", "Ultimate Web API v1");
                s.SwaggerEndpoint("/swagger/v2/swagger.json", "Ultimate Web API v2");
            });

            app.UseAuthentication(); //for identity
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
