// Taken from https://github.com/microsoft/perfview/blob/main/src/TraceEvent/Symbols/NativeSymbolModule.cs with adaptations

using Dia2Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Utilities;

namespace Microsoft.Diagnostics.Symbols
{
    /// <summary>
    /// A NativeSymbolModule represents symbol information for a native code module.   
    /// NativeSymbolModules can potentially represent Managed modules (which is why it is a subclass of that interface).  
    /// 
    /// NativeSymbolModule should just be the CONTRACT for Native Symbols (some subclass implements
    /// it for a particular format like Windows PDBs), however today because we have only one file format we
    /// simply implement Windows PDBS here.   This can be factored out of this class when we 
    /// support other formats (e.g. Dwarf).
    /// 
    /// To implmente support for Windows PDBs we use the Debug Interface Access (DIA).  See 
    /// http://msdn.microsoft.com/library/x93ctkx8.aspx for more.   I have only exposed what
    /// I need, and the interface is quite large (and not super pretty).  
    /// </summary>
    public unsafe class SymbolModule : ManagedSymbolModule, IDisposable
    {
        /// <summary>
        /// Returns the name of the type allocated for a given relative virtual address.
        /// Returns null if the given rva does not match a known heap allocation site.
        /// </summary>
        public string GetTypeForHeapAllocationSite(uint rva)
        {
            ThrowIfDisposed();

            return m_heapAllocationSites.Value.TryGetValue(rva, out var name) ? name : null;
        }

        /// <summary>
        /// Finds a (method) symbolic name for a given relative virtual address of some code.  
        /// Returns an empty string if a name could not be found. 
        /// </summary>
        public string FindNameForRva(uint rva)
        {
            ThrowIfDisposed();

            uint dummy = 0;
            return FindNameForRva(rva, ref dummy);
        }
        /// <summary>
        /// Finds a (method) symbolic name for a given relative virtual address of some code.  
        /// Returns an empty string if a name could not be found.  
        /// symbolStartRva is set to the start of the symbol start 
        /// </summary>
        public string FindNameForRva(uint rva, ref uint symbolStartRva)
        {
            ThrowIfDisposed();

            System.Threading.Thread.Sleep(0);           // Allow cancellation.  
            if (m_symbolsByAddr == null)
            {
                return "";
            }

            IDiaSymbol symbol = m_symbolsByAddr.symbolByRVA(rva);
            if (symbol == null)
            {
                Debug.WriteLine(string.Format("Warning: address 0x{0:x} not found.", rva));
                return "";
            }

            var ret = symbol.name;
            if (ret == null)
            {
                Debug.WriteLine(string.Format("Warning: address 0x{0:x} had a null symbol name.", rva));
                return "";
            }
            var symbolLen = symbol.length;
            if (symbolLen == 0)
            {
                Debug.WriteLine(string.Format("Warning: address 0x{0:x} symbol {1} has length 0", rva, ret));
            }
            symbolStartRva = symbol.relativeVirtualAddress;

            // TODO determine why this happens!
            var symbolRva = symbol.relativeVirtualAddress;
            if (!(symbolRva <= rva && rva < symbolRva + symbolLen) && symbolLen != 0)
            {
                m_reader.Log.WriteLine("Warning: NOT IN RANGE: address 0x{0:x} start {2:x} end {3:x} Offset {4:x} Len {5:x}, symbol {1}, prefixing with ??.",
                    rva, ret, symbolRva, symbolRva + symbolLen, rva - symbolRva, symbolLen);
                ret = "??" + ret;   // Prefix with ?? to indicate it is questionable.  
            }

            // TODO FIX NOW, should not need to do this hand-unmangling.
            if (0 <= ret.IndexOf('@'))
            {
                // TODO relatively inefficient.  
                string unmangled = null;
                symbol.get_undecoratedNameEx(0x1000, out unmangled);
                if (unmangled != null)
                {
                    ret = unmangled;
                }

                if (ret.StartsWith("@"))
                {
                    ret = ret.Substring(1);
                }

                if (ret.StartsWith("_"))
                {
                    ret = ret.Substring(1);
                }

                var atIdx = ret.IndexOf('@');
                if (0 < atIdx)
                {
                    ret = ret.Substring(0, atIdx);
                }
            }

            // See if this is a NGEN mangled name, which is $#Assembly#Token suffix.  If so strip it off. 
            var dollarIdx = ret.LastIndexOf('$');
            if (0 <= dollarIdx && dollarIdx + 2 < ret.Length && ret[dollarIdx + 1] == '#' && 0 <= ret.IndexOf('#', dollarIdx + 2))
            {
                ret = ret.Substring(0, dollarIdx);
            }

            //// See if we have a Project N map that maps $_NN to a pre-merged assembly name 
            //var mergedAssembliesMap = GetMergedAssembliesMap();
            //if (mergedAssembliesMap != null)
            //{
            //    bool prefixMatchFound = false;
            //    Regex prefixMatch = new Regex(@"\$(\d+)_");
            //    ret = prefixMatch.Replace(ret, delegate (Match m)
            //    {
            //        prefixMatchFound = true;
            //        var original = m.Groups[1].Value;
            //        var moduleIndex = int.Parse(original);
            //        return GetAssemblyNameFromModuleIndex(mergedAssembliesMap, moduleIndex, original);
            //    });

            //    // By default - .NET native compilers do not generate a $#_ prefix for the methods coming from 
            //    // the assembly containing System.Object - the implicit module number is int.MaxValue

            //    if (!prefixMatchFound)
            //    {
            //        ret = GetAssemblyNameFromModuleIndex(mergedAssembliesMap, int.MaxValue, String.Empty) + ret;
            //    }
            //}
            return ret;
        }

