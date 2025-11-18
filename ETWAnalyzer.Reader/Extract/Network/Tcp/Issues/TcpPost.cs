//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using ETWAnalyzer.Extractor.Tcp;
using System;
using static ETWAnalyzer.Extract.Network.Tcp.TcpStatistics;

namespace ETWAnalyzer.Extract.Network.Tcp.Issues
{
    /// <summary>
    /// 
    /// </summary>
    public class TcpPostIssue : ITcpPostIssue
    {
        /// <summary>
        /// 
        /// </summary>
        public TcpPost PreviousPosted { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TcpPost Injected { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TcpPost OutOfOrderPost { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ConnectionIdx ConnectionIdx { get; set; }

        ITcpPost ITcpPostIssue.PreviousPosted => PreviousPosted;

        ITcpPost ITcpPostIssue.Injected => Injected;

        ITcpPost ITcpPostIssue.OutOfOrderPost => OutOfOrderPost;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="extract"></param>
        /// <returns></returns>
        public ITcpConnection GetConnection(IETWExtract extract)
        {
            return extract.Network.TcpData.Connections[(int)ConnectionIdx];
        }

        /// <summary>
        /// Used by Json.NET serializer
        /// </summary>
        public TcpPostIssue()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="previousPost"></param>
        /// <param name="injected"></param>
        /// <param name="nextPosted"></param>
        /// <param name="connectionIdx"></param>
        internal TcpPostIssue(ITcpSendPosted previousPost, ITcpSendPosted injected, ITcpSendPosted nextPosted, ConnectionIdx connectionIdx)
        {
            PreviousPosted = new TcpPost()
            {
                InjectedReason = InjectReason.Posted,
                Time = previousPost.Timestamp,
                NumBytes = previousPost.NumBytes,
                SndNext = previousPost.SndNext,
            };

            Injected = new TcpPost()
            {
                InjectedReason = InjectReason.Injected,
                Time = injected.Timestamp,
                NumBytes = injected.NumBytes,
                SndNext = injected.SndNext
            };

            OutOfOrderPost = new TcpPost()
            {
                InjectedReason = InjectReason.Posted,
                Time = nextPosted.Timestamp,
                NumBytes = nextPosted.NumBytes,
                SndNext = nextPosted.SndNext
            };
            ConnectionIdx = connectionIdx;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class TcpPost : ITcpPost
    {
        /// <summary>
        /// Time of the event
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public InjectReason InjectedReason { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public uint NumBytes { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public uint SndNext { get; set; }
    }

    /// <summary>
    /// A firewall will drop packets which are posted and inject them later again potentially modified.
    /// </summary>
    public enum InjectReason
    {
        /// <summary>
        /// By default network packets issued by a socket send operation are posted to the TCP network stack.
        /// </summary>
        Posted,

        /// <summary>
        /// When such events exist they are usually the sign of a firewall inspecting packets where the previously posted packets are removed from TCP send queue and injected later.
        /// </summary>
        Injected,
    }

    /// <summary>
    /// Represents a TCP post operation, providing details about the reason for injection,  the number of bytes
    /// involved, and the next sequence number to send. 
    /// </summary>
    public interface ITcpPost
    {
        /// <summary>
        /// Time of the event
        /// </summary>
        DateTimeOffset Time { get; }

        /// <summary>
        /// Injection reason
        /// </summary>
        InjectReason InjectedReason { get; }

        /// <summary>
        /// Number of bytes to send
        /// </summary>
        uint NumBytes { get; }

        /// <summary>
        /// Sequence number at insert time when event was added. Multiple packets with same SndNext values can exist. 
        /// The actual sequence number will be determined by send TCP send event.
        /// </summary>
        uint SndNext { get; }
    }


    /// <summary>
    /// Heuristically detected issue where tcp data is sent out of socket send order.
    /// </summary>
    public interface ITcpPostIssue
    {
        /// <summary>
        /// Posted network packet which is swallwed and injected later via <see cref="Injected"/> again.
        /// </summary>
        ITcpPost PreviousPosted { get; }

        /// <summary>
        /// Injected network packet
        /// </summary>
        ITcpPost Injected { get; }

        /// <summary>
        /// Posted network packet which is posted after the previously previously posted packet but after the injected packet. Not corresponding
        /// Injected packet exists. If this such a packet is found it is a strong indication of network data corruption.
        /// </summary>
        ITcpPost OutOfOrderPost { get; }


        /// <summary>
        /// Connection index to <see cref="IETWExtract.Network"/>.Connections array.    
        /// </summary>
        ConnectionIdx ConnectionIdx { get; }

        /// <summary>
        /// Get actual connection object from extract.
        /// </summary>
        /// <param name="extract"></param>
        /// <returns>TCP connection</returns>
        ITcpConnection GetConnection(IETWExtract extract);
    }

}
