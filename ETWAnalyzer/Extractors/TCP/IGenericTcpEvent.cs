//// SPDX-FileCopyrightText:  © 2023 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System;

namespace ETWAnalyzer.Extractors.TCP
{
    internal interface IGenericTcpEvent
    {
        TcpRequestConnect Connection { get; set; }
        ulong Tcb { get; set; }
        DateTimeOffset Timestamp { get; set; }
    }
}