        private static string GetAssemblyNameFromModuleIndex(Dictionary<int, string> mergedAssembliesMap, int moduleIndex, string defaultValue)
        {
            string fullAssemblyName;
            if (mergedAssembliesMap.TryGetValue(moduleIndex, out fullAssemblyName))
            {
                try
                {
                    var assemblyName = new AssemblyName(fullAssemblyName);
                    return assemblyName.Name + "!";
                }
                catch (Exception) { } // Catch all AssemblyName fails with ' in the name.   
            }
            return defaultValue;
        }


        /// <summary>
        /// This overload of SourceLocationForRva like the one that takes only an RVA will return a source location
        /// if it can.   However this version has additional support for NGEN images.   In the case of NGEN images 
        /// for .NET V4.6.1 or later), the NGEN images can't convert all the way back to a source location, but they 
        /// can convert the RVA back to IL artifacts (ilAssemblyName, methodMetadataToken, iloffset).  THese can then
        /// be used to look up the source line using the IL PDB.  
        /// 
        /// Thus if the return value from this is null, check to see if the ilAssemblyName is non-null, and if not 
        /// you can look up the source location using that information.  
        /// </summary>
        public void SourceLocationForRva(uint rva, out string ilAssemblyName, out uint methodMetadataToken, out int ilOffset)
        {
            ThrowIfDisposed();

            ilAssemblyName = null;
            methodMetadataToken = 0;
            ilOffset = -1;
            //    m_reader.m_log.WriteLine("SourceLocationForRva: looking up RVA {0:x} ", rva);

            // First fetch the line number information 'normally'.  (for the non-NGEN case, and old style NGEN (with /lines)). 
            uint fetchCount;
            IDiaEnumLineNumbers sourceLocs;
            m_session.findLinesByRVA(rva, 0, out sourceLocs);
            IDiaLineNumber sourceLoc;
            sourceLocs.Next(1, out sourceLoc, out fetchCount);
            if (fetchCount == 0)
            {
                // We have no native line number information.   See if we are an NGEN image and we can convert the RVA to an IL Offset.   
                //  m_reader.m_log.WriteLine("SourceLocationForRva: did not find line info Looking for mangled symbol name (for NGEN pdbs)");
                IDiaSymbol method = m_symbolsByAddr.symbolByRVA(rva);
                if (method != null)
                {
                    // Check to see if the method name follows the .NET V4.6.1 conventions
                    // of $#ASSEMBLY#TOKEN.   If so the line number we got back is not a line number at all but
                    // an ILOffset. 
                    string name = method.name;
                    if (name != null)
                    {
                    }
                }
                //  m_reader.m_log.WriteLine("SourceLocationForRva: No lines for RVA {0:x} ", rva);

            }
        }

