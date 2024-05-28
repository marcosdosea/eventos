using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Service;

namespace EventoWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<EventoContext>(
                options => options.UseMySQL(builder.Configuration.GetConnectionString("EventoDatabase")));

            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddTransient<ITipoInscricaoService, TipoInscricaoService>();
            builder.Services.AddTransient<IAreaInteresseService, AreaInteresseService>();
            builder.Services.AddTransient<ISubeventoService, SubeventoService>();
            builder.Services.AddTransient<IEventoService, EventoService>();
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

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}