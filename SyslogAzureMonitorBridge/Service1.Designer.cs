// (c) 2020 Manabu Tonosaki
// Licensed under the MIT license.

namespace SyslogAzureMonitorBridge
{
    partial class SyslogAzureMonitorBridge
    {
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Cleanup resources for dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.eventLog = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).BeginInit();
            // 
            // eventLog
            // 
            this.eventLog.Log = "Application";
            this.eventLog.Source = "SyslogAzureMonitorBridge";
            // 
            // SyslogAzureMonitorBridge
            // 
            this.ServiceName = "SyslogAzureMonitorBridge";
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).EndInit();

        }

        private System.Diagnostics.EventLog eventLog;
    }
}
