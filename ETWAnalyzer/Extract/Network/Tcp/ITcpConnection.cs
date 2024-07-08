//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.Network.Tcp
{
    /// <summary>
    /// One TCP connection which gets all summary data how many packets were sent/received. When connection was created/closed and to which host/port the connection was established.
    /// </summary>
    public interface ITcpConnection
    {
        /// <summary>
        /// Received data over duration of trace
        /// </summary>
        ulong BytesReceived { get; }

        /// <summary>
        /// Sent data over duration of trace
        /// </summary>
        ulong BytesSent { get; }

        /// <summary>
        /// Number of received network packets
        /// </summary>
        int DatagramsReceived { get; }

        /// <summary>
        /// Number of sent datagrams
        /// </summary>
        int DatagramsSent { get; }

        /// <summary>
        /// Last applied TCP template which defines RTO and many other TCP parameters. 
        /// </summary>
        string LastTcpTemplate { get; }

        /// <summary>
        /// Local IP and port
        /// </summary>
        SocketConnection LocalIpAndPort { get; }

        /// <summary>
        /// Index to <see cref="IETWExtract.Processes"/> array or Invalid if no matching process could be found.
        /// </summary>
        ETWProcessIndex ProcessIdx { get; }

        /// <summary>
        /// Remote IP and port
        /// </summary>
        SocketConnection RemoteIpAndPort { get; }

        /// <summary>
        /// Transmission Control Block kernel address which is used by other events are correlation identifier for a socket connection.
        /// </summary>
        ulong Tcb { get; }

        /// <summary>
        /// When connection as closed
        /// </summary>
        DateTimeOffset? TimeStampClose { get; }

        /// <summary>
        /// When connection was created
        /// </summary>
        DateTimeOffset? TimeStampOpen { get; }

        /// <summary>
        /// Time when data was last sent
        /// </summary>
        public DateTimeOffset? LastSent { get; }

        /// <summary>
        /// Time when data was last received
        /// </summary>
        public DateTimeOffset? LastReceived { get; }

        /// <summary>
        /// Time when connection was closed due to retransmission timeout
        /// </summary>
        public DateTimeOffset? RetransmitTimeout { get; }

        /// <summary>
        /// Check if connection was active at a given time.
        /// </summary>
        /// <param name="tcb">TCB pointer value of etw event which uses it to correlate events.</param>
        /// <param name="time">Time of event to find when this TCB value was part of an open TCP connection.</param>
        /// <returns>true if connection with given TCB wsa open at this time. False otherwise.</returns>
        bool IsMatching(ulong tcb, DateTimeOffset time);
    }
}