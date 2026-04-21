# Java to C# Conversion — Differences & Design Decisions

This document records every meaningful difference between the original Java library
(`java-cme-mdp3-handler`) and its C# port (`Epam.CmeMdp3Handler`).

---

## 1. Project & Build System

| Java | C# |
|------|----|
| Gradle multi-project build (`core`, `mbp-only`) | MSBuild solution with 3 projects: `Epam.CmeMdp3Handler.Core`, `Epam.CmeMdp3Handler.MbpOnly`, `Epam.CmeMdp3Handler.Samples` |
| Maven artifact coordinates (`com.epam.cme.mdp3`) | NuGet / project references; root namespace `Epam.CmeMdp3Handler` |
| JDK 8+ | .NET 8.0, `AllowUnsafeBlocks=true` |
| SLF4J + Log4j2 | `Microsoft.Extensions.Logging.Abstractions` (ILogger / LoggerFactory) |

---

## 2. Off-Heap Buffer → Managed Byte Array

**Java** uses `net.openhft.chronicle.bytes.NativeBytesStore<Void>` (off-heap direct
memory) together with `BytesStore<?,?>` / `Bytes<?>` for zero-copy SBE parsing.

**C#** uses a plain managed `byte[]` + `System.Buffers.Binary.BinaryPrimitives` for
all little-endian reads. The `ISbeBuffer` / `SbeBufferImpl` interface mirrors the Java
`SbeBuffer` interface but every read is `BinaryPrimitives.ReadInt16LittleEndian(span)` etc.
`IncrementalRefreshHolder` keeps a `byte[]` store that is grown on demand (replacing
`NativeBytesStore.nativeStoreWithFixedCapacity` + `store.release()`).

---

## 3. Primitive-Key Map: Koloboke → Dictionary

**Java** uses `net.openhft.koloboke.collect.map.hash.HashIntObjMaps.newMutableMap()`
which avoids boxing overhead for `int` keys.

**C#** uses `Dictionary<int, InstrumentController>`. Modern .NET JIT and GC make this
performant enough for this use case; no third-party collection library required.
All iteration over entries uses `foreach (var kv in _instruments)`.

---

## 4. JAXB / SAX Namespace Filter → XmlSerializer + NamespaceStrippingXmlReader

**Java** uses JAXB `Unmarshaller` with a custom SAX `NamespaceFilter` that strips the
`http://www.fixprotocol.org/ns/simple/1.0` namespace from the SBE schema XML so that
plain (unqualified) JAXB annotations on the VO classes can match.

**C#** uses `System.Xml.Serialization.XmlSerializer` with a custom
`NamespaceStrippingXmlReader : XmlTextReader` that overrides the `NamespaceURI` property
to return `""` for all elements, achieving the same effect.

---

## 5. Apache Commons Configuration → XmlDocument

**Java** uses `org.apache.commons.configuration.XMLConfiguration` to parse the CME
`config.xml` channel configuration file.

**C#** uses `System.Xml.XmlDocument` with manual XPath-style `SelectNodes` / `GetAttribute`
calls in `Configuration.cs`.

---

## 6. Interface Default Methods → Abstract Base Class

**Java** `VoidChannelListener` is an `interface` with `default` no-op implementations for
every method, so users only override what they need.

**C#** (targeting net8.0) supports Default Interface Methods (DIM) syntactically, but
DIM have subtle limitations (no state, accessibility restrictions). The C# port instead
uses an `abstract class VoidChannelListener` with `virtual` no-op implementations.
Users inherit from `VoidChannelListener` and override only what they need — identical
ergonomics to the Java version.

---

## 7. Java NIO DatagramChannel / Selector → .NET Socket

**Java** `MdpFeedWorker` uses `java.nio.channels.DatagramChannel`, `Selector`, and
`MembershipKey` for non-blocking UDP multicast. The worker calls `selector.select()`
in a tight loop and processes keys.

**C#** `MdpFeedWorker` uses `System.Net.Sockets.Socket` (UDP) with
`SetSocketOption(SocketOptionName.AddMembership, MulticastOption)` for multicast group
join, and a blocking `Socket.ReceiveFrom` call in a background thread. The cancellation
model (`_feedState` volatile int + `Interlocked.CompareExchange`) mirrors Java's
`AtomicReference<MdpFeedRtmState>`.

---

## 8. ScheduledExecutorService → System.Threading.Timer

**Java** `DefaultScheduledServiceHolder` exposes `Executors.newScheduledThreadPool(1)`.
The idle-check callback is registered with `scheduleWithFixedDelay(action, 100, 100, SECONDS)`.

