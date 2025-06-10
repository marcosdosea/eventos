using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using Service;
using Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.UI.Services;
using Core.DTO;
using EventoWeb.Models;

namespace EventoWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<PessoaModel, Pessoa>().ReverseMap();
                cfg.CreateMap<PessoaSimpleDTO, ColaboradorDTO>()
                    .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0)) // Valor padrão
                    .ForMember(dest => dest.Cpf, opt => opt.MapFrom(src => src.Cpf))
                    .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Nome))
                    .ForMember(dest => dest.NomeCracha, opt => opt.MapFrom(src => string.Empty))
                    .ForMember(dest => dest.Sexo, opt => opt.MapFrom(src => string.Empty))
                    .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                    .ForMember(dest => dest.Telefone1, opt => opt.MapFrom(src => src.Telefone1))
                    .ForMember(dest => dest.Telefone2, opt => opt.MapFrom(src => src.Telefone1 ?? string.Empty))
                    .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true)) // Valor padrão
                    .ForMember(dest => dest.RegistrationDate, opt => opt.MapFrom(src => DateTime.Now)) // Valor padrão
                    .ForMember(dest => dest.LastLogin, opt => opt.MapFrom(src => (DateTime?)null)); // Valor padrão
            }, AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddTransient<IAdministradorService, AdministradorService>();
            builder.Services.AddTransient<ITipoInscricaoService, TipoInscricaoService>();
            builder.Services.AddTransient<IAreaInteresseService, AreaInteresseService>();
            builder.Services.AddTransient<IPessoaService, PessoaService>();
            builder.Services.AddTransient<ISubeventoService, SubeventoService>();
            builder.Services.AddTransient<IEventoService, EventoService>();
            builder.Services.AddTransient<IModelocrachaService, ModelocrachaService>();
            builder.Services.AddTransient<IInscricaoService, InscricaoService>();
            builder.Services.AddTransient<IEstadosbrasilService, EstadosbrasilService>();
            builder.Services.AddTransient<ITipoeventoService, TipoeventoService>();
            builder.Services.AddTransient<IEmailSender, EmailSender>();
            builder.Services.AddTransient<IParticipacaoPessoaEventoService, ParticipacaoPessoaEventoService>();
            builder.Services.AddTransient<IColaboradorService, ColaboradorService>();

            builder.Services.AddDbContext<EventoContext>(
                options => options.UseMySQL(builder.Configuration.GetConnectionString("EventoDatabase")));

            builder.Services.AddDbContext<IdentityContext>(
                options => options.UseMySQL(builder.Configuration.GetConnectionString("ItatechUsersDatabase")));

            builder.Services.AddDefaultIdentity<UsuarioIdentity>(
                options => {
                    // SignIn settings
                    options.SignIn.RequireConfirmedAccount = true;
                    options.SignIn.RequireConfirmedEmail = true;
                    options.SignIn.RequireConfirmedPhoneNumber = false;

                    // Password settings
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 6;

                    // Default User settings.
                    options.User.AllowedUserNameCharacters =
                            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                    //options.User.RequireUniqueEmail = true;

                    // Default Lockout settings
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;
                }).AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<IdentityContext>();

            //Configure tokens life
            builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
                //sets a 2 hour lifetime of the generated token to reset password/email/phone number
                options.TokenLifespan = TimeSpan.FromHours(2)
            );

            builder.Services.ConfigureApplicationCookie(options =>
            {
                //options.AccessDeniedPath = "/Identity/Autenticar";
                options.Cookie.Name = "YourAppCookieName";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                //options.LoginPath = "/Identity/Autenticar";
                // ReturnUrlParameter requires 
                options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
                options.SlidingExpiration = true;
            });

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[] { new CultureInfo("pt-BR") };
                options.DefaultRequestCulture = new RequestCulture("pt-BR");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

            app.UseAuthentication();
            app.UseAuthorization();

            // Inicializa os roles do sistema
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                IdentityInitializer.InitializeRoles(roleManager).Wait();
            }

            app.Use(async (context, next) =>
            {
                var isAuthenticated = context.User.Identity.IsAuthenticated;
                var isAdmin = context.User.IsInRole("ADMINISTRADOR");

                if (isAuthenticated)
                {
                    System.Diagnostics.Debug.WriteLine($"User is authenticated. Is Admin: {isAdmin}");
                    if (isAdmin)
                    {
                        context.Items["Layout"] = "_LayoutAdministrativo";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("User is NOT authenticated");
                }

                await next();
            });

            app.MapRazorPages();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapControllers();

            app.Run();
        }
    }
}