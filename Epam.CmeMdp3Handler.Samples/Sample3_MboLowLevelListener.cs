// Sample 3: Full low-level MBO+MBP listener subscription example (Channel 311, all instruments)
// Mirrors the Java README "Full low level listener subscription example" using the mbp-with-mbo module.
//
// Java original:
//   new MdpChannelBuilder("311", cfgURI, schemaURI)
//       .usingListener(new ChannelListenerImpl())
//       .usingGapThreshold(3)
//       .setMBOEnable(true)
//       .build();
//
// Notable differences from Java:
//   - ChannelListener is IChannelListener; VoidChannelListener is abstract base class.
//   - onIncrementalMBORefresh/onIncrementalMBPRefresh renamed to OnIncrementalMBORefresh/OnIncrementalMBPRefresh.
//   - setMBOEnable -> SetMboEnable.
//   - Logger is ILogger from Microsoft.Extensions.Logging (replaces SLF4J).
//   - mdpChannel311.startFeed(FeedType.SMBO, ...) -> mdpChannel311.StartFeed(FeedType.SMBO, ...).

using System;
using Epam.CmeMdp3Handler;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.MbpWithMbo;
using Epam.CmeMdp3Handler.MbpWithMbo.Channel;
using Microsoft.Extensions.Logging;

namespace Epam.CmeMdp3Handler.Samples
{
    public static class Sample3_MboLowLevelListener
    {
        private static readonly ILogger Logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Sample3");

        private class ChannelListenerImpl : Epam.CmeMdp3Handler.MbpWithMbo.VoidChannelListener
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
                => Logger.LogInformation("Channel '{ChannelId}' state is changed from '{Prev}' to '{New}'", channelId, prevState, newState);

            public override int OnSecurityDefinition(string channelId, IMdpMessage mdpMessage)
            {
                Logger.LogInformation("Received SecurityDefinition(d). Schema Id: {SchemaId}", mdpMessage.GetSchemaId());
                return MdEventFlags.MESSAGE;
            }

            public override void OnIncrementalMBORefresh(IMdpMessage mdpMessage, string channelId, int securityId,
                string? secDesc, long msgSeqNum, IFieldSet orderEntry, IFieldSet? mdEntry)
            {
                Logger.LogInformation("[{SeqNum}] OnIncrementalMBORefresh: ChannelId: {ChannelId}, SecurityId: {SecId}-{Desc}, OrderId: {OrderId}",
                    msgSeqNum, channelId, securityId, secDesc, orderEntry.GetUInt64(37));
            }

            public override void OnIncrementalMBPRefresh(IMdpMessage mdpMessage, string channelId, int securityId,
                string? secDesc, long msgSeqNum, IFieldSet mdEntry)
            {
                Logger.LogInformation("[{SeqNum}] OnIncrementalMBPRefresh: SecurityId: {SecId}-{Desc}. RptSeqNum(83): {Rpt}",
                    msgSeqNum, securityId, secDesc, mdEntry.GetUInt32(83));
            }

            public override void OnSnapshotMBOFullRefresh(string channelId, string? secDesc, IMdpMessage snptMessage)
                => Logger.LogInformation("OnMBOFullRefresh: ChannelId: {ChannelId}, SecurityId: {SecId}-{Desc}.",
                    channelId, snptMessage.GetInt32(48), secDesc);

            public override void OnSnapshotMBPFullRefresh(string channelId, string? secDesc, IMdpMessage snptMessage)
                => Logger.LogInformation("OnMBPFullRefresh: SecurityId: {SecId}-{Desc}. RptSeqNum(83): {Rpt}",
                    snptMessage.GetInt32(48), secDesc, snptMessage.GetUInt32(83));

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
                    .SetMboEnable(true)
                    .Build();

                mdpChannel311.StartFeed(FeedType.N, Feed.A);
                mdpChannel311.StartFeed(FeedType.I, Feed.A);
                mdpChannel311.StartFeed(FeedType.I, Feed.B);
                mdpChannel311.StartFeed(FeedType.SMBO, Feed.A);

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
