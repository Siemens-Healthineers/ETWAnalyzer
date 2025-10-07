//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Helper;
using Xunit;
using System.IO;
using System.Xml.Serialization;
using ETWAnalyzer.Analyzers;
using ETWAnalyzer.Extractors;
using ETWAnalyzer.Extractors.Exceptions;

namespace ETWAnalyzer_uTest
{
    
    public class ExceptionFilterTests
    {
        readonly XmlSerializer mySerializer = new(typeof(ExceptionFilters));

        [Fact]
        public void Exception_Messages_Are_Filtered()
        {
            var filter = new ExceptionFilterItem
            {
                Message = "Part1;part2;Part3" // not relevant things
            };

            ExceptionFilters filters = new();
            filters.Filters.Add(filter);

            Assert.False(filters.IsRelevantException("deven.exe", "System.ArgumentNullException", "Part1", null));  // exact match (not relevant)
            Assert.False(filters.IsRelevantException("deven.exe", "System.ArgumentNullException", "Part2", null));  // case insensitive match (not relevant)
            Assert.False(filters.IsRelevantException("deven.exe", "System.ArgumentNullException", "part3xxxxxx", null));  // case insenstive substring match (not relevant)
            Assert.True(filters.IsRelevantException("deven.exe", "System.ArgumentNullException", "something different", null));  // case insenstive with no substring not match (relevant)
        }

        /// <summary>
        /// Relevant: Is a unknown Exception
        /// Not Relevant: The Exception is already known
        /// </summary>
        [Fact]
        public void Exception_ProcessWithId_Are_Filtered()
        {
            var filter = new ExceptionFilterItem
            {
                ProcessName = "devenv;PerfWatson;XDesProc;syngo.Services.Workflow.Server" 
            };

            ExceptionFilters filters = new();
            filters.Filters.Add(filter);

            Assert.False(filters.IsRelevantException("devenv", "System.ArgumentNullException", "Any Message", null)); // exact match (not relevant)
            Assert.True(filters.IsRelevantException("perfWatson", "System.ArgumentNullException", "Any Message", null)); // case insensitive do not match (relevant)
            Assert.False(filters.IsRelevantException("XDesProcblablabla;", "System.ArgumentNullException", "Any Message", null)); // case sensitive with substring match (not relevant)
            Assert.False(filters.IsRelevantException("syngo.Services.Workflow.Server", "System.ArgumentNullException", "Any Message", null)); // exact match (not relevant)
            Assert.True(filters.IsRelevantException("Something Different", "System.ArgumentNullException", "Any Message", null)); // substring not in ProcessName do not match (relevant)
        }

