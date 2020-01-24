// (c) 2020 Manabu Tonosaki
// Licensed under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tono;

namespace SyslogAzureMonitorBridge
{
    public class AzureUploader
    {
        public string WorkspaceID { get; set; }
        public string Key1 { get; set; }
        public string LogName { get; set; } = "Syslog";
        public Action<string, bool> Logger { get; set; }

        public Func<Queue<SyslogMessageEventArgs>> Messages { get; set; }

        public async Task PorlingMessagesAsync(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await Task.Delay(10000, cancellationToken); // upload 10s each to reduce request count to Azure.

                var queue = Messages?.Invoke();
                List<SyslogMessageEventArgs> chunk;

                lock (queue)
                {
                    chunk = queue.ToList();
                    queue.Clear();
                }
                if (chunk.Count < 1)
                {
                    continue;
                }
                var recs = new List<LogRecord>();
                foreach (var ev in chunk)
                {
                    // <189>[INSPECT] LAN2[out][101100] TCP 192.168.20.3:54874 > 172.217.25.193:443 (2020/01/23 14:12:42)
                    // <190>[NAT(1000):LAN2] Released UDP 172.16.20.3.44249 <-> 192.168.20.254.10708 ==> 172.16.20.1.53
                    var rec = new LogRecord();
                    var pristr = StrUtil.LeftOn(ev.Message, "^<[0-9]+>");
                    if (pristr.Length > 2)
                    {
                        // Priority = facility * 8 + severity level   ex. 190 = 23*8+6
                        var pri = int.Parse(StrUtil.Mid(pristr, 1, pristr.Length - 2));
                        rec.Facility = Facilities.GetValueOrDefault(pri / 8, "n/a");
                        rec.SeverityLevel = SeverityLevels.GetValueOrDefault(pri % 8, "n/a");
                    }
                    rec.EventTime = ev.EventUtcTime;
                    rec.HostIP = ev.Remote.Address.ToString();
                    rec.HostName = /* Dns.GetHostEntry(ev.Remote.Address)?.HostName ?? */ ev.Remote.Address.ToString(); // Do not use Dns.GetHostEntry because of block 5 seconds each for local IPs.
                    rec.Computer = rec.HostName;
                    rec.SyslogMessage = StrUtil.MidSkip(ev.Message, "^<[0-9]+>").TrimStart(' ', '\t', '\r', '\n', '\b');
                    recs.Add(rec);
                }
                var jsonStr = JsonConvert.SerializeObject(recs, new IsoDateTimeConverter());
                var datestring = DateTime.UtcNow.ToString("r");
                var jsonBytes = Encoding.UTF8.GetBytes(jsonStr);
                var stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";
                var hashedString = BuildSignature(stringToHash, Key1);
                var signature = "SharedKey " + WorkspaceID + ":" + hashedString;
                PostData(signature, datestring, jsonStr);
            }
        }

        public string BuildSignature(string message, string secret)
        {
            var encoding = new ASCIIEncoding();
            var keyByte = Convert.FromBase64String(secret);
            var messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                var hash = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
        }

        private static HttpClient client = new HttpClient();

        public void PostData(string signature, string date, string json)
        {
            try
            {
                var url = "https://" + WorkspaceID + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Log-Type", LogName);
                client.DefaultRequestHeaders.Add("Authorization", signature);
                client.DefaultRequestHeaders.Add("x-ms-date", date);
                client.DefaultRequestHeaders.Add("time-generated-field", "");

                var httpContent = new StringContent(json, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = client.PostAsync(new Uri(url), httpContent);

                var responseContent = response.Result.Content;
                string result = responseContent.ReadAsStringAsync().Result;
            }
            catch (Exception excep)
            {
                Logger?.Invoke("API Post Exception: " + excep.Message, true);
            }
        }

        public static readonly Dictionary<int, string> Facilities = new Dictionary<int, string>
        {
            [0] = "kern",
            [1] = "user",
            [2] = "mail",
            [3] = "daemon",
            [4] = "auth",
            [5] = "syslog",
            [6] = "lpr",
            [7] = "news",
            [8] = "uucp",
            [9] = "cron",
            [10] = "authpriv",
            [11] = "ftp",
            [12] = "ntp",
            [13] = "audit",  // log audit
            [14] = "alert",  // log alert
            [15] = "clock",  // clock daemon
            [16] = "local0",
            [17] = "local1",
            [18] = "local2",
            [19] = "local3",
            [20] = "local4",
            [21] = "local5",
            [22] = "local6",
            [23] = "local7",
        };

        public static readonly Dictionary<int, string> SeverityLevels = new Dictionary<int, string>
        {
            [0] = "emerg",
            [1] = "alert",
            [2] = "crit",
            [3] = "err",
            [4] = "warning",
            [5] = "notice",
            [6] = "info",
            [7] = "debug",
        };

        public class LogRecord
        {
            public string Facility { get; set; }
            public string SeverityLevel { get; set; }
            public DateTime EventTime { get; set; } // 2020-01-21T22:33:33Z
            public string Computer { get; set; }
            public string HostIP { get; set; }
            public string HostName { get; set; }
            public string SyslogMessage { get; set; }

        };
    }
}
