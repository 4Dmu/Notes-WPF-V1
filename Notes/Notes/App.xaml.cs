using Microsoft.Extensions.Hosting;
using Shell.Core.Extensions;
using Shell.WPF.Shell.Controls;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Notes
{
    public partial class App : Application
    {
        private IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(s =>
                {
                    s.AutoRegisterDependencies(this.GetType().Assembly.GetTypes());
                })
                .Build();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _host.Start();

            NavShell.SetServiceProvider(_host.Services);

            MainWindow = new NavShell()
            {

            };

            MainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
            _host = null;
            base.OnExit(e);
        }
    }
}
