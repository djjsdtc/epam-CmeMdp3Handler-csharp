using System;
using System.Collections.Generic;
using System.Xml;

namespace Epam.CmeMdp3Handler.Core.Cfg
{
    /// <summary>
    /// CME MDP Configuration.
    /// Parses CME config.xml using System.Xml.XmlDocument.
    /// Replaces Java's Apache Commons XMLConfiguration.
    /// </summary>
    public class Configuration
    {
        private readonly Dictionary<string, ChannelCfg> _channelCfgs = new();
        private readonly Dictionary<string, ConnectionCfg> _connCfgs = new();

        public Configuration(Uri uri)
        {
            Load(uri);
        }

        private void Load(Uri uri)
        {
            var doc = new XmlDocument();
            doc.Load(uri.ToString());

            var channels = doc.SelectNodes("//channel");
            if (channels == null) return;

            foreach (XmlNode channelNode in channels)
            {
                string id    = channelNode.Attributes?["id"]?.Value ?? "";
                string label = channelNode.Attributes?["label"]?.Value ?? "";
                var channel  = new ChannelCfg(id, label);

                var connections = channelNode.SelectNodes("connections/connection");
                if (connections != null)
                {
                    foreach (XmlNode connNode in connections)
                    {
                        string connId   = connNode.Attributes?["id"]?.Value ?? "";
                        string typeStr  = connNode.SelectSingleNode("type/@feed-type")?.Value ?? "";
                        string protStr  = connNode.SelectSingleNode("protocol")?.InnerText ?? "";
                        string feedStr  = connNode.SelectSingleNode("feed")?.InnerText ?? "";
                        string ip       = connNode.SelectSingleNode("ip")?.InnerText ?? "";
                        int port        = int.Parse(connNode.SelectSingleNode("port")?.InnerText ?? "0");

                        // Extract just the first 3 chars of protocol (e.g. "UDP/IP" -> "UDP")
                        string proto3 = protStr.Length >= 3 ? protStr.Substring(0, 3) : protStr;

                        var feedType = Enum.Parse<FeedType>(typeStr);
                        var protocol = Enum.Parse<TransportProtocol>(proto3);
                        var feed     = Enum.Parse<Feed>(feedStr);

                        var hostIPs  = new List<string>();
                        var hostIPNodes = connNode.SelectNodes("host-ip");
                        if (hostIPNodes != null)
                            foreach (XmlNode hip in hostIPNodes)
                                hostIPs.Add(hip.InnerText);

                        var conn = new ConnectionCfg(feed, connId, feedType, protocol, ip, hostIPs, port);
                        channel.AddConnection(conn);
                        _connCfgs[conn.Id] = conn;
                    }
                }
                _channelCfgs[channel.Id] = channel;
            }
        }

        // todo: add support for multiple configuration sources

        /// <summary>Returns the channel configuration for the given channel ID.</summary>
        /// <param name="id">Channel ID</param>
        public ChannelCfg? GetChannel(string id) =>
            _channelCfgs.TryGetValue(id, out var ch) ? ch : null;

        /// <summary>Returns the connection configuration for the given connection ID.</summary>
        /// <param name="id">Connection ID</param>
        public ConnectionCfg? GetConnection(string id) =>
            _connCfgs.TryGetValue(id, out var conn) ? conn : null;
    }
}
