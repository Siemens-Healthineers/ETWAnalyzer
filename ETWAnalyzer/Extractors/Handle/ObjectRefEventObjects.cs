//// SPDX-FileCopyrightText:  © 2024 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Microsoft.Windows.EventTracing;
using Microsoft.Windows.EventTracing.Processes;
using Microsoft.Windows.EventTracing.Symbols;
using System;
using System.Runtime.InteropServices;

namespace ETWAnalyzer.Extractors.Handle
{
    // https://learn.microsoft.com/en-us/windows/win32/etw/obtrace

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DeleteObjectETW
    {
        public long ObjectPtr;
        public UInt16 ObjectType;
        public UInt32 RefCount;
        public UInt16 Unknown;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CreateObjectETW
    {
        public long ObjectPtr;
        public UInt16 ObjectType;
        public UInt16 Reserved;
    }

    // Object="0xFFFF810C862E09A0" Handle="0xFFFFFFFF80000044" ObjectType="19" ObjectName="\KernelObjects\HighMemoryCondition"/>
    //  EventName="Object/TypeDCEnd" ObjectType="6" ObjectTypeName="Job"/>
    /* https://learn.microsoft.com/en-us/windows/win32/etw/obhandlerundownevent
        [Dynamic, EventType{38,39}, EventTypeName{HandleDCStart,HandleDCEnd}]
        class ObHandleRundownEvent : ObTrace
        {
          uint32 Handle;
          uint32 Object;
          string ObjectName;
          uint16 ObjectType;
          uint32 ProcessId;
        };
     */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct HandleDCEndETW
    {
        public long ObjectPtr;
        public UInt32 ProcessId;
        public UInt32 Handle;
        public UInt16 ObjectType;
        //  string ObjectName;
    }

    //  EventName="Object/TypeDCEnd" ObjectType="6" ObjectTypeName="Job"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct TypeDCEndETW
    {
        public UInt16 ObjectType;
        public UInt16 Reserved;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CreateHandleETW
    {
        public long ObjectPtr;
        public UInt32 Handle;
        public UInt16 ObjectType;
        public UInt16 Reserved;
    }

    // Object="0xFFFFC283AEEA3E60" SourceHandle="0x00005648" TargetHandle="-2,147,476,004" SourceProcessID="20,648" TargetHandleID="4" ObjectType="16"
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DuplicateHandleETW
    {
        public long ObjectPtr;
        public UInt32 SourceHandle;
        public UInt32 TargetHandle;
        public UInt32 TargetHandleId;
        public UInt16 ObjectType;
        public int SourceProcessId;
    }


    //  // Object="0xFFFFC28194C533E0" Handle="0x000007A0" ObjectType="16" ObjectName="" ObjectTypeName="Event"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CloseHandleETW
    {
        public long ObjectPtr;
        public UInt32 Handle;
        public UInt16 ObjectType;
        public UInt16 Reserved;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct IncreaseObjectRefCountETW
    {
        public long ObjectPtr;
        public UInt32 Tag;
        public UInt32 RefCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DecreaseObjectRefCountETW
    {
        public long ObjectPtr;
        public UInt32 Tag;
        public UInt32 RefCount;
    }


    class BaseEvent
    {
        internal TraceTimestamp TimeStamp { get; set; }
        public int ProcessId { get; set; }
        public int ThreadId { get; set; }

        public string StackTrace { get; set; }

        public IStackSnapshot GetStack(IPendingResult<IStackDataSource> stackSource) => stackSource.Result.GetStack(TimeStamp, ThreadId);

        public IProcess GetProcess(IPendingResult<IProcessDataSource> processSource) => processSource.Result.GetProcess(TimeStamp, ProcessId);
    }

    class ObjectTraceBase : BaseEvent
    {
        public long ObjectPtr { get; set; }
    }

    class CreateObjectEvent : ObjectTraceBase
    {
        public UInt16 ObjectType { get; set; }
    }

    class DuplicateObjectEvent : ObjectTraceBase
    {
        public UInt32 SourceHandle { get; set; }
        public UInt32 TargetHandle { get; set; }
        public int SourceProcessId { get; set; }
        public UInt32 TargetHandleId { get; set; }
        public UInt16 ObjectType { get; set; }
    }

    class HandleTypeEvent : ObjectTraceBase
    {
        public UInt16 ObjectType { get; set; }
        public string Name { get; set; }
    }

    class CreateHandleEvent : ObjectTraceBase
    {
        public UInt16 ObjectType { get; set; }
        public ulong HandleType { get; set; }
        public ulong HandleValue { get; set; }
    }

    class CloseHandleEvent : ObjectTraceBase
    {
        public UInt16 ObjectType { get; set; }
        public ulong HandleType { get; set; }
        public ulong HandleValue { get; set; }
        public string Name { get; set; }
    }


    class DeleteObjectEvent : ObjectTraceBase
    {
        public UInt16 ObjectType { get; set; }
    }


    class IncreaseRefCount : ObjectTraceBase
    {
        public UInt32 Tag { get; set; }

        /// <summary>
        /// Value by how much the Refcount will be incremented.
        /// </summary>
        public Int64 RefCount { get; set; }
    }

    class DecreaseRefCount : ObjectTraceBase
    {
        public UInt32 Tag { get; set; }

        /// <summary>
        /// Value by how much the Refcount will be decremented.
        /// </summary>
        public Int64 RefCount { get; set; }
    }

    /*
     * 
       Object/TypeDCEnd                                        Count:            69 SizeInBytes:          2,124 Id:       Name: MSNT_SystemTrace                                PGUID: 9e814aad-3204-11d2-9a82-006008a86939 Task: 89497f50-effe-4440-8cf2-ce6b1cdcaca7 OpCode:   37 Keywords: 0x0
       Object/CreateObject                                     Count:        26,664 SizeInBytes:        426,624 Id:       Name: MSNT_SystemTrace                                PGUID: 9e814aad-3204-11d2-9a82-006008a86939 Task: 89497f50-effe-4440-8cf2-ce6b1cdcaca7 OpCode:   48 Keywords: 0x0
       Object/DeleteObject                                     Count:        26,682 SizeInBytes:        426,912 Id:       Name: MSNT_SystemTrace                                PGUID: 9e814aad-3204-11d2-9a82-006008a86939 Task: 89497f50-effe-4440-8cf2-ce6b1cdcaca7 OpCode:   49 Keywords: 0x0
       Object/ReferenceObject                                  Count:     7,355,950 SizeInBytes:    117,695,200 Id:       Name: MSNT_SystemTrace                                PGUID: 9e814aad-3204-11d2-9a82-006008a86939 Task: 89497f50-effe-4440-8cf2-ce6b1cdcaca7 OpCode:   50 Keywords: 0x0
       Object/DereferenceObject                                Count:     7,443,960 SizeInBytes:    119,103,360 Id:       Name: MSNT_SystemTrace                                PGUID: 9e814aad-3204-11d2-9a82-006008a86939 Task: 89497f50-effe-4440-8cf2-ce6b1cdcaca7 OpCode:   51 Keywords: 0x0

    <Event MSec="1476393.6906" PID="2180" PName= "svchost" TID="30132" EventName="Object/CloseHandle"
  TimeStamp="04/20/24 01:32:30.518184" ID="Illegal" Version="2" Keywords="0x00000000" TimeStampQPC="859,673,163,366" QPCTime="0.100us"
  Level="Always" ProviderName="Windows Kernel" ProviderGuid="9e814aad-3204-11d2-9a82-006008a86939" ClassicProvider="True" ProcessorNumber="0"
  Opcode="33" TaskGuid="89497f50-effe-4440-8cf2-ce6b1cdcaca7" Channel="0" PointerSize="8"
  CPU="0" EventIndex="23726" TemplateType="ObjectHandleTraceData">
  <PrettyPrint>
    <Event MSec="1476393.6906" PID="2180" PName= "svchost" TID="30132" EventName="Object/CloseHandle" Object="0xFFFFC28194C533E0" Handle="0x000007A0" ObjectType="16" ObjectName="" ObjectTypeName="Event"/>
  </PrettyPrint>
  <Payload Length="16">
       0:  e0 33 c5 94 81 c2 ff ff | a0  7  0  0 10  0  0  0   .3...... ........
  </Payload>
</Event>


    DereferenceObject Size 16 Bytes

        0x31 OpCode 51
      ntoskrnl.exe!NtClose
      ntoskrnl.exe!ObpCloseHandle
      ntoskrnl.exe!ObfDereferenceObjectWithTag
      ntoskrnl.exe!ObpDeregisterObject

     <Event MSec= "29511.6774" PID="76500" PName="ServiceHub.VSDetouredHost" TID="55472" EventName="Object/DereferenceObject"
      TimeStamp="04/18/24 11:22:56.455579" ID="Illegal" Version="2" Keywords="0x00000000" TimeStampQPC="3,433,172,867,104" QPCTime="0.100us"
      Level="Always" ProviderName="MSNT_SystemTrace" ProviderGuid="9e814aad-3204-11d2-9a82-006008a86939" ClassicProvider="True" ProcessorNumber="15"
      Opcode="51" TaskGuid="89497f50-effe-4440-8cf2-ce6b1cdcaca7" Channel="0" PointerSize="8"
      CPU="15" EventIndex="53144" TemplateType="DynamicTraceEventData">
      <PrettyPrint>
        <Event MSec= "29511.6774" PID="76500" PName="ServiceHub.VSDetouredHost" TID="55472" EventName="Object/DereferenceObject" ProviderName="MSNT_SystemTrace" Object="0xffffa889efe5a050" Tag="1,953,261,124" Count="1"/>
      </PrettyPrint>
      <Payload Length="16">
           0:  50 a0 e5 ef 89 a8 ff ff | 44 66 6c 74  1  0  0  0   P....... Dflt....
      </Payload>

    ReferenceObject Id=0x32 == OpCode 50
    16 bytes 
    PGUID: 9e814aad-3204-11d2-9a82-006008a86939 Task: 89497f50-effe-4440-8cf2-ce6b1cdcaca7 OpCode:   50 Keywords: 0x0

    <Event MSec= "29511.6705" PID="76500" PName="ServiceHub.VSDetouredHost" TID="55472" EventName="Object/ReferenceObject"
  TimeStamp="04/18/24 11:22:56.455572" ID="Illegal" Version="2" Keywords="0x00000000" TimeStampQPC="3,433,172,867,035" QPCTime="0.100us"
  Level="Always" ProviderName="MSNT_SystemTrace" ProviderGuid="9e814aad-3204-11d2-9a82-006008a86939" ClassicProvider="True" ProcessorNumber="15"
  Opcode="50" TaskGuid="89497f50-effe-4440-8cf2-ce6b1cdcaca7" Channel="0" PointerSize="8"
  CPU="15" EventIndex="53140" TemplateType="DynamicTraceEventData">
  <PrettyPrint>
    <Event MSec= "29511.6705" PID="76500" PName="ServiceHub.VSDetouredHost" TID="55472" EventName="Object/ReferenceObject" ProviderName="MSNT_SystemTrace" Object="0xffffa889b33d24b0" Tag="1,953,261,124" Count="1"/>
  </PrettyPrint>
  <Payload Length="16">
       0:  b0 24 3d b3 89 a8 ff ff | 44 66 6c 74  1  0  0  0   .$=..... Dflt....
  </Payload>
</Event>

  |    ntoskrnl.exe!NtCreateEvent
  |    ntoskrnl.exe!ObInsertObjectEx
  |    ntoskrnl.exe!ObpCreateHandle
  |    ntoskrnl.exe!ObpPushStackInfo


    Object/CreateObject Id=0x30 == OpCode 48
       ntdll.dll!NtCreateEvent,,128
       ntoskrnl.exe!KiSystemServiceCopyEnd,,128
       ntoskrnl.exe!NtCreateEvent,,128
       ntoskrnl.exe!ObCreateObjectEx,,128
       ntoskrnl.exe!ObpRegisterObject,,128


    <Event MSec= "29512.0888" PID= "308" PName="Registry" TID="55472" EventName="Object/CreateObject"
  TimeStamp="04/18/24 11:22:56.455990" ID="Illegal" Version="2" Keywords="0x00000000" TimeStampQPC="3,433,172,871,218" QPCTime="0.100us"
  Level="Always" ProviderName="MSNT_SystemTrace" ProviderGuid="9e814aad-3204-11d2-9a82-006008a86939" ClassicProvider="True" ProcessorNumber="15"
  Opcode="48" TaskGuid="89497f50-effe-4440-8cf2-ce6b1cdcaca7" Channel="0" PointerSize="8"
  CPU="15" EventIndex="53349" TemplateType="DynamicTraceEventData">
  <PrettyPrint>
    <Event MSec= "29512.0888" PID= "308" PName="Registry" TID="55472" EventName="Object/CreateObject" ProviderName="MSNT_SystemTrace" Object="0xffffa889a613c800" ObjectType="48"/>
  </PrettyPrint>
  <Payload Length="16">
       0:   0 c8 13 a6 89 a8 ff ff | 30  0  0  0  0  0  0  0   ........ 0.......
  </Payload>


    DeleteObject 49 

    <Event MSec= "29511.9672" PID="76500" PName="ServiceHub.VSDetouredHost" TID="55472" EventName="Object/DeleteObject"
  TimeStamp="04/18/24 11:22:56.455868" ID="Illegal" Version="2" Keywords="0x00000000" TimeStampQPC="3,433,172,870,002" QPCTime="0.100us"
  Level="Always" ProviderName="MSNT_SystemTrace" ProviderGuid="9e814aad-3204-11d2-9a82-006008a86939" ClassicProvider="True" ProcessorNumber="15"
  Opcode="49" TaskGuid="89497f50-effe-4440-8cf2-ce6b1cdcaca7" Channel="0" PointerSize="8"
  CPU="15" EventIndex="53292" TemplateType="DynamicTraceEventData">
  <PrettyPrint>
    <Event MSec= "29511.9672" PID="76500" PName="ServiceHub.VSDetouredHost" TID="55472" EventName="Object/DeleteObject" ProviderName="MSNT_SystemTrace" Object="0xffffa889a6139d80" ObjectType="48"/>
  </PrettyPrint>
  <Payload Length="16">
       0:  80 9d 13 a6 89 a8 ff ff | 30  0  0  0  0  0  0  0   ........ 0.......
  </Payload>
     */


    /* 
     Handle Provider Events

    <Event MSec="1476985.7542" PID="20648" PName="Host" TID="7692" EventName="Object/CreateHandle"
  TimeStamp="04/20/24 01:32:31.110248" ID="Illegal" Version="2" Keywords="0x00000000" TimeStampQPC="859,679,084,002" QPCTime="0.100us"
  Level="Always" ProviderName="Windows Kernel" ProviderGuid="9e814aad-3204-11d2-9a82-006008a86939" ClassicProvider="True" ProcessorNumber="0"
  Opcode="32" TaskGuid="89497f50-effe-4440-8cf2-ce6b1cdcaca7" Channel="0" PointerSize="8"
  CPU="0" EventIndex="27230" TemplateType="ObjectHandleTraceData">
  <PrettyPrint>
    <Event MSec="1476985.7542" PID="20648" PName="Host" TID="7692" EventName="Object/CreateHandle" Object="0xFFFFC283AEE8CD60" Handle="0x00003A78" ObjectType="16" ObjectName="" ObjectTypeName="Event"/>
  </PrettyPrint>
  <Payload Length="16">
       0:  60 cd e8 ae 83 c2 ff ff | 78 3a  0  0 10  0  0  0   `....... x:......
  </Payload>
</Event>

        <Event MSec="1477337.9157" PID="20648" PName="Host" TID="28360" EventName="Object/CloseHandle"
  TimeStamp="04/20/24 01:32:31.462410" ID="Illegal" Version="2" Keywords="0x00000000" TimeStampQPC="859,682,605,617" QPCTime="0.100us"
  Level="Always" ProviderName="Windows Kernel" ProviderGuid="9e814aad-3204-11d2-9a82-006008a86939" ClassicProvider="True" ProcessorNumber="0"
  Opcode="33" TaskGuid="89497f50-effe-4440-8cf2-ce6b1cdcaca7" Channel="0" PointerSize="8"
  CPU="0" EventIndex="30333" TemplateType="ObjectHandleTraceData">
  <PrettyPrint>
    <Event MSec="1477337.9157" PID="20648" PName="Host" TID="28360" EventName="Object/CloseHandle" Object="0xFFFFC283AEF90260" Handle="0x00004944" ObjectType="16" ObjectName="" ObjectTypeName="Event"/>
  </PrettyPrint>
  <Payload Length="16">
       0:  60  2 f9 ae 83 c2 ff ff | 44 49  0  0 10  0  0  0   `....... DI......
  </Payload>
</Event>

<Event MSec="1476909.4275" PID="20648" PName="Host" TID="7692" EventName="Object/DuplicateHandle"
  TimeStamp="04/20/24 01:32:31.033921" ID="Illegal" Version="3" Keywords="0x00000000" TimeStampQPC="859,678,320,735" QPCTime="0.100us"
  Level="Always" ProviderName="Windows Kernel" ProviderGuid="9e814aad-3204-11d2-9a82-006008a86939" ClassicProvider="True" ProcessorNumber="0"
  Opcode="34" TaskGuid="89497f50-effe-4440-8cf2-ce6b1cdcaca7" Channel="0" PointerSize="8"
  CPU="0" EventIndex="26439" TemplateType="ObjectDuplicateHandleTraceData">
  <PrettyPrint>
    <Event MSec="1476909.4275" PID="20648" PName="Host" TID="7692" EventName="Object/DuplicateHandle" Object="0xFFFFC283AEEA3E60" SourceHandle="0x00005648" TargetHandle="-2,147,476,004" SourceProcessID="20,648" TargetHandleID="4" ObjectType="16" ObjectName="" ObjectTypeName="Event"/>
  </PrettyPrint>
  <Payload Length="26">
       0:  60 3e ea ae 83 c2 ff ff | 48 56  0  0 dc 1d  0 80   `&gt;...... HV......
      10:   4  0  0  0 10  0 a8 50 |  0  0                     .......P ..
  </Payload>
</Event>

    <Event MSec= "14339.9633" PID=  "-1" PName=        "" TID=  "-1" EventName="Object/TypeDCEnd"
  TimeStamp="04/25/24 21:56:33.788975" ID="Illegal" Version="2" Keywords="0x00000000" TimeStampQPC="9,861,340,898,118" QPCTime="0.100us"
  Level="Always" ProviderName="Windows Kernel" ProviderGuid="9e814aad-3204-11d2-9a82-006008a86939" ClassicProvider="True" ProcessorNumber="8"
  Opcode="37" TaskGuid="89497f50-effe-4440-8cf2-ce6b1cdcaca7" Channel="0" PointerSize="8"
  CPU="8" EventIndex="2207886" TemplateType="ObjectTypeNameTraceData">
  <PrettyPrint>
    <Event MSec= "14339.9633" PID=  "-1" PName=        "" TID=  "-1" EventName="Object/TypeDCEnd" ObjectType="8" ObjectTypeName="Thread"/>
  </PrettyPrint>
  <Payload Length="18">
       0:   8  0  0  0 54  0 68  0 | 72  0 65  0 61  0 64  0   ....T.h. r.e.a.d.
      10:   0  0                   |                           ..
  </Payload>


    <Event MSec= "13914.7962" PID="6029331" PName="Process(6029331)" TID=  "-1" EventName="Object/HandleDCEnd"
  TimeStamp="04/25/24 21:56:33.363808" ID="Illegal" Version="2" Keywords="0x00000000" TimeStampQPC="9,861,336,646,447" QPCTime="0.100us"
  Level="Always" ProviderName="Windows Kernel" ProviderGuid="9e814aad-3204-11d2-9a82-006008a86939" ClassicProvider="True" ProcessorNumber="8"
  Opcode="39" TaskGuid="89497f50-effe-4440-8cf2-ce6b1cdcaca7" Channel="0" PointerSize="8"
  CPU="8" EventIndex="1666297" TemplateType="ObjectNameTraceData">
  <PrettyPrint>
    <Event MSec= "13914.7962" PID="6029331" PName="Process(6029331)" TID=  "-1" EventName="Object/HandleDCEnd" Object="0xFFFF810C862E09A0" Handle="0xFFFFFFFF80000044" ObjectType="19" ObjectName="\KernelObjects\HighMemoryCondition"/>
  </PrettyPrint>
  <Payload Length="88">
       0:  a0  9 2e 86  c 81 ff ff |  4  0  0  0 44  0  0 80   ........ ....D...
      10:  13  0 5c  0 4b  0 65  0 | 72  0 6e  0 65  0 6c  0   ..\.K.e. r.n.e.l.
      20:  4f  0 62  0 6a  0 65  0 | 63  0 74  0 73  0 5c  0   O.b.j.e. c.t.s.\.
      30:  48  0 69  0 67  0 68  0 | 4d  0 65  0 6d  0 6f  0   H.i.g.h. M.e.m.o.
      40:  72  0 79  0 43  0 6f  0 | 6e  0 64  0 69  0 74  0   r.y.C.o. n.d.i.t.
      50:  69  0 6f  0 6e  0  0  0 |                           i.o.n... 
  </Payload>
</Event

    */
}
