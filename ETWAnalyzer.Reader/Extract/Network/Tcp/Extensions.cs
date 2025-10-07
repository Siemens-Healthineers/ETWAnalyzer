//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extract.Network.Tcp
{
    /// <summary>
    /// Helper methods which cannot be part of classes because Json.NET would try to serialize them by default.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Calculate the time difference between the first packet which was sent with given sequence number and a retransmission event.
        /// </summary>
        /// <param name="retrans">Retransmission event</param>
        /// <returns>Time when retransmission did happen after packee was not acked yet.</returns>
        public static TimeSpan RetransmitDiff(this ITcpRetransmission retrans)
        {
            return retrans.RetransmitTime - retrans.SendTime;
        }
    }
}
