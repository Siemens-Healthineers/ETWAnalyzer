using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Extractors.Handle
{
    //    [dynamic: ToInstance, EventType{37, 38, 39, 40}]
    //class FileIo_V2_MapFile : FileIo_V2
    //{
    //    [WmiDataId(1), pointer, read] uint32 ViewBase;
    //    [WmiDataId(2), pointer, read] uint32 FileObject;
    //    [WmiDataId(3), format("x"), read] uint64 MiscInfo;
    //    [WmiDataId(4), extension("SizeT"), read] object ViewSize;
    //    [WmiDataId(5), read] uint32 ProcessId;
    //};
    /*
    <Event MSec = "283.4576" PID="6528" PName=     "dwm" TID="31380" EventName="FileIO/MapFile"
  TimeStamp="04/29/24 15:59:43.731700" ID="Illegal" Version="3" Keywords="0x00000000" TimeStampQPC="413,396,564,514" QPCTime="0.100us"
  Level="Always" ProviderName="Windows Kernel" ProviderGuid="9e814aad-3204-11d2-9a82-006008a86939" ClassicProvider="True" ProcessorNumber="9"
  Opcode="37" TaskGuid="90cbdc39-4a3e-11d1-84f4-0000f80464e3" Channel="0" PointerSize="8"
  CPU="9" EventIndex="2389" TemplateType="MapFileTraceData">
  <PrettyPrint>
    <Event MSec = "283.4576" PID="6528" PName=     "dwm" TID="31380" EventName="FileIO/MapFile" ViewBase="0x000002B93E7E0000" FileKey="0xFFFF82082C3FFB70" MiscInfo="0x00C1000000000000" ViewSize="0x00001000" ByteOffset="0x00000000" FileName=""/>
  </PrettyPrint>
  <Payload Length = "44" >
       0:   0  0 7e 3e b9  2  0  0 | 70 fb 3f 2c  8 82 ff ff..~&gt;.... p.?,....
      10:   0  0  0  0  0  0 c1  0 |  0 10  0  0  0  0  0  0   ........ ........
      20:   0  0  0  0  0  0  0  0 | 80 19  0  0               ........ ....
  </Payload>
</Event>
    */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct MapFileETW
    {
        public long ViewBase;
        public long FileKey;
        public long MiscInfo;
        public long ViewSize;
        public long ByteOffset;
        public UInt32 ProcessId;
    }

    class MapFileEvent : ObjectTraceBase
    {
        public long ViewBase { get; set; }
        public long FileObject { get; set; }
        public long MiscInfo { get; set; }
        public long ViewSize { get; set; }
        public long ByteOffset { get; set; }
    }

    class UnMapFileEvent : MapFileEvent
    {
    }
}
