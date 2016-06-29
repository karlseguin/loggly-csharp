using System;
using System.Collections.Generic;
using System.Linq;
using Loggly.Config;
using Loggly.Transports.Syslog;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Loggly
{

    public class LogglyClient : ILogglyClient
    {
        public LogglyClient() {}

        public LogglyClient(string customerToken, string appName = null) {
            LogglyConfig.Instance.CustomerToken = customerToken;
            if (appName != null) {
                LogglyConfig.Instance.ApplicationName = appName;
            }
        }

        public async Task<LogResponse> Log(LogglyEvent logglyEvent)
        {
            return await LogWorker(new [] {logglyEvent}).ConfigureAwait(false);
        }

        public async Task<LogResponse> Log(IEnumerable<LogglyEvent> logglyEvents)
        {
            return await LogWorker(logglyEvents.ToArray()).ConfigureAwait(false);
        }

        private async Task<LogResponse> LogWorker(LogglyEvent[] events)
        {
            var response = new LogResponse {Code = ResponseCode.Unknown};
            try
            {
                if (LogglyConfig.Instance.IsEnabled)
                {
                    if (LogglyConfig.Instance.Transport.LogTransport == LogTransport.Https)
                    {
						if (!LogglyConfig.Instance.Transport.IsOmitTimestamp)
						{
							foreach (var e in events)
							{
								// syslog has this data in the header, only need to add it for Http
								e.Data.AddIfAbsent("timestamp", e.Timestamp);
							}
						}
                    }
                    
                    IMessageTransport transporter = TransportFactory();
                    response = await transporter.Send(events.Select(x => new LogglyMessage
                    {
                        Timestamp = x.Timestamp,
                        Syslog = x.Syslog,
                        Type = MessageType.Json,
                        Content = ToJson(x.Data),
                        CustomTags = x.Options.Tags
                    })).ConfigureAwait(false);
                }
                else
                {
                    response = new LogResponse {Code = ResponseCode.SendDisabled};
                }
            }
            catch (Exception e)
            {
                LogglyException.Throw(e);
            }
            return response;
        }

        private static string ToJson(object value)
        {
            return JsonConvert.SerializeObject(value, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
            });
        }
        
        private IMessageTransport TransportFactory()
        {
            var transport = LogglyConfig.Instance.Transport.LogTransport;
            switch (transport)
            {
                case LogTransport.Https: return new HttpMessageTransport();
                case LogTransport.SyslogUdp: return new SyslogUdpTransport();
                case LogTransport.SyslogTcp: return new SyslogTcpTransport();
                case LogTransport.SyslogSecure: return new SyslogSecureTransport();
                default: throw new NotSupportedException("Unsupported transport: " + transport);
            }
        }
    }
}