using System.Globalization;
using System.Threading.RateLimiting;
using Core;
using Core.Service;
using EventoApi.Filter;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Service;

namespace EventoApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DotNetEnv.Env.Load();

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container with Exception handling.
            builder.Services.AddControllers(options =>
                options.Filters.Add(new HttpResponseExceptionFilter()));

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "EventoApi - Boralá",
                    Version = "v1",
                    Description = "API de serviços e integração do ecossistema de Eventos (Boralá)."
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Insira o token Bearer no formato: Bearer {seu_token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

            // Services de Negócio
            builder.Services.AddTransient<IEventoService, EventoService>();
            builder.Services.AddTransient<ISubeventoService, SubeventoService>();
            builder.Services.AddTransient<IInscricaoService, InscricaoService>();
            builder.Services.AddTransient<ISincronizacaoFrequenciaService, SincronizacaoFrequenciaService>();
            builder.Services.AddTransient<IPessoaService, PessoaService>();
            builder.Services.AddTransient<IParticipacaoPessoaEventoService, ParticipacaoPessoaEventoService>();
            builder.Services.AddTransient<IParticipacaoPessoaSubEventoService, ParticipacaoPessoaSubEventoService>();

            // AutoMapper
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // Cache distribuído em memória para idempotência
            builder.Services.AddDistributedMemoryCache();

            // Bancos de Dados (Dual DbContext)
            builder.Services.AddDbContext<EventoContext>(options =>
                options.UseMySQL(builder.Configuration.GetConnectionString("EventoDatabase") ?? string.Empty));

            builder.Services.AddDbContext<IdentityContext>(options =>
                options.UseMySQL(builder.Configuration.GetConnectionString("ItatechUsersDatabase") ?? string.Empty));

            // ASP.NET Core Identity
            builder.Services.AddIdentityCore<UsuarioIdentity>(options =>
            {
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

                // User settings
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddSignInManager()
            .AddErrorDescriber<IdentityErrorDescriberPtBr>()
            .AddEntityFrameworkStores<IdentityContext>();

            // Configurações de Autenticação e Autorização Bearer
            builder.Services.AddAuthentication()
                .AddBearerToken(IdentityConstants.BearerScheme, options =>
                    options.BearerTokenExpiration = TimeSpan.FromHours(1));

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("BipouOperador", policy =>
                    policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme)
                        .RequireAuthenticatedUser()
                        .RequireRole("GESTOR", "COLABORADOR"));
            });

            // Rate Limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("bipou-sessao", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "local",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            });

            // Compressão HTTP para payloads de carga
            builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

            // Localização em Português
            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[] { new CultureInfo("pt-BR") };
                options.DefaultRequestCulture = new RequestCulture("pt-BR");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            var app = builder.Build();

            // Configure HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseRateLimiter();

            app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/bipou/v1/eventos"),
                pipeline => pipeline.UseResponseCompression());

            app.UseRequestLocalization();

            app.UseAuthentication();
            app.UseAuthorization();

            // Inicializa os roles do sistema
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                IdentityInitializer.InitializeRoles(roleManager).Wait();
            }

            app.MapControllers();

            app.Run();
        }
    }
}

