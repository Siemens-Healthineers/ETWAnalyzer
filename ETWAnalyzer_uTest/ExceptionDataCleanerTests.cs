//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Analyzers.ExceptionDifferenceAnalyzer;
using ETWAnalyzer.Extract;
using ETWAnalyzer.Helper;
using ETWAnalyzer.ScreenshotBitmapping;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace ETWAnalyzer_uTest
{
    public class ExceptionDataCleanerTests
    {
        readonly string ThrowStringOne =    "RtlpExecuteHandlerForException\n" +
                                            "RtlDispatchException\n" +
                                            "RtlRaiseException\n" +
                                            "RaiseException\n" +
                                            "RaiseTheExceptionInternalOnly\n" +
                                            "IL_Throw\n" +
                                            "System.Data.SQLite.SQLite3.Reset(System.Data.SQLite.SQLiteStatement)\n" +
                                            "System.Data.SQLite.SQLite3.Step(System.Data.SQLite.SQLiteStatement)\n" +
                                            "System.Data.SQLite.SQLiteDataReader.NextResult()\n" +
                                            "System.Data.SQLite.SQLiteDataReader..ctor(System.Data.SQLite.SQLiteCommand, System.Data.CommandBehavior)\n" +
                                            "System.Data.SQLite.SQLiteCommand.ExecuteReader(System.Data.CommandBehavior)";

        readonly string ThrowStringTwo =    "RaiseException\n" +
                                            "RaiseTheExceptionInternalOnly\n" +
                                            "IL_Throw\n" +
                                            "System.Net.HttpWebRequest.GetConnectingContext()\n" +
                                            "System.Net.Connection.CompleteConnection(Boolean, System.Net.HttpWebRequest)\n" +
                                            "System.Net.Connection.CompleteConnectionWrapper(System.Object, System.Object)\n" +
                                            "System.Net.PooledStream.ConnectionCallback(System.Object, System.Exception, System.Net.Sockets.Socket, System.Net.IPAddress)\n" +
                                            "System.Net.ServicePoint.ConnectSocketCallback(System.IAsyncResult)\nSystem.Net.LazyAsyncResult.Complete(IntPtr)";

        readonly string ExecuteString =     "syngoClientBootstrapping.Core.ClientInstallation.GetDescriptor(System.String)" +
                                            "\nsyngo.Common.Container.Extension.Update.UpdaterPlugin.DetermineClientInstallationState(System.String, System.String ByRef)" +
                                            "\nsyngo.Common.Container.Extension.Update.UpdaterService+<>c.<.ctor>b__34_1(System.String)" +
                                            "\nsyngo.Common.Container.Extension.Update.UpdaterService.UpdateDeploymentPoolWorker(System.Object)" +
                                            "\nSystem.Threading.Tasks.Task.Execute()" +
                                            "\nSystem.Threading.ExecutionContext.RunInternal(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object, Boolean)" +
                                            "\nSystem.Threading.ExecutionContext.Run(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object, Boolean)";

        readonly string MsgDateGuidFile =   @"Violation of UNIQUE KEY constraint 'IX_LockingContextSet_NameUnique'.\n"+
                                            @"Cannot insert duplicate key in object 'dbo.LockingContextSet'.\n The duplicate key value is (c7091693-b33a-418d-a3eb-8ce765f86b10).\n"+
                                            @"P1 E:\storagefw\v0\c4404b75e60ea81129fcf272d115633f20c1e85b_mr_k_2020_20110902\1.3.12.2.1107.5.99.3.30000017101111144956100000154_160a2376-fd5f-460f-881f-a653f83493e9\n"+
                                            @"Access to the path 'C:\Store\log\DataStorage\syngo.Viewing.Shell.Host.exe_log\"+DateTime.Now.Year.ToString()+ @".03.04_21.53.51.819_+01' is denied.\n" +
                                            @"Access to the path 'C:\Store\log\DataStorage\syngo.Viewing.Shell.Host.exe_log\" +DateTime.Now.Year.ToString()+ @".03.04_24.55.51.819_+01' is denied.\n" +
                                            @"Details:Cannot perform directory move operation from: 'e:\storagefw\v0\c\b60ad15703ce0a83' to:'e:\storagefw\v0\c\b60ad15703ce0a83_remove_pending'";

        readonly string DateVariety = @"Access to the path 'C:\Store\log\DataStorage\syngo.Viewing.Shell.Host.exe_log\"+DateTime.Now.Year.ToString()+ @".07.22_21.34.05.78743_+02' is denied."+"\n"+
                                      @"Init/Raised in Thread: , ID: 14788, ProcessID: 35284, at: 9/30/"+DateTime.Now.Year.ToString()+ @" 4:19:37 PM" + "\n" +
                                      @"\Microsoft Visual Studio\" +DateTime.Now.Year.ToString()+ @"\Community\Common7\" + "\n" +
                                      @"SomeText043Text 05/29/" +DateTime.Now.Year.ToString()+ @"lskflö" + "\n" +
                                      @"NextTextMonday, 29 May " +DateTime.Now.Year.ToString()+ @" slkf" + "\n" +
                                      @"sdfTue, 29 May " +DateTime.Now.Year.ToString()+ @" 05:50sfsd" + "\n" +
                                      @"sdfWednesday, 29 September " +DateTime.Now.Year.ToString()+ @" 05:50 AMsfs" + "\n" +
                                      @"sdfThursday, 29 December " +DateTime.Now.Year.ToString()+ @" 5:50sfds" + "\n" +
                                      @"sfdFriday, 29 June " +DateTime.Now.Year.ToString()+ @" 5:50 AM skld" + "\n" +
                                      @"sdfSat, 29 May " +DateTime.Now.Year.ToString()+ @" 05:50:06 sklfdj" + "\n" +
                                      @"sdf05/29/" +DateTime.Now.Year.ToString()+ @" 05:50kjslfj" + "\n" +
                                      @"sdf05/29/" +DateTime.Now.Year.ToString()+ @" 05:50 AMklsjf434" + "\n" +
                                      @"sd05/29/" +DateTime.Now.Year.ToString()+ @" 5:50 skdjfl34" + "\n" +
                                      @"sdf05/29/" +DateTime.Now.Year.ToString()+ @" 5:50 AM s kjösdj3" + "\n" +
                                      @"s24dfs05/29/" +DateTime.Now.Year.ToString()+ @" 05:50:06 ksfj3" + "\n" +
                                      @"sldjsd34fslö:" +DateTime.Now.Year.ToString()+ @"-05-16T05:50:06.7199222-04:00hhl" + "\n" +
                                      @"skljf34: Fri, 16 May " +DateTime.Now.Year.ToString()+ @" 05:50:06 GMTkslf" + "\n" +
                                      @"ksl34:" +DateTime.Now.Year.ToString()+@"-05-16T05:50:06 ksldjf:434\n ";
        [Fact]
        public void Can_Detect_Relevant_StackRows_Below()
        {
            string cleanup = ExceptionDataCleaner.GetLastRowsBelow(ThrowStringOne);
            Assert.StartsWith("IL_Throw\nSystem.Data.SQLite.SQLite3.Reset(System.Data.SQLite.SQLiteStatement)", cleanup);
            Assert.EndsWith("System.Data.SQLite.SQLiteDataReader.NextResult()\n", cleanup);

            cleanup = ExceptionDataCleaner.GetLastRowsBelow(ThrowStringOne, '\n', 5);
            Assert.StartsWith("IL_Throw\nSystem.Data.SQLite.SQLite3.Reset(System.Data.SQLite.SQLiteStatement)", cleanup);
            Assert.EndsWith("System.Data.SQLite.SQLiteDataReader..ctor(System.Data.SQLite.SQLiteCommand, System.Data.CommandBehavior)\n", cleanup);

            cleanup = ExceptionDataCleaner.GetLastRowsBelow(ThrowStringTwo + ThrowStringOne);
            Assert.StartsWith("IL_Throw\nSystem.Data.SQLite.SQLite3.Reset(System.Data.SQLite.SQLiteStatement)", cleanup);
            Assert.EndsWith("System.Data.SQLite.SQLiteDataReader.NextResult()\n", cleanup);

        }
        [Fact]
        public void Can_Detect_Relevant_StackRows_Above()
        {
            string cleanup = ExceptionDataCleaner.GetFirstRowsAbove(ExecuteString);
            Assert.StartsWith("syngoClientBootstrapping.Core.ClientInstallation.GetDescriptor(System.String)", cleanup);
            Assert.EndsWith("System.Threading.Tasks.Task.Execute()\n", cleanup);

            cleanup = ExceptionDataCleaner.GetFirstRowsAbove(ExecuteString, '\n', 3);
            Assert.StartsWith("syngo.Common.Container.Extension.Update.UpdaterService+<>c.<.ctor>b__34_1(System.String)", cleanup);
            Assert.EndsWith("System.Threading.Tasks.Task.Execute()\n", cleanup);
        }

        [Fact]
        public void Can_Detect_Relevant_StackRows_AboveAndBelow()
        {
            string cleanup = ExceptionDataCleaner.CleanUpStack(ThrowStringTwo + ThrowStringOne + ExecuteString);
            Assert.StartsWith("IL_Throw\nSystem.Data.SQLite.SQLite3.Reset(System.Data.SQLite.SQLiteStatement)", cleanup);
            Assert.EndsWith("System.Data.SQLite.SQLiteDataReader.NextResult()\n", cleanup);
            Trace.WriteLine(ThrowStringTwo + ThrowStringOne + ExecuteString+"\n\n");
            Trace.WriteLine(cleanup);
        }
        [Fact]
        public void Can_Cleanup_FileNames()
        {
            Assert.Contains(@"e:\storagefw\v0\c\b60ad15703ce0a83", MsgDateGuidFile);
            Assert.Contains(@"e:\storagefw\v0\c\b60ad15703ce0a83_remove_pending", MsgDateGuidFile);
            string cleanup = ExceptionDataCleaner.CleanUpFileNames(MsgDateGuidFile);
            Assert.DoesNotContain(@"e:\storagefw\v0\c\b60ad15703ce0a83", cleanup);
            Assert.DoesNotContain(@"e:\storagefw\v0\c\b60ad15703ce0a83_remove_pending", cleanup);
            Assert.Equal(18, cleanup.LastIndexOf("  IsFileName  ") - cleanup.IndexOf("  IsFileName  ")) ;
        }

        [Fact]
        public void Can_Cleanup_Guids()
        {
            string cleanup = ExceptionDataCleaner.CleanUpGuid(MsgDateGuidFile);
            Assert.Equal(144, cleanup.LastIndexOf("  IsGuid  ") - cleanup.IndexOf("  IsGuid  "));
        }
        [Fact]
        public void Can_Cleanup_Dates()
        {
            string cleanup = ExceptionDataCleaner.CleanUpVariableDates(DateVariety);
            Assert.Equal(17, cleanup.Split('\n').Where(x => x.Contains("  IsTimeStamp  ")).Count());
            Assert.Equal(536, cleanup.LastIndexOf("  IsTimeStamp  ") - cleanup.IndexOf("  IsTimeStamp  "));
        }
        [Fact]
        public void Can_Cleanup_FilesGuidAndDates()
        {
            string filesMsgDates = MsgDateGuidFile + DateVariety;
            string cleanup = ExceptionDataCleaner.CleanUpMessage(filesMsgDates);
            Assert.Equal(18, cleanup.LastIndexOf("  IsFileName  ") - cleanup.IndexOf("  IsFileName  "));
            Assert.Equal(17, cleanup.Split('\n').Where(x => x.Contains("  IsTimeStamp  ")).Count());
            Assert.Equal(836, cleanup.LastIndexOf("  IsTimeStamp  ") - cleanup.IndexOf("  IsTimeStamp  "));
        }       
    }
}
