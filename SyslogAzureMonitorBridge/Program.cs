using System;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

namespace SyslogAzureMonitorBridge
{
    internal static class Program
    {
        /// <summary>
        /// Program entry point
        /// </summary>
        /// <param name="args">
        /// -----
        /// /i : Register this service to Windows Service
        /// /u : Uninstall this service to Windows Service
        /// -----
        /// /n=MySyslog    : $table name. ex.MySyslog --＞ MySyslog_CL
        /// /p=514 : Listening port number of syslog
        /// /w=12345678-abcd-ef01-2345-123456789012  : Work Space ID of Azure LogAnalytics
        /// /k=99aXaEa1C4OXL/EJVBqDH87qF9fHYE4XfGpKnJO/UCPv3xg/n6pAb7k6wSKFE+i40BSRuE0Apo3oP2W8G9nWXr==   : Main key (ARM -> LogAnalytics -> Detail -> Connected Sources -> Windows Servers -> Key1
        /// </param>
        private static void Main(string[] args)
        {
            var service = new SyslogAzureMonitorBridge();

            if (Environment.UserInteractive)    // Console Version
            {
                if (args.Length > 0)
                {
                    var isServiceExists = ServiceController.GetServices().Any(s => s.ServiceName == service.ServiceName);
                    var path = Assembly.GetExecutingAssembly().Location;
                    switch (args[0].ToLower())
                    {
                        case "/i":
                            if (isServiceExists)
                            {
                                Console.WriteLine($"The service '{service.ServiceName}' has already registered.");
                            }
                            else
                            {
                                ManagedInstallerClass.InstallHelper(new[] { path });
                            }
                            return;
                        case "/u":
                            if (isServiceExists)
                            {
                                ManagedInstallerClass.InstallHelper(new[] { "/u", path });
                            }
                            else
                            {
                                Console.WriteLine($"The service '{service.ServiceName}' is not installed yet.");
                            }
                            return;
                    }
                }
                service.OnStartConsole(args);
                service.OnStopConsole();
            }
            else
            {
                ServiceBase.Run(new ServiceBase[] { service });
            }
        }
    }
}