        /// <summary>
        /// The a unique identifier that is used to relate the DLL and its PDB.   
        /// </summary>
        public override Guid PdbGuid
        {
            get
            {
                ThrowIfDisposed();
                return m_session.globalScope.guid;
            }
        }

        /// <summary>
        /// Along with the PdbGuid, there is a small integer 
        /// call the age is also used to find the PDB (it represents the different 
        /// post link transformations the DLL has undergone).  
        /// </summary>
        public override int PdbAge
        {
            get
            {
                ThrowIfDisposed();
                return (int)m_session.globalScope.age;
            }
        }

        static Dictionary<uint, string> myEmpty = new Dictionary<uint, string>();

        private SymbolModule(SymbolReader reader, string pdbFilePath, Action<IDiaDataSource3> loadData) : base(reader, pdbFilePath)
        {
            m_reader = reader;

            m_source = DiaLoader.GetDiaSourceObject();
            loadData(m_source);
            m_source.openSession(out m_session);
            m_session.getSymbolsByAddr(out m_symbolsByAddr);

            m_heapAllocationSites = new Lazy<IReadOnlyDictionary<uint, string>>(() =>
            {
                // Retrieves the S_HEAPALLOCSITE information from the pdb as described here:
                // https://docs.microsoft.com/visualstudio/profiling/custom-native-etw-heap-events
                Dictionary<uint, string> result = null;
                m_session.getHeapAllocationSites(out var diaEnumSymbols);
                for (; ; )
                {
                    diaEnumSymbols.Next(1, out var sym, out var fetchCount);
                    if (fetchCount == 0)
                    {
                        return (IReadOnlyDictionary<uint, string>)result ?? myEmpty;
                    }

                    result = result ?? new Dictionary<uint, string>();
                    m_session.symbolById(sym.typeId, out var typeSym);
                    result[sym.relativeVirtualAddress + (uint)sym.length] = HeapAllocationTypeInfo.GetTypeName(typeSym);
                }
            });

            //    m_reader.m_log.WriteLine("Opening PDB {0} with signature GUID {1} Age {2}", pdbFilePath, PdbGuid, PdbAge);
        }

        internal SymbolModule(SymbolReader reader, string pdbFilePath)
            : this(reader, pdbFilePath, s => s.loadDataFromPdb(pdbFilePath))
        {
        }

        internal SymbolModule(SymbolReader reader, string pdbFilePath, Stream pdbStream)
            : this(reader, pdbFilePath, s => s.loadDataFromIStream(new ComStreamWrapper(pdbStream)))
        {
        }

        internal void LogManagedInfo(string pdbName, Guid pdbGuid, int pdbAge)
        {
            // Simply remember this if we decide we need it for source server support
            m_managedPdbName = pdbName;
            m_managedPdbGuid = pdbGuid;
            m_managedPdbAge = pdbAge;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                m_isDisposed = true;

                if (m_session is IDiaSession3 diaSession3)
                {
                    int hr = diaSession3.dispose();
                    Debug.Assert(hr == 0, "IDiaSession3.dispose failed");
                }
            }
        }

