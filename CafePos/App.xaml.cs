using System.Windows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using CafePos.Hubs;
using System.Threading.Tasks;

namespace CafePos
{
    public partial class App : Application
    {
        private IHost _host;

        public App()
        {
            // Kestrel Host Builder'ı yapılandırıyoruz
            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // Uygulama tüm ağlardan 5000 portu üzerinden istek kabul edecek
                    // Örn: http://192.168.1.10:5000
                    webBuilder.UseUrls("http://0.0.0.0:5000");
                    webBuilder.Configure(ConfigureApp);
                    webBuilder.ConfigureServices(ConfigureServices);
                })
                .Build();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // SignalR servisini Kestrel'e ekliyoruz
            services.AddSignalR();

            // İleride buraya CORS (farklı cihazlardan erişim izni) ayarlarını da ekleyebiliriz
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });
        }

        private void ConfigureApp(IApplicationBuilder app)
        {
            app.UseCors("AllowAll");
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                // SignalR uç noktamız: http://localhost:5000/posHub adresinden dinleyecek
                endpoints.MapHub<PosHub>("/posHub");
            });
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Kestrel sunucusunu asenkron olarak başlat
            await _host.StartAsync();

            // Ana penceremizi aç
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // Uygulama kapandığında Kestrel'i de temiz bir şekilde durdur
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }
}