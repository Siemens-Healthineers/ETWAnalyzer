//// SPDX-FileCopyrightText:  © 2025 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT


using System.Collections.Generic;

namespace ETWAnalyzer.Extract.Network.Tcp.Issues
{
    /// <summary>
    /// Contains automatically detected TCP issues like firewall problems, ...
    /// </summary>
    public interface ITcpIssues
    {
        /// <summary>
        /// Collection of TCP Post issues like firewall problems, ...
        /// </summary>
        IReadOnlyList<ITcpPostIssue> PostIssues { get; }
    }


    /// <summary>
    /// 
    /// </summary>
    public class TcpIssues : ITcpIssues
    {
        /// <summary>
        /// 
        /// </summary>
        public List<TcpPostIssue> PostIssues { get; set; } = new();

        /// <summary>
        /// 
        /// </summary>
        IReadOnlyList<ITcpPostIssue> ITcpIssues.PostIssues { get => PostIssues;  }
    }
}