        /// <summary>
        /// This function checks if the SymbolModule is disposed before proceeding with the call.
        /// This is important because DIA doesn't provide any guarantees as to what will happen if 
        /// one attempts to call after the session is disposed, so this at least ensure that we
        /// fail cleanly in non-concurrent cases.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (m_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SymbolModule));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodMetadataToken"></param>
        /// <param name="ilOffset"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override SourceLocation SourceLocationForManagedCode(uint methodMetadataToken, int ilOffset)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This static class contains the GetTypeName method for retrieving the type name of 
        /// a heap allocation site. 
        /// 
        /// See https://github.com/KirillOsenkov/Dia2Dump/blob/master/PrintSymbol.cpp for more details
        /// </summary>
        private static class HeapAllocationTypeInfo
        {
            internal static string GetTypeName(IDiaSymbol symbol)
            {
                var name = symbol.name ?? "<unknown>";

                switch ((SymTagEnum)symbol.symTag)
                {
                    case SymTagEnum.UDT:
                    case SymTagEnum.Enum:
                    case SymTagEnum.Typedef:
                        return name;
                    case SymTagEnum.FunctionType:
                        return "function";
                    case SymTagEnum.PointerType:
                        return $"{GetTypeName(symbol.type)} {(symbol.reference != 0 ? "&" : "*")}";
                    case SymTagEnum.ArrayType:
                        return "array";
                    case SymTagEnum.BaseType:
                        var sb = new StringBuilder();
                        switch ((BasicType)symbol.baseType)
                        {
                            case BasicType.btUInt:
                                sb.Append("unsigned ");
                                goto case BasicType.btInt;
                            case BasicType.btInt:
                                switch (symbol.length)
                                {
                                    case 1:
                                        sb.Append("char");
                                        break;
                                    case 2:
                                        sb.Append("short");
                                        break;
                                    case 4:
                                        sb.Append("int");
                                        break;
                                    case 8:
                                        sb.Append("long");
                                        break;
                                }
                                return sb.ToString();
                            case BasicType.btFloat:
                                return symbol.length == 4 ? "float" : "double";
                            default:
                                return BaseTypes.Length > symbol.baseType ? BaseTypes[symbol.baseType] : $"base type {symbol.baseType}";
                        }
                }

                return $"unhandled symbol tag {symbol.symTag}";
            }

            private enum SymTagEnum
            {
                Null,
                Exe,
                Compiland,
                CompilandDetails,
                CompilandEnv,
                Function,
                Block,
                Data,
                Annotation,
                Label,
                PublicSymbol,
                UDT,
                Enum,
                FunctionType,
                PointerType,
                ArrayType,
                BaseType,
                Typedef,
                BaseClass,
                Friend,
                FunctionArgType,
                FuncDebugStart,
                FuncDebugEnd,
                UsingNamespace,
                VTableShape,
                VTable,
                Custom,
                Thunk,
                CustomType,
                ManagedType,
                Dimension,
                CallSite,
                InlineSite,
                BaseInterface,
                VectorType,
                MatrixType,
                HLSLType
            };

            // See https://learn.microsoft.com/visualstudio/debugger/debug-interface-access/basictype
            private enum BasicType
            {
                btNoType = 0,
                btVoid = 1,
                btChar = 2,
                btWChar = 3,
                btInt = 6,
                btUInt = 7,
                btFloat = 8,
                btBCD = 9,
                btBool = 10,
                btLong = 13,
                btULong = 14,
                btCurrency = 25,
                btDate = 26,
                btVariant = 27,
                btComplex = 28,
                btBit = 29,
                btBSTR = 30,
                btHresult = 31,
                btChar16 = 32,  // char16_t
                btChar32 = 33,  // char32_t
                btChar8 = 34,   // char8_t
            };