**C#** `DefaultScheduledServiceHolder.SchedulerHolder.ScheduleWithFixedDelay` wraps a
`System.Threading.Timer` (`new Timer(_ => action(), null, initialDelayMs, periodMs)`).
The `IDisposable` returned can be disposed to cancel the timer; the channel `Close()`
method disposes the handle.

---

## 9. AtomicReference / AtomicInteger → volatile + Interlocked

| Java | C# |
|------|----|
| `AtomicReference<MdpFeedRtmState>` | `volatile int _feedState` + `Interlocked.CompareExchange` |
| `AtomicInteger msgCountDown` | `volatile int _msgCountDown` + `Interlocked.CompareExchange` / `Interlocked.Decrement` |

---

## 10. ReentrantLock → lock / Monitor

**Java** uses `java.util.concurrent.locks.ReentrantLock` (explicit `lock()` / `unlock()`).

**C#** uses the `lock (obj) { }` statement which maps to `Monitor.Enter` / `Monitor.Exit`,
and for handler-level locking (`AbstractMktDataHandler.Lock()`) an `object _lock` field
is used with `Monitor.Enter` / `Monitor.Exit` to keep the same explicit API shape.

---

## 11. Java Enum with Methods → Class with Static Instances

**Java** `SbePrimitiveType` is an `enum` with rich per-constant fields (`size`, `nullValue`,
`fromString`).

**C#** cannot have enum members with arbitrary instance data, so `SbePrimitiveType` is a
`sealed class` with `static readonly` instances (`Int8`, `UInt8`, `Int64` …) and a
`static Dictionary<string, SbePrimitiveType>` for `FromString(name)`.

---

## 12. Functional Interface → delegate

**Java** `EventCommitFunction` is a `@FunctionalInterface`:
```java
interface EventCommitFunction { void onCommit(int securityId); }
```
**C#** is replaced by a delegate:
```csharp
public delegate void EventCommitFunction(int securityId);
```

---

## 13. BigInteger in Schema VOs → int? / long?

**Java** schema VO fields whose XSD type is `xs:integer` (e.g. `minValue`, `maxValue`,
`nullValue` on `EncodedDataType`) are mapped to `java.math.BigInteger`.

**C#** maps them to `long?` (or `int?` for IDs), parsed via `long.TryParse` from the
string attribute value. This covers all practical SBE null-value ranges.

---

## 14. Interface Naming Convention

All Java interfaces (`FieldSet`, `MdpGroup`, `MdpMessage`, etc.) are prefixed with `I`
in C# (`IFieldSet`, `IMdpGroup`, `IMdpMessage`, `IMdpChannel`, …) following .NET conventions.

---

## 15. Package / Namespace Mapping

| Java package | C# namespace |
|---|---|
| `com.epam.cme.mdp3` | `Epam.CmeMdp3Handler` |
| `com.epam.cme.mdp3.core.cfg` | `Epam.CmeMdp3Handler.Cfg` |
| `com.epam.cme.mdp3.core.channel` | `Epam.CmeMdp3Handler.Channel` |
| `com.epam.cme.mdp3.core.control` | `Epam.CmeMdp3Handler.Control` |
| `com.epam.cme.mdp3.mktdata` | `Epam.CmeMdp3Handler.MktData` |
| `com.epam.cme.mdp3.mktdata.enums` | `Epam.CmeMdp3Handler.MktData.Enums` |
| `com.epam.cme.mdp3.sbe.message` | `Epam.CmeMdp3Handler.Sbe.Message` |
| `com.epam.cme.mdp3.sbe.message.meta` | `Epam.CmeMdp3Handler.Sbe.Message.Meta` |
| `com.epam.cme.mdp3.sbe.schema` | `Epam.CmeMdp3Handler.Sbe.Schema` |
| `com.epam.cme.mdp3.sbe.schema.vo` | `Epam.CmeMdp3Handler.Sbe.Schema.Vo` |
| `com.epam.cme.mdp3.service` | `Epam.CmeMdp3Handler.Service` |

---

## 16. Method Naming Convention

Java uses `camelCase` for methods. C# uses `PascalCase`. All method names have been
renamed accordingly (e.g. `getInt32(tag)` → `GetInt32(tag)`, `hashNext()` → `HasNext()`,
`getMsgSeqNum()` → `GetMsgSeqNum()`).

---

## 17. Deprecated Classes

`PacketHolder` and `PacketQueue` are marked `[Obsolete]` in C# (matching Java's
`@Deprecated`) and are not generated in the C# port because they are unused in the
current processing flow.

---

## 18. Incomplete Implementations

The following handlers are incomplete in the original Java source and remain so in C#:

