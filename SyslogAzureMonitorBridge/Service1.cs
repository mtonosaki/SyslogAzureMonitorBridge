using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Tono;

namespace SyslogAzureMonitorBridge
{
    public partial class SyslogAzureMonitorBridge : ServiceBase
    {
        private Dictionary<string, string> Params;
        private readonly CancellationTokenSource CancelHandler = new CancellationTokenSource();
        private readonly Queue<SyslogMessageEventArgs> Messages = new Queue<SyslogMessageEventArgs>();

        public SyslogAzureMonitorBridge()
        {
            InitializeComponent();
        }

        private AzureUploader uploader = null;
        private SyslogListener listener = null;

        protected override void OnStart(string[] args)
        {
            args = Environment.GetCommandLineArgs();
            if (ParseArgs(args))
            {
                eventlog($"Start SyslogAzureMonitorBridge. Listening UDP:{Params["/p"]}");
            }
            else
            {
                var mes = $"Starting Error SyslogAzureMonitorBridge. Need to set the all parameters. /n=LogName /p=PortNo /w=WorkspaceID of Azure LogAnalitycs /k=Key1";
                eventlog(mes, true);
                new Timer(prm =>
                {
                    Stop(); // Delay Service Stop when error.  (otherwise, Service Stop --> Start in Event log)
                }, null, 1000, Timeout.Infinite);
                return;
            }

            // Prepare Azure Monitor Http Uploader
            uploader = new AzureUploader
            {
                LogName = Params["/n"],
                WorkspaceID = Params["/w"],
                Key1 = Params["/k"],
                Messages = () => Messages,
                Logger = eventlog,
            };
            var task1 = uploader.PorlingMessagesAsync(CancelHandler.Token);

            // Prepare UDP listener
            listener = new SyslogListener
            {
                PortNo = int.Parse(Params["/p"]),
            };
            listener.OnMessage += Listener_OnMessage;
            listener.OnError += Listener_OnError;
            var task2 = listener.RunAsync(CancelHandler.Token);
        }

        private void Listener_OnError(object sender, SyslogErrorEventArgs e)
        {
            eventlog(e.Exception.Message);
            Stop();
        }


        private void Listener_OnMessage(object sender, SyslogMessageEventArgs e)
        {
            lock (Messages)
            {
                Messages.Enqueue(e);
            }
        }

        public void OnStartConsole(string[] args)
        {
            OnStart(args);

            for (; ; )
            {
                eventlog("To stop debugging, press stop button of Visual Studio");
                Task.Delay(10000).Wait();
            }
        }

        /// <summary>
        /// Parse command line
        /// </summary>
        /// <param name="args"></param>
        /// <returns>true=OK / false = Insufficient parameter setting.</returns>
        private bool ParseArgs(string[] args)
        {
            var prms = new[] { "/p", "/w", "/k", "/n" };
            Params = prms.ToDictionary(a => a);

            foreach (var arg in args)
            {
                foreach (var prm in prms)
                {
                    var key = prm + "=";
                    if (arg.StartsWith(key))
                    {
                        Params[prm] = StrUtil.MidSkip(arg, key);
                        break;
                    }
                }
            }
            foreach (var kv in Params)
            {
                if (kv.Key == kv.Value)
                {
                    return false;
                }
            }
            return true;
        }

        protected override void OnStop()
        {
            CancelHandler.Cancel();
            eventlog($"Stopped SyslogAzureMonitorBridge");
        }

        public void OnStopConsole()
        {
        }

        private void eventlog(string mes, bool isError = false)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine(mes);
            }
            else
            {
                eventLog.WriteEntry(mes, isError ? EventLogEntryType.Error : EventLogEntryType.Information);
            }
        }

    }
}
