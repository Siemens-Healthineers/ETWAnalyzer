//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using Microsoft.Windows.EventTracing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Network.Tcp
{

    /// <summary>
    /// Serializable socket connection class
    /// </summary>
    public class SocketConnection
    {
        /// <summary>
        /// IPV4 e.g. 10.81.1.34 or IPV6 e.g. [::1] address
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// Port number > 0 
        /// </summary>
        public int Port { get; }


        /// <summary>
        /// Needed to deserialize private setters
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        [JsonConstructor]
        public SocketConnection(string address, int port)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException($"{nameof(address)} is null or empty.");
            }

            if (port == 0)
            {
                throw new ArgumentNullException(nameof(port));
            }

            Address = address;
            Port = port;
        }

        /// <summary>
        /// Convert an IPEndpoint to SocketConnection
        /// </summary>
        /// <param name="ipEndPoint"></param>
        public SocketConnection(IPEndPoint ipEndPoint)
        {
            if (ipEndPoint == null)
            {
                throw new ArgumentNullException(nameof(ipEndPoint));
            }

            Port = ipEndPoint.Port;
            Address = ipEndPoint.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{ipEndPoint.Address}]" : ipEndPoint.Address.ToString();
        }

        /// <summary>
        /// Stringify object and show in debugger values
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Address}:{Port}";
        }
    }

}