        [Fact]
        public void Serious_Gencmd_Exception_IsLeft_Intact()
        {
            const string stack = "ZwTraceEvent\r\nEtwpEventWriteFull\r\nEtwEventWrite\r\nCoTemplate_zzpqhh\r\nETW::ExceptionLog::ExceptionThrown\r\nExceptionTracker::ProcessManagedCallFrame\r\nExceptionTracker::ProcessOSExceptionNotification\r\nProcessCLRException\r\nRtlpExecuteHandlerForException\r\nRtlDispatchException\r\nRtlRaiseException\r\nRaiseException\r\nRaiseTheExceptionInternalOnly\r\nIL_Rethrow\r\nsyngo.Common.Communication.DynamicServices.ServiceOfflineHandler.Execute(CmdFlags, syngo.Common.Communication.IGenCmdExecuteParam)\r\nExceptionTracker::CallHandler\r\nExceptionTracker::CallCatchHandler\r\nProcessCLRException\r\nRtlpExecuteHandlerForUnwind\r\nRtlUnwindEx\r\nClrUnwindEx\r\nProcessCLRException\r\nRtlpExecuteHandlerForException\r\nRtlDispatchException\r\nRtlRaiseException\r\nRaiseException\r\nRaiseTheExceptionInternalOnly\r\nIL_Rethrow\r\nsyngo.Common.Communication.Internal.GenCmdClientChannelBaseImpl.Execute_(syngo.Common.Communication.Internal.GenCmdClientCallContextImpl)\r\nExceptionTracker::CallHandler\r\nExceptionTracker::CallCatchHandler\r\nProcessCLRException\r\nRtlpExecuteHandlerForUnwind\r\nRtlUnwindEx\r\nClrUnwindEx\r\nProcessCLRException\r\nRtlpExecuteHandlerForException\r\nRtlDispatchException\r\nRtlRaiseException\r\nRaiseException\r\nRaiseTheExceptionInternalOnly\r\nIL_Throw\r\nsyngo.Common.Communication.Internal.GenCmdClientChannelBaseImpl.Execute_(syngo.Common.Communication.Internal.GenCmdClientCallContextImpl)\r\nsyngo.Common.Communication.DynamicServices.ServiceOfflineHandler.Execute(CmdFlags, syngo.Common.Communication.IGenCmdExecuteParam)\r\nsyngo.Common.Communication.DynamicServices.ServiceOfflineHandler.sendToMgr(syngo.Common.Communication.DynamicServices.InterfaceParameter, CmdFlags, System.String, syngo.Common.Communication.IGenCmdExecuteParam, System.String, Options)\r\nsyngo.Common.Communication.DynamicServices.ServiceOfflineHandler.Connect(syngo.Common.Communication.DynamicServices.InterfaceParameter, System.String, Options)\r\nsyngo.Common.Communication.DynamicServices.RemotableInterfaceController.registerClientToServer(syngo.Common.Communication.DynamicServices.ServerCreation, Options)\r\nsyngo.Common.Communication.DynamicServices.RemotableInterfaceController.doOutProcRequestAndConnect(syngo.Common.Communication.DynamicServices.MethodDescription, System.Object[], syngo.Common.Communication.DynamicServices.SvcIndex)\r\nsyngo.Common.Communication.DynamicServices.RemotableInterfaceController.doRequest(Int32, System.Object[], syngo.Common.Communication.DynamicServices.SvcIndex)\r\nsyngo.Common.Communication.DynamicServices.ServiceAdapter.callFunc(Int32, System.Object[])\r\nsyngo.Falcon.Products.Common.TraceListeners.GatewayNodeLoggingForwarder.ProcessLogEntries()\r\nSystem.Threading.Tasks.Task.Execute()\r\nSystem.Threading.ExecutionContext.RunInternal(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object, Boolean)\r\nSystem.Threading.ExecutionContext.Run(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object, Boolean)\r\nSystem.Threading.Tasks.Task.ExecuteWithThreadLocal(System.Threading.Tasks.Task ByRef)\r\nSystem.Threading.Tasks.Task.ExecuteEntry(Boolean)\r\nSystem.Threading.ThreadPoolWorkQueue.Dispatch()\r\nCallDescrWorkerInternal\r\nCallDescrWorkerWithHandler\r\nMethodDescCallSite::CallTargetWorker\r\nQueueUserWorkItemManagedCallback\r\nManagedThreadBase_DispatchInner\r\nManagedThreadBase_DispatchMiddle\r\nManagedThreadBase_DispatchOuter\r\nManagedThreadBase_FullTransitionWithAD\r\nManagedPerAppDomainTPCount::DispatchWorkItem\r\nThreadpoolMgr::ExecuteWorkRequest\r\nThreadpoolMgr::WorkerThreadStart\r\nThread::intermediateThreadProc\r\nBaseThreadInitThunk\r\nRtlUserThreadStart\r\n";

            ExceptionFilters filters = ExceptionExtractor.ExceptionFilters();
            Assert.True(filters.IsRelevantException("syngo.Common.Communication.DynamicServices.Host.exe", "syngo.Common.Communication.CommunicationException", "Execute failed", stack));

        }

        [Fact]
        public void Serious_WCF_ServerSideException_Is_Left_Intact()
        {
            ExceptionFilters filters = ExceptionExtractor.ExceptionFilters();
            const string stack = "ZwTraceEvent\r\nEtwpEventWriteFull\r\nEtwEventWrite\r\nCoTemplate_zzpqhh\r\nETW::ExceptionLog::ExceptionThrown\r\nExceptionTracker::ProcessManagedCallFrame\r\nExceptionTracker::ProcessOSExceptionNotification\r\nProcessCLRException\r\nRtlpExecuteHandlerForException\r\nRtlDispatchException\r\nRtlRaiseException\r\nRaiseException\r\nRaiseTheExceptionInternalOnly\r\nIL_Throw\r\nSystem.Runtime.Remoting.Proxies.RealProxy.HandleReturnMessage(System.Runtime.Remoting.Messaging.IMessage, System.Runtime.Remoting.Messaging.IMessage)\r\nSystem.Runtime.Remoting.Proxies.RealProxy.PrivateInvoke(System.Runtime.Remoting.Proxies.MessageData ByRef, Int32)\r\nCTPMethodTable__CallTargetHelper3\r\nCallTargetWorker2\r\nTransparentProxyStubWorker\r\nTransparentProxyStub_CrossContext\r\nsyngo.Services.Workflow.Adapters.Core.AdapterWorkitem.InternalWorkitemChangeUpdatedEvent(syngo.Services.Workflow.Adapters.Core.WorkitemChangeEventArgs)\r\nSystem.Threading.Tasks.Task.Execute()\r\nSystem.Threading.ExecutionContext.RunInternal(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object, Boolean)\r\nSystem.Threading.ExecutionContext.Run(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object, Boolean)\r\nSystem.Threading.Tasks.Task.ExecuteWithThreadLocal(System.Threading.Tasks.Task ByRef)\r\nSystem.Threading.Tasks.Task.ExecuteEntry(Boolean)\r\nSystem.Threading.ThreadPoolWorkQueue.Dispatch()\r\nCallDescrWorkerInternal\r\nCallDescrWorkerWithHandler\r\nMethodDescCallSite::CallTargetWorker\r\nQueueUserWorkItemManagedCallback\r\nManagedThreadBase_DispatchInner\r\nManagedThreadBase_DispatchMiddle\r\nManagedThreadBase_DispatchOuter\r\nManagedThreadBase_DispatchInCorrectAD\r\nThread::DoADCallBack\r\nManagedThreadBase_DispatchInner\r\nManagedThreadBase_DispatchMiddle\r\nManagedThreadBase_DispatchOuter\r\nManagedThreadBase_FullTransitionWithAD\r\nManagedPerAppDomainTPCount::DispatchWorkItem\r\nThreadpoolMgr::ExecuteWorkRequest\r\nThreadpoolMgr::WorkerThreadStart\r\nThread::intermediateThreadProc\r\nBaseThreadInitThunk\r\nRtlUserThreadStart\r\n";
            Assert.True(filters.IsRelevantException( processWithId: "syngo.Common.Communication.DynamicServices.Host.exe",
                                                     exceptionType: "System.ServiceModel.CommunicationObjectAbortedException",
                                                     message:       "The communication object, System.ServiceModel.Channels.ServiceChannel, cannot be used for communication because it has been Aborted.",
                                                     stackTrace: stack));
        }

