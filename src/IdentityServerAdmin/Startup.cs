using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IdentityServer4.AspNetIdentity;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using IdentityServerAdmin.Configuration;
using IdentityServerAdmin.Data;
using IdentityServerAdmin.Dtos;
using IdentityServerAdmin.Handlers;
using IdentityServerAdmin.Interfaces;
using IdentityServerAdmin.Models;
using IdentityServerAdmin.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace IdentityServerAdmin
{
    public class Startup
    {
        //adding certificate code
        //private IHostingEnvironment _environment;
        public Startup(IHostingEnvironment env)
        {
            //adding certificate code
            //_environment = env;
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see https://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }
        private Container Container { get; } = new Container();
        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // uncomment below to find add certification to identity Server. Make sure certificatie is placed at the content root path of the application
            //var certificate  = new X509Certificate2(Path.Combine(_environment.ContentRootPath, "idsvr3test.pfx", "idsrv3test"));
            // Add framework services.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("AspNetIdentity")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            var identityServerPipeline = services.AddIdentityServer()
                // yoload signing certificatio and uncomment line below and remove AllTemporarySigningCredentily line69
                //.AddSigningCredential(certificate)
                .AddTemporarySigningCredential()
                .AddAspNetIdentity<ApplicationUser>()
                .AddConfigurationStore(builder =>
                    builder.UseSqlServer(Configuration.GetConnectionString("IdentityServer"), options =>
                        options.MigrationsAssembly(migrationsAssembly)))
                .AddOperationalStore(builder =>
                    builder.UseSqlServer(Configuration.GetConnectionString("IdentityServer"), options =>
                        options.MigrationsAssembly(migrationsAssembly)));

            identityServerPipeline.Services
                .AddTransient<IResourceOwnerPasswordValidator, ResourceOwnerPasswordValidator>();
            identityServerPipeline.Services.AddTransient<IProfileService, ProfileService>();


            services.AddMvc();

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IClientService, ClientService>();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("SuperAdminOnly",
                    policy => policy.Requirements.Add(new SuperAdminOnly()));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            
            InitializeDatabase(app, userManager,roleManager);
            InitializeContainer(app, userManager);
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseIdentity();
            app.UseIdentityServer();

            // Add external authentication middleware below. To configure them please see https://go.microsoft.com/fwlink/?LinkID=532715

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void InitializeContainer(IApplicationBuilder app, UserManager<ApplicationUser> userManager, Action<Container> additionalConfigurationAction = null)
        {
            Container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {

                ConfigurationDbContext configurationContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                ApplicationDbContext applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Container.Register(() => configurationContext, Lifestyle.Scoped);
                Container.Register(() => applicationDbContext, Lifestyle.Scoped);
                Container.Register(() => userManager, Lifestyle.Scoped);
                additionalConfigurationAction?.Invoke(Container);
                Container.Verify();
            }

        }
        private async void InitializeDatabase(IApplicationBuilder app, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var configurationContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();



                configurationContext.Database.Migrate();
                //This will populate records based on config file for Identity Server
                if (!configurationContext.Clients.Any())
                {
                    foreach (var client in InitialConfig.GetClients())
                    {
                        configurationContext.Clients.Add(client.ToEntity());
                    }
                    configurationContext.SaveChanges();
                }

                if (!configurationContext.IdentityResources.Any())
                {
                    foreach (var resource in InitialConfig.GetIdentityResources())
                    {
                        configurationContext.IdentityResources.Add(resource.ToEntity());
                    }
                    configurationContext.SaveChanges();
                }

                if (!configurationContext.ApiResources.Any())
                {
                    foreach (var resource in InitialConfig.GetApiResources())
                    {
                        configurationContext.ApiResources.Add(resource.ToEntity());
                    }
                    configurationContext.SaveChanges();
                }
                var authenticationContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                authenticationContext.Database.Migrate();

                //this will populate default superadmin user.
                if (!authenticationContext.Users.Any())
                {
                    //TODO Add this to a message broker or separate service with retry policies so if error occurs while creating initila use application can still continue
                    UserDto superAdmin = InitialConfig.GetUsers().First();

                    await userManager.CreateAsync(
                        new ApplicationUser { UserName = superAdmin.Username, IsSuperAdmin = true },
                        superAdmin.Password);

                }
                if (!authenticationContext.Roles.Any())
                {
                    IEnumerable<RoleDto> roles = InitialConfig.GetRoles();
                    foreach (RoleDto roleDto in roles)
                    {
                        await roleManager.CreateAsync(
                            new IdentityRole { Name = roleDto.Name, NormalizedName = roleDto.Name.ToUpper() });
                    }
                }
            }
        }
    }
}
