// (c) 2020 Manabu Tonosaki
// Licensed under the MIT license.

using System.ComponentModel;

namespace SyslogAzureMonitorBridge
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
