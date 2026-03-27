// Sample 1: Full low-level listener subscription example (Channel 311, all instruments)
// Mirrors the Java README "Full low level listener subscription example"

using System;
using Epam.CmeMdp3Handler;
using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.Sbe.Message;
using Microsoft.Extensions.Logging;

namespace Epam.CmeMdp3Handler.Samples
{
    public static class Sample1_LowLevelListener
    {
        private static readonly ILogger Logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Sample1");

        private class ChannelListenerImpl : VoidChannelListener
        {
            public override void OnFeedStarted(string channelId, FeedType feedType, Feed feed)
                => Logger.LogInformation("Channel '{ChannelId}': {FeedType} feed {Feed} is started", channelId, feedType, feed);

            public override void OnFeedStopped(string channelId, FeedType feedType, Feed feed)
                => Logger.LogInformation("Channel '{ChannelId}': {FeedType} feed {Feed} is stopped", channelId, feedType, feed);

            public override void OnPacket(string channelId, FeedType feedType, Feed feed, MdpPacket mdpPacket)
                => Logger.LogInformation("{FeedType} {Feed}: {Packet}", feedType, feed, mdpPacket.ToString());

            public override void OnBeforeChannelReset(string channelId, IMdpMessage resetMessage)
                => Logger.LogInformation("Channel '{ChannelId}' is broken, all books should be restored", channelId);

            public override void OnFinishedChannelReset(string channelId, IMdpMessage resetMessage)
                => Logger.LogInformation("Channel '{ChannelId}' has been reset and restored", channelId);

            public override void OnChannelStateChanged(string channelId, ChannelState prevState, ChannelState newState)
                => Logger.LogInformation("Channel '{ChannelId}' state changed from '{Prev}' to '{New}'", channelId, prevState, newState);

            public override int OnSecurityDefinition(string channelId, IMdpMessage mdpMessage)
            {
                Logger.LogInformation("Received SecurityDefinition. Schema Id: {SchemaId}", mdpMessage.GetSchemaId());
                return MdEventFlags.MESSAGE;
            }

            public override void OnIncrementalRefresh(string channelId, short matchEventIndicator,
                int securityId, string? secDesc, long msgSeqNum, IFieldSet mdEntry)
            {
                Logger.LogInformation("[{SeqNum}] OnIncrementalRefresh: ChannelId: {ChannelId}, SecurityId: {SecId}-{Desc}, RptSeqNum(83): {Rpt}",
                    msgSeqNum, channelId, securityId, secDesc, mdEntry.GetUInt32(83));
            }

            public override void OnSnapshotFullRefresh(string channelId, string? secDesc, IMdpMessage snptMessage)
            {
                Logger.LogInformation("OnSnapshotFullRefresh: ChannelId: {ChannelId}, SecurityId: {SecId}-{Desc}. RptSeqNum(83): {Rpt}",
                    channelId, snptMessage.GetInt32(48), secDesc, snptMessage.GetUInt32(83));
            }

            public override void OnRequestForQuote(string channelId, IMdpMessage rfqMessage)
                => Logger.LogInformation("OnRequestForQuote");

            public override void OnSecurityStatus(string channelId, int securityId, IMdpMessage secStatusMessage)
                => Logger.LogInformation("OnSecurityStatus. SecurityId: {SecId}, RptSeqNum(83): {Rpt}",
                    securityId, secStatusMessage.GetUInt32(83));
        }

        public static void Run(string[] args)
        {
            try
            {
                var cfgUri = new Uri("file:///path/to/config.xml");
                var schemaUri = new Uri("file:///path/to/templates_FixBinary.xml");

                var mdpChannel311 = new MdpChannelBuilder("311", cfgUri, schemaUri)
                    .UsingListener(new ChannelListenerImpl())
                    .UsingGapThreshold(3)
                    .Build();

                mdpChannel311.StartInstrumentFeedA();
                mdpChannel311.StartIncrementalFeedA();
                mdpChannel311.StartIncrementalFeedB();

                Console.WriteLine("Press Enter to shutdown.");
                Console.ReadLine();
                mdpChannel311.Close();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}
