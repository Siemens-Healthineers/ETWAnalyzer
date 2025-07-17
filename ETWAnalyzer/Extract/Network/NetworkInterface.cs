//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;

namespace ETWAnalyzer.Extract.Network
{
    /// <summary>
    /// Represents a network interface with properties for IP addresses, DNS server addresses, NIC description, physical address, and indices for IPv4 and IPv6.
    /// </summary>
    public class NetworkInterface : INetworkInterface, IEquatable<NetworkInterface>
    {
        /// <summary>
        /// DNS server addresses used by the network interface, formatted as a semicolon-separated string.
        /// </summary>
        public string DnsServerAddresses { get; set; }

        /// <summary>
        /// IP addresses assigned to the network interface, formatted as a semicolon-separated string.
        /// </summary>
        public string IpAddresses { get; set; }

        /// <summary>
        /// Adapter index for IPv4 NIC. The adapter index may change when an adapter is disabled and then enabled, or under other circumstances, and should not be considered persistent.
        /// </summary>
        public int IPv4Index { get; set; }

        /// <summary>
        /// Adapter index for IPv6 NIC. The adapter index may change when an adapter is disabled and then enabled, or under other circumstances, and should not be considered persistent.
        /// </summary>
        public int IPv6Index { get; set; }

        /// <summary>
        /// Description of the network interface card (NIC), typically the name or model of the NIC.
        /// </summary>
        public string NicDescription { get; set; }

        /// <summary>
        /// Physical address (MAC address) of the network interface, represented as a string.
        /// </summary>
        public string PhysicalAddress { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkInterface"/> class with specified properties.
        /// </summary>
        /// <param name="ipAddresses"></param>
        /// <param name="dnsServerAddresses"></param>
        /// <param name="nicDescription"></param>
        /// <param name="physicalAddress"></param>
        /// <param name="iPv4Index"></param>
        /// <param name="iPv6Index"></param>
        public NetworkInterface(string ipAddresses, string dnsServerAddresses, string nicDescription, string physicalAddress, int iPv4Index, int iPv6Index)
        {
            IpAddresses = ipAddresses;
            DnsServerAddresses = dnsServerAddresses;
            NicDescription = nicDescription;
            PhysicalAddress = physicalAddress;
            IPv4Index = iPv4Index;
            IPv6Index = iPv6Index;
        }

        /// <summary>
        /// Determines whether the specified <see cref="NetworkInterface"/> is equal to the current <see cref="NetworkInterface"/>.
        /// </summary>
        /// <param name="other">The <see cref="NetworkInterface"/> to compare with the current <see cref="NetworkInterface"/>.</param>
        /// <returns>true if the specified <see cref="NetworkInterface"/> is equal to the current <see cref="NetworkInterface"/>; otherwise, false.</returns>
        public bool Equals(NetworkInterface other)
        {
            if (ReferenceEquals(other, null)) return false;
            return DnsServerAddresses == other.DnsServerAddresses &&
                   IpAddresses == other.IpAddresses &&
                   NicDescription == other.NicDescription &&
                   PhysicalAddress == other.PhysicalAddress &&
                   IPv4Index == other.IPv4Index &&
                   IPv6Index == other.IPv6Index;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="NetworkInterface"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current <see cref="NetworkInterface"/>.</param>
        /// <returns>true if the specified object is equal to the current <see cref="NetworkInterface"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as NetworkInterface);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (DnsServerAddresses != null ? DnsServerAddresses.GetHashCode() : 0);
                hash = hash * 23 + (IpAddresses != null ? IpAddresses.GetHashCode() : 0);
                hash = hash * 23 + (NicDescription != null ? NicDescription.GetHashCode() : 0);
                hash = hash * 23 + (PhysicalAddress != null ? PhysicalAddress.GetHashCode() : 0);
                hash = hash * 23 + IPv4Index.GetHashCode();
                hash = hash * 23 + IPv6Index.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>
    /// Represents a network interface with properties for IP addresses, DNS server addresses, NIC description, physical address, and indices for IPv4 and IPv6.
    /// </summary>
    public interface INetworkInterface
    {
        /// <summary>
        /// DNS server addresses used by the network interface, formatted as a semicolon-separated string.
        /// </summary>
        string DnsServerAddresses { get; }

        /// <summary>
        /// IP addresses assigned to the network interface, formatted as a semicolon-separated string.
        /// </summary>
        string IpAddresses { get; }

        /// <summary>
        /// Adapter index for IPv4 NIC. The adapter index may change when an adapter is disabled and then enabled, or under other circumstances, and should not be considered persistent.
        /// </summary>
        int IPv4Index { get; }

        /// <summary>
        /// Adapter index for IPv6 NIC. The adapter index may change when an adapter is disabled and then enabled, or under other circumstances, and should not be considered persistent.
        /// </summary>
        int IPv6Index { get; }

        /// <summary>
        /// Description of the network interface card (NIC), typically the name or model of the NIC.
        /// </summary>
        string NicDescription { get; }

        /// <summary>
        /// Physical address (MAC address) of the network interface, represented as a string.
        /// </summary>
        string PhysicalAddress { get; }
    }
}
