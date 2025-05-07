//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using Microsoft.Windows.EventTracing.Events;
using System;
using System.Collections.Generic;

namespace ETWAnalyzer.Extractors.TCP
{


    /// <summary>
    /// This tracks the IP address to the MAC address mapping which is done by ARP https://en.wikipedia.org/wiki/Address_Resolution_Protocol or / https://en.wikipedia.org/wiki/Neighbor_Discovery_Protocol requests. When there are errors
    /// it usually means that multiple machines have the same IP address.
    /// </summary>
    internal class IpNeighborState
    {
        const string InterfaceKey = "Interface";
        //const string "IpAddrLength"
        const string IpAddressKey = "IP Address";
        //const string "DlAddrLength"
        const string MacAddressKey = "DL Address";
        const string OldNeighborStateKey = "Old Neighbor State";
        const string NewNeighborStateKey = "New Neighbor State";
        const string NeighborEventKey = "Neighbor Event";
        const string CompartmentIdKey = "CompartmentId";

        public enum NeighborStates : UInt32
        {
            Unreachable = 0,
            Incomplete = 1,
            Probe = 2,
            Delay = 3,
            Stale = 4,
            Reachable = 5,
            Permanent = 6,
        }

        public enum NeighborEvents : UInt32
        {
            Map = 0x0,
            Configure = 0x1,
            TlSuspectsReachability = 0x2,
            TlConfirmsReachability = 0x3,
            NaConfirmsReachability = 0x4,
            ProbeReachability = 0x5,
            DadSolicitation = 0x6,
            NewDlAddress = 0x7,
            TriggerNud = 0x8,
            Resolve = 0x9,
            Timeout = 0xa,
            SendingNeighborSolicitation = 0xb,
            ReceivedNeighborSolicitation = 0xc,
            SendingNeighborAdvertisement = 0xd,
            ReceivedNeighborAdvertisement = 0xe,
            SendingRouterSolicitation = 0xf,
            ReceivedRouterSolicitation = 0x10,
            SendingRouterAdvertisement = 0x11,
            ReceivedRouterAdvertisement = 0x12,
        }

        public struct MacAddress
        {
            public byte V0 { get; set; }
            public byte V1 { get; set; }
            public byte V2 { get; set; }
            public byte V3 { get; set; }
            public byte V4 { get; set; }
            public byte V5 { get; set; }

            public MacAddress(IReadOnlyList<byte> bytes)
            {
                if (bytes.Count != 6)
                {
                    throw new InvalidOperationException($"Mac address bytes did contain {bytes.Count} but expected 6.");
                }

                V0 = bytes[0];
                V1 = bytes[1];
                V2 = bytes[2];
                V3 = bytes[3];
                V4 = bytes[4];
                V5 = bytes[5];
            }

            public override string ToString()
            {
                return $"{V0:X}:{V1:X}:{V2:X}:{V3:X}:{V4:X}:{V5:X}";
            }
        }

        public int Compartment { get; set; }
        public uint InterfaceId { get; set;  }
        public NeighborStates OldNeighborState { get; set; }
        public NeighborStates NewNeighborState { get; set; }

        public MacAddress InterfaceAddress { get; set;  }

        public NeighborEvents NeighborEvent { get; set; }

        public IpNeighborState(IGenericEvent ev)
        {
            InterfaceId = ev.Fields[InterfaceKey].AsUInt32;
            InterfaceAddress = new MacAddress(ev.Fields[MacAddressKey].AsBinary);
            OldNeighborState = (NeighborStates) ev.Fields[OldNeighborStateKey].AsUInt32;
            NewNeighborState = (NeighborStates) ev.Fields[NewNeighborStateKey].AsUInt32;
            NeighborEvent = (NeighborEvents)ev.Fields[NeighborEventKey].AsUInt32;
        }

    }
}