- `StatisticsHandler.Clear()` — throws `NotSupportedException` (Java: `UnsupportedOperationException`)
- `StatisticsHandler.UpdateSettlementPrice()` — throws `NotSupportedException`
- `StatisticsHandler.UpdateThresholdLimitsAndPriceBandVariation()` — throws `NotSupportedException`
- `TradeHandler.UpdateTradeSummary()` — throws `NotSupportedException`
- `TradeHandler.UpdateElectronicVolume()` — throws `NotSupportedException`
- `TradeHandler.Clear()` — throws `NotSupportedException`

---

## 19. Sample Programs

Two sample programs from the Java README are included in `Epam.CmeMdp3Handler.Samples`:

| File | Description |
|------|-------------|
| `Sample1_LowLevelListener.cs` | Full low-level raw-packet listener for Channel 311 (all instruments) |
| `Sample2_PrintAllSecurities.cs` | Prints all security definitions for Channel 311 then exits |

Both samples require real `config.xml` and `templates_FixBinary.xml` files from the CME
certification or production environment, referenced via file URIs.

---

## 20. New Module: Epam.CmeMdp3Handler.MbpWithMbo

The Java `mbp-with-mbo` module has been converted to a new C# project
`Epam.CmeMdp3Handler.MbpWithMbo`. The following sections describe the differences
introduced in this module.

---

## 21. Project Structure (mbp-with-mbo)

| Java | C# |
|------|----|
| `com.epam.cme.mdp3` (top-level module classes) | `Epam.CmeMdp3Handler.MbpWithMbo` |
| `com.epam.cme.mdp3.control` | `Epam.CmeMdp3Handler.MbpWithMbo.Control` |
| `com.epam.cme.mdp3.core.channel.tcp` | `Epam.CmeMdp3Handler.Core.Channel.Tcp` (added to Core project) |
| Gradle sub-project `mbp-with-mbo` | MSBuild project `Epam.CmeMdp3Handler.MbpWithMbo.csproj` |

The TCP channel classes (`MdpTCPMessageRequester`, `MdpTCPChannel`, `ITCPMessageRequester`,
`ITCPPacketListener`, `ITCPChannel`) existed in the Java `core` module but were missing
from the initial C# conversion. They have been added to `Epam.CmeMdp3Handler.Core` under
`Channel/Tcp/`.

Namespace additions to `JAVA_TO_CSHARP_DIFFERENCES.md` section 15:

| Java package | C# namespace |
|---|---|
| `com.epam.cme.mdp3` (mbp-with-mbo root) | `Epam.CmeMdp3Handler.MbpWithMbo` |
| `com.epam.cme.mdp3.control` (mbp-with-mbo) | `Epam.CmeMdp3Handler.MbpWithMbo.Control` |
| `com.epam.cme.mdp3.core.channel.tcp` | `Epam.CmeMdp3Handler.Core.Channel.Tcp` |

---

## 22. Off-Heap Chronicle Bytes LongArray → Managed long[]

**Java** `OffHeapSnapshotCycleHandler` uses `net.openhft.chronicle.bytes.NativeBytesStore`
and Chronicle Bytes `LongArray` (allocated off-heap) to store per-security chunk tracking
arrays. The `Long2ObjectHashMap` from agrona is used as the container.

**C#** uses a plain managed `long[]` array wrapped in a private sealed inner class
`MutableLongToLongArrayPair` which replaces Java's generic `MutableLongToObjPair<LongArray>`.
`Dictionary<long, MutableLongToLongArrayPair>` replaces `Long2ObjectHashMap`. No off-heap
allocation is performed — the arrays are GC-managed. The behavior is functionally equivalent.

---

## 23. agrona IntHashSet → HashSet\<int\>

**Java** `ChannelControllerRouter` uses `org.agrona.collections.IntHashSet` (a
primitive-specialised set that avoids boxing) to track security IDs within a single packet.

**C#** uses `HashSet<int>`. Modern .NET's `HashSet<int>` has no boxing overhead for
value types; the functional and performance characteristics are equivalent.

---

## 24. Apache Commons MutablePair → ValueTuple

**Java** `LowLevelMdpChannel` uses `org.apache.commons.lang3.tuple.MutablePair<MdpFeedWorker, Thread>`
to associate each feed worker with its thread in the `_feeds` map.

**C#** uses `ValueTuple<MdpFeedWorker, Thread?>` stored in
`Dictionary<string, (MdpFeedWorker Worker, Thread? Thread)>`. The immutability concern is
addressed by replacing the pair entirely rather than mutating individual fields.

---

## 25. Java Consumer\<MdpMessage\> → Action\<IMdpMessage\>

**Java** `ChannelControllerRouter` accepts a `List<Consumer<MdpMessage>> emptyBookConsumers`
for empty-book notifications, following the Java `@FunctionalInterface` pattern.