        [Fact]
        public void Exception_ExceptionType_Are_Filtered()
        {
            var filter = new ExceptionFilterItem
            { ExceptionType = "System.Security.SecurityException" };

            ExceptionFilters filters = new();
            filters.Filters.Add(filter);

            Assert.False(filters.IsRelevantException("devenv", "System.Security.SecurityException", "Any Message",null)); // ONLY exact ExceptionType match (not relevant)
            Assert.True(filters.IsRelevantException("devenv", "system.security.securityException", "Any Message", null)); // case insensitive do not match (relevant)
            Assert.True(filters.IsRelevantException("devenv", "System.Security.SecurityExceptionXxxxxxxxxxxxxxbb", "Any Message", null)); // case sensitive with substring do not match (relevant)
            Assert.True(filters.IsRelevantException("devenv", "Something Different", "Any Message", null)); // substring not in ExceptionType do not match (relevant)
        }

        [Fact]
        public void Exception_StackTrace_Are_Filtered()
        {
            var filter = new ExceptionFilterItem
            {
                StackTracePart = "System.Diagnostics.PerformanceCounter.InitializeImpl;Stacktrace2"
            };

            ExceptionFilters filters = new();
            filters.Filters.Add(filter);

            Assert.False(filters.IsRelevantException("devenv", "System.Security.SecurityException", "Any Message", "System.Diagnostics.PerformanceCounter.InitializeImpl")); // exact Stacktrace match (not relevant)
            Assert.True(filters.IsRelevantException("devenv", "System.Security.SecurityException", "Any Message", "system.diagnostics.performanceCounter.initializeImpl")); // case insensitive do not match (relevant)
            Assert.False(filters.IsRelevantException("devenv", "System.Security.SecurityException", "Any Message", "System.Diagnostics.PerformanceCounter.InitializeImplXXXxxxxxxxxxxxxx")); // case sensitive with substring match (not relevant)
            Assert.True(filters.IsRelevantException("devenv", "System.Security.SecurityException", "Any Message", "Any.Other.Stacktrace")); // substring not in Stacktracepart not match (relevant)


        }


        [Fact]
        public void WriteFilterToDisk()
        {
            using var tmp = TempDir.Create();
            string outFile = Path.Combine(tmp.Name, "ExceptionFilterCfg.xml");
            WriteDataTo(outFile);
            Assert.True(File.Exists(outFile));
        }

        void WriteDataTo(string file)
        {
            var filter = new ExceptionFilterItem
            {
                Message = "Part1;part2;Part3"
                
            };

            ExceptionFilters filters = new();
            filters.Filters.Add(filter);

            using var outFile = File.CreateText(file);
            mySerializer.Serialize(outFile, filters);

        }

        [Fact]
        public void ReadExceptionFilter()
        {
            using var tmp = TempDir.Create();
            string inOutFile = Path.Combine(tmp.Name, "ExceptionFilterCfg.xml");
            WriteDataTo(inOutFile);

            using var inFile = File.OpenRead(inOutFile);
            var deserializedFilters = (ExceptionFilters)mySerializer.Deserialize(inFile);
            Assert.NotNull(deserializedFilters);
            Assert.Single(deserializedFilters.Filters);
        }

    }
}
