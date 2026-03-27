using System.Collections.Generic;

namespace Epam.CmeMdp3Handler.Core.Cfg
{
    /// <summary>
    /// Holder of CME MDP Channel Configurations.
    /// </summary>
    public class ChannelCfg
    {
        public string Id { get; }
        public string Label { get; }
        private readonly List<ConnectionCfg> _connections = new();

        public ChannelCfg(string id, string label)
        {
            Id = id;
            Label = label;
        }

        /// <summary>Adds a connection configuration to this channel.</summary>
        /// <param name="cfg">The connection configuration to add</param>
        public void AddConnection(ConnectionCfg cfg) => _connections.Add(cfg);

        /// <summary>Returns the connection configuration for the given feed type and feed side.</summary>
        /// <param name="feedType">The type of feed (Incremental, Snapshot, Instrument)</param>
        /// <param name="feed">Feed A or B</param>
        /// <returns>The matching connection configuration, or null if not found</returns>
        public ConnectionCfg? GetConnectionCfg(FeedType feedType, Feed feed)
        {
            foreach (var cfg in _connections)
                if (cfg.Feed == feed && cfg.FeedType == feedType) return cfg;
            return null;
        }
    }
}