**C#** uses `IList<Action<IMdpMessage>>`. The `LowLevelMdpChannel` passes
`_channelController.Accept` as a method-group delegate. `Action<T>` is the standard
.NET equivalent of Java's `Consumer<T>`.

---

## 26. Java Runnable (TCP Recovery) → ThreadPool.QueueUserWorkItem

**Java** `GapChannelController` submits TCP recovery work using
`_scheduledExecutorService.execute(tcpRecoveryProcessor)` where `TcpRecoveryProcessor`
implements `Runnable`.

**C#** uses `ThreadPool.QueueUserWorkItem(_ => _tcpRecoveryProcessor.Run())`. The
`ScheduledExecutorService` parameter was removed from the `GapChannelController`
constructor — the thread-pool submission is self-contained.

---

## 27. Java synchronized Method → lock Statement

**Java** `MdpTCPMessageRequester.askForLostMessages(...)` is declared `synchronized`,
giving it an implicit per-instance monitor lock.

**C#** `MdpTcpMessageRequester.AskForLostMessages(...)` uses an explicit
`private readonly object _lock = new object()` field and wraps the method body in
`lock (_lock) { }`. The behaviour is identical.

---

## 28. Java NIO SocketChannel → TcpClient / NetworkStream

**Java** `MdpTCPChannel` uses `java.nio.channels.SocketChannel` in blocking mode for
TCP replay feed connectivity.

**C#** `MdpTcpChannel` uses `System.Net.Sockets.TcpClient` with `NetworkStream` for
reads and writes. The `Connect()` / `Disconnect()` / `Send()` / `Receive()` contract of
`ITcpChannel` is identical to the Java `ITCPChannel` interface.

---

## 29. @Deprecated → [Obsolete]

**Java** `HeapSnapshotCycleHandler` is annotated `@Deprecated` (replaced by
`OffHeapSnapshotCycleHandler`).

**C#** `HeapSnapshotCycleHandler` is annotated `[Obsolete("Use OffHeapSnapshotCycleHandler instead.")]`,
the idiomatic .NET equivalent.

---

## 30. IMdpChannelController Default Methods

**Java** `MdpChannelController` is an `interface` with `default` method implementations
(`updateSemanticMsgType`, `isIncrementalMessageSupported`, `isIncrementOnlyForMbo`,
`isMboSnapshot`).

**C#** `IMdpChannelController` uses **C# 8 default interface methods** (DIM) for the same
four helpers. Unlike `VoidChannelListener` (section 6 above), these helpers have no state
and are small predicates — DIM are appropriate here. Concrete call sites use the explicit
interface cast `((IMdpChannelController)this).MethodName(...)` because the methods are
defined on the interface, not the class.

---

## 31. Sentinel Value Writing (MdpOffHeapBuffer)

**Java** `MDPOffHeapBuffer` writes `Integer.MAX_VALUE` as a sentinel sequence number
directly into a `NativeBytesStore` using `bytesStore.writeInt(offset, Integer.MAX_VALUE)`.

**C#** `MdpOffHeapBuffer` writes the sentinel via:
```csharp
BinaryPrimitives.WriteUInt32LittleEndian(
    sentinel.AsSpan(SbeConstants.MESSAGE_SEQ_NUM_OFFSET),
    (uint)UndefinedValue   // int.MaxValue cast to uint
);
```
and wraps the buffer with `MdpPacket.WrapForParse`. The value and byte-order are identical.

---

## 32. DecimalFormat → string.Format("F3")

**Java** `LowLevelMdpChannel.IncrementalStatistics` uses `java.text.DecimalFormat("#.###")`
to format the average gap rate.

**C#** uses `string.Format("{0:F3}", value)` (or `$"{value:F3}"`) for the same three-decimal
fixed-point formatting.

---

## 33. Sample Programs (mbp-with-mbo)

Two additional sample programs have been added to `Epam.CmeMdp3Handler.Samples` for the
mbp-with-mbo module:

| File | Description |
|------|-------------|
| `Sample3_MboLowLevelListener.cs` | Full MBO+MBP low-level listener for Channel 311 (README Sample 1 re-implemented for mbp-with-mbo API) |
| `Sample4_MboPrintAllSecurities.cs` | Prints all security definitions for Channel 311 using the mbp-with-mbo module (README Sample 2 re-implemented) |

The Java README Sample 1 shows an `onIncrementalMBORefresh` callback with a
`matchEventIndicator` parameter that does not match the actual Java interface definition.
The C# `Sample3` follows the actual `IChannelListener` interface signature (no
`matchEventIndicator` parameter).
