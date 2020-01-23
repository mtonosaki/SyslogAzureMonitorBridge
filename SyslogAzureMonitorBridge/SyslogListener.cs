using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyslogAzureMonitorBridge
{
    public class SyslogListener
    {
        public event EventHandler<SyslogMessageEventArgs> OnMessage;
        public event EventHandler<SyslogErrorEventArgs> OnError;
        public int PortNo { get; set; }

        /// <summary>
        /// Listen UDP syslog message
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                for (int retry = 0; retry < 5 && cancellationToken.IsCancellationRequested == false; retry--)
                {
                    try
                    {
                        var server = new UdpClient(new IPEndPoint(IPAddress.Any, PortNo));
                        while (cancellationToken.IsCancellationRequested == false)
                        {
                            try
                            {
                                IPEndPoint remoteEP = null;
                                var rcvBytes = server.Receive(ref remoteEP);    // BLOCK HERE
                                var rcvMsg = Encoding.UTF8.GetString(rcvBytes);
                                OnMessage?.Invoke(this, new SyslogMessageEventArgs
                                {
                                    Message = rcvMsg,
                                    Remote = remoteEP,
                                    EventUtcTime = DateTime.UtcNow,
                                });
                                retry = 0;
                            }
                            catch (Exception ex)
                            {
                                OnError?.Invoke(this, new SyslogErrorEventArgs
                                {
                                    Exception = ex,
                                });
                            }
                            Task.Delay(23, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(this, new SyslogErrorEventArgs
                        {
                            Exception = ex,
                        });
                    }
                    Task.Delay(333 * 2 ^ retry);
                }
            });
        }
    }
    public class SyslogMessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public IPEndPoint Remote { get; set; }
        public DateTime EventUtcTime { get; set; }
    }
    public class SyslogErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
    }
}