            private static readonly string[] BaseTypes = new[]
            {
                "<NoType>",             // btNoType = 0,
                "void",                 // btVoid = 1,
                "char",                 // btChar = 2,
                "wchar_t",              // btWChar = 3,
                "signed char",
                "unsigned char",
                "int",                  // btInt = 6,
                "unsigned int",         // btUInt = 7,
                "float",                // btFloat = 8,
                "<BCD>",                // btBCD = 9,
                "bool",                 // btBool = 10,
                "short",
                "unsigned short",
                "long",                 // btLong = 13,
                "unsigned long",        // btULong = 14,
                "__int8",
                "__int16",
                "__int32",
                "__int64",
                "__int128",
                "unsigned __int8",
                "unsigned __int16",
                "unsigned __int32",
                "unsigned __int64",
                "unsigned __int128",
                "<currency>",           // btCurrency = 25,
                "<date>",               // btDate = 26,
                "VARIANT",              // btVariant = 27,
                "<complex>",            // btComplex = 28,
                "<bit>",                // btBit = 29,
                "BSTR",                 // btBSTR = 30,
                "HRESULT",              // btHresult = 31,
                "char16_t",             // btChar16 = 32,
                "char32_t",             // btChar32 = 33,
                "char8_t",              // btChar8 = 34
            };
        }

        private bool m_isDisposed;

        private string m_managedPdbName;
        private Guid m_managedPdbGuid;
        private int m_managedPdbAge;

        internal readonly IDiaSession m_session;
        private readonly SymbolReader m_reader;
        private readonly IDiaDataSource3 m_source;
        private readonly IDiaEnumSymbolsByAddr m_symbolsByAddr;
        private readonly Lazy<IReadOnlyDictionary<uint, string>> m_heapAllocationSites; // rva => typename

        [StructLayout(LayoutKind.Sequential)]
        struct SrcFormatHeader
        {
            public Guid language;
            public Guid languageVendor;
            public Guid documentType;
            public Guid algorithmId;
            public UInt32 checkSumSize;
            public UInt32 sourceSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SrcFormat
        {
            public SrcFormatHeader Header;
            public fixed byte checksumBytes[512 / 8]; // this size of this may be smaller, it is controlled by the size of the `checksumSize` field
        }

        private static readonly Guid guidMD5 = new Guid("406ea660-64cf-4c82-b6f0-42d48172a799");
        private static readonly Guid guidSHA1 = new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460");
        private static readonly Guid guidSHA256 = new Guid("8829d00f-11b8-4213-878b-770e8597ac16");

    }
}

 

#region private classes

internal sealed class ComStreamWrapper : IStream
{
    private readonly Stream stream;

    public ComStreamWrapper(Stream stream)
    {
        this.stream = stream;
    }

    public void Commit(uint grfCommitFlags)
    {
        throw new NotSupportedException();
    }

    public unsafe void RemoteRead(out byte pv, uint cb, out uint pcbRead)
    {
        byte[] buf = new byte[cb];

        int bytesRead = stream.Read(buf, 0, (int)cb);
        pcbRead = (uint)bytesRead;

        fixed (byte* p = &pv)
        {
            for (int i = 0; i < bytesRead; i++)
            {
                p[i] = buf[i];
            }
        }
    }

    public unsafe void RemoteSeek(_LARGE_INTEGER dlibMove, uint origin, out _ULARGE_INTEGER plibNewPosition)
    {
        long newPosition = stream.Seek(dlibMove.QuadPart, (SeekOrigin)origin);
        plibNewPosition.QuadPart = (ulong)newPosition;
    }

    public void SetSize(_ULARGE_INTEGER libNewSize)
    {
        throw new NotSupportedException();
    }

    public void Stat(out tagSTATSTG pstatstg, uint grfStatFlag)
    {
        pstatstg = new tagSTATSTG()
        {
            cbSize = new _ULARGE_INTEGER() { QuadPart = (ulong)stream.Length }
        };
    }

    public unsafe void RemoteWrite(ref byte pv, uint cb, out uint pcbWritten)
    {
        throw new NotSupportedException();
    }

    public void Clone(out IStream ppstm)
    {
        throw new NotSupportedException();
    }

