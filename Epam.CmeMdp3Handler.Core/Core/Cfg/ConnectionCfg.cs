using System.Collections.Generic;

namespace Epam.CmeMdp3Handler.Core.Cfg
{
    /// <summary>
    /// CME MDP Channel's Connection Configuration.
    /// </summary>
    public class ConnectionCfg
    {
        public Feed Feed { get; }
        public string Id { get; }
        public FeedType FeedType { get; }
        public TransportProtocol Protocol { get; }
        public string Ip { get; }
        public List<string> HostIPs { get; }
        public int Port { get; }

        private readonly string _fullDesc;

        public ConnectionCfg(Feed feed, string id, FeedType feedType, TransportProtocol protocol,
            string ip, List<string> hostIPs, int port)
        {
            Feed = feed;
            Id = id;
            FeedType = feedType;
            Protocol = protocol;
            Ip = ip;
            HostIPs = hostIPs;
            Port = port;
            _fullDesc = $"ConnectionCfg{{feed={feed}, id='{id}', feedType={feedType}, protocol={protocol}, ip='{ip}', hostIPs=[{string.Join(",", hostIPs)}], port={port}}}";
        }

        public override string ToString() => _fullDesc;
    }
}
