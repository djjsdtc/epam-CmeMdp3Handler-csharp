// Sample 2: Getting all Security Definitions of Channel 311
// Mirrors the Java README "Getting all Security Definitions of Channel 311"

using System;
using System.Threading;
using Epam.CmeMdp3Handler;
using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.Sbe.Message;
using Microsoft.Extensions.Logging;

namespace Epam.CmeMdp3Handler.Samples
{
    public static class Sample2_PrintAllSecurities
    {
        private static readonly ILogger Logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Sample2");

        private static readonly IMdpGroup Group1141 = SbeGroup.Instance();
        private static readonly SbeString Tag1022Value = SbeString.Allocate(3);
        private static readonly SbeString Tag55Value = SbeString.Allocate(20);
        private static readonly SbeString Tag1151Value = SbeString.Allocate(6);
        private static readonly SbeString Tag6937Value = SbeString.Allocate(6);
        private static readonly SbeString Tag167Value = SbeString.Allocate(6);
        private static int _counter = 0;
        private static readonly object ResultIsReady = new object();

        private class ChannelListenerImpl : VoidChannelListener
        {
            public override void OnFeedStarted(string channelId, FeedType feedType, Feed feed)
                => Logger.LogInformation("Channel '{ChannelId}': {FeedType} feed {Feed} is started", channelId, feedType, feed);

            public override void OnFeedStopped(string channelId, FeedType feedType, Feed feed)
            {
                Logger.LogInformation("Channel '{ChannelId}': {FeedType} feed {Feed} is stopped", channelId, feedType, feed);
                lock (ResultIsReady) { Monitor.Pulse(ResultIsReady); }
            }

            public override void OnPacket(string channelId, FeedType feedType, Feed feed, MdpPacket mdpPacket)
                => Logger.LogInformation("Channel '{ChannelId}': {FeedType} feed {Feed} received MDP packet {Packet}",
                    channelId, feedType, feed, mdpPacket.ToString());

            public override int OnSecurityDefinition(string channelId, IMdpMessage mdpMessage)
            {
                int securityId = mdpMessage.GetInt32(48);
                Interlocked.Increment(ref _counter);
                Logger.LogInformation("Channel {ChannelId}'s security: {SecId}", channelId, securityId);

                mdpMessage.GetString(55, Tag55Value);
                Logger.LogInformation("   Symbol : {Value}", Tag55Value.GetString());

                mdpMessage.GetString(1151, Tag1151Value);
                Logger.LogInformation("   SecurityGroup : {Value}", Tag1151Value.GetString());

                mdpMessage.GetString(6937, Tag6937Value);
                Logger.LogInformation("   Asset : {Value}", Tag6937Value.GetString());

                mdpMessage.GetString(167, Tag167Value);
                Logger.LogInformation("   SecurityType : {Value}", Tag167Value.GetString());

                mdpMessage.GetGroup(1141, Group1141);
                while (Group1141.HasNext())
                {
                    Group1141.Next();
                    Group1141.GetString(1022, Tag1022Value);
                    int depth = Group1141.GetInt8(264);
                    Logger.LogInformation("   {FeedType} depth : {Depth}", Tag1022Value.GetString(), depth);
                }

                return MdEventFlags.NOTHING;
            }
        }

        public static void Run(string[] args)
        {
            try
            {
                var cfgUri = new Uri("file:///path/to/config.xml");
                var schemaUri = new Uri("file:///path/to/templates_FixBinary.xml");

                var mdpChannel311 = new MdpChannelBuilder("311", cfgUri, schemaUri)
                    .UsingListener(new ChannelListenerImpl())
                    .Build();

                mdpChannel311.StartInstrumentFeedA();

                lock (ResultIsReady) { Monitor.Wait(ResultIsReady); }

                Logger.LogInformation("Received security definitions: {Count}", _counter);
                mdpChannel311.Close();
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
            }
        }
    }
}