    public void RemoteCopyTo(IStream pstm, _ULARGE_INTEGER cb, out _ULARGE_INTEGER pcbRead, out _ULARGE_INTEGER pcbWritten)
    {
        throw new NotSupportedException();
    }

    public void LockRegion(_ULARGE_INTEGER libOffset, _ULARGE_INTEGER cb, uint lockType)
    {
        throw new NotSupportedException();
    }

    public void Revert()
    {
        throw new NotSupportedException();
    }

    public void UnlockRegion(_ULARGE_INTEGER libOffset, _ULARGE_INTEGER cb, uint lockType)
    {
        throw new NotSupportedException();
    }
}

namespace Dia2Lib
{
    /// <summary>
    /// The DiaLoader class knows how to load the msdia140.dll (the Debug Access Interface) (see docs at
    /// http://msdn.microsoft.com/en-us/library/x93ctkx8.aspx), without it being registered as a COM object.
    /// Basically it just called the DllGetClassObject interface directly.
    /// 
    /// It has one public method 'GetDiaSourceObject' which knows how to create a IDiaDataSource object. 
    /// From there you can do anything you need.  
    /// 
    /// In order to get IDiaDataSource3 which includes'getStreamSize' API, you need to use the 
    /// vctools\langapi\idl\dia2_internal.idl file from devdiv to produce Dia2Lib.dll
    /// 
    /// roughly what you need to do is 
    ///     copy vctools\langapi\idl\dia2_internal.idl .
    ///     copy vctools\langapi\idl\dia2.idl .
    ///     copy vctools\langapi\include\cvconst.h .
    ///     Change dia2.idl to include interface IDiaDataSource3 inside library Dia2Lib->importlib->coclass DiaSource
    ///     midl dia2_internal.idl /D CC_DP_CXX
    ///     tlbimp dia2_internal.tlb
    ///     REM result is Dia2Lib.dll 
    /// </summary>
    internal static class DiaLoader
    {
        /// <summary>
        /// Load the msdia100 dll and get a IDiaDataSource from it.  This is your gateway to PDB reading.   
        /// </summary>
        public static IDiaDataSource3 GetDiaSourceObject()
        {
            if (!s_loadedNativeDll)
            {
                // Ensure that the native DLL we need exist.  
                NativeDlls.LoadNative("msdia140.dll");
                s_loadedNativeDll = true;
            }

            // This is the value it was for msdia120 and before 
            // var diaSourceClassGuid = new Guid("{3BFCEA48-620F-4B6B-81F7-B9AF75454C7D}");

            // This is the value for msdia140.  
            var diaSourceClassGuid = new Guid("{e6756135-1e65-4d17-8576-610761398c3c}");
            var comClassFactory = (IClassFactory)DllGetClassObject(diaSourceClassGuid, typeof(IClassFactory).GetTypeInfo().GUID);

            object comObject = null;
            Guid iDataDataSourceGuid = typeof(IDiaDataSource3).GetTypeInfo().GUID;
            comClassFactory.CreateInstance(null, ref iDataDataSourceGuid, out comObject);
            return (comObject as IDiaDataSource3);
        }
        #region private
        [ComImport, ComVisible(false), Guid("00000001-0000-0000-C000-000000000046"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IClassFactory
        {
            void CreateInstance([MarshalAs(UnmanagedType.Interface)] object aggregator,
                                ref Guid refiid,
                                [MarshalAs(UnmanagedType.Interface)] out object createdObject);
            void LockServer(bool incrementRefCount);
        }

        // Methods
        [return: MarshalAs(UnmanagedType.Interface)]
        [DllImport("msdia140.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        private static extern object DllGetClassObject(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);

        /// <summary>
        /// Used to ensure the native library is loaded at least once prior to trying to use it. No protection is
        /// included to avoid multiple loads, but this is not a problem since we aren't trying to unload the library
        /// after use.
        /// </summary>
        private static bool s_loadedNativeDll;
        #endregion
    }

    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GuidAttribute("52585014-e2b6-49fe-aa72-3a1e178682ee")]
    interface IDiaSession3
    {
        #region Uncalled methods to declare the VTable correctly
        void Reserved01(); // get_loadAddress
        void Reserved02(); // put_loadAddress
        void Reserved03(); // get_globalScope
        void Reserved04(); // getEnumTables
        void Reserved05(); // getSymbolsByAddr
        void Reserved06(); // findChildren
        void Reserved07(); // findChildrenEx
        void Reserved08(); // findChildrenExByAddr
        void Reserved09(); // findChildrenExByVA
        void Reserved10(); // findChildrenExByRVA
        void Reserved11(); // findSymbolByAddr
        void Reserved12(); // findSymbolByRVA
        void Reserved13(); // findSymbolByVA
        void Reserved14(); // findSymbolByToken
        void Reserved15(); // symsAreEquiv
        void Reserved16(); // symbolById
        void Reserved17(); // findSymbolByRVAEx
        void Reserved18(); // findSymbolByVAEx
        void Reserved19(); // findFile
        void Reserved20(); // findFileById
        void Reserved21(); // findLines
        void Reserved22(); // findLinesByAddr
        void Reserved23(); // findLinesByRVA
        void Reserved24(); // findLinesByVA
        void Reserved25(); // findLinesByLinenum
        void Reserved26(); // findInjectedSource
        void Reserved27(); // getEnumDebugStreams
        void Reserved28(); // findInlineFramesByAddr
        void Reserved29(); // findInlineFramesByRVA
        void Reserved30(); // findInlineFramesByVA
        void Reserved31(); // findInlineeLines
        void Reserved32(); // findInlineeLinesByAddr
        void Reserved33(); // findInlineeLinesByRVA
        void Reserved34(); // findInlineeLinesByVA
        void Reserved35(); // findInlineeLinesByLinenum
        void Reserved36(); // findInlineesByName
        void Reserved37(); // findAcceleratorInlineeLinesByLinenum
        void Reserved38(); // findSymbolsForAcceleratorPointerTag
        void Reserved39(); // findSymbolsByRVAForAcceleratorPointerTag
        void Reserved40(); // findAcceleratorInlineesByName
        void Reserved41(); // addressForVA
        void Reserved42(); // addressForRVA
        void Reserved43(); // findILOffsetsByAddr
        void Reserved44(); // findILOffsetsByRVA
        void Reserved45(); // findILOffsetsByVA
        void Reserved46(); // findInputAssemblyFiles
        void Reserved47(); // findInputAssembly
        void Reserved48(); // findInputAssemblyById
        void Reserved49(); // getFuncMDTokenMapSize
        void Reserved50(); // getFuncMDTokenMap
        void Reserved51(); // getTypeMDTokenMapSize
        void Reserved52(); // getTypeMDTokenMap
        void Reserved53(); // getNumberOfFunctionFragments_VA
        void Reserved54(); // getNumberOfFunctionFragments_RVA
        void Reserved55(); // getFunctionFragments_VA
        void Reserved56(); // getFunctionFragments_RVA
        void Reserved57(); // getExports
        void Reserved58(); // getHeapAllocationSites
        void Reserved59(); // findInputAssemblyFile
        void Reserved60(); // addPublicSymbol
        void Reserved61(); // addStaticSymbol
        void Reserved62(); // findSectionAddressByCrc
        void Reserved63(); // findThunkSymbol
        void Reserved64(); // makeThunkSymbol
        void Reserved65(); // mergeObjPDB
        void Reserved66(); // commitObjPDBMerge
        void Reserved67(); // cancelObjPDBMerge
        void Reserved68(); // getLinkInfo
        void Reserved69(); // isMiniPDB
        void Reserved70(); // prepareEnCRebuild
        #endregion

        [PreserveSig] 
        int dispose();
    };
}
#endregion
