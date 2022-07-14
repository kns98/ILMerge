// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
#if FxCop
using System.Collections.Generic;
using AssemblyReferenceList = Microsoft.Cci.AssemblyReferenceCollection;
using AttributeList = Microsoft.Cci.AttributeNodeCollection;
using BlockList = Microsoft.Cci.BlockCollection;
using ExpressionList = Microsoft.Cci.ExpressionCollection;
using InstructionList = Microsoft.Cci.InstructionCollection;
using Int32List = System.Collections.Generic.List<int>;
using InterfaceList = Microsoft.Cci.InterfaceCollection;
using LocalList = Microsoft.Cci.LocalCollection;
using MemberList = Microsoft.Cci.MemberCollection;
using MethodList = Microsoft.Cci.MethodCollection;
using ModuleReferenceList = Microsoft.Cci.ModuleReferenceCollection;
using NamespaceList = Microsoft.Cci.NamespaceCollection;
using ParameterList = Microsoft.Cci.ParameterCollection;
using ResourceList = Microsoft.Cci.ResourceCollection;
using SecurityAttributeList = Microsoft.Cci.SecurityAttributeCollection;
using StatementList = Microsoft.Cci.StatementCollection;
using TypeNodeList = Microsoft.Cci.TypeNodeCollection;
using Win32ResourceList = Microsoft.Cci.Win32ResourceCollection;
using Property = Microsoft.Cci.PropertyNode;
using Module = Microsoft.Cci.ModuleNode;
using Class = Microsoft.Cci.ClassNode;
using Interface = Microsoft.Cci.InterfaceNode;
using Event = Microsoft.Cci.EventNode;
using Return = Microsoft.Cci.ReturnNode;
using Throw = Microsoft.Cci.ThrowNode;
#endif
#if CCINamespace
using Microsoft.Cci;
#else
#endif

#if CCINamespace
namespace Microsoft.Cci.Metadata{
#else
namespace System.Compiler.Metadata
{
#endif

#if !ROTOR
    internal enum CorOpenFlags : uint
    {
        ofRead = 0x00000000, // Open scope for read
        ofWrite = 0x00000001, // Open scope for write.
        ofCopyMemory = 0x00000002, // Open scope with memory. Ask metadata to maintain its own copy of memory.
        ofCacheImage = 0x00000004, // EE maps but does not do relocations or verify image
        ofNoTypeLib = 0x00000080 // Don't OpenScope on a typelib.
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("809c652e-7396-11d2-9771-00A0C9B4D50C")]
    internal interface IMetaDataDispenser
    {
        void DefineScope(ref Guid clsid, uint createFlags, [In] ref Guid iid,
            [MarshalAs(UnmanagedType.IUnknown)] out object retval);

        [PreserveSig]
        int OpenScope(string scope, uint openFlags, [In] ref Guid iid,
            [MarshalAs(UnmanagedType.IUnknown)] out object import);

        void OpenScopeOnMemory(IntPtr data, uint dataSize, uint openFlags, [In] ref Guid iid,
            [MarshalAs(UnmanagedType.IUnknown)] out object retval);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("AA544D42-28CB-11d3-BD22-0000F80849BD")]
    internal interface ISymUnmanagedBinder
    {
        [PreserveSig]
        int GetReaderForFile([MarshalAs(UnmanagedType.IUnknown)] object importer, string filename, string searchPath,
            out ISymUnmanagedReader reader);

        ISymUnmanagedReader GetReaderForStream([MarshalAs(UnmanagedType.IUnknown)] object importer,
            [MarshalAs(UnmanagedType.IUnknown)] object stream);
    }

    [ComImport]
    [Guid("ACCEE350-89AF-4ccb-8B40-1C2C4C6F9434")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    internal interface ISymUnmanagedBinder2 : ISymUnmanagedBinder
    {
        void GetReaderForFile(IntPtr importer, [MarshalAs(UnmanagedType.LPWStr)] string filename,
            [MarshalAs(UnmanagedType.LPWStr)] string SearchPath,
            [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);

        void GetReaderFromStream(IntPtr importer, IntPtr stream,
            [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);

        [PreserveSig]
        int GetReaderForFile2([MarshalAs(UnmanagedType.IUnknown)] object importer,
            [MarshalAs(UnmanagedType.LPWStr)] string fileName, [MarshalAs(UnmanagedType.LPWStr)] string searchPath,
            int searchPolicy, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader pRetVal);
//    void GetReaderForFile3(IntPtr importer, [MarshalAs(UnmanagedType.LPWStr)] String fileName, [MarshalAs(UnmanagedType.LPWStr)] String searchPath, int searchPolicy, IntPtr callback, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader pRetVal);
    }

    [ComImport]
    [Guid("AA544D41-28CB-11d3-BD22-0000F80849BD")]
    internal class CorSymBinder
    {
    }

    [ComImport]
    [Guid("0A29FF9E-7F9C-4437-8B11-F424491E3931")]
    internal class CorSymBinder2
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("B4CE6286-2A6B-3712-A3B7-1EE1DAD467B5")]
    internal interface ISymUnmanagedReader
    {
        ISymUnmanagedDocument GetDocument(string url, ref Guid language, ref Guid languageVendor,
            ref Guid documentType);

        void GetDocuments(uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedDocument[] docs);

        uint GetUserEntryPoint();

        [PreserveSig]
        int GetMethod(uint token, ref ISymUnmanagedMethod method);

        ISymUnmanagedMethod GetMethodByVersion(uint token, int version);

        void GetVariables(uint parent, uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ISymUnmanagedVariable[] vars);

        void GetGlobalVariables(uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] vars);

        ISymUnmanagedMethod GetMethodFromDocumentPosition(ISymUnmanagedDocument document, uint line, uint column);

        void GetSymAttribute(uint parent, string name, ulong size, ref uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] buffer);

        void GetNamespaces(uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] namespaces);

        void Initialize([MarshalAs(UnmanagedType.IUnknown)] object importer, string filename, string searchPath,
            [MarshalAs(UnmanagedType.IUnknown)] object stream);

        void UpdateSymbolStore(string filename, [MarshalAs(UnmanagedType.IUnknown)] object stream);
        void ReplaceSymbolStore(string filename, [MarshalAs(UnmanagedType.IUnknown)] object stream);

        void GetSymbolStoreFileName(uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] name);

        void GetMethodsFromDocumentPosition(ISymUnmanagedDocument document, uint line, uint column, uint size,
            out uint length, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] ISymUnmanagedMethod[] retval);

        void GetDocumentVersion(ISymUnmanagedDocument doc, out int version, out bool isLatest);
        void GetMethodVersion(ISymUnmanagedMethod method, out int version);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("B62B923C-B500-3158-A543-24F307A8B7E1")]
    internal interface ISymUnmanagedMethod
    {
        uint GetToken();
        uint GetSequencePointCount();
        ISymUnmanagedScope GetRootScope();
        ISymUnmanagedScope GetScopeFromOffset(uint offset);
        uint Getoffset(ISymUnmanagedDocument document, uint line, uint column);

        void GetRanges(ISymUnmanagedDocument document, uint line, uint column, uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] ranges);

        void GetParameters(uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] parms);

        IntPtr GetNamespace();

        bool GetSourceStartEnd([MarshalAs(UnmanagedType.LPArray, SizeConst = 2)] ISymUnmanagedDocument[] docs,
            [MarshalAs(UnmanagedType.LPArray)] uint[] lines, [MarshalAs(UnmanagedType.LPArray)] uint[] columns);

        void GetSequencePoints(uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
            uint[] offsets,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.IUnknown, SizeParamIndex = 0)]
            IntPtr[] documents,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
            uint[] lines,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
            uint[] columns,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
            uint[] endLines,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
            uint[] endColumns);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("40DE4037-7C81-3E1E-B022-AE1ABFF2CA08")]
    internal interface ISymUnmanagedDocument
    {
        void GetURL(uint size, out uint length, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] url);
        void GetDocumentType(out Guid retval);
        void GetLanguage(out Guid retval);
        void GetLanguageVendor(out Guid retval);
        void GetCheckSumAlgorithmId(out Guid retval);

        void GetCheckSum(uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] data);

        uint FindClosestLine(uint line);
        bool HasEmbeddedSource();
        uint GetSourceLength();

        void GetSourceRange(uint startLine, uint startColumn, uint endLine, uint endColumn, uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] source);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("9F60EEBE-2D9A-3F7C-BF58-80BC991C60BB")]
    internal interface ISymUnmanagedVariable
    {
        void GetName(uint size, out uint length, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] name);
        uint GetAttributes();

        void GetSignature(uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] name);

        uint GetAddressKind();
        uint GetAddressField1();
        uint GetAddressField2();
        uint GetAddressField3();
        uint GetStartOffset();
        uint GetEndOffset();
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("68005D0F-B8E0-3B01-84D5-A11A94154942")]
    internal interface ISymUnmanagedScope
    {
        ISymUnmanagedMethod GetMethod();
        ISymUnmanagedScope GetParent();

        void GetChildren(uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] children);

        uint GetStartOffset();
        uint GetEndOffset();
        uint GetLocalCount();

        void GetLocals(uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] locals);

        void GetNamespaces(uint size, out uint length,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] namespaces);
    }
#endif
    internal sealed class UnmanagedBuffer : IDisposable
    {
        internal IntPtr Pointer;

        internal UnmanagedBuffer(int length)
        {
            Pointer = Marshal.AllocHGlobal(length);
        }

        public void Dispose()
        {
            if (Pointer != IntPtr.Zero)
                Marshal.FreeHGlobal(Pointer);
            Pointer = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }

        ~UnmanagedBuffer()
        {
            Dispose();
        }
    }

    internal unsafe class Reader : IDisposable
    {
        private readonly string directory;
        private readonly string fileName;
        private readonly bool doNotLockFile;

        private Module /*!*/
            module = new Module();

        internal TypeNode currentType;
        private long sortedTablesMask;

        internal MetadataReader /*!*/
            tables;

        private UnmanagedBuffer unmanagedBuffer;
        private int bufferLength;

        private readonly IDictionary /*!*/
            localAssemblyCache; //use for simple names

        internal static readonly IDictionary /*!*/
            StaticAssemblyCache = new SynchronizedWeakDictionary(); //use for strong names

        private readonly bool useStaticCache;

        //^ [Microsoft.Contracts.SpecInternal]
        private TrivialHashtable namespaceTable;
        internal NamespaceList namespaceList;
#if CodeContracts
        internal PdbInfo pdbInfo;
#endif
#if !ROTOR
        internal ISymUnmanagedReader debugReader;
        private Dictionary<IntPtr, UnmanagedDocument> debugDocuments;
#endif
#if FxCop
    internal static bool probeGAC = true;
#endif
        internal bool getDebugSymbols;
        private bool getDebugSymbolsFailed;
        private TypeNodeList currentTypeParameters;
        private TypeNodeList currentMethodTypeParameters;
        internal bool preserveShortBranches;
#if !MinimalReader
        internal Reader(byte[] /*!*/ buffer, IDictionary localAssemblyCache, bool doNotLockFile, bool getDebugInfo,
            bool useStaticCache, bool preserveShortBranches)
        {
            Debug.Assert(buffer != null);
            if (localAssemblyCache == null) localAssemblyCache = new Hashtable();
            this.localAssemblyCache = localAssemblyCache;
            getDebugSymbols = getDebugInfo;
            this.doNotLockFile = false;
            this.useStaticCache = useStaticCache;
            this.preserveShortBranches = preserveShortBranches;
            var n = bufferLength = buffer.Length;
            unmanagedBuffer = new UnmanagedBuffer(n);
            //^ base();
            var pb = (byte*)unmanagedBuffer.Pointer;
            for (var i = 0; i < n; i++) *pb++ = buffer[i];
        }
#endif
        internal Reader(string /*!*/ fileName, IDictionary localAssemblyCache, bool doNotLockFile, bool getDebugInfo,
            bool useStaticCache, bool preserveShortBranches)
        {
            if (localAssemblyCache == null) localAssemblyCache = new Hashtable();
            this.localAssemblyCache = localAssemblyCache;
            fileName = Path.GetFullPath(fileName);
            this.fileName = fileName;
            directory = Path.GetDirectoryName(fileName);
            getDebugSymbols = getDebugInfo;
            this.doNotLockFile = doNotLockFile;
            this.useStaticCache = useStaticCache;
            this.preserveShortBranches = preserveShortBranches;
            //^ base();
        }

        internal Reader(IDictionary localAssemblyCache, bool doNotLockFile, bool getDebugInfo, bool useStaticCache,
            bool preserveShortBranches)
        {
            if (localAssemblyCache == null) localAssemblyCache = new Hashtable();
            this.localAssemblyCache = localAssemblyCache;
            directory = Directory.GetCurrentDirectory();
            getDebugSymbols = getDebugInfo;
            this.doNotLockFile = doNotLockFile;
            this.useStaticCache = useStaticCache;
            this.preserveShortBranches = preserveShortBranches;
            //^ base();
        }

        public void Dispose()
        {
            if (unmanagedBuffer != null)
                unmanagedBuffer.Dispose();
            unmanagedBuffer = null;
            if (tables != null)
                tables.Dispose();
            //this.tables = null;
#if !ROTOR
            if (debugReader != null)
                Marshal.ReleaseComObject(debugReader);
            debugReader = null;
            debugDocuments = null;
#endif
        }

        private void SetupReader()
        {
            Debug.Assert(localAssemblyCache != null);
#if !ROTOR
            if (doNotLockFile)
            {
#endif
                using (var inputStream = new FileStream(fileName,
                           FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ReadFileIntoUnmanagedBuffer(inputStream);
                }
#if !ROTOR
            }

            if (unmanagedBuffer == null)
                tables = new MetadataReader(fileName); //Uses a memory map that locks the file
            else
#endif
                tables = new MetadataReader((byte*)unmanagedBuffer.Pointer, bufferLength);
            //^ assume this.tables.tablesHeader != null;
            sortedTablesMask = tables.tablesHeader.maskSorted;
        }
#if !ROTOR
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadFile(IntPtr FileHandle, byte* Buffer, int NumberOfBytesToRead,
            int* NumberOfBytesRead, IntPtr Overlapped);
#endif
        private void ReadFileIntoUnmanagedBuffer(FileStream /*!*/ inputStream)
        {
            var size = inputStream.Seek(0, IO.SeekOrigin.End);
            if (size > int.MaxValue) throw new FileLoadException();
            inputStream.Seek(0, IO.SeekOrigin.Begin);
            var n = (int)size;
            bufferLength = n;
            unmanagedBuffer = new UnmanagedBuffer(n);
            var pb = (byte*)unmanagedBuffer.Pointer;
#if !ROTOR
#if WHIDBEY && !OldWhidbey
            if (!ReadFile(inputStream.SafeFileHandle.DangerousGetHandle(), pb, n, &n, IntPtr.Zero))
                throw new FileLoadException();
#else
      if (!Reader.ReadFile(inputStream.Handle, pb, n, &n, IntPtr.Zero)) throw new System.IO.FileLoadException();
#endif
#else
      //Read a fixed length block at a time, so that the GC does not come under pressure from lots of large byte arrays.
      int bufferLen = 8096;
      byte[] buffer = new byte[bufferLen];
      while (n > 0){
        if (n < bufferLen) bufferLen = n;
        inputStream.Read(buffer, 0, bufferLen);
        for (int i = 0; i < bufferLen; i++) *pb++ = buffer[i];
        n -= bufferLen;
      }
#endif
        }

        internal void SetupDebugReader(string filename, string pdbSearchPath)
        {
#if CodeContracts
            var pdbFileName = BetterPath.ChangeExtension(filename, "pdb");
            getDebugSymbolsFailed = true;
            //TODO: use search path
            if (File.Exists(pdbFileName))
                using (var inputStream = new FileStream(pdbFileName,
                           FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    pdbInfo = new PdbInfo(inputStream, this);
                    getDebugSymbolsFailed = false;
                }
#endif
#if !ROTOR
            if (filename == null) return;
            CorSymBinder binderObj1 = null;
            CorSymBinder2 binderObj2 = null;
            getDebugSymbolsFailed = false;
            object importer = null;
            try
            {
                var hresult = 0;
                try
                {
                    binderObj2 = new CorSymBinder2();
                    var binder2 = (ISymUnmanagedBinder2)binderObj2;
#if !NoWriter
                    importer = new Ir2md(new Module());
#else
            importer = new EmptyImporter();
#endif
                    hresult = binder2.GetReaderForFile(importer, filename, pdbSearchPath, out debugReader);
                }
                catch (COMException e)
                {
                    // could not instantiate ISymUnmanagedBinder2, fall back to ISymUnmanagedBinder
                    if ((uint)e.ErrorCode == 0x80040111)
                    {
                        binderObj1 = new CorSymBinder();
                        var binder = (ISymUnmanagedBinder)binderObj1;
                        hresult = binder.GetReaderForFile(importer, filename, null, out debugReader);
                    }
                    else
                    {
                        throw;
                    }
                }

                switch ((uint)hresult)
                {
                    case 0x0: break;
                    case 0x806d0005: // EC_NOT_FOUND
                    case 0x806d0014: // EC_INVALID_EXE_TIMESTAMP
#if FxCop
            this.getDebugSymbols = false;
            this.getDebugSymbolsFailed = true;
#else
                        // Sometimes GetReaderForFile erroneously reports missing pdb files as being "out of date", 
                        // so we check if the file actually exists before reporting the error.
                        // The mere absence of a pdb file is not an error. If not present, do not report.
                        if (File.Exists(Path.ChangeExtension(filename, ".pdb")))
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                ExceptionStrings.PdbAssociatedWithFileIsOutOfDate, filename));
#endif
                        break;
                    default:
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                            ExceptionStrings.GetReaderForFileReturnedUnexpectedHResult, hresult.ToString("X")));
                }
#if !FxCop
            }
            catch (Exception e)
            {
                getDebugSymbols = false;
                getDebugSymbolsFailed = true;
                if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                module.MetadataImportErrors.Add(e);
#endif
            }
            finally
            {
                if (binderObj1 != null) Marshal.ReleaseComObject(binderObj1);
                if (binderObj2 != null) Marshal.ReleaseComObject(binderObj2);
            }
#endif // !ROTOR
        }
#if !MinimalReader
        private AssemblyNode ReadAssembly()
        {
            return ReadAssembly(null);
        }

        private AssemblyNode ReadAssembly(AssemblyNode.PostAssemblyLoadProcessor postLoadEvent)
        {
#else
    private AssemblyNode ReadAssembly(){
#endif
            try
            {
                var assembly = new AssemblyNode(GetTypeFromName,
                    GetTypeList, GetCustomAttributesFor,
                    GetResources, directory);
                assembly.reader = this;
                ReadModuleProperties(assembly);
                ReadAssemblyProperties(assembly); //Hashvalue, Name, etc.
                module = assembly;
                ReadAssemblyReferences(assembly);
                ReadModuleReferences(assembly);
                var cachedAssembly = GetCachedAssembly(assembly);
                if (cachedAssembly != null) return cachedAssembly;
                if (getDebugSymbols) assembly.SetupDebugReader(null);
#if !MinimalReader
                if (postLoadEvent != null)
                {
                    assembly.AfterAssemblyLoad += postLoadEvent;
                    postLoadEvent(assembly);
                }
#endif
                return assembly;
#if !FxCop
            }
            catch (Exception e)
            {
                if (module == null) return null;
                if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                module.MetadataImportErrors.Add(e);
                return module as AssemblyNode;
            }
#else
      }finally{}
#endif
        }

        private AssemblyNode GetCachedAssembly(AssemblyNode /*!*/ assembly)
        {
            //Always return the one true mscorlib. Things get too weird if more than one mscorlib is being read at the same time.
            //if (CoreSystemTypes.SystemAssembly != null && CoreSystemTypes.SystemAssembly.Name == assembly.Name && CoreSystemTypes.SystemAssembly.reader != null) {
            //  if (CoreSystemTypes.SystemAssembly.reader != this) {
            //    if (this.getDebugSymbols && !CoreSystemTypes.SystemAssembly.reader.getDebugSymbols && !CoreSystemTypes.SystemAssembly.reader.getDebugSymbolsFailed)
            //      CoreSystemTypes.SystemAssembly.SetupDebugReader(null);
            //    this.Dispose();
            //  }
            //  return CoreSystemTypes.SystemAssembly;
            //}
            if (assembly.PublicKeyOrToken == null || assembly.PublicKeyOrToken.Length == 0)
            {
                AssemblyNode cachedAssembly = null;
                if (assembly.Location != null)
                    cachedAssembly = localAssemblyCache[assembly.Location] as AssemblyNode;
                if (cachedAssembly == null && assembly.Name != null)
                {
                    cachedAssembly = localAssemblyCache[assembly.Name] as AssemblyNode;
                    if (cachedAssembly != null && assembly.Location != null)
                        localAssemblyCache[assembly.Location] = cachedAssembly;
                }

                if (cachedAssembly != null)
                {
                    if (cachedAssembly.reader != this && cachedAssembly.reader != null)
                    {
                        if (getDebugSymbols && !cachedAssembly.reader.getDebugSymbols &&
                            !cachedAssembly.reader.getDebugSymbolsFailed)
                            cachedAssembly.SetupDebugReader(null);
                        Dispose();
                    }

                    return cachedAssembly;
                }

                lock (StaticAssemblyCache)
                {
                    if (assembly.Name != null)
                        localAssemblyCache[assembly.Name] = assembly;
                    if (fileName != null)
                        localAssemblyCache[fileName] = assembly;
                }
            }
            else
            {
                var assemblyStrongName = assembly.StrongName;
                AssemblyNode cachedAssembly = null;
                if (useStaticCache)
                {
                    //See if assembly is a platform assembly (and apply unification)
                    var assemblyReference = new AssemblyReference(assembly);
                    var aRef = (AssemblyReference)TargetPlatform.AssemblyReferenceFor[
                        Identifier.For(assemblyReference.Name).UniqueIdKey];
                    if (aRef != null && assemblyReference.Version != null &&
                        aRef.Version >= assemblyReference.Version && aRef.MatchesIgnoringVersion(assemblyReference))
                    {
                        var platformAssembly = aRef.assembly;
                        if (platformAssembly == null)
                        {
                            Debug.Assert(aRef.Location != null);
                            if (Path.GetFullPath(aRef.Location) == assembly.Location)
                            {
                                if (aRef.Version != assemblyReference.Version)
                                    HandleError(assembly,
                                        string.Format(CultureInfo.CurrentCulture,
                                            ExceptionStrings.BadTargetPlatformLocation, assembly.Name,
                                            TargetPlatform.PlatformAssembliesLocation, assembly.Version, aRef.Version));
                                lock (StaticAssemblyCache)
                                {
                                    StaticAssemblyCache[assemblyStrongName] = assembly;
                                    if (aRef.Location != null)
                                        StaticAssemblyCache[aRef.Location] = assembly;
                                    aRef.Assembly = assembly;
                                }

                                return null; //Prevents infinite recursion
                            }

                            platformAssembly = AssemblyNode.GetAssembly(aRef.Location, doNotLockFile, getDebugSymbols,
                                useStaticCache);
                        }

                        if (platformAssembly != null)
                        {
                            lock (StaticAssemblyCache)
                            {
                                if (aRef.Location != null)
                                    StaticAssemblyCache[aRef.Location] = platformAssembly;
                                StaticAssemblyCache[assemblyStrongName] = platformAssembly;
                            }

                            return aRef.assembly = platformAssembly;
                        }
                    }

                    cachedAssembly = StaticAssemblyCache[assemblyStrongName] as AssemblyNode;
                    if (cachedAssembly != null)
                    {
                        if (aRef == null && assembly.FileLastWriteTimeUtc > cachedAssembly.FileLastWriteTimeUtc &&
                            assembly.Location != null && cachedAssembly.Location != null &&
                            assembly.Location == cachedAssembly.Location)
                        {
                            lock (StaticAssemblyCache)
                            {
                                StaticAssemblyCache[assemblyStrongName] = assembly;
                            }

                            return null;
                        }

                        if (cachedAssembly.reader != this && cachedAssembly.reader != null)
                        {
                            if (getDebugSymbols && !cachedAssembly.reader.getDebugSymbols &&
                                !cachedAssembly.reader.getDebugSymbolsFailed)
                                cachedAssembly.SetupDebugReader(null);
                            Dispose();
                        }

                        return cachedAssembly;
                    }

                    lock (StaticAssemblyCache)
                    {
                        StaticAssemblyCache[assemblyStrongName] = assembly;
                        if (fileName != null) StaticAssemblyCache[fileName] = assembly;
                    }
                }
                else
                {
                    cachedAssembly = localAssemblyCache[assemblyStrongName] as AssemblyNode;
                    if (cachedAssembly != null)
                    {
                        if (assembly.FileLastWriteTimeUtc > cachedAssembly.FileLastWriteTimeUtc &&
                            assembly.Location != null && cachedAssembly.Location != null &&
                            assembly.Location == cachedAssembly.Location)
                        {
                            localAssemblyCache[assemblyStrongName] = assembly;
                            return null;
                        }

                        if (cachedAssembly.reader != this && cachedAssembly.reader != null)
                        {
#if !ROTOR
                            if (getDebugSymbols && cachedAssembly.reader.debugReader == null &&
                                !cachedAssembly.reader.getDebugSymbolsFailed)
                                cachedAssembly.SetupDebugReader(null);
#endif
                            Dispose();
                        }

                        return cachedAssembly;
                    }

                    localAssemblyCache[assemblyStrongName] = assembly;
                    if (fileName != null) localAssemblyCache[fileName] = assembly;
                }
            }

            return null;
        }
#if !MinimalReader
        internal Module ReadModule()
        {
            return ReadModule(null);
        }

        internal Module ReadModule(AssemblyNode.PostAssemblyLoadProcessor postLoadEvent)
        {
#else
    internal Module ReadModule(){
#endif
            try
            {
                if (fileName != null)
                {
                    if (!File.Exists(fileName)) return null;
                    AssemblyNode cachedAssembly;
                    if (useStaticCache)
                    {
                        cachedAssembly = StaticAssemblyCache[fileName] as AssemblyNode;
                        if (cachedAssembly != null &&
                            cachedAssembly.FileLastWriteTimeUtc == File.GetLastWriteTimeUtc(fileName))
                        {
                            Dispose();
                            return cachedAssembly;
                        }
                    }

                    cachedAssembly = localAssemblyCache[fileName] as AssemblyNode;
                    if (cachedAssembly != null &&
                        cachedAssembly.FileLastWriteTimeUtc == File.GetLastWriteTimeUtc(fileName))
                    {
                        Dispose();
                        return cachedAssembly;
                    }
                }

                SetupReader();
                if (tables.AssemblyTable.Length > 0)
#if !MinimalReader
                    return ReadAssembly(postLoadEvent);
#else
          return this.ReadAssembly();
#endif
                var module = this.module = new Module(GetTypeFromName,
                    GetTypeList, GetCustomAttributesFor,
                    GetResources);
                module.reader = this;
                ReadModuleProperties(module);
                this.module = module;
                ReadAssemblyReferences(module);
                ReadModuleReferences(module);
                if (getDebugSymbols) SetupDebugReader(fileName, null);
                return module;
#if !FxCop
            }
            catch (Exception e)
            {
                if (module == null) return null;
                if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                module.MetadataImportErrors.Add(e);
                return module;
            }
#else
      }finally{}
#endif
        }

        private void ReadModuleProperties(Module /*!*/ module)
        {
            var mods = tables.ModuleTable;
            if (mods.Length != 1) throw new InvalidMetadataException(ExceptionStrings.InvalidModuleTable);
            var mrow = mods[0];
            module.reader = this;
            module.DllCharacteristics = tables.dllCharacteristics;
            module.FileAlignment = tables.fileAlignment;
            module.HashValue = tables.HashValue;
            module.Kind = tables.moduleKind;
            module.Location = fileName;
            module.TargetRuntimeVersion = tables.targetRuntimeVersion;
            module.LinkerMajorVersion = tables.linkerMajorVersion;
            module.LinkerMinorVersion = tables.linkerMinorVersion;
            module.MetadataFormatMajorVersion = tables.metadataFormatMajorVersion;
            module.MetadataFormatMinorVersion = tables.metadataFormatMinorVersion;
            module.Name = tables.GetString(mrow.Name);
            module.Mvid = tables.GetGuid(mrow.Mvid);
            module.PEKind = tables.peKind;
            module.TrackDebugData = tables.TrackDebugData;
        }

        private void ReadAssemblyProperties(AssemblyNode /*!*/ assembly)
        {
            var assemblyRow = tables.AssemblyTable[0];
            assembly.HashAlgorithm = (AssemblyHashAlgorithm)assemblyRow.HashAlgId;
            assembly.Version = new Version(assemblyRow.MajorVersion, assemblyRow.MinorVersion, assemblyRow.BuildNumber,
                assemblyRow.RevisionNumber);
            assembly.Flags = (AssemblyFlags)assemblyRow.Flags;
            assembly.PublicKeyOrToken = tables.GetBlob(assemblyRow.PublicKey);
            assembly.ModuleName = assembly.Name;
            assembly.Name = tables.GetString(assemblyRow.Name);
            assembly.Culture = tables.GetString(assemblyRow.Culture);
            if (fileName != null) assembly.FileLastWriteTimeUtc = File.GetLastWriteTimeUtc(fileName);
            assembly.ContainingAssembly = assembly;
        }

        private void ReadAssemblyReferences(Module /*!*/ module)
        {
            var assems = tables.AssemblyRefTable;
            var n = assems.Length;
            var assemblies = module.AssemblyReferences = new AssemblyReferenceList(n);
            for (var i = 0; i < n; i++)
            {
                var arr = assems[i];
                var assemRef = new AssemblyReference();
                assemRef.Version = new Version(arr.MajorVersion, arr.MinorVersion, arr.BuildNumber, arr.RevisionNumber);
                assemRef.Flags = (AssemblyFlags)arr.Flags;
                assemRef.PublicKeyOrToken = tables.GetBlob(arr.PublicKeyOrToken);
                assemRef.Name = tables.GetString(arr.Name);
                //if (CoreSystemTypes.SystemAssembly != null && CoreSystemTypes.SystemAssembly.Name == assemRef.Name && 
                //  assemRef.Version > CoreSystemTypes.SystemAssembly.Version){
                //  HandleError(module, ExceptionStrings.ModuleOrAssemblyDependsOnMoreRecentVersionOfCoreLibrary);
                //}
                assemRef.Culture = tables.GetString(arr.Culture);
                if (assemRef.Culture != null && assemRef.Culture.Length == 0) assemRef.Culture = null;
                assemRef.HashValue = tables.GetBlob(arr.HashValue);
                assemRef.Reader = this;
                assems[i].AssemblyReference = assemRef;
                assemblies.Add(assemRef);
            }
        }

        private void ReadModuleReferences(Module /*!*/ module)
        {
            var files = tables.FileTable;
            var modRefs = tables.ModuleRefTable;
            var n = modRefs.Length;
            var modules = module.ModuleReferences = new ModuleReferenceList(n);
            for (var i = 0; i < n; i++)
            {
                Module mod;
                var nameIndex = modRefs[i].Name;
                var name = tables.GetString(nameIndex);
                var dir = BetterPath.GetDirectoryName(this.module.Location);
                var location = BetterPath.Combine(dir, name);
                for (int j = 0, m = files == null ? 0 : files.Length; j < m; j++)
                {
                    if (files[j].Name != nameIndex) continue;
                    if ((files[j].Flags & (int)FileFlags.ContainsNoMetaData) == 0)
                        mod = Module.GetModule(location, doNotLockFile, getDebugSymbols, false);
                    else
                        mod = null;
                    if (mod == null)
                    {
                        mod = new Module();
                        mod.Name = name;
                        mod.Location = location;
                        mod.Kind = ModuleKindFlags.UnmanagedDynamicallyLinkedLibrary;
                    }

                    mod.HashValue = tables.GetBlob(files[j].HashValue);
                    mod.ContainingAssembly = module.ContainingAssembly;
                    modRefs[i].Module = mod;
                    modules.Add(new ModuleReference(name, mod));
                    goto nextModRef;
                }

                mod = new Module();
                mod.Name = name;
                mod.Kind = ModuleKindFlags.UnmanagedDynamicallyLinkedLibrary;
                if (File.Exists(location)) mod.Location = location;
                mod.ContainingAssembly = module.ContainingAssembly;
                modRefs[i].Module = mod;
                modules.Add(new ModuleReference(name, mod));
                nextModRef: ;
            }
        }

        private static string ReadSerString(MemoryCursor /*!*/ sigReader)
        {
            var n = sigReader.ReadCompressedInt();
            if (n < 0) return null;
            return sigReader.ReadUTF8(n);
        }

        private void AddFieldsToType(TypeNode /*!*/ type, FieldRow[] /*!*/ fieldDefs, FieldPtrRow[] /*!*/ fieldPtrs,
            int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                var ii = i;
                if (fieldPtrs.Length > 0) ii = fieldPtrs[i - 1].Field;
                var field = GetFieldFromDef(ii, type);
                if (field != null) type.Members.Add(field);
            }
        }

        private void GetUnderlyingTypeOfEnumNode(EnumNode /*!*/enumNode, FieldRow[] /*!*/ fieldDefs,
            FieldPtrRow[] /*!*/ fieldPtrs, int start, int end)
        {
            TypeNode underlyingType = null;
            for (var i = start; i < end; i++)
            {
                var ii = i;
                if (fieldPtrs.Length > 0) ii = fieldPtrs[i - 1].Field;
                var fld = fieldDefs[ii - 1];
                if (fld.Field != null && !fld.Field.IsStatic)
                {
                    underlyingType = fld.Field.Type;
                    break;
                }

                var fieldFlags = (FieldFlags)fld.Flags;
                if ((fieldFlags & FieldFlags.Static) == 0)
                {
                    tables.GetSignatureLength(fld.Signature);
                    var sigReader = tables.GetNewCursor();
                    GetAndCheckSignatureToken(6, sigReader);
                    underlyingType = ParseTypeSignature(sigReader);
                    break;
                }
            }

            enumNode.underlyingType = underlyingType;
        }

        private void AddMethodsToType(TypeNode /*!*/ type, MethodPtrRow[] /*!*/ methodPtrs, int start, int end)
            //^ requires type.members != null;
        {
            for (var i = start; i < end; i++)
            {
                var ii = i;
                if (methodPtrs.Length > 0) ii = methodPtrs[i - 1].Method;
                var method = GetMethodFromDef(ii, type);
                if (method != null && ((method.Flags & MethodFlags.RTSpecialName) == 0 ||
                                       method.Name.UniqueIdKey != StandardIds._Deleted.UniqueIdKey))
                    type.members.Add(method);
            }
        }

        private void AddMoreStuffToParameters(Method method, ParameterList parameters, int start, int end)
        {
            var pars = tables.ParamTable;
            var n = parameters == null ? 0 : parameters.Count;
            for (var i = start; i < end; i++)
            {
                var pr = pars[i - 1];
                if (pr.Sequence == 0 && method != null)
                {
                    //The parameter entry with sequence 0 is used as a target for custom attributes that apply to the return value
                    method.ReturnAttributes = GetCustomAttributesFor((i << 5) | 4);
                    if ((pr.Flags & (int)ParameterFlags.HasFieldMarshal) != 0)
                        method.ReturnTypeMarshallingInformation = GetMarshallingInformation((i << 1) | 1);
                    AddMoreStuffToParameters(null, parameters, start + 1, end);
                    return;
                }

                var j = pr.Sequence;
                if (j < 1 || j > n) continue; //Bad metadata, ignore
                if (parameters == null) continue;
                var par = parameters[j - 1];
                par.Attributes = GetCustomAttributesFor((i << 5) | 4);
                par.Flags = (ParameterFlags)pr.Flags;
                if ((par.Flags & ParameterFlags.HasDefault) != 0)
                    par.DefaultValue = GetLiteral((i << 2) | 1, par.Type);
                if ((par.Flags & ParameterFlags.HasFieldMarshal) != 0)
                    par.MarshallingInformation = GetMarshallingInformation((i << 1) | 1);
                par.Name = tables.GetIdentifier(pr.Name);
#if ExtendedRuntime
        for (int k = 0, al = par.Attributes == null ? 0 : par.Attributes.Count; k < al; k++) {
          if (par.Attributes[k].Type == ExtendedRuntimeTypes.NotNullAttribute) {
            Reference r = par.Type as Reference;
            if (r != null){
              // need to make it a reference to a non-null type and not a non-null wrapper around the reference
              // also *must* make it a new Reference.
              OptionalModifier om = OptionalModifier.For(ExtendedRuntimeTypes.NonNullType, r.ElementType);
              par.Type = om.GetReferenceType();
            }else{
              par.Type = OptionalModifier.For(ExtendedRuntimeTypes.NonNullType, par.Type);
            }
            // Someone putting an attribute directly on the "real" method is still a
            // kind of out-of-band contract.
            // This marking is the way to signal that any override or implementing method being compiled
            // should not have its non-null annotations persisted as optional modifiers.
            par.DeclaringMethod.HasOutOfBandContract = true;
          } else if (par.Attributes[k].Type == ExtendedRuntimeTypes.NotNullArrayElementsAttribute) {
            Stack s = new Stack();
            Reference r = par.Type as Reference;
            TypeNode t;
            if (r != null) {
              // need to make it a reference to an array of non-null type and not a non-null wrapper around the reference
              t = r.ElementType;
            }
            else {
              t = par.Type;
            }
            while (t is OptionalModifier) {
              OptionalModifier om = t as OptionalModifier;
              s.Push(om.Modifier);
              t = om.ModifiedType;
            }
            ArrayType at = t as ArrayType;
            if (at != null) { // just silently ignore if attribute is on a non-array type?
              TypeNode newTypeForParameter;
              OptionalModifier om = OptionalModifier.For(ExtendedRuntimeTypes.NonNullType, at.ElementType);
              while (0 < s.Count) {
                om = OptionalModifier.For((TypeNode)s.Pop(), om);
              }
              // also *must* make it a new array type, can't set the ElementType.
              newTypeForParameter = om.GetArrayType(1);
              if (r != null) {
                // also *must* make it a new Reference, can't set the ElementType.
                newTypeForParameter = newTypeForParameter.GetReferenceType();
              }
              par.Type = newTypeForParameter;
              // Someone putting an attribute directly on the "real" method is still a
              // kind of out-of-band contract.
              // This marking is the way to signal that any override or implementing method being compiled
              // should not have its non-null annotations persisted as optional modifiers.
              par.DeclaringMethod.HasOutOfBandContract = true;
            }
          }
        }
#endif
            }
        }

        private void AddPropertiesToType(TypeNode /*!*/ type, PropertyRow[] /*!*/ propertyDefs,
                PropertyPtrRow[] /*!*/ propertyPtrs, int start, int end)
            //requires type.members != null;
        {
            var tables = this.tables;
            for (var i = start; i < end; i++)
            {
                var ii = i;
                if (propertyPtrs.Length > 0) ii = propertyPtrs[i - 1].Property;
                var prop = propertyDefs[ii - 1];
                var property = new Property();
                property.Attributes = GetCustomAttributesFor((ii << 5) | 9);
                property.DeclaringType = type;
                property.Flags = (PropertyFlags)prop.Flags;
                property.Name = tables.GetIdentifier(prop.Name);
                if ((property.Flags & PropertyFlags.RTSpecialName) == 0 ||
                    property.Name.UniqueIdKey != StandardIds._Deleted.UniqueIdKey)
                {
                    AddMethodsToProperty(ii, property);
                    type.members.Add(property);
                }
                //REVIEW: the signature seems to be redundant. Is there any point in retrieving it?
            }
        }

        private void AddMethodsToProperty(int propIndex, Property /*!*/ property)
        {
            var codedPropIndex = (propIndex << 1) | 1;
            var tables = this.tables;
            var methods = tables.MethodTable;
            var methodSemantics = tables.MethodSemanticsTable;
            int i = 0, n = methodSemantics.Length, j = n - 1;
            var sorted = (sortedTablesMask >> (int)TableIndices.MethodSemantics) % 2 == 1;
            if (sorted)
            {
                while (i < j)
                {
                    var k = (i + j) / 2;
                    if (methodSemantics[k].Association < codedPropIndex)
                        i = k + 1;
                    else
                        j = k;
                }

                while (i > 0 && methodSemantics[i - 1].Association == codedPropIndex) i--;
            }

            for (; i < n; i++)
            {
                var meth = methodSemantics[i];
                var propertyMethod = methods[meth.Method - 1].Method;
                if (propertyMethod == null) continue;
                if (meth.Association == codedPropIndex)
                {
                    propertyMethod.DeclaringMember = property;
                    switch (meth.Semantics)
                    {
                        case 0x0001:
                            property.Setter = propertyMethod;
                            break;
                        case 0x0002:
                            property.Getter = propertyMethod;
                            break;
                        default:
                            if (property.OtherMethods == null) property.OtherMethods = new MethodList();
                            property.OtherMethods.Add(propertyMethod);
                            break;
                    }
                }
                else if (sorted)
                {
                    break;
                }
            }
        }

        private void AddEventsToType(TypeNode /*!*/ type, EventRow[] /*!*/ eventDefs, EventPtrRow[] /*!*/ eventPtrs,
            int start, int end)
        {
            var tables = this.tables;
            for (var i = start; i < end; i++)
            {
                var ii = i;
                if (eventPtrs.Length > 0) ii = eventPtrs[i].Event;
                var ev = eventDefs[ii - 1];
                var evnt = new Event();
                evnt.Attributes = GetCustomAttributesFor((ii << 5) | 10);
                evnt.DeclaringType = type;
                evnt.Flags = (EventFlags)ev.Flags;
                evnt.HandlerType = DecodeAndGetTypeDefOrRefOrSpec(ev.EventType);
                evnt.Name = tables.GetIdentifier(ev.Name);
                if ((evnt.Flags & EventFlags.RTSpecialName) == 0 ||
                    evnt.Name.UniqueIdKey != StandardIds._Deleted.UniqueIdKey)
                {
                    AddMethodsToEvent(ii, evnt);
                    type.Members.Add(evnt);
                }
            }
        }

        private void AddMethodsToEvent(int eventIndex, Event /*!*/ evnt)
        {
            var codedEventIndex = eventIndex << 1;
            var tables = this.tables;
            var methods = tables.MethodTable;
            var methodSemantics = tables.MethodSemanticsTable;
            int i = 0, n = methodSemantics.Length, j = n - 1;
            var sorted = (sortedTablesMask >> (int)TableIndices.MethodSemantics) % 2 == 1;
            if (sorted)
            {
                while (i < j)
                {
                    var k = (i + j) / 2;
                    if (methodSemantics[k].Association < codedEventIndex)
                        i = k + 1;
                    else
                        j = k;
                }

                while (i > 0 && methodSemantics[i - 1].Association == codedEventIndex) i--;
            }

            MethodFlags handlerFlags = 0;
            for (; i < n; i++)
            {
                var meth = methodSemantics[i];
                var eventMethod = methods[meth.Method - 1].Method;
                if (eventMethod == null) continue;
                if (meth.Association == codedEventIndex)
                {
                    eventMethod.DeclaringMember = evnt;
                    switch (meth.Semantics)
                    {
                        case 0x0008:
                            evnt.HandlerAdder = eventMethod;
                            handlerFlags = eventMethod.Flags;
                            break;
                        case 0x0010:
                            evnt.HandlerRemover = eventMethod;
                            handlerFlags = eventMethod.Flags;
                            break;
                        case 0x0020:
                            evnt.HandlerCaller = eventMethod;
                            break;
                        default:
                            if (evnt.OtherMethods == null) evnt.OtherMethods = new MethodList();
                            evnt.OtherMethods.Add(eventMethod);
                            break;
                    }
                }
                else if (sorted)
                {
                    break;
                }
            }

            evnt.HandlerFlags = handlerFlags;
        }

        private bool TypeDefOrRefOrSpecIsClass(int codedIndex)
        {
            if (codedIndex == 0) return false;
            switch (codedIndex & 0x3)
            {
                case 0x00: return TypeDefIsClass(codedIndex >> 2);
                case 0x01:
                    var t = GetTypeFromRef(codedIndex >> 2);
                    return t is Class;
                case 0x02: return TypeSpecIsClass(codedIndex >> 2);
            }

            throw new InvalidMetadataException(ExceptionStrings.BadTypeDefOrRef);
        }

        private bool TypeDefOrRefOrSpecIsClassButNotValueTypeBaseClass(int codedIndex)
        {
            if (codedIndex == 0) return false;
            switch (codedIndex & 0x3)
            {
                case 0x00: return TypeDefIsClassButNotValueTypeBaseClass(codedIndex >> 2);
                case 0x01:
                    var t = GetTypeFromRef(codedIndex >> 2);
                    return t != CoreSystemTypes.ValueType && t != CoreSystemTypes.Enum && t is Class;
                case 0x02: return TypeSpecIsClass(codedIndex >> 2);
            }

            throw new InvalidMetadataException(ExceptionStrings.BadTypeDefOrRef);
        }

        private TypeNode DecodeAndGetTypeDefOrRefOrSpec(int codedIndex)
        {
            if (codedIndex == 0) return null;
            switch (codedIndex & 0x3)
            {
                case 0x00: return GetTypeFromDef(codedIndex >> 2);
                case 0x01: return GetTypeFromRef(codedIndex >> 2);
                case 0x02: return GetTypeFromSpec(codedIndex >> 2);
            }

            throw new InvalidMetadataException(ExceptionStrings.BadTypeDefOrRef);
        }

        private TypeNode DecodeAndGetTypeDefOrRefOrSpec(int codedIndex, bool expectStruct)
        {
            if (codedIndex == 0) return null;
            switch (codedIndex & 0x3)
            {
                case 0x00: return GetTypeFromDef(codedIndex >> 2);
                case 0x01: return GetTypeFromRef(codedIndex >> 2, expectStruct);
                case 0x02: return GetTypeFromSpec(codedIndex >> 2);
            }

            throw new InvalidMetadataException(ExceptionStrings.BadTypeDefOrRef);
        }
#if ExtendedRuntime
    private Interface GetInterfaceIfNotGenericInstance(int codedIndex){
      if (codedIndex == 0) return null;
      switch(codedIndex & 0x3){
        case 0x00 : return this.GetTypeFromDef(codedIndex >> 2) as Interface;
        case 0x01 : return this.GetTypeFromRef(codedIndex >> 2, false) as Interface;
      }
      return null;
    }
#endif
        private TypeNode GetTypeIfNotGenericInstance(int codedIndex)
        {
            if (codedIndex == 0) return null;
            switch (codedIndex & 0x3)
            {
                case 0x00: return GetTypeFromDef(codedIndex >> 2);
                case 0x01: return GetTypeFromRef(codedIndex >> 2, false);
            }

            return null;
        }

        internal AssemblyNode /*!*/ GetAssemblyFromReference(AssemblyReference /*!*/ assemblyReference)
        {
            lock (Module.GlobalLock)
            {
                if (SystemAssemblyLocation.ParsedAssembly != null &&
                    (assemblyReference.Name == "mscorlib" || assemblyReference.Name == "basetypes" ||
                     assemblyReference.Name == "ioconfig"
                     || assemblyReference.Name == "singularity.v1"))
                    return SystemAssemblyLocation.ParsedAssembly;
                if (CoreSystemTypes.SystemAssembly != null &&
                    CoreSystemTypes.SystemAssembly.Name == assemblyReference.Name)
                    return CoreSystemTypes.SystemAssembly;
                string strongName = null;
                object cachedValue = null;
                if (assemblyReference.PublicKeyOrToken == null || assemblyReference.PublicKeyOrToken.Length == 0)
                {
                    if (assemblyReference.Location != null)
                        cachedValue = localAssemblyCache[assemblyReference.Location];
                    if (cachedValue == null)
                    {
                        cachedValue = localAssemblyCache[assemblyReference.Name];
                        if (cachedValue != null && assemblyReference.Location != null)
                            localAssemblyCache[assemblyReference.Location] = cachedValue;
                    }
                }
                else
                {
                    strongName = assemblyReference.StrongName;
                    if (useStaticCache)
                    {
                        //See if reference is to an assembly that lives in the GAC.
                        if (assemblyReference.Location != null)
                            cachedValue = StaticAssemblyCache[assemblyReference.Location];
                        if (cachedValue == null)
                            cachedValue = StaticAssemblyCache[strongName];
                    }

                    if (cachedValue == null)
                        cachedValue = localAssemblyCache[strongName];
                }

                if (cachedValue == null)
                {
                    //See if assembly is a platform assembly (and apply unification)
                    var aRef = (AssemblyReference)TargetPlatform.AssemblyReferenceFor[
                        Identifier.For(assemblyReference.Name).UniqueIdKey];
                    if (aRef != null && assemblyReference.Version != null &&
                        aRef.Version >= assemblyReference.Version && aRef.MatchesIgnoringVersion(assemblyReference))
                    {
                        var platformAssembly = aRef.assembly;
                        if (platformAssembly == null)
                        {
                            Debug.Assert(aRef.Location != null);
#if MinimalReader
              platformAssembly =
 AssemblyNode.GetAssembly(aRef.Location, this.doNotLockFile, this.getDebugSymbols, this.useStaticCache);
#else
                            platformAssembly = AssemblyNode.GetAssembly(aRef.Location, doNotLockFile, getDebugSymbols,
                                useStaticCache, ReferringAssemblyPostLoad);
#endif
                        }

                        if (platformAssembly != null)
                        {
                            if (strongName == null) strongName = assemblyReference.Name;
                            lock (StaticAssemblyCache)
                            {
                                if (aRef.Location != null)
                                    StaticAssemblyCache[aRef.Location] = platformAssembly;
                                StaticAssemblyCache[strongName] = platformAssembly;
                            }

                            aRef.assembly = platformAssembly;
                            return platformAssembly;
                        }
                    }
                }

                var assembly = cachedValue as AssemblyNode;
                if (assembly != null) goto done;

                //No cached assembly and no cached reader for this assembly. Look for a resolver.
                if (module != null)
                {
                    assembly = module.Resolve(assemblyReference);
                    if (assembly != null)
                    {
                        if (strongName == null)
                        {
                            localAssemblyCache[assembly.Name] = assembly;
                            if (assembly.Location != null) localAssemblyCache[assembly.Location] = assembly;
                        }
                        else
                        {
                            if (CoreSystemTypes.SystemAssembly != null &&
                                CoreSystemTypes.SystemAssembly.Name == assembly.Name)
                                return CoreSystemTypes.SystemAssembly;
                            lock (StaticAssemblyCache)
                            {
                                if (useStaticCache)
                                {
                                    if (assembly.Location != null)
                                        StaticAssemblyCache[assembly.Location] = assembly;
                                    StaticAssemblyCache[strongName] = assembly;
                                }
                                else
                                {
                                    localAssemblyCache[strongName] = assembly;
                                    if (assembly.Location != null) localAssemblyCache[assembly.Location] = assembly;
                                }
                            }
                        }

                        goto done;
                    }
                }

                //Look for an assembly with the given name in the same directory as the referencing module
                if (directory != null)
                {
                    var fileName = Path.Combine(directory, assemblyReference.Name + ".dll");
                    if (File.Exists(fileName))
                    {
#if MinimalReader
            assembly =
 AssemblyNode.GetAssembly(fileName, this.localAssemblyCache, this.doNotLockFile, this.getDebugSymbols, this.useStaticCache);
#else
                        assembly = AssemblyNode.GetAssembly(fileName, localAssemblyCache, doNotLockFile,
                            getDebugSymbols, useStaticCache, ReferringAssemblyPostLoad);
#endif
                        if (assembly != null)
                        {
                            if (strongName == null) goto cacheIt; //found something
                            //return assembly only if it matches the strong name of the reference
                            if (assemblyReference.Matches(assembly.Name, assembly.Version, assembly.Culture,
                                    assembly.PublicKeyToken)) goto cacheIt;
                        }
                    }

                    fileName = Path.Combine(directory, assemblyReference.Name + ".exe");
                    if (File.Exists(fileName))
                    {
#if MinimalReader
            assembly =
 AssemblyNode.GetAssembly(fileName, this.localAssemblyCache, this.doNotLockFile, this.getDebugSymbols, this.useStaticCache);
#else
                        assembly = AssemblyNode.GetAssembly(fileName, localAssemblyCache, doNotLockFile,
                            getDebugSymbols, useStaticCache, ReferringAssemblyPostLoad);
#endif
                        if (assembly != null)
                        {
                            if (strongName == null) goto cacheIt; //found something
                            //return assembly only if it matches the strong name of the reference
                            if (assemblyReference.Matches(assembly.Name, assembly.Version, assembly.Culture,
                                    assembly.PublicKeyToken)) goto cacheIt;
                        }
                    }

                    fileName = Path.Combine(directory, assemblyReference.Name + ".winmd");
                    if (File.Exists(fileName))
                    {
#if MinimalReader
            assembly =
 AssemblyNode.GetAssembly(fileName, this.localAssemblyCache, this.doNotLockFile, this.getDebugSymbols, this.useStaticCache);
#else
                        assembly = AssemblyNode.GetAssembly(fileName, localAssemblyCache, doNotLockFile,
                            getDebugSymbols, useStaticCache, ReferringAssemblyPostLoad);
#endif
                        if (assembly != null)
                        {
                            if (strongName == null) goto cacheIt; //found something
                            //return assembly only if it matches the strong name of the reference
                            if (assemblyReference.Matches(assembly.Name, assembly.Version, assembly.Culture,
                                    assembly.PublicKeyToken)) goto cacheIt;
                        }
                    }

                    fileName = Path.Combine(directory, assemblyReference.Name + ".ill");
                    if (File.Exists(fileName))
                    {
#if MinimalReader
            assembly =
 AssemblyNode.GetAssembly(fileName, this.localAssemblyCache, this.doNotLockFile, this.getDebugSymbols, this.useStaticCache);
#else
                        assembly = AssemblyNode.GetAssembly(fileName, localAssemblyCache, doNotLockFile,
                            getDebugSymbols, useStaticCache, ReferringAssemblyPostLoad);
#endif
                        if (assembly != null)
                        {
                            if (strongName == null) goto cacheIt; //found something
                            //return assembly only if it matches the strong name of the reference
                            if (assemblyReference.Matches(assembly.Name, assembly.Version, assembly.Culture,
                                    assembly.PublicKeyToken)) goto cacheIt;
                        }
                    }
                }

                //Look for an assembly in the same directory as the application using Reader.
                {
                    var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyReference.Name + ".dll");
                    if (File.Exists(fileName))
                    {
#if MinimalReader
            assembly =
 AssemblyNode.GetAssembly(fileName, this.localAssemblyCache, this.doNotLockFile, this.getDebugSymbols, this.useStaticCache);
#else
                        assembly = AssemblyNode.GetAssembly(fileName, localAssemblyCache, doNotLockFile,
                            getDebugSymbols, useStaticCache, ReferringAssemblyPostLoad);
#endif
                        if (assembly != null)
                        {
                            if (strongName == null) goto cacheIt; //found something
                            //return assembly only if it matches the strong name of the reference
                            if (assemblyReference.Matches(assembly.Name, assembly.Version, assembly.Culture,
                                    assembly.PublicKeyToken)) goto cacheIt;
                        }
                    }

                    fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyReference.Name + ".exe");
                    if (File.Exists(fileName))
                    {
#if MinimalReader
            assembly =
 AssemblyNode.GetAssembly(fileName, this.localAssemblyCache, this.doNotLockFile, this.getDebugSymbols, this.useStaticCache);
#else
                        assembly = AssemblyNode.GetAssembly(fileName, localAssemblyCache, doNotLockFile,
                            getDebugSymbols, useStaticCache, ReferringAssemblyPostLoad);
#endif
                        if (assembly != null)
                        {
                            if (strongName == null) goto cacheIt; //found something
                            //return assembly only if it matches the strong name of the reference
                            if (assemblyReference.Matches(assembly.Name, assembly.Version, assembly.Culture,
                                    assembly.PublicKeyToken)) goto cacheIt;
                        }
                    }

                    fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyReference.Name + ".winmd");
                    if (File.Exists(fileName))
                    {
#if MinimalReader
            assembly =
 AssemblyNode.GetAssembly(fileName, this.localAssemblyCache, this.doNotLockFile, this.getDebugSymbols, this.useStaticCache);
#else
                        assembly = AssemblyNode.GetAssembly(fileName, localAssemblyCache, doNotLockFile,
                            getDebugSymbols, useStaticCache, ReferringAssemblyPostLoad);
#endif
                        if (assembly != null)
                        {
                            if (strongName == null) goto cacheIt; //found something
                            //return assembly only if it matches the strong name of the reference
                            if (assemblyReference.Matches(assembly.Name, assembly.Version, assembly.Culture,
                                    assembly.PublicKeyToken)) goto cacheIt;
                        }
                    }
                }
                assembly = null;

                //Probe the GAC
#if FxCop
        if(probeGAC){
#endif
                string gacLocation = null;
                if (strongName != null)
                {
#if !ROTOR
                    //Look for the assembly in the system's Global Assembly Cache
                    gacLocation = GlobalAssemblyCache.GetLocation(assemblyReference);
                    if (gacLocation != null && gacLocation.Length == 0) gacLocation = null;
#else
          //TODO: look in the ROTOR GAC
#endif
                    if (gacLocation != null)
                    {
#if MinimalReader
            assembly =
 AssemblyNode.GetAssembly(gacLocation, this.useStaticCache ? Reader.StaticAssemblyCache : this.localAssemblyCache, this.doNotLockFile, this.getDebugSymbols, this.useStaticCache);
#else
                        assembly = AssemblyNode.GetAssembly(gacLocation,
                            useStaticCache ? StaticAssemblyCache : localAssemblyCache, doNotLockFile, getDebugSymbols,
                            useStaticCache, ReferringAssemblyPostLoad);
#endif
                        if (assembly != null)
                            lock (StaticAssemblyCache)
                            {
                                if (useStaticCache)
                                {
                                    StaticAssemblyCache[gacLocation] = assembly;
                                    StaticAssemblyCache[strongName] = assembly;
                                }
                                else
                                {
                                    localAssemblyCache[gacLocation] = assembly;
                                    localAssemblyCache[strongName] = assembly;
                                }
                            }
                    }
                }
#if FxCop
        }
#endif
                goto done;
                cacheIt:
                if (strongName == null)
                {
                    localAssemblyCache[assembly.Name] = assembly;
                    if (assembly.Location != null) localAssemblyCache[assembly.Location] = assembly;
                }
                else
                {
                    localAssemblyCache[strongName] = assembly;
                    if (assembly.Location != null) localAssemblyCache[assembly.Location] = assembly;
                }
#if !MinimalReader
                // the post load event should fire when we load the assembly the first time
//        assembly.InitializePostAssemblyLoadAndFire(this.module);
#endif
                done:
                if (assembly != null)
                    assembly.InitializeAssemblyReferenceResolution(module);
                if (assembly == null)
                {
                    if (module != null)
                    {
                        assembly = module.ResolveAfterProbingFailed(assemblyReference);
                        if (assembly != null) goto cacheIt;
                        HandleError(module,
                            string.Format(CultureInfo.CurrentCulture, ExceptionStrings.AssemblyReferenceNotResolved,
                                assemblyReference.StrongName));
                    }

                    assembly = new AssemblyNode();
                    assembly.Culture = assemblyReference.Culture;
                    assembly.Name = assemblyReference.Name;
                    assembly.PublicKeyOrToken = assemblyReference.PublicKeyOrToken;
                    assembly.Version = assemblyReference.Version;
                    assembly.Location = "unknown:location";
                    goto cacheIt;
                }

                return assembly;
            }
        }
#if !MinimalReader
        private AssemblyNode.PostAssemblyLoadProcessor ReferringAssemblyPostLoad
        {
            get
            {
                var assem = module as AssemblyNode;
                if (assem == null) return null;
                return assem.GetAfterAssemblyLoad();
            }
        }
#endif
        private static void GetAndCheckSignatureToken(int expectedToken, MemoryCursor /*!*/ sigReader)
        {
            var tok = sigReader.ReadCompressedInt();
            if (tok != expectedToken) throw new InvalidMetadataException(ExceptionStrings.MalformedSignature);
        }

        private Method GetConstructorDefOrRef(int codedIndex, out TypeNodeList varArgTypes)
        {
            varArgTypes = null;
            switch (codedIndex & 0x7)
            {
                case 0x02: return GetMethodFromDef(codedIndex >> 3);
                case 0x03: return (Method)GetMemberFromRef(codedIndex >> 3, out varArgTypes);
            }

            throw new InvalidMetadataException(ExceptionStrings.BadCustomAttributeTypeEncodedToken);
        }

        private void GetResources(Module /*!*/ module)
        {
            var manifestResourceTable = tables.ManifestResourceTable;
            var n = manifestResourceTable.Length;
            var resources = new ResourceList(n);
            for (var i = 0; i < n; i++)
            {
                var mrr = manifestResourceTable[i];
                var r = new Resource();
                r.Name = tables.GetString(mrr.Name);
                r.IsPublic = (mrr.Flags & 7) == 1;
                var impl = mrr.Implementation;
                if (impl != 0)
                {
                    switch (impl & 0x3)
                    {
                        case 0x0:
                            var modName = tables.GetString(tables.FileTable[(impl >> 2) - 1].Name);
                            if ((tables.FileTable[(impl >> 2) - 1].Flags & (int)FileFlags.ContainsNoMetaData) != 0)
                            {
                                r.DefiningModule = new Module();
                                r.DefiningModule.Directory = module.Directory;
                                r.DefiningModule.Location = Path.Combine(module.Directory, modName);
                                r.DefiningModule.Name = modName;
                                r.DefiningModule.Kind = ModuleKindFlags.ManifestResourceFile;
                                r.DefiningModule.ContainingAssembly = module.ContainingAssembly;
                                r.DefiningModule.HashValue =
                                    tables.GetBlob(tables.FileTable[(impl >> 2) - 1].HashValue);
                            }
                            else
                            {
                                var modLocation = modName;
                                r.DefiningModule = GetNestedModule(module, modName, ref modLocation);
                            }

                            break;
                        case 0x1:
                            r.DefiningModule = tables.AssemblyRefTable[(impl >> 2) - 1].AssemblyReference.Assembly;
                            break;
                    }
                }
                else
                {
                    r.DefiningModule = module;
                    r.Data = tables.GetResourceData(mrr.Offset);
                }

                resources.Add(r);
            }

            module.Resources = resources;
            module.Win32Resources = tables.ReadWin32Resources();
        }

        private SecurityAttribute GetSecurityAttribute(int i)
        {
            var dsr = tables.DeclSecurityTable[i];
            var attr = new SecurityAttribute();
            attr.Action = (SecurityAction)dsr.Action;
            if (module.MetadataFormatMajorVersion > 1 || module.MetadataFormatMinorVersion > 0)
            {
                attr.PermissionAttributes = GetPermissionAttributes(dsr.PermissionSet, attr.Action);
                if (attr.PermissionAttributes != null) return attr;
            }

            attr.SerializedPermissions = tables.GetBlobString(dsr.PermissionSet);
            return attr;
        }

        private AttributeList GetPermissionAttributes(int blobIndex, SecurityAction action)
        {
            var result = new AttributeList();
            int blobLength;
            var sigReader = tables.GetBlobCursor(blobIndex, out blobLength);
            if (blobLength == 0) return null;
            var header = sigReader.ReadByte();
            if (header != (byte)'*')
            {
                if (header == (byte)'<') return null;
                if (header == (byte)'.') return GetPermissionAttributes2(blobIndex, action);
                HandleError(module, ExceptionStrings.BadSecurityPermissionSetBlob);
                return null;
            }

            sigReader.ReadInt32(); //Skip over the token for the attribute target
            sigReader.ReadInt32(); //Skip over the security action
            var numAttrs = sigReader.ReadInt32();
            for (var i = 0; i < numAttrs; i++)
                result.Add(GetPermissionAttribute(sigReader));
            return result;
        }

        private AttributeNode GetPermissionAttribute(MemoryCursor /*!*/ sigReader)
        {
            sigReader.ReadInt32(); //Skip over index
            var typeNameLength = sigReader.ReadInt32();
            sigReader.ReadUTF8(typeNameLength); //Skip over type name
            var constructorToken = sigReader.ReadInt32();
            sigReader.ReadInt32(); //Skip over attribute type token
            sigReader.ReadInt32(); //Skip over assembly ref token
            var caBlobLength = sigReader.ReadInt32();
            sigReader.ReadInt32(); //Skip over the number of parameters in the CA blob
            TypeNodeList varArgTypes; //Ignored because vararg constructors are not allowed in Custom Attributes
            var cons = GetConstructorDefOrRef(constructorToken, out varArgTypes);
            if (cons == null) cons = new Method();
            return GetCustomAttribute(cons, sigReader, caBlobLength);
        }

        private AttributeList GetPermissionAttributes2(int blobIndex, SecurityAction action)
        {
            var result = new AttributeList();
            int blobLength;
            var sigReader = tables.GetBlobCursor(blobIndex, out blobLength);
            if (blobLength == 0) return null;
            var header = sigReader.ReadByte();
            if (header != (byte)'.')
            {
                HandleError(module, ExceptionStrings.BadSecurityPermissionSetBlob);
                return null;
            }

            var numAttrs = sigReader.ReadCompressedInt();
            for (var i = 0; i < numAttrs; i++)
                result.Add(GetPermissionAttribute2(sigReader, action));
            return result;
        }

        private AttributeNode GetPermissionAttribute2(MemoryCursor /*!*/ sigReader, SecurityAction action)
        {
            var typeNameLength = sigReader.ReadCompressedInt();
            var serializedTypeName = sigReader.ReadUTF8(typeNameLength);
            TypeNode attrType = null;
            try
            {
                attrType = GetTypeFromSerializedName(serializedTypeName);
            }
            catch (InvalidMetadataException)
            {
            }

            if (attrType == null)
            {
                HandleError(module,
                    string.Format(CultureInfo.CurrentCulture, ExceptionStrings.CouldNotResolveType,
                        serializedTypeName));
                return null;
            }

            var cons = attrType.GetConstructor(CoreSystemTypes.SecurityAction);
            if (cons == null)
            {
                HandleError(module, string.Format(CultureInfo.CurrentCulture,
                    ExceptionStrings.SecurityAttributeTypeDoesNotHaveADefaultConstructor, serializedTypeName));
                return null;
            }

            sigReader.ReadCompressedInt(); //caBlobLength
            var numProps = sigReader.ReadCompressedInt(); //Skip over the number of properties in the CA blob
            var arguments = new ExpressionList(numProps + 1);
            arguments.Add(new Literal(action, CoreSystemTypes.SecurityAction));
            GetCustomAttributeNamedArguments(arguments, (ushort)numProps, sigReader);
            return new AttributeNode(new MemberBinding(null, cons), arguments);
        }

        private static void HandleError(Module mod, string errorMessage)
        {
#if !FxCop
            if (mod != null && (mod.ContainingAssembly == null ||
                                (mod.ContainingAssembly.Flags & AssemblyFlags.ContainsForeignTypes) == 0))
            {
                if (mod.MetadataImportErrors == null) mod.MetadataImportErrors = new ArrayList();
                mod.MetadataImportErrors.Add(new InvalidMetadataException(errorMessage));
            }
#else
      throw new InvalidMetadataException(String.Format(CultureInfo.CurrentCulture, ExceptionStrings.ModuleError, mod.Name, errorMessage));
#endif
        }

        private AttributeNode GetCustomAttribute(int i)
        {
            var ca = tables.CustomAttributeTable[i];
            TypeNodeList varArgTypes; //Ignored because vararg constructors are not allowed in Custom Attributes
            var cons = GetConstructorDefOrRef(ca.Constructor, out varArgTypes);
            if (cons == null) cons = new Method();
            int blobLength;
            var sigReader = tables.GetBlobCursor(ca.Value, out blobLength);
            return GetCustomAttribute(cons, sigReader, blobLength);
        }

        private AttributeNode GetCustomAttribute(Method /*!*/ cons, MemoryCursor /*!*/ sigReader, int blobLength)
        {
            var attr = new AttributeNode();
            attr.Constructor = new MemberBinding(null, cons);
            var n = cons.Parameters == null ? 0 : cons.Parameters.Count;
            var arguments = attr.Expressions = new ExpressionList(n);
            var posAtBlobStart = sigReader.Position;
            sigReader.ReadUInt16(); //Prolog
            for (var j = 0; j < n; j++)
            {
                var t = TypeNode.StripModifiers(cons.Parameters[j].Type);
                if (t == null) continue;
                var /*!*/
                    pt = t;
                object val = null;
                try
                {
                    val = GetCustomAttributeLiteralValue(sigReader, ref pt);
#if !FxCop
                }
                catch (Exception e)
                {
                    if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                    module.MetadataImportErrors.Add(e);
                }
#else
        }finally{}
#endif
                var lit = val as Literal;
                if (lit == null) lit = new Literal(val, pt);
                arguments.Add(lit);
            }

            if (sigReader.Position + 1 < posAtBlobStart + blobLength)
            {
                var numNamed = sigReader.ReadUInt16();
                GetCustomAttributeNamedArguments(arguments, numNamed, sigReader);
            }

            return attr;
        }

        private void GetCustomAttributeNamedArguments(ExpressionList /*!*/ arguments, ushort numNamed,
            MemoryCursor /*!*/ sigReader)
        {
            for (var j = 0; j < numNamed; j++)
            {
                int nameTag = sigReader.ReadByte();
                var mustBox = sigReader.Byte(0) == (byte)ElementType.BoxedEnum;
                var /*!*/
                    vType = ParseTypeSignature(sigReader);
                var id = sigReader.ReadIdentifierFromSerString();
                var val = GetCustomAttributeLiteralValue(sigReader, ref vType);
                var lit = val as Literal;
                if (lit == null) lit = new Literal(val, vType);
                var narg = new NamedArgument(id, lit);
                narg.Type = vType;
                narg.IsCustomAttributeProperty = nameTag == 0x54;
                narg.ValueIsBoxed = mustBox;
                arguments.Add(narg);
            }
        }

        private object GetCustomAttributeLiteralValue(MemoryCursor /*!*/ sigReader, TypeNode /*!*/ type)
        {
            var /*!*/
                t = type;
            var result = GetCustomAttributeLiteralValue(sigReader, ref t);
            var enumType = t as EnumNode;
            if (enumType != null && type == CoreSystemTypes.Object)
            {
                result = new Literal(result, enumType);
            }
            else if (type == CoreSystemTypes.Object && t != CoreSystemTypes.Object)
            {
                var lit = result as Literal;
                if (lit == null) result = new Literal(result, t);
            }

            return result;
        }

        private object GetCustomAttributeLiteralValue(MemoryCursor /*!*/ sigReader, ref TypeNode /*!*/ type)
        {
            if (type == null) return sigReader.ReadInt32();
            switch (type.typeCode)
            {
                case ElementType.Boolean: return sigReader.ReadBoolean();
                case ElementType.Char: return sigReader.ReadChar();
                case ElementType.Double: return sigReader.ReadDouble();
                case ElementType.Single: return sigReader.ReadSingle();
                case ElementType.Int16: return sigReader.ReadInt16();
                case ElementType.Int32: return sigReader.ReadInt32();
                case ElementType.Int64: return sigReader.ReadInt64();
                case ElementType.Int8: return sigReader.ReadSByte();
                case ElementType.UInt16: return sigReader.ReadUInt16();
                case ElementType.UInt32: return sigReader.ReadUInt32();
                case ElementType.UInt64: return sigReader.ReadUInt64();
                case ElementType.UInt8: return sigReader.ReadByte();
                case ElementType.String: return ReadSerString(sigReader);
                case ElementType.ValueType:
                    var etype = GetCustomAttributeEnumNode(ref type);
                    var enumVal = GetCustomAttributeLiteralValue(sigReader, etype.UnderlyingType);
#if !MinimalReader
                    if (module.ContainingAssembly != null &&
                        (module.ContainingAssembly.Flags & AssemblyFlags.ContainsForeignTypes) != 0)
                        if (etype == SystemTypes.AttributeTargets)
                            switch ((int)enumVal)
                            {
                                case 0x00000001:
                                    enumVal = 0x00001000;
                                    break;
                                case 0x00000002:
                                    enumVal = 0x00000010;
                                    break;
                                case 0x00000004:
                                    enumVal = 0x00000200;
                                    break;
                                case 0x00000008:
                                    enumVal = 0x00000100;
                                    break;
                                case 0x00000010:
                                    enumVal = 0x00000400;
                                    break;
                                case 0x00000020:
                                    enumVal = 0x00000000;
                                    break;
                                case 0x00000040:
                                    enumVal = 0x00000040;
                                    break;
                                case 0x00000080:
                                    enumVal = 0x00000800;
                                    break;
                                case 0x00000100:
                                    enumVal = 0x00000080;
                                    break;
                                case 0x00000200:
                                    enumVal = 0x00000004;
                                    break;
                                case 0x00000400:
                                    enumVal = 0x00000008;
                                    break;
                                case 0x00000800:
                                    enumVal = 0x00000000;
                                    break;
                                case -1:
                                    enumVal = 0x00007FFF;
                                    break;
                            }
#endif
                    return enumVal;
                case ElementType.Class: return GetTypeFromSerializedName(ReadSerString(sigReader));
                case ElementType.SzArray:
                    var numElems = sigReader.ReadInt32();
                    var elemType = ((ArrayType)type).ElementType;
                    return GetCustomAttributeLiteralArray(sigReader, numElems, elemType);
                case ElementType.Object:
                {
                    type = ParseTypeSignature(sigReader);
                    return GetCustomAttributeLiteralValue(sigReader, ref type);
                }
            }

            throw new InvalidMetadataException(ExceptionStrings.UnexpectedTypeInCustomAttribute);
        }

        private static EnumNode /*!*/ GetCustomAttributeEnumNode(ref TypeNode /*!*/ type)
        {
            var etype = type as EnumNode;
            if (etype == null || etype.UnderlyingType == null)
            {
                //Happens when type is declared in a assembly that has not been resolved. In that case only the type name
                //and the fact that it is a value type is known. There is no completely safe recovery from it, but at this point we
                //can fake up an enum with Int32 as underlying type. This works in most situations.
                etype = new EnumNode();
                etype.Name = type.Name;
                etype.Namespace = type.Namespace;
                etype.DeclaringModule = type.DeclaringModule;
                etype.UnderlyingType = CoreSystemTypes.Int32;
                type = etype;
            }

            return etype;
        }

        private Array GetCustomAttributeLiteralArray(MemoryCursor /*!*/ sigReader, int numElems,
            TypeNode /*!*/ elemType)
        {
            var array = ConstructCustomAttributeLiteralArray(numElems, elemType);
            for (var i = 0; i < numElems; i++)
            {
                var elem = GetCustomAttributeLiteralValue(sigReader, elemType);
                array.SetValue(elem, i);
            }

            return array;
        }

        private Array ConstructCustomAttributeLiteralArray(int numElems, TypeNode /*!*/ elemType)
        {
            if (numElems == -1) return null;
            if (numElems < 0) throw new InvalidMetadataException(ExceptionStrings.UnexpectedTypeInCustomAttribute);
            switch (elemType.typeCode)
            {
                case ElementType.Boolean: return new bool[numElems];
                case ElementType.Char: return new char[numElems];
                case ElementType.Double: return new double[numElems];
                case ElementType.Single: return new float[numElems];
                case ElementType.Int16: return new short[numElems];
                case ElementType.Int32: return new int[numElems];
                case ElementType.Int64: return new long[numElems];
                case ElementType.Int8: return new sbyte[numElems];
                case ElementType.UInt16: return new ushort[numElems];
                case ElementType.UInt32: return new uint[numElems];
                case ElementType.UInt64: return new ulong[numElems];
                case ElementType.UInt8: return new byte[numElems];
                case ElementType.String: return new string[numElems];
                // Only enum value types are legal in attribute instances as stated in section 17.1.3 of the C# 1.0 spec
                case ElementType.ValueType:
                    var /*!*/
                        elType = elemType;
                    var eType = GetCustomAttributeEnumNode(ref elType);
                    return ConstructCustomAttributeLiteralArray(numElems, eType.UnderlyingType);
                // This needs to be a TypeNode since GetCustomAttributeLiteralValue will return a Struct if the Type is a value type
                case ElementType.Class: return new TypeNode[numElems];
                // REVIEW: Is this the right exception? Is this the right exception string?
                // Multi-dimensional arrays are not legal in attribute instances according section 17.1.3 of the C# 1.0 spec
                case ElementType.SzArray:
                    throw new InvalidMetadataException(ExceptionStrings.BadCustomAttributeTypeEncodedToken);
                case ElementType.Object: return new object[numElems];
            }

            throw new InvalidMetadataException(ExceptionStrings.UnexpectedTypeInCustomAttribute);
        }

        //TODO: rewrite this entire mess using a proper grammar based parser
        private TypeNode /*!*/ GetTypeFromSerializedName(string serializedName)
        {
            if (serializedName == null) return null;
            string assemblyName = null;
            var typeName = serializedName;
            var firstComma = FindFirstCommaOutsideBrackets(serializedName);
            if (firstComma > 0)
            {
                var i = 1;
                while (firstComma + i < serializedName.Length && serializedName[firstComma + i] == ' ') i++;
                assemblyName = serializedName.Substring(firstComma + i);
                typeName = serializedName.Substring(0, firstComma);
            }

            return GetTypeFromSerializedName(typeName, assemblyName);
        }

        private static int FindFirstCommaOutsideBrackets(string /*!*/ serializedName)
        {
            var numBrackets = 0;
            var numAngles = 0;
            for (int i = 0, n = serializedName == null ? 0 : serializedName.Length; i < n; i++)
            {
                var ch = serializedName[i];
                if (ch == '[')
                {
                    numBrackets++;
                }
                else if (ch == ']')
                {
                    if (--numBrackets < 0) return -1;
                }
                else if (ch == '<')
                {
                    numAngles++;
                }
                else if (ch == '>')
                {
                    if (--numAngles < 0) return -1;
                }
                else if (ch == ',' && numBrackets == 0 && numAngles == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private TypeNode /*!*/ GetTypeFromSerializedName(string /*!*/ typeName, string assemblyName)
        {
            string /*!*/
                nspace, name;
            int i;
            ParseTypeName(typeName, out nspace, out name, out i);
            Module tMod = null;
            var t = LookupType(nspace, name, assemblyName, out tMod);
            if (t == null)
            {
                if (i < typeName.Length && typeName[i] == '!')
                {
                    var codedIndex = 0;
                    if (PlatformHelpers.TryParseInt32(typeName.Substring(0, i), out codedIndex))
                    {
                        t = DecodeAndGetTypeDefOrRefOrSpec(codedIndex);
                        if (t != null) return t;
                    }
                }

                t = GetDummyTypeNode(Identifier.For(nspace), Identifier.For(name), tMod, null, false);
            }

            if (i >= typeName.Length) return t;
            var ch = typeName[i];
            if (ch == '+') return GetTypeFromSerializedName(typeName.Substring(i + 1), t);
            if (ch == '&') return t.GetReferenceType();
            if (ch == '*') return t.GetPointerType();
            if (ch == '[') return ParseArrayOrGenericType(typeName.Substring(i + 1, typeName.Length - 1 - i), t);
            throw new InvalidMetadataException(ExceptionStrings.BadSerializedTypeName);
        }

        private TypeNode /*!*/ GetTypeFromSerializedName(string /*!*/ typeName, TypeNode /*!*/ nestingType)
        {
            string /*!*/
                name;
            var i = 0;
            ParseSimpleTypeName(typeName, out name, ref i);
            var t = nestingType.GetNestedType(Identifier.For(name));
            if (t == null)
                t = GetDummyTypeNode(Identifier.Empty, Identifier.For(name), nestingType.DeclaringModule, nestingType,
                    false);
            if (i >= typeName.Length) return t;
            var ch = typeName[i];
            if (ch == '+') return GetTypeFromSerializedName(typeName.Substring(i + 1), t);
            if (ch == '&') return t.GetReferenceType();
            if (ch == '*') return t.GetPointerType();
            if (ch == '[') return ParseArrayOrGenericType(typeName.Substring(i + 1, typeName.Length - 1 - i), t);
            throw new InvalidMetadataException(ExceptionStrings.BadSerializedTypeName);
        }

        private TypeNode /*!*/ ParseArrayOrGenericType(string typeName, TypeNode /*!*/ rootType)
        {
            if (typeName == null || rootType == null)
            {
                Debug.Assert(false);
                return rootType;
            }

            //Get here after "rootType[" has been parsed. What follows is either an array type specifier or some generic type arguments.
            if (typeName.Length == 0)
                throw new InvalidMetadataException(ExceptionStrings
                    .BadSerializedTypeName); //Something ought to follow the [
            if (typeName[0] == ']')
            {
                //Single dimensional array with zero lower bound
                if (typeName.Length == 1) return rootType.GetArrayType(1);
                if (typeName[1] == '[' && typeName.Length > 2)
                    return ParseArrayOrGenericType(typeName.Substring(2), rootType.GetArrayType(1));
                throw new InvalidMetadataException(ExceptionStrings.BadSerializedTypeName);
            }

            if (typeName[0] == '*')
            {
                //Single dimensional array with unknown lower bound
                if (typeName.Length > 1 && typeName[1] == ']')
                {
                    if (typeName.Length == 2) return rootType.GetArrayType(1, true);
                    if (typeName[2] == '[' && typeName.Length > 3)
                        return ParseArrayOrGenericType(typeName.Substring(3), rootType.GetArrayType(1, true));
                }

                throw new InvalidMetadataException(ExceptionStrings.BadSerializedTypeName);
            }

            if (typeName[0] == ',')
            {
                //Muti dimensional array
                var rank = 1;
                while (rank < typeName.Length && typeName[rank] == ',') rank++;
                if (rank < typeName.Length && typeName[rank] == ']')
                {
                    if (typeName.Length == rank + 1) return rootType.GetArrayType(rank + 1);
                    if (typeName[rank + 1] == '[' && typeName.Length > rank + 2)
                        return ParseArrayOrGenericType(typeName.Substring(rank + 2), rootType.GetArrayType(rank));
                }

                throw new InvalidMetadataException(ExceptionStrings.BadSerializedTypeName);
            }

            //Generic type instance
            var offset = 0;
            if (typeName[0] == '[')
                offset = 1; //Assembly qualified type name forming part of a generic parameter list        
            var arguments = new TypeNodeList();
            var commaPos = FindFirstCommaOutsideBrackets(typeName);
            while (commaPos > 1)
            {
                arguments.Add(GetTypeFromSerializedName(typeName.Substring(offset, commaPos - offset)));
                typeName = typeName.Substring(commaPos + 1);
                offset = typeName[0] == '[' ? 1 : 0;
                commaPos = FindFirstCommaOutsideBrackets(typeName);
            }

            //Find the position of the first unbalanced ].
            var lastCharPos = offset;
            for (var leftBracketCount = 0; lastCharPos < typeName.Length; lastCharPos++)
            {
                var ch = typeName[lastCharPos];
                if (ch == '[')
                {
                    leftBracketCount++;
                }
                else if (ch == ']')
                {
                    leftBracketCount--;
                    if (leftBracketCount < 0) break;
                }
            }

            arguments.Add(GetTypeFromSerializedName(typeName.Substring(offset, lastCharPos - offset)));
            var retVal = rootType.GetGenericTemplateInstance(module, arguments);
            if (lastCharPos + 1 < typeName.Length && typeName[lastCharPos + 1] == ']')
                lastCharPos++;
            if (lastCharPos + 1 < typeName.Length)
            {
                //The generic type is complete, but there is yet more to the type
                var ch = typeName[lastCharPos + 1];
                if (ch == '+') retVal = GetTypeFromSerializedName(typeName.Substring(lastCharPos + 2), retVal);
                if (ch == '&') retVal = retVal.GetReferenceType();
                if (ch == '*') retVal = retVal.GetPointerType();
                if (ch == '[')
                    retVal = ParseArrayOrGenericType(
                        typeName.Substring(lastCharPos + 2, typeName.Length - 1 - lastCharPos - 1), retVal);
            }

            return retVal;
        }

        private static void ParseSimpleTypeName(string /*!*/ source, out string /*!*/ name, ref int i)
        {
            var n = source.Length;
            var start = i;
            var sb = new StringBuilder();
            for (; i < n; i++)
            {
                var ch = source[i];
                if (ch == '\\')
                {
                    i++;
                    continue;
                }

                if (ch == '.' || ch == '+' || ch == '&' || ch == '*' || ch == '[' || ch == '!') break;
                sb.Append(ch);
                if (ch == '<')
                {
                    var unmatched = 1;
                    while (unmatched > 0 && ++i < n)
                    {
                        ch = source[i];
                        if (ch != '\\') sb.Append(ch);
                        else sb.Append(source[i + 1]);
                        if (ch == '\\') i++;
                        else if (ch == '<') unmatched++;
                        else if (ch == '>') unmatched--;
                    }
                }
            }

            name = sb.ToString();
            return;
            if (i < n)
                name = source.Substring(start, i - start);
            else
                name = source.Substring(start);
        }

        private static void ParseTypeName(string /*!*/ source, out string /*!*/ nspace, out string /*!*/ name,
            out int i)
        {
            i = 0;
            var n = source.Length;
            nspace = string.Empty;
            while (true)
            {
                var start = i;
                ParseSimpleTypeName(source, out name, ref i);
                if (i < n && source[i] == '.')
                {
                    i++;
                    continue;
                }

                if (start != 0) nspace = source.Substring(0, start - 1);
                return;
            }
        }

        private TypeNode LookupType(string /*!*/ nameSpace, string /*!*/ name, string assemblyName, out Module module)
        {
            var namespaceId = Identifier.For(nameSpace);
            var nameId = Identifier.For(name);
            module = this.module;
            //^ assume module != null;
            if (assemblyName == null)
            {
                var t = module.GetType(namespaceId, nameId);
                if (t != null) return t;
                module = CoreSystemTypes.SystemAssembly;
                return CoreSystemTypes.SystemAssembly.GetType(namespaceId, nameId);
            }

            //See if the type is in one of the assemblies explcitly referenced by the current module
            var arefs = module.AssemblyReferences;
            for (int i = 0, n = arefs == null ? 0 : arefs.Count; i < n; i++)
            {
                var aref = arefs[i];
                if (aref != null && aref.StrongName == assemblyName && aref.Assembly != null)
                {
                    module = aref.Assembly;
                    return aref.Assembly.GetType(namespaceId, nameId);
                }
            }

            //Construct an assembly reference and probe for it
            var aRef = new AssemblyReference(assemblyName);
            var referringAssembly = this.module as AssemblyNode;
            if (referringAssembly != null && (referringAssembly.Flags & AssemblyFlags.Retargetable) != 0)
                aRef.Flags |= AssemblyFlags.Retargetable;
            var aNode = GetAssemblyFromReference(aRef);
            if (aNode != null)
            {
                module = aNode;
                var result = aNode.GetType(namespaceId, nameId);
                return result;
            }

            return null;
        }

        private void GetCustomAttributesFor(Module /*!*/ module)
        {
            try
            {
                if (tables.entryPointToken != 0)
                    module.EntryPoint = (Method)GetMemberFromToken(tables.entryPointToken);
                else
                    module.EntryPoint = Module.NoSuchMethod;
                if (module.NodeType == NodeType.Module)
                {
                    module.Attributes = GetCustomAttributesNonNullFor((1 << 5) | 7);
                    return;
                }

                var assembly = (AssemblyNode)module;
                assembly.SecurityAttributes = GetSecurityAttributesFor((1 << 2) | 2);
                assembly.Attributes = GetCustomAttributesNonNullFor((1 << 5) | 14);
                assembly.ModuleAttributes = GetCustomAttributesNonNullFor((1 << 5) | 7);
#if !FxCop
            }
            catch (Exception e)
            {
                if (this.module == null) return;
                if (this.module.MetadataImportErrors == null) this.module.MetadataImportErrors = new ArrayList();
                this.module.MetadataImportErrors.Add(e);
                module.Attributes = new AttributeList(0);
            }
#else
      }finally{}
#endif
        }

        private AttributeList /*!*/ GetCustomAttributesNonNullFor(int parentIndex)
        {
            var result = GetCustomAttributesFor(parentIndex);
            if (result != null) return result;
            return new AttributeList(0);
        }

        private AttributeList GetCustomAttributesFor(int parentIndex)
        {
            var customAttributes = tables.CustomAttributeTable;
            AttributeList attributes = null;
            try
            {
                int i = 0, n = customAttributes.Length, j = n - 1;
                if (n == 0) return null;
                var sorted = (sortedTablesMask >> (int)TableIndices.CustomAttribute) % 2 == 1;
                if (sorted)
                {
                    while (i < j)
                    {
                        var k = (i + j) / 2;
                        if (customAttributes[k].Parent < parentIndex)
                            i = k + 1;
                        else
                            j = k;
                    }

                    while (i > 0 && customAttributes[i - 1].Parent == parentIndex) i--;
                }

                var count = 0;
                for (var l = i; l < n; l++)
                    if (customAttributes[l].Parent == parentIndex)
                        count++;
                    else if (sorted)
                        break;
                if (count > 0) attributes = new AttributeList(count);
                for (; i < n; i++)
                    if (customAttributes[i].Parent == parentIndex)
                        attributes.Add(GetCustomAttribute(i));
                    else if (sorted)
                        break;
#if !FxCop
            }
            catch (Exception e)
            {
                if (module == null) return attributes;
                if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                module.MetadataImportErrors.Add(e);
            }
#else
      }finally{}
#endif
            return attributes;
        }

        private SecurityAttributeList GetSecurityAttributesFor(int parentIndex)
        {
            var securityAttributes = tables.DeclSecurityTable;
            var attributes = new SecurityAttributeList();
            try
            {
                int i = 0, n = securityAttributes.Length, j = n - 1;
                if (n == 0) return attributes;
                var sorted = (sortedTablesMask >> (int)TableIndices.DeclSecurity) % 2 == 1;
                if (sorted)
                {
                    while (i < j)
                    {
                        var k = (i + j) / 2;
                        if (securityAttributes[k].Parent < parentIndex)
                            i = k + 1;
                        else
                            j = k;
                    }

                    while (i > 0 && securityAttributes[i - 1].Parent == parentIndex) i--;
                }

                for (; i < n; i++)
                    if (securityAttributes[i].Parent == parentIndex)
                        attributes.Add(GetSecurityAttribute(i));
                    else if (sorted)
                        break;
#if !FxCop
            }
            catch (Exception e)
            {
                if (module == null) return attributes;
                if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                module.MetadataImportErrors.Add(e);
            }
#else
      }finally{}
#endif
            return attributes;
        }

        private void GetTypeParameterConstraints(int parentIndex, TypeNodeList parameters)
        {
            if (parameters == null) return;
            var genericParameters = tables.GenericParamTable;
            int i = 0, n = genericParameters.Length, j = n - 1;
            var sorted = (sortedTablesMask >> (int)TableIndices.GenericParam) % 2 == 1;
            if (sorted)
            {
                while (i < j)
                {
                    var k = (i + j) / 2;
                    if (genericParameters[k].Owner < parentIndex)
                        i = k + 1;
                    else
                        j = k;
                }

                while (i > 0 && genericParameters[i - 1].Owner == parentIndex) i--;
            }

            for (var k = 0; i < n && k < parameters.Count; i++, k++)
                if (genericParameters[i].Owner == parentIndex)
                {
                    var gp = parameters[k];
                    GetGenericParameterConstraints(i, ref gp);
                    parameters[k] = gp;
                }
                else if (sorted)
                {
                    break;
                }
        }

        private TypeNodeList GetTypeParametersFor(int parentIndex, Member parent)
        {
            var genericParameters = tables.GenericParamTable;
            var types = new TypeNodeList();
            int i = 0, n = genericParameters.Length, j = n - 1;
            var sorted = (sortedTablesMask >> (int)TableIndices.GenericParam) % 2 == 1;
            if (sorted)
            {
                while (i < j)
                {
                    var k = (i + j) / 2;
                    if (genericParameters[k].Owner < parentIndex)
                        i = k + 1;
                    else
                        j = k;
                }

                while (i > 0 && genericParameters[i - 1].Owner == parentIndex) i--;
            }

            for (var index = 0; i < n; i++, index++)
                if (genericParameters[i].Owner == parentIndex)
                    types.Add(GetGenericParameter(i, index, parent));
                else if (sorted)
                    break;
            if (types.Count == 0) return null;
            return types;
        }

        private TypeNode GetGenericParameter(int index, int parameterListIndex, Member parent)
        {
            var genericParameters = tables.GenericParamTable;
            var gpr = genericParameters[index++];
            var name = tables.GetString(gpr.Name);
            var genericParameterConstraints = tables.GenericParamConstraintTable;
            var isClass = false;
            int i = 0, n = genericParameterConstraints.Length, j = n - 1;
            var sorted = (sortedTablesMask >> (int)TableIndices.GenericParamConstraint) % 2 == 1;
            if (sorted)
            {
                while (i < j)
                {
                    var k = (i + j) / 2;
                    if (genericParameterConstraints[k].Param < index)
                        i = k + 1;
                    else
                        j = k;
                }

                while (i > 0 && genericParameterConstraints[i - 1].Param == index) i--;
            }

            for (; i < n && !isClass; i++)
                if (genericParameterConstraints[i].Param == index)
                    isClass = TypeDefOrRefOrSpecIsClass(genericParameterConstraints[i].Constraint);
                else if (sorted)
                    break;
            if (isClass)
            {
                var cp = parent is Method ? new MethodClassParameter() : new ClassParameter();
                cp.DeclaringMember = parent;
                cp.ParameterListIndex = parameterListIndex;
                cp.Name = Identifier.For(name);
                cp.DeclaringModule = module;
                cp.TypeParameterFlags = (TypeParameterFlags)gpr.Flags;
                cp.ProvideTypeAttributes = GetTypeParameterAttributes;
                cp.ProviderHandle = index;
                return cp;
            }

            var tp = parent is Method ? new MethodTypeParameter() : new TypeParameter();
            tp.DeclaringMember = parent;
            tp.ParameterListIndex = parameterListIndex;
            tp.Name = Identifier.For(name);
            tp.DeclaringModule = module;
            tp.TypeParameterFlags = (TypeParameterFlags)gpr.Flags;
            tp.ProvideTypeAttributes = GetTypeParameterAttributes;
            tp.ProviderHandle = index;
            return tp;
        }

        private void GetGenericParameterConstraints(int index, ref TypeNode /*!*/ parameter)
        {
            Debug.Assert(parameter != null);
            index++;
            var genericParameterConstraints = tables.GenericParamConstraintTable;
            var constraints = new TypeNodeList();
            Class baseClass = null;
            var interfaces = new InterfaceList();
            int i = 0, n = genericParameterConstraints.Length, j = n - 1;
            var sorted = (sortedTablesMask >> (int)TableIndices.GenericParamConstraint) % 2 == 1;
            if (sorted)
            {
                while (i < j)
                {
                    var k = (i + j) / 2;
                    if (genericParameterConstraints[k].Param < index)
                        i = k + 1;
                    else
                        j = k;
                }

                while (i > 0 && genericParameterConstraints[i - 1].Param == index) i--;
            }

            for (; i < n; i++)
                if (genericParameterConstraints[i].Param == index)
                {
                    var t = DecodeAndGetTypeDefOrRefOrSpec(genericParameterConstraints[i].Constraint);
                    var c = t as Class;
                    if (c != null)
                    {
                        if (baseClass == null) baseClass = c;
                    }
                    else if (t is Interface)
                    {
                        interfaces.Add((Interface)t);
                    }

                    constraints.Add(t);
                }
                else if (sorted)
                {
                    break;
                }

            var cp = parameter as ClassParameter;
            if (cp == null && baseClass != null)
            {
                cp = ((ITypeParameter)parameter).DeclaringMember is Method
                    ? new MethodClassParameter()
                    : new ClassParameter();
                cp.Name = parameter.Name;
                cp.DeclaringMember = ((ITypeParameter)parameter).DeclaringMember;
                cp.ParameterListIndex = ((ITypeParameter)parameter).ParameterListIndex;
                cp.DeclaringModule = module;
                cp.TypeParameterFlags = ((ITypeParameter)parameter).TypeParameterFlags;
                cp.ProvideTypeAttributes = GetTypeParameterAttributes;
                cp.ProviderHandle = index;
                parameter = cp;
            }

            if (cp != null)
                cp.structuralElementTypes = constraints;
            else
                ((TypeParameter)parameter).structuralElementTypes = constraints;
            if (baseClass != null && cp != null) cp.BaseClass = baseClass;
            parameter.Interfaces = interfaces;
        }

        internal static Block /*!*/ GetOrCreateBlock(TrivialHashtable /*!*/ blockMap, int address)
        {
            var block = (Block)blockMap[address + 1];
            if (block == null)
            {
                blockMap[address + 1] = block = new Block(new StatementList());
#if !FxCop && !CodeContracts
        var sctx = block.SourceContext;
        sctx.StartPos = address;
        block.SourceContext = sctx;
#else
                block.ILOffset = address;
#endif
#if ILOFFSETS
                block.ILOffset = address;
#endif
            }

            return block;
        }

        internal Field GetFieldFromDef(int i)
        {
            return GetFieldFromDef(i, null);
        }

        internal Field GetFieldFromDef(int i, TypeNode declaringType)
        {
            var fieldDefs = tables.FieldTable;
            var fld = fieldDefs[i - 1];
            if (fld.Field != null) return fld.Field;
            var field = new Field();
            fieldDefs[i - 1].Field = field;
            field.Attributes = GetCustomAttributesFor((i << 5) | 1);
            field.Flags = (FieldFlags)fld.Flags;
            field.Name = tables.GetIdentifier(fld.Name);
            if ((field.Flags & FieldFlags.RTSpecialName) != 0 &&
                field.Name.UniqueIdKey == StandardIds._Deleted.UniqueIdKey) return null;
            tables.GetSignatureLength(fld.Signature); //sigLength
            var sigReader = tables.GetNewCursor();
            GetAndCheckSignatureToken(6, sigReader);
            field.Type = ParseTypeSignature(sigReader);
            var reqMod = field.Type as RequiredModifier;
            if (reqMod != null && reqMod.Modifier == CoreSystemTypes.IsVolatile)
            {
                field.IsVolatile = true;
                field.Type = reqMod.ModifiedType;
            }

            if ((field.Flags & FieldFlags.HasDefault) != 0)
                field.DefaultValue = GetLiteral(i << 2, field.Type);
            if ((field.Flags & FieldFlags.HasFieldMarshal) != 0)
                field.MarshallingInformation = GetMarshallingInformation((i << 1) | 0);
            if ((field.Flags & FieldFlags.HasFieldRVA) != 0)
                field.InitialData = GetInitialData(i, field.Type, out field.section);
            if (declaringType == null)
            {
                var typeDefs = tables.TypeDefTable;
                var indx = i;
                var fieldPtrs = tables.FieldPtrTable;
                var n = fieldPtrs.Length;
                for (var j = 0; j < n; j++)
                    if (fieldPtrs[j].Field == i)
                    {
                        indx = j + 1;
                        break;
                    }

                n = typeDefs.Length;
                for (var j = n - 1; j >= 0; j--)
                {
                    //TODO: binary search
                    var tdr = typeDefs[j];
                    if (tdr.FieldList <= indx)
                    {
                        declaringType = GetTypeFromDef(j + 1);
                        break;
                    }
                }
            }

            field.DeclaringType = declaringType;
            if (declaringType != null && (declaringType.Flags & TypeFlags.ExplicitLayout) != 0)
            {
                var fieldLayouts = tables.FieldLayoutTable;
                var n = fieldLayouts.Length;
                for (var j = n - 1; j >= 0; j--)
                {
                    //TODO: binary search
                    var flr = fieldLayouts[j];
                    if (flr.Field == i)
                    {
                        field.Offset = flr.Offset;
                        break;
                    }
                }
            }

            return field;
        }

        private byte[] GetInitialData(int fieldIndex, TypeNode fieldType, out PESection targetSection)
        {
            targetSection = PESection.Text;
            var fieldRvaTable = tables.FieldRvaTable;
            var sorted = (sortedTablesMask >> (int)TableIndices.FieldRva) % 2 == 1;
            int i = 0, n = fieldRvaTable.Length, j = n - 1;
            if (n == 0) return null;
            if (sorted)
                while (i < j)
                {
                    var k = (i + j) / 2;
                    if (fieldRvaTable[k].Field < fieldIndex)
                        i = k + 1;
                    else
                        j = k;
                }
            else
                for (; i < j; i++)
                    if (fieldRvaTable[i].Field == fieldIndex)
                        break;

            var frr = fieldRvaTable[i];
            if (frr.Field != fieldIndex) return null;
            var fld = tables.FieldTable[fieldIndex - 1].Field;
            if (fld != null) fld.Offset = frr.RVA;
            fieldType = TypeNode.StripModifiers(fieldType);
            var enumType = fieldType as EnumNode;
            if (enumType != null) fieldType = TypeNode.StripModifiers(enumType.UnderlyingType);
            if (fieldType == null)
            {
                Debug.Fail("");
                return null;
            }

            var size = fieldType.ClassSize;
            if (size <= 0)
                switch (fieldType.typeCode)
                {
                    case ElementType.Boolean:
                        size = 1;
                        break;
                    case ElementType.Char:
                        size = 2;
                        break;
                    case ElementType.Double:
                        size = 8;
                        break;
                    case ElementType.Int16:
                        size = 2;
                        break;
                    case ElementType.Int32:
                        size = 4;
                        break;
                    case ElementType.Int64:
                        size = 8;
                        break;
                    case ElementType.Int8:
                        size = 1;
                        break;
                    case ElementType.Single:
                        size = 4;
                        break;
                    case ElementType.UInt16:
                        size = 2;
                        break;
                    case ElementType.UInt32:
                        size = 4;
                        break;
                    case ElementType.UInt64:
                        size = 8;
                        break;
                    case ElementType.UInt8:
                        size = 1;
                        break;
                    default:
                        if (fieldType is Pointer || fieldType is FunctionPointer)
                        {
                            size = 4;
                            break;
                        }

                        //TODO: this seems wrong
                        if (i < n - 1)
                            size = fieldRvaTable[i + 1].RVA - frr.RVA;
                        else if (targetSection != PESection.Text)
                            size = tables.GetOffsetToEndOfSection(frr.RVA);
                        break;
                }

            if (size <= 0) return null;
            if (tables.NoOffsetFor(frr.RVA) || tables.NoOffsetFor(frr.RVA + size - 1))
                return null;
            var c = tables.GetNewCursor(frr.RVA, out targetSection);
            var result = new byte[size];
            for (i = 0; i < size; i++)
                result[i] = c.ReadByte();
            return result;
        }

        private Literal GetLiteral(int parentCodedIndex, TypeNode /*!*/ type)
        {
            var constants = tables.ConstantTable;
            //TODO: do a binary search
            for (int i = 0, n = constants.Length; i < n; i++)
            {
                if (constants[i].Parent != parentCodedIndex) continue;
                var value = tables.GetValueFromBlob(constants[i].Type, constants[i].Value);
                var valTypeCode = Convert.GetTypeCode(value);
                var underlyingType = type;
                if (type is EnumNode) underlyingType = ((EnumNode)type).UnderlyingType;
                if (underlyingType.TypeCode != valTypeCode) type = CoreSystemTypes.Object;
                if (type == CoreSystemTypes.Object && value != null)
                    switch (valTypeCode)
                    {
                        case TypeCode.Boolean:
                            type = CoreSystemTypes.Boolean;
                            break;
                        case TypeCode.Byte:
                            type = CoreSystemTypes.UInt8;
                            break;
                        case TypeCode.Char:
                            type = CoreSystemTypes.Char;
                            break;
                        case TypeCode.Double:
                            type = CoreSystemTypes.Double;
                            break;
                        case TypeCode.Int16:
                            type = CoreSystemTypes.Int16;
                            break;
                        case TypeCode.Int32:
                            type = CoreSystemTypes.Int32;
                            break;
                        case TypeCode.Int64:
                            type = CoreSystemTypes.Int64;
                            break;
                        case TypeCode.SByte:
                            type = CoreSystemTypes.Int8;
                            break;
                        case TypeCode.Single:
                            type = CoreSystemTypes.Single;
                            break;
                        case TypeCode.String:
                            type = CoreSystemTypes.String;
                            break;
                        case TypeCode.UInt16:
                            type = CoreSystemTypes.UInt16;
                            break;
                        case TypeCode.UInt32:
                            type = CoreSystemTypes.UInt32;
                            break;
                        case TypeCode.UInt64:
                            type = CoreSystemTypes.UInt64;
                            break;
                        case TypeCode.Empty:
                        case TypeCode.Object:
                            type = CoreSystemTypes.Type;
                            break;
                    }

                return new Literal(value, type);
            }

            throw new InvalidMetadataException(ExceptionStrings.BadConstantParentIndex);
        }

        internal FunctionPointer GetCalliSignature(int ssigToken)
        {
#if !FxCop
            var ssr = tables.StandAloneSigTable[(ssigToken & 0xFFFFFF) - 1];
#else
      int index = (ssigToken & 0xFFFFFF) - 1;
      if (index < 0 || index >= this.tables.StandAloneSigTable.Length)
        return null;

      StandAloneSigRow ssr = this.tables.StandAloneSigTable[index];
#endif
            var sigReader = tables.GetBlobCursor(ssr.Signature);
            return ParseFunctionPointer(sigReader);
        }

        internal void GetLocals(int localIndex, LocalList /*!*/ locals,
            Dictionary<int, LocalInfo> /*!*/ localSourceNames)
        {
            if (localIndex == 0) return;
            var ssr = tables.StandAloneSigTable[(localIndex & 0xFFFFFF) - 1];
            tables.GetSignatureLength(ssr.Signature);
            var sigReader = tables.GetNewCursor();
            if (sigReader.ReadByte() != 0x7) throw new InvalidMetadataException(ExceptionStrings.InvalidLocalSignature);
            var count = sigReader.ReadCompressedInt();
            for (var i = 0; i < count; i++)
            {
                LocalInfo localInfo;
                var hasPDBInfo = localSourceNames.TryGetValue(i, out localInfo);
                var lookupName = localInfo.Name;
                var hasNoPDBName = string.IsNullOrEmpty(lookupName);
#if !FxCop
                var name = hasNoPDBName ? "local" + i : lookupName;
#else
        string name = hasNoPDBName ? "local$"+i : lookupName;
#endif
                var pinned = false;
                var locType = ParseTypeSignature(sigReader, ref pinned);
                var loc = new Local(Identifier.For(name), locType);
                loc.Pinned = pinned;
                loc.HasNoPDBInfo = !hasPDBInfo;
                loc.Attributes = localInfo.Attributes;
                locals.Add(loc);
            }
        }
#if !ROTOR
        internal void GetLocalSourceNames(ISymUnmanagedScope /*!*/ scope,
            Dictionary<int, LocalInfo> /*!*/ localSourceNames)
        {
            var numLocals = scope.GetLocalCount();
            var localPtrs = new IntPtr[numLocals];
            scope.GetLocals((uint)localPtrs.Length, out numLocals, localPtrs);

            var nameBuffer = new char[100];
            uint nameLen;
            for (var i = 0; i < numLocals; i++)
            {
                var local =
                    (ISymUnmanagedVariable)Marshal.GetTypedObjectForIUnknown(localPtrs[i],
                        typeof(ISymUnmanagedVariable));
                if (local != null)
                {
                    local.GetName(0, out nameLen, null);
                    if (nameLen > nameBuffer.Length) nameBuffer = new char[nameLen];
                    local.GetName((uint)nameBuffer.Length, out nameLen, nameBuffer);
                    var localIndex = (int)local.GetAddressField1();
                    var localName = new string(nameBuffer, 0, (int)nameLen - 1);
                    var attributes = local.GetAttributes();
                    localSourceNames[localIndex] = new LocalInfo(localName, attributes);
                    Marshal.ReleaseComObject(local);
                }

                Marshal.Release(localPtrs[i]);
            }

            var subscopes = new IntPtr[100];
            uint numScopes;
            scope.GetChildren((uint)subscopes.Length, out numScopes, subscopes);
            for (var i = 0; i < numScopes; i++)
            {
                var subscope =
                    (ISymUnmanagedScope)Marshal.GetTypedObjectForIUnknown(subscopes[i], typeof(ISymUnmanagedScope));
                if (subscope != null)
                {
                    GetLocalSourceNames(subscope, localSourceNames);
                    Marshal.ReleaseComObject(subscope);
                }

                Marshal.Release(subscopes[i]);
                //TODO: need to figure out how map these scope to blocks and set HasLocals on those blocks
            }
        }
#endif
        private MarshallingInformation GetMarshallingInformation(int parentCodedIndex)
        {
            var mtypes = tables.FieldMarshalTable;
            var sorted = (sortedTablesMask >> (int)TableIndices.FieldMarshal) % 2 == 1;
            int i = 0, n = mtypes.Length, j = n - 1;
            if (n == 0) return null;
            if (sorted)
            {
                while (i < j)
                {
                    var k = (i + j) / 2;
                    if (mtypes[k].Parent < parentCodedIndex)
                        i = k + 1;
                    else
                        j = k;
                }

                while (i > 0 && mtypes[i - 1].Parent == parentCodedIndex) i--;
            }
            else
            {
                for (; i < j; i++)
                    if (mtypes[i].Parent == parentCodedIndex)
                        break;
            }

            var fmr = mtypes[i];
            if (fmr.Parent != parentCodedIndex) return null;
            var result = new MarshallingInformation();
            var blobSize = 0;
            var c = tables.GetBlobCursor(fmr.NativeType, out blobSize);
            var initialPosition = c.Position;
            result.NativeType = (NativeType)c.ReadByte();
            if (result.NativeType == NativeType.CustomMarshaler)
            {
                c.ReadUInt16(); //Skip over 0
                result.Class = ReadSerString(c);
                result.Cookie = ReadSerString(c);
            }
            else if (blobSize > 1)
            {
                if (result.NativeType == NativeType.LPArray)
                {
                    result.ElementType = (NativeType)c.ReadByte();
                    result.ParamIndex = -1;
                    var bytesRead = 2;
                    if (bytesRead < blobSize)
                    {
                        var pos = c.Position;
                        result.ParamIndex = c.ReadCompressedInt();
                        bytesRead += c.Position - pos;
                        if (bytesRead < blobSize)
                        {
                            pos = c.Position;
                            result.ElementSize = c.ReadCompressedInt();
                            bytesRead += c.Position - pos;
                            if (bytesRead < blobSize)
                                result.NumberOfElements = c.ReadCompressedInt();
                        }
                    }
                }
                else if (result.NativeType == NativeType.SafeArray)
                {
                    result.ElementType =
                        (NativeType)c
                            .ReadByte(); //Actually a variant type. TODO: what about VT_VECTOR VT_ARRAY and VT_BYREF?
                    if (c.Position < initialPosition + blobSize - 1)
                        result.Class = ReadSerString(c);
                }
                else
                {
                    result.Size = c.ReadCompressedInt();
                    if (result.NativeType == NativeType.ByValArray)
                    {
                        if (c.Position < initialPosition + blobSize)
                            result.ElementType = (NativeType)c.ReadByte();
                        else
                            result.ElementType = NativeType.NotSpecified;
                    }
                }
            }

            return result;
        }

        private void GetMethodBody(Method /*!*/ method, object /*!*/ i, bool asInstructionList)
        {
            if (asInstructionList)
            {
                GetMethodInstructions(method, i);
                return;
            }

            var savedCurrentMethodTypeParameters = currentMethodTypeParameters;
            currentMethodTypeParameters = method.templateParameters;
            var savedCurrentType = currentType;
            currentType = method.DeclaringType;
            var savedCurrentTypeParameters = currentTypeParameters;
            currentTypeParameters = currentType.TemplateParameters;
            try
            {
                var meth = tables.MethodTable[(int)i - 1];
                StatementList statements;
                if (meth.RVA != 0 && ((MethodImplFlags)meth.ImplFlags & MethodImplFlags.ManagedMask) ==
                    MethodImplFlags.Managed)
                {
                    if (getDebugSymbols) GetMethodDebugSymbols(method, 0x6000000 | (uint)(int)i);
                    statements = ParseMethodBody(method, (int)i, meth.RVA);
                }
                else
                {
                    statements = new StatementList(0);
                }

                method.Body = new Block(statements);
#if FxCop
        if (statements.Count > 0) {
          SourceContext context = statements[0].SourceContext;
          method.SourceContext = context;
          method.Body.SourceContext = context;
        }
#endif
#if !MinimalReader
                method.Body.HasLocals = true;
#endif
#if !FxCop
            }
            catch (Exception e)
            {
                if (module != null)
                {
                    if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                    module.MetadataImportErrors.Add(e);
                }

                method.Body = new Block(new StatementList(0));
#endif
            }
            finally
            {
                currentMethodTypeParameters = savedCurrentMethodTypeParameters;
                currentType = savedCurrentType;
                currentTypeParameters = savedCurrentTypeParameters;
            }
        }

        private void GetMethodDebugSymbols(Method /*!*/ method, uint methodToken)
            //^ requires this.debugReader != null;
        {
#if !ROTOR
            ISymUnmanagedMethod methodInfo = null;
            try
            {
                try
                {
                    debugReader.GetMethod(methodToken, ref methodInfo);
                    debugDocuments = new Dictionary<IntPtr, UnmanagedDocument>(2); // typically methods have 1 doc
                    method.RecordSequencePoints(methodInfo, debugDocuments);
                }
                catch (COMException)
                {
                }
                catch (InvalidCastException)
                {
                }
                catch (InvalidComObjectException)
                {
                }
            }
            finally
            {
                if (methodInfo != null)
                    Marshal.ReleaseComObject(methodInfo);
            }
#endif
        }

        private void GetMethodInstructions(Method /*!*/ method, object /*!*/ i)
        {
            var savedCurrentMethodTypeParameters = currentMethodTypeParameters;
            currentMethodTypeParameters = method.templateParameters;
            var savedCurrentType = currentType;
            currentType = method.DeclaringType;
            try
            {
                var meth = tables.MethodTable[(int)i - 1];
                if (meth.RVA != 0 && ((MethodImplFlags)meth.ImplFlags & MethodImplFlags.ManagedMask) ==
                    MethodImplFlags.Managed)
                {
                    if (getDebugSymbols) GetMethodDebugSymbols(method, 0x6000000 | (uint)(int)i);
                    method.Instructions = ParseMethodInstructions(method, (int)i, meth.RVA);
                }
                else
                {
                    method.Instructions = new InstructionList(0);
                }
#if !FxCop
            }
            catch (Exception e)
            {
                if (module != null)
                {
                    if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                    module.MetadataImportErrors.Add(e);
                }

                method.Instructions = new InstructionList(0);
#endif
            }
            finally
            {
                currentMethodTypeParameters = savedCurrentMethodTypeParameters;
                currentType = savedCurrentType;
            }
        }

        internal Method GetMethodDefOrRef(int codedIndex)
        {
            switch (codedIndex & 0x1)
            {
                case 0x00: return GetMethodFromDef(codedIndex >> 1);
                case 0x01:
                    TypeNodeList varArgTypes;
                    return (Method)GetMemberFromRef(codedIndex >> 1, out varArgTypes);
            }

            throw new InvalidMetadataException(ExceptionStrings.BadCustomAttributeTypeEncodedToken);
        }

        private Method GetMethodDefOrRef(int codedIndex, int numberOfGenericArguments)
        {
            switch (codedIndex & 0x1)
            {
                case 0x00: return GetMethodFromDef(codedIndex >> 1);
                case 0x01:
                    TypeNodeList varArgTypes;
                    return (Method)GetMemberFromRef(codedIndex >> 1, out varArgTypes, numberOfGenericArguments);
            }

            throw new InvalidMetadataException(ExceptionStrings.BadCustomAttributeTypeEncodedToken);
        }

        internal Method /*!*/ GetMethodFromDef(int index)
        {
            return GetMethodFromDef(index, null);
        }

        internal Method /*!*/ GetMethodFromDef(int index, TypeNode declaringType)
        {
            var savedCurrentMethodTypeParameters = currentMethodTypeParameters;
            var savedCurrentTypeParameters = currentTypeParameters;
            var methodDefs = tables.MethodTable;
            var meth = methodDefs[index - 1];
            if (meth.Method != null) return meth.Method;
            if (declaringType == null)
            {
                var indx = index;
                var methodPtrs = tables.MethodPtrTable;
                int n = methodPtrs.Length, i = 0, j = n - 1;
                var sorted = (sortedTablesMask >> (int)TableIndices.MethodPtr) % 2 == 1;
                if (sorted)
                {
                    while (i < j)
                    {
                        var k = (i + j) / 2;
                        if (methodPtrs[k].Method < index)
                            i = k + 1;
                        else
                            j = k;
                    }

                    while (i > 0 && methodPtrs[i - 1].Method == index) i--;
                }

                for (; i < n; i++)
                    if (methodPtrs[i].Method == index)
                    {
                        indx = i + 1;
                        break;
                    }

                var typeDefs = tables.TypeDefTable;
                n = typeDefs.Length;
                i = 0;
                j = n - 1;
                sorted = (sortedTablesMask >> (int)TableIndices.TypeDef) % 2 == 1;
                if (sorted)
                {
                    while (i < j)
                    {
                        var k = (i + j) / 2;
                        if (typeDefs[k].MethodList < indx)
                            i = k + 1;
                        else
                            j = k;
                    }

                    j = i;
                    while (j < n - 1 && typeDefs[j + 1].MethodList == indx) j++;
                }

                for (; j >= 0; j--)
                    if (typeDefs[j].MethodList <= indx)
                    {
                        declaringType = GetTypeFromDef(j + 1);
                        break;
                    }
            }

            Method.MethodBodyProvider provider = GetMethodBody;
            var name = tables.GetIdentifier(meth.Name);
            Method method;
            if (((MethodFlags)meth.Flags & MethodFlags.SpecialName) != 0 &&
                ((MethodFlags)meth.Flags & MethodFlags.SpecialName) != 0)
            {
                if (name.Name == ".ctor")
                {
#if ILOFFSETS
                    method = methodDefs[index - 1].Method =
                        new InstanceInitializer(provider, index, ((int)TableIndices.Method << 24) | index);
#else
          method = methodDefs[index - 1].Method = new InstanceInitializer(provider, index);
#endif
                }
                else if (name.Name == ".cctor")
                {
#if ILOFFSETS
                    method = methodDefs[index - 1].Method =
                        new StaticInitializer(provider, index, ((int)TableIndices.Method << 24) | index);
#else
          method = methodDefs[index - 1].Method = new StaticInitializer(provider, index);
#endif
                }
                else
                {
#if ILOFFSETS
                    method = methodDefs[index - 1].Method =
                        new Method(provider, index, ((int)TableIndices.Method << 24) | index);
#else
          method = methodDefs[index - 1].Method = new Method(provider, index);
#endif
                }
            }
            else
            {
#if ILOFFSETS
                method = methodDefs[index - 1].Method =
                    new Method(provider, index, ((int)TableIndices.Method << 24) | index);
#else
        method = methodDefs[index - 1].Method = new Method(provider, index);
#endif
            }

            method.ProvideMethodAttributes = GetMethodAttributes;
            //method.Attributes = this.GetCustomAttributesFor((index << 5)|0); //TODO: get attributes lazily
            method.Flags = (MethodFlags)meth.Flags;
            method.ImplFlags = (MethodImplFlags)meth.ImplFlags;
            method.Name = name;
            if (declaringType != null)
            {
                if (declaringType.IsGeneric)
                {
                    if (declaringType.Template != null)
                        currentTypeParameters = declaringType.ConsolidatedTemplateArguments;
                    else
                        currentTypeParameters = declaringType.ConsolidatedTemplateParameters;
                }

                if (module.ProjectTypesContainedInModule && (declaringType.Flags & TypeFlags.IsForeign) != 0)
                {
                    if (method.IsStatic) method.Flags &= ~MethodFlags.NewSlot;
                    if (declaringType is DelegateNode)
                    {
                        method.Flags &= ~MethodFlags.MethodAccessMask;
                        method.Flags |= MethodFlags.Public;
                    }
                    else
                    {
                        method.ImplFlags |= MethodImplFlags.Runtime | MethodImplFlags.InternalCall;
                    }
                }
            }

            tables.GetSignatureLength(meth.Signature);
            var sigReader = tables.GetNewCursor();
            method.CallingConvention = (CallingConventionFlags)sigReader.ReadByte();
            if (method.IsGeneric = (method.CallingConvention & CallingConventionFlags.Generic) != 0)
            {
                var numTemplateParameters = sigReader.ReadCompressedInt();
                currentMethodTypeParameters = new TypeNodeList(numTemplateParameters);
                currentMethodTypeParameters =
                    method.TemplateParameters = GetTypeParametersFor((index << 1) | 1, method);
                GetTypeParameterConstraints((index << 1) | 1, method.TemplateParameters);
            }

            var numParams = sigReader.ReadCompressedInt();
            method.ReturnType = ParseTypeSignature(sigReader);
#if false
      if (declaringType != null && declaringType.IsValueType)
        method.ThisParameter = new This(declaringType.GetReferenceType());
      else
        method.ThisParameter = new This(declaringType);
#endif // materialized on demand
            var paramList = method.Parameters = new ParameterList(numParams);
            if (numParams > 0)
            {
                var offset = method.IsStatic ? 0 : 1;
                for (var i = 0; i < numParams; i++)
                {
                    var param = new Parameter();
                    param.ParameterListIndex = i;
                    param.ArgumentListIndex = i + offset;
                    param.Type = ParseTypeSignature(sigReader);
                    param.DeclaringMethod = method;
                    paramList.Add(param);
                }

                var end = tables.ParamTable.Length + 1;
                if (index < methodDefs.Length) end = methodDefs[index].ParamList;
                AddMoreStuffToParameters(method, paramList, meth.ParamList, end);
                for (var i = 0; i < numParams; i++)
                {
                    var param = paramList[i];
                    if (param.Name == null)
                    {
                        param.Name = Identifier.For("param" + i);
                        param.Flags |= ParameterFlags.ParameterNameMissing;
                    }
                }
            }
            else if (method.ReturnType != CoreSystemTypes.Void)
            {
                //check for custom attributes and marshalling information on return value
                var i = meth.ParamList;
                var parPtrs =
                    tables.ParamPtrTable; //TODO: why use ParamPtrTable in the branch and not the one above? Factor this out.
                var pars = tables.ParamTable;
                var n = methodDefs.Length;
                var m = pars.Length;
                if (index < n) m = methodDefs[index].ParamList - 1;
                if (parPtrs.Length > 0)
                {
                    if (pars != null && 0 < i && i <= m)
                    {
                        var j = parPtrs[i - 1].Param;
                        var pr = pars[j - 1];
                        if (pr.Sequence == 0)
                            AddMoreStuffToParameters(method, null, j, j + 1);
                    }
                }
                else
                {
                    if (pars != null && 0 < i && i <= m)
                    {
                        var pr = pars[i - 1];
                        if (pr.Sequence == 0)
                            AddMoreStuffToParameters(method, null, i, i + 1);
                    }
                }
            }
#if ExtendedRuntime
      for (int k = 0, al = method.ReturnAttributes == null ? 0 : method.ReturnAttributes.Count; k < al; k++) {
        if (method.ReturnAttributes[k].Type == ExtendedRuntimeTypes.NotNullAttribute) {
          method.ReturnType = OptionalModifier.For(ExtendedRuntimeTypes.NonNullType, method.ReturnType);
          // Someone putting an attribute directly on the "real" method is still a
          // kind of out-of-band contract.
          // This marking is the way to signal that any override or implementing method being compiled
          // should not have its non-null annotations persisted as optional modifiers.
          method.HasOutOfBandContract = true;
        } else  if (method.ReturnAttributes[k].Type == ExtendedRuntimeTypes.NotNullArrayElementsAttribute) {
          Stack s = new Stack();
          TypeNode t = method.ReturnType;
          while (t is OptionalModifier) {
            OptionalModifier om = t as OptionalModifier;
            s.Push(om.Modifier);
            t = om.ModifiedType;
          }
          ArrayType at = t as ArrayType;
          if (at != null) { // just silently ignore if attribute is on a non-array type?
            OptionalModifier om = OptionalModifier.For(ExtendedRuntimeTypes.NonNullType, at.ElementType);
            while (0 < s.Count) {
              om = OptionalModifier.For((TypeNode)s.Pop(), om);
            }
            // also *must* make it a new array type, can't set the ElementType.
            method.ReturnType = om.GetArrayType(1);
            // Someone putting an attribute directly on the "real" method is still a
            // kind of out-of-band contract.
            // This marking is the way to signal that any override or implementing method being compiled
            // should not have its non-null annotations persisted as optional modifiers.
            method.HasOutOfBandContract = true;
          }
        }
      }
#endif
            //if ((method.Flags & MethodFlags.HasSecurity) != 0)
            //  method.SecurityAttributes = this.GetSecurityAttributesFor((index << 2)|1);
            if ((method.Flags & MethodFlags.PInvokeImpl) != 0)
            {
                var implMaps = tables.ImplMapTable;
                int n = implMaps.Length, i = 0, j = n - 1;
                var sorted = (sortedTablesMask >> (int)TableIndices.ImplMap) % 2 == 1;
                if (sorted)
                {
                    while (i < j)
                    {
                        var k = (i + j) / 2;
                        if (implMaps[k].MemberForwarded >> 1 < index)
                            i = k + 1;
                        else
                            j = k;
                    }

                    while (i > 0 && implMaps[i - 1].MemberForwarded >> 1 == index) i--;
                }

                for (; i < n; i++)
                {
                    var imr = implMaps[i];
                    if (imr.MemberForwarded >> 1 == index)
                    {
                        method.PInvokeFlags = (PInvokeFlags)imr.MappingFlags;
                        method.PInvokeImportName = tables.GetString(imr.ImportName);
                        method.PInvokeModule = module.ModuleReferences[imr.ImportScope - 1].Module;
                        break;
                    }
                }
            }

            method.DeclaringType = declaringType;
            currentMethodTypeParameters = savedCurrentMethodTypeParameters;
            currentTypeParameters = savedCurrentTypeParameters;
            return method;
        }

        private void GetMethodAttributes(Method /*!*/ method, object /*!*/ handle)
        {
            var savedCurrentTypeParameters = currentTypeParameters;
            var savedCurrentMethodTypeParameters = currentMethodTypeParameters;
            try
            {
                var tables = this.tables;
                var index = (int)handle;
                var methodDefs = tables.MethodTable;
                var n = methodDefs.Length;
                if (index < 1 || index > n)
                    throw new ArgumentOutOfRangeException("handle", ExceptionStrings.InvalidTypeTableIndex);
                var md = methodDefs[index - 1];
                if (method != md.Method)
                    throw new ArgumentOutOfRangeException("handle", ExceptionStrings.InvalidTypeTableIndex);
                //Get custom attributes   
                method.Attributes = GetCustomAttributesNonNullFor((index << 5) | 0);
                currentTypeParameters = savedCurrentTypeParameters;
                currentMethodTypeParameters = savedCurrentMethodTypeParameters;
                //Get security attributes
                if ((method.Flags & MethodFlags.HasSecurity) != 0)
                    method.SecurityAttributes = GetSecurityAttributesFor((index << 2) | 1);
#if !FxCop
            }
            catch (Exception e)
            {
                if (module != null)
                {
                    if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                    module.MetadataImportErrors.Add(e);
                }

                method.Attributes = new AttributeList(0);
                currentTypeParameters = savedCurrentTypeParameters;
                currentMethodTypeParameters = savedCurrentMethodTypeParameters;
            }
#else
      }finally{}
#endif
        }

        private Method /*!*/ GetMethodFromSpec(int i)
        {
            var methodSpecs = tables.MethodSpecTable;
            var msr = methodSpecs[i - 1];
            if (msr.InstantiatedMethod != null) return msr.InstantiatedMethod;
            var sigReader = tables.GetBlobCursor(msr.Instantiation);
            var header = sigReader.ReadByte(); //skip over redundant header byte
            Debug.Assert(header == 0x0a);
            var templateArguments = ParseTypeList(sigReader);
            var template = GetMethodDefOrRef(msr.Method, templateArguments.Count);
            if (template == null) return new Method();
            if (template.TemplateParameters == null) return template; //Likely a dummy method
            return template.GetTemplateInstance(currentType, templateArguments);
        }

        internal Member /*!*/ GetMemberFromToken(int tok, object memberInfo = null)
        {
            TypeNodeList varArgTypes;
            return GetMemberFromToken(tok, out varArgTypes, memberInfo);
        }

        internal Member /*!*/ GetMemberFromToken(int tok, out TypeNodeList varArgTypes, object memberInfo = null)
        {
            varArgTypes = null;
            Member member = null;
            switch ((TableIndices)(tok >> 24))
            {
                case TableIndices.Field:
                    member = GetFieldFromDef(tok & 0xFFFFFF);
                    break;
                case TableIndices.Method:
                    member = GetMethodFromDef(tok & 0xFFFFFF);
                    break;
                case TableIndices.MemberRef:
                    member = GetMemberFromRef(tok & 0xFFFFFF, out varArgTypes, memberInfo);
                    break;
                case TableIndices.TypeDef:
                    member = GetTypeFromDef(tok & 0xFFFFFF);
                    break;
                case TableIndices.TypeRef:
                    member = GetTypeFromRef(tok & 0xFFFFFF);
                    break;
                case TableIndices.TypeSpec:
                    member = GetTypeFromSpec(tok & 0xFFFFFF);
                    break;
                case TableIndices.MethodSpec:
                    member = GetMethodFromSpec(tok & 0xFFFFFF);
                    break;
                default: throw new InvalidMetadataException(ExceptionStrings.BadMemberToken);
            }

            if (member == null) throw new InvalidMetadataException(ExceptionStrings.BadMemberToken);
            return member;
        }

        internal Member GetMemberFromRef(int i, out TypeNodeList varArgTypes, object memberInfo = null)
        {
            return GetMemberFromRef(i, out varArgTypes, 0, memberInfo);
        }

        internal Member GetMemberFromRef(int i, out TypeNodeList varArgTypes, int numGenericArgs,
            object memberInfo = null)
        {
            var mref = tables.MemberRefTable[i - 1];
            if (mref.Member != null)
            {
                varArgTypes = mref.VarargTypes;
                return mref.Member;
            }

            varArgTypes = null;
            Member result = null;
            var codedIndex = mref.Class;
            if (codedIndex == 0) return null;
            TypeNode parent = null;
            var savedCurrentTypeParameters = currentTypeParameters;
            switch (codedIndex & 0x7)
            {
                case 0x00:
                    parent = GetTypeFromDef(codedIndex >> 3);
                    break;
                case 0x01:
                    parent = GetTypeFromRef(codedIndex >> 3);
                    break;
                case 0x02:
                    parent = GetTypeGlobalMemberContainerTypeFromModule(codedIndex >> 3);
                    break;
                case 0x03:
                    result = GetMethodFromDef(codedIndex >> 3);
                    if ((((Method)result).CallingConvention & CallingConventionFlags.VarArg) != 0)
                    {
                        var sRdr = tables.GetBlobCursor(mref.Signature);
                        sRdr.ReadByte(); //hdr
                        var pCount = sRdr.ReadCompressedInt();
                        ParseTypeSignature(sRdr); //rType
                        var genParameterEncountered = false;
                        ParseParameterTypes(out varArgTypes, sRdr, pCount, ref genParameterEncountered);
                    }

                    goto done;
                case 0x04:
                    parent = GetTypeFromSpec(codedIndex >> 3);
                    break;
                default: throw new InvalidMetadataException("");
            }

            if (parent != null && parent.IsGeneric)
            {
                if (parent.Template != null)
                    currentTypeParameters = parent.ConsolidatedTemplateArguments;
                else
                    currentTypeParameters = parent.ConsolidatedTemplateParameters;
            }

            var memberName = tables.GetIdentifier(mref.Name);
            Method methodToSkipPastWhenLookingForMethodParameters = null;
            tryAgain:
            var sigReader = tables.GetBlobCursor(mref.Signature);
            var header = sigReader.ReadByte();
            if (header == 0x6)
            {
                var fieldType = ParseTypeSignature(sigReader);
                var fType = TypeNode.StripModifiers(fieldType);
                var parnt = parent;
                while (parnt != null)
                {
                    var members = parnt.Members;
                    for (int j = 0, n = members.Count; j < n; j++)
                    {
                        var f = members[j] as Field;
                        if (f == null) continue;
                        if (f.Name.UniqueIdKey != memberName.UniqueIdKey) continue;
                        if (TypeNode.StripModifiers(f.Type) == fType)
                        {
                            result = f;
                            goto done;
                        }
                    }

                    var c = parnt as Class;
                    if (c != null) parnt = c.BaseClass;
                    else break;
                }

                if (result == null)
                {
                    result = new Field(memberName);
                    result.DeclaringType = parent;
                    ((Field)result).Type = fieldType;
                    var fieldInfo = memberInfo as FieldInfo;
                    if (fieldInfo != null && fieldInfo.IsStatic) ((Field)result).Flags |= FieldFlags.Static;
                    goto error;
                }

                goto done;
            }

            var typeParamCount = int.MinValue;
            var callingConvention = CallingConventionFlags.Default;
            if ((header & 0x20) != 0) callingConvention |= CallingConventionFlags.HasThis;
            if ((header & 0x40) != 0) callingConvention |= CallingConventionFlags.ExplicitThis;
            switch (header & 7)
            {
                case 1:
                    callingConvention |= CallingConventionFlags.C;
                    break;
                case 2:
                    callingConvention |= CallingConventionFlags.StandardCall;
                    break;
                case 3:
                    callingConvention |= CallingConventionFlags.ThisCall;
                    break;
                case 4:
                    callingConvention |= CallingConventionFlags.FastCall;
                    break;
                case 5:
                    callingConvention |= CallingConventionFlags.VarArg;
                    break;
            }

            if ((header & 0x10) != 0)
            {
                typeParamCount = sigReader.ReadCompressedInt();
                callingConvention |= CallingConventionFlags.Generic;
            }

            var paramCount = sigReader.ReadCompressedInt();
            var savedMethodTypeParameters = currentMethodTypeParameters;
            var pnt = parent;
            if (numGenericArgs > 0)
            {
                var skip = methodToSkipPastWhenLookingForMethodParameters != null;
                while (pnt != null)
                {
                    var members = pnt.GetMembersNamed(memberName);
                    for (int k = 0, n = members.Count; k < n; k++)
                    {
                        var m = members[k] as Method;
                        if (m == null) continue;
                        if (skip)
                        {
                            if (m == methodToSkipPastWhenLookingForMethodParameters) skip = false;
                            continue;
                        }

                        if (m.TemplateParameters == null || m.TemplateParameters.Count != numGenericArgs) continue;
                        if (m.Parameters == null || m.Parameters.Count != paramCount) continue;
                        currentMethodTypeParameters = m.TemplateParameters;
                        currentTypeParameters = pnt.ConsolidatedTemplateArguments;
                        methodToSkipPastWhenLookingForMethodParameters = m;
                        goto parseSignature;
                    }

                    var c = pnt as Class;
                    if (c != null) pnt = c.BaseClass;
                    else break;
                }

                methodToSkipPastWhenLookingForMethodParameters = null;
            }

            parseSignature:
            var returnType = ParseTypeSignature(sigReader);
            if (returnType == null) returnType = CoreSystemTypes.Object;
            var genericParameterEncountered = returnType.IsGeneric;
            var paramTypes =
                ParseParameterTypes(out varArgTypes, sigReader, paramCount, ref genericParameterEncountered);
            currentMethodTypeParameters = savedMethodTypeParameters;
            currentTypeParameters = savedCurrentTypeParameters;
            pnt = parent;
            while (pnt != null)
            {
                var members = pnt.GetMembersNamed(memberName);
                for (int k = 0, n = members.Count; k < n; k++)
                {
                    var m = members[k] as Method;
                    if (m == null) continue;
                    if (m.ReturnType == null) continue;
                    var mrtype = TypeNode.StripModifiers(m.ReturnType);
                    //^ assert mrtype != null;
                    if (!mrtype.IsStructurallyEquivalentTo(TypeNode.StripModifiers(returnType))) continue;
                    if (!m.ParameterTypesMatchStructurally(paramTypes)) continue;
                    if (m.CallingConvention != callingConvention) continue;
                    if (typeParamCount != int.MinValue && (!m.IsGeneric || m.TemplateParameters == null ||
                                                           m.TemplateParameters.Count != typeParamCount))
                        continue;
                    result = m;
                    goto done;
                }

                if (memberName.UniqueIdKey == StandardIds.Ctor.UniqueIdKey)
                {
                    //Can't run up the base class chain for constructors.
                    members = pnt.GetConstructors();
                    if (members != null && members.Count == 1 && paramCount == 0)
                    {
                        //Only one constructor. The CLR metadata API's seem to think that this should match the empty signature
                        result = members[0];
                        goto done;
                    }

                    break;
                }

                var c = pnt as Class;
                if (c != null)
                {
                    pnt = c.BaseClass;
                }
                else
                {
                    var iface = pnt as Interface;
                    if (iface != null)
                        for (int k = 0, n = iface == null ? 0 : iface.Interfaces.Count; k < n; k++)
                        {
                            result = SearchBaseInterface(iface.Interfaces[k], memberName, returnType, paramTypes,
                                typeParamCount, callingConvention);
                            if (result != null) goto done;
                        }

                    break;
                }
            }

            if (result == null)
            {
                if (methodToSkipPastWhenLookingForMethodParameters != null) goto tryAgain;
                var parameters = new ParameterList(paramCount);
                for (var j = 0; j < paramCount; j++)
                {
                    var p = new Parameter(Identifier.Empty, paramTypes[j]);
                    parameters.Add(p);
                }

                //TODO: let the caller indicate if it expects a constructor
                var meth = new Method(parent, null, memberName, parameters, returnType, null);
                meth.CallingConvention = callingConvention;
                if ((callingConvention & CallingConventionFlags.HasThis) == 0) meth.Flags |= MethodFlags.Static;
                result = meth;
            }

            error:
            if (module != null)
            {
                HandleError(module, string.Format(CultureInfo.CurrentCulture,
                    ExceptionStrings.CouldNotResolveMemberReference, parent.FullName + "::" + memberName));
                if (parent != null) parent.Members.Add(result);
            }

            done:
            if (CanCacheMember(result))
            {
                tables.MemberRefTable[i - 1].Member = result;
                tables.MemberRefTable[i - 1].VarargTypes = varArgTypes;
            }

            currentTypeParameters = savedCurrentTypeParameters;
            return result;
        }

        private Method SearchBaseInterface(Interface iface, Identifier memberName, TypeNode returnType,
            TypeNodeList paramTypes, int typeParamCount, CallingConventionFlags callingConvention)
        {
            var members = iface.GetMembersNamed(memberName);
            for (int k = 0, n = members.Count; k < n; k++)
            {
                var m = members[k] as Method;
                if (m == null) continue;
                if (m.ReturnType == null) continue;
                var mrtype = TypeNode.StripModifiers(m.ReturnType);
                //^ assert mrtype != null;
                if (!mrtype.IsStructurallyEquivalentTo(TypeNode.StripModifiers(returnType))) continue;
                if (!m.ParameterTypesMatchStructurally(paramTypes)) continue;
                if (m.CallingConvention != callingConvention) continue;
                if (typeParamCount != int.MinValue && (!m.IsGeneric || m.TemplateParameters == null ||
                                                       m.TemplateParameters.Count != typeParamCount))
                    continue;
                return m;
            }

            for (int k = 0, n = iface == null ? 0 : iface.Interfaces.Count; k < n; k++)
            {
                var m = SearchBaseInterface(iface.Interfaces[k], memberName, returnType, paramTypes, typeParamCount,
                    callingConvention);
                if (m != null) return m;
            }

            return null;
        }

        private static bool CanCacheMethodHelper(Method /*!*/ method)
        {
            if (method.IsGeneric)
            {
                if (method.TemplateArguments == null)
                    return false;
                for (var i = 0; i < method.TemplateArguments.Count; i++)
                    if (!CanCacheTypeNode(method.TemplateArguments[i]))
                        return false;
            }

            return true;
        }

        private static bool CanCacheMember(Member /*!*/ member)
        {
            return (member.DeclaringType == null || CanCacheTypeNode(member.DeclaringType)) &&
                   (member.NodeType != NodeType.Method || CanCacheMethodHelper((Method)member));
        }

        private TypeNodeList /*!*/ ParseParameterTypes(out TypeNodeList varArgTypes, MemoryCursor /*!*/ sigReader,
            int paramCount, ref bool genericParameterEncountered)
        {
            varArgTypes = null;
            var paramTypes = new TypeNodeList(paramCount);
            for (var j = 0; j < paramCount; j++)
            {
                var paramType = ParseTypeSignature(sigReader);
                if (paramType == null)
                {
                    //got a sentinel
                    varArgTypes = new TypeNodeList(paramCount - j);
                    j--;
                    continue;
                }

                if (varArgTypes != null)
                {
                    varArgTypes.Add(paramType);
                    continue;
                }

                if (paramType.IsGeneric) genericParameterEncountered = true;
                paramTypes.Add(paramType);
            }

            return paramTypes;
        }

        private bool TypeDefIsClass(int i)
        {
            if (i == 0) return false;
            var typeDef = tables.TypeDefTable[i - 1];
            if (typeDef.Type != null) return typeDef.Type is Class;
            if ((typeDef.Flags & (int)TypeFlags.Interface) != 0) return false;
            return TypeDefOrRefOrSpecIsClassButNotValueTypeBaseClass(typeDef.Extends);
        }

        private bool TypeDefIsClassButNotValueTypeBaseClass(int i)
        {
            if (i == 0) return false;
            var typeDef = tables.TypeDefTable[i - 1];
            if (typeDef.Type != null)
                return typeDef.Type != CoreSystemTypes.ValueType && typeDef.Type != CoreSystemTypes.Enum &&
                       typeDef.Type is Class;
            if ((typeDef.Flags & (int)TypeFlags.Interface) != 0) return false;
            return TypeDefOrRefOrSpecIsClassButNotValueTypeBaseClass(typeDef.Extends);
        }

        internal TypeNodeList GetInstantiatedTypes()
        {
            TypeNodeList result = null;
            var typeDefs = tables.TypeDefTable;
            for (int i = 0, n = typeDefs.Length; i < n; i++)
            {
                var t = typeDefs[i].Type;
                if (t == null) continue;
                if (result == null) result = new TypeNodeList();
                result.Add(t);
            }

            return result;
        }

        internal TypeNode /*!*/ GetTypeFromDef(int i)
        {
            var typeDef = tables.TypeDefTable[i - 1];
            if (typeDef.Type != null) return typeDef.Type;
            // Save current state because the helper might change it but this method must not.
            var savedCurrentTypeParameters = currentTypeParameters;
            var savedCurrentType = currentType;
            try
            {
                return GetTypeFromDefHelper(i);
#if !FxCop
            }
            catch (Exception e)
            {
                if (module == null) return new Class();
                if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                module.MetadataImportErrors.Add(e);
                return new Class();
#endif
            }
            finally
            {
                currentTypeParameters = savedCurrentTypeParameters;
                currentType = savedCurrentType;
            }
        }

        internal TypeNode /*!*/ GetTypeFromDefHelper(int i)
        {
            // This is added to prevent loops. 
            //  Check the code in GetTypeFromDef which checks != null before callig this function
            tables.TypeDefTable[i - 1].Type = Class.Dummy;
            var typeDef = tables.TypeDefTable[i - 1];
            var name = tables.GetIdentifier(typeDef.Name);
            var namesp = tables.GetIdentifier(typeDef.Namespace);
            if (namesp.Name.Length > 0 &&
                ((TypeFlags)typeDef.Flags & TypeFlags.VisibilityMask) >= TypeFlags.NestedPublic)
            {
                name = Identifier.For(namesp.Name + "." + name.Name);
                namesp = Identifier.Empty;
            }

            var result = GetReplacedTypeFromName(namesp, name);
            if (result != null)
            {
                tables.TypeDefTable[i - 1].Type = result;
                currentType = result;
                return result;
            }

            int firstInterfaceIndex;
            int lastInterfaceIndex;
            GetInterfaceIndices(i, out firstInterfaceIndex, out lastInterfaceIndex);
            var interfaces = new InterfaceList();
            result = ConstructCorrectTypeNodeSubclass(i, namesp, firstInterfaceIndex, lastInterfaceIndex,
                (TypeFlags)typeDef.Flags, interfaces, typeDef.Extends,
                name.UniqueIdKey == StandardIds.Enum.UniqueIdKey &&
                namesp.UniqueIdKey == StandardIds.System.UniqueIdKey);
            result.DeclaringModule = module;
            result.Name = name;
            result.Namespace = namesp;
            var typeParameters = currentTypeParameters = GetTypeParametersFor((i << 1) | 0, result);
            result.TemplateParameters = typeParameters;
            result.IsGeneric = typeParameters != null;
            tables.TypeDefTable[i - 1].Type = result;
            currentType = result;
            RemoveTypeParametersBelongingToDeclaringType(i, ref typeParameters, result);
            //Now that the type instance has been allocated, it is safe to get hold of things that could refer to this type.
            if (result is Class && result.BaseType == null)
            {
                var baseType = DecodeAndGetTypeDefOrRefOrSpec(typeDef.Extends);
                ((Class)result).BaseClass = baseType as Class;
                if (baseType != null && !(baseType is Class) && module != null)
                    HandleError(module, ExceptionStrings.InvalidBaseClass);
            }

            if (result.IsGeneric)
            {
                GetTypeParameterConstraints((i << 1) | 0, typeParameters);
                if (result.templateParameters != null)
                    for (int j = 0,
                         offset = typeParameters.Count - result.templateParameters.Count,
                         n = result.templateParameters.Count;
                         j < n;
                         j++)
                        result.templateParameters[j] = typeParameters[j + offset];
            }

            if (firstInterfaceIndex >= 0)
                GetInterfaces(i, firstInterfaceIndex, interfaces);
            if ((result.Flags & (TypeFlags.ExplicitLayout | TypeFlags.SequentialLayout)) != 0)
                GetClassSizeAndPackingSize(i, result);
            return result;
        }

        private void GetInterfaceIndices(int i, out int firstInterfaceIndex, out int lastInterfaceIndex)
        {
            firstInterfaceIndex = -1;
            lastInterfaceIndex = -1;
            var intfaces = tables.InterfaceImplTable;
            //TODO: binary search
            for (int j = 0, n = intfaces.Length; j < n; j++)
            {
                if (intfaces[j].Class != i) continue;
                if (firstInterfaceIndex == -1)
                    firstInterfaceIndex = j;
                lastInterfaceIndex = j;
            }
        }

        private void GetClassSizeAndPackingSize(int i, TypeNode /*!*/ result)
        {
            var classLayouts = tables.ClassLayoutTable;
            for (int j = 0, n = classLayouts.Length; j < n; j++)
            {
                //TODO: binary search
                var clr = classLayouts[j];
                if (clr.Parent == i)
                {
                    result.ClassSize = clr.ClassSize;
                    result.PackingSize = clr.PackingSize;
                    break;
                }
            }
        }

        private void GetInterfaces(int i, int firstInterfaceIndex, InterfaceList /*!*/ interfaces)
        {
            var intfaces = tables.InterfaceImplTable;
            for (int j = firstInterfaceIndex, n = intfaces.Length; j < n; j++)
            {
                if (intfaces[j].Class != i) continue; //TODO: break if sorted
                var ifaceT = DecodeAndGetTypeDefOrRefOrSpec(intfaces[j].Interface);
                var iface = ifaceT as Interface;
                if (iface == null)
                {
                    iface = new Interface();
                    if (ifaceT != null)
                    {
                        iface.DeclaringModule = ifaceT.DeclaringModule;
                        iface.Namespace = ifaceT.Namespace;
                        iface.Name = ifaceT.Name;
                    }
                }

                interfaces.Add(iface);
                var implAttrs = GetCustomAttributesFor((j << 5) | 5);
                if (implAttrs != null)
                    interfaces.AddAttributes(interfaces.Count - 1, implAttrs);
            }
        }

        private void RemoveTypeParametersBelongingToDeclaringType(int i, ref TypeNodeList typeParameters,
            TypeNode /*!*/ type)
        {
            var nestedClasses = tables.NestedClassTable;
            for (int j = 0, n = nestedClasses.Length; j < n; j++)
            {
                //TODO: binary search
                var ncr = nestedClasses[j];
                if (ncr.NestedClass == i)
                {
                    type.DeclaringType = GetTypeFromDef(ncr.EnclosingClass);
                    if (type.DeclaringType != null &&
                        type.DeclaringType
                            .IsGeneric) //remove type parameters that belong to declaring type from nested type's list
                        if (type.templateParameters != null)
                        {
                            var icount = GetInheritedTypeParameterCount(type);
                            var rcount = type.templateParameters.Count;
                            if (icount >= rcount)
                            {
                                type.templateParameters = null;
                            }
                            else
                            {
                                var tpars = new TypeNodeList(rcount - icount);
                                for (var k = icount; k < rcount; k++)
                                    tpars.Add(type.templateParameters[k]);
                                type.templateParameters = tpars;
                            }

                            currentTypeParameters = typeParameters = type.ConsolidatedTemplateParameters;
                        }

                    break;
                }
            }
        }

        private TypeNode /*!*/ ConstructCorrectTypeNodeSubclass(int i, Identifier /*!*/ namesp, int firstInterfaceIndex,
            int lastInterfaceIndex,
            TypeFlags flags, InterfaceList interfaces, int baseTypeCodedIndex, bool isSystemEnum)
        {
            TypeNode result;
            TypeNode.TypeAttributeProvider attributeProvider = GetTypeAttributes;
            TypeNode.NestedTypeProvider nestedTypeProvider = GetNestedTypes;
            TypeNode.TypeMemberProvider memberProvider = GetTypeMembers;
            var isTemplateParameter = false;
#if ExtendedRuntime
      InterfaceImplRow[] intfaces = this.tables.InterfaceImplTable;
      Interface firstInterface = null;
      Interface lastInterface = null;
      if (firstInterfaceIndex >= 0){
        firstInterface = this.GetInterfaceIfNotGenericInstance(intfaces[firstInterfaceIndex].Interface);
        if (firstInterface != null){
          lastInterface = this.GetInterfaceIfNotGenericInstance(intfaces[lastInterfaceIndex].Interface);
          isTemplateParameter =
 CoreSystemTypes.IsInitialized && lastInterface != null && lastInterface == ExtendedRuntimeTypes.ITemplateParameter;
        }
      }
#endif
            if ((flags & TypeFlags.Interface) != 0)
            {
                if (isTemplateParameter)
                    result = new TypeParameter(interfaces, nestedTypeProvider, attributeProvider, memberProvider, i);
                else
                    result = new Interface(interfaces, nestedTypeProvider, attributeProvider, memberProvider, i);
            }
            else if (isTemplateParameter)
            {
                result = new ClassParameter(nestedTypeProvider, attributeProvider, memberProvider, i);
            }
            else
            {
                result = null;
                var baseClass = GetTypeIfNotGenericInstance(baseTypeCodedIndex);
                if (baseClass != null)
                {
                    if (baseClass == CoreSystemTypes.MulticastDelegate) //TODO: handle single cast delegates
                    {
                        result = new DelegateNode(nestedTypeProvider, attributeProvider, memberProvider, i);
                    }
                    else if (baseClass == CoreSystemTypes.Enum ||
                             (baseClass.Namespace.UniqueIdKey == StandardIds.System.UniqueIdKey &&
                              baseClass.Name.UniqueIdKey == StandardIds.Enum.UniqueIdKey))
                    {
                        result = new EnumNode(nestedTypeProvider, attributeProvider, memberProvider, i);
                    }
                    else if (baseClass == CoreSystemTypes.ValueType &&
                             !(isSystemEnum && (flags & TypeFlags.Sealed) == 0))
                    {
#if ExtendedRuntime
            Struct st = null;
            if (firstInterface != null){
              if (namesp.UniqueIdKey == StandardIds.StructuralTypes.UniqueIdKey){
                if (CoreSystemTypes.IsInitialized && firstInterface == ExtendedRuntimeTypes.TupleType)
                  st = new TupleType(nestedTypeProvider, attributeProvider, memberProvider, i);
                else if (CoreSystemTypes.IsInitialized && firstInterface == ExtendedRuntimeTypes.TypeIntersection)
                  st = new TypeIntersection(nestedTypeProvider, attributeProvider, memberProvider, i);
                else if (CoreSystemTypes.IsInitialized && firstInterface == ExtendedRuntimeTypes.TypeUnion)
                  st = new TypeUnion(nestedTypeProvider, attributeProvider, memberProvider, i);
                else if (CoreSystemTypes.IsInitialized && firstInterface == ExtendedRuntimeTypes.ConstrainedType)
                  st = new ConstrainedType(nestedTypeProvider, attributeProvider, memberProvider, i);
                else
                  st = new Struct(nestedTypeProvider, attributeProvider, memberProvider, i);
              }
              else if (CoreSystemTypes.IsInitialized && firstInterface == ExtendedRuntimeTypes.TypeAlias)
                st = new TypeAlias(nestedTypeProvider, attributeProvider, memberProvider, i, false);
              else if (CoreSystemTypes.IsInitialized && firstInterface == ExtendedRuntimeTypes.TypeDefinition)
                st = new TypeAlias(nestedTypeProvider, attributeProvider, memberProvider, i, true);
            }
            if (st == null && lastInterface != null) {
              result =
 this.GetTypeExtensionFromDef(nestedTypeProvider, attributeProvider, memberProvider, i, baseClass, lastInterface);
            }
            else {
              result = st;
            }
            if (result == null)
#endif
                        result = new Struct(nestedTypeProvider, attributeProvider, memberProvider, i);
                    }
                }

                if (result == null)
                {
#if ExtendedRuntime
          if (lastInterface != null)
            result =
 this.GetTypeExtensionFromDef(nestedTypeProvider, attributeProvider, memberProvider, i, baseClass, lastInterface);
          if (result == null)
#endif
                    result = new Class(nestedTypeProvider, attributeProvider, memberProvider, i);
                }
            }

            result.Flags = flags;
            result.Interfaces = interfaces;
            return result;
        }
#if !MinimalReader
        private readonly TrivialHashtable /*<Ident,TypeExtensionProvider>*/ /*!*/
            TypeExtensionTable = new TrivialHashtable();

        private delegate TypeNode TypeExtensionProvider(TypeNode.NestedTypeProvider nprovider,
            TypeNode.TypeAttributeProvider aprovider, TypeNode.TypeMemberProvider mprovider, TypeNode baseType,
            object handle);

        private static TypeNode DummyTypeExtensionProvider(TypeNode.NestedTypeProvider nprovider,
            TypeNode.TypeAttributeProvider aprovider, TypeNode.TypeMemberProvider mprovider, TypeNode baseType,
            object handle)
        {
            return null;
        }

        private readonly TypeExtensionProvider /*!*/
            dummyTEProvider = DummyTypeExtensionProvider;

        private TypeNode GetTypeExtensionFromDef(TypeNode.NestedTypeProvider nestedTypeProvider,
            TypeNode.TypeAttributeProvider attributeProvider, TypeNode.TypeMemberProvider memberProvider, object handle,
            TypeNode baseType, Interface /*!*/ lastInterface)
        {
            if (lastInterface.Namespace.UniqueIdKey == StandardIds.CciTypeExtensions.UniqueIdKey)
            {
                var teprovider = (TypeExtensionProvider)TypeExtensionTable[lastInterface.Name.UniqueIdKey];
                if (teprovider == null)
                {
                    var loc = lastInterface.DeclaringModule.Location.ToLower(CultureInfo.InvariantCulture);
                    if (loc.EndsWith(".runtime.dll"))
                    {
                        loc = Path.GetFileName(loc);
                        var compilerDllName = loc.Replace(".runtime.dll", "");
                        Assembly rassem;
                        try
                        {
                            rassem = Assembly.Load(compilerDllName);
                        }
                        catch
                        {
                            HandleError(module,
                                string.Format(CultureInfo.CurrentCulture, ExceptionStrings.CannotLoadTypeExtension,
                                    lastInterface.FullName, compilerDllName));
                            goto ExtensionNotFound;
                        }

                        if (rassem == null) goto ExtensionNotFound;
                        var tprov = rassem.GetType(StandardIds.CciTypeExtensions.Name + "." + lastInterface.Name.Name +
                                                   "Provider");
                        if (tprov == null) goto ExtensionNotFound;
                        var providerMethod = tprov.GetMethod("For");
                        if (providerMethod == null) goto ExtensionNotFound;
                        teprovider =
                            (TypeExtensionProvider)Delegate.CreateDelegate(typeof(TypeExtensionProvider),
                                providerMethod);
                        ExtensionNotFound: ;
                        if (teprovider == null) // install a not-found dummy provider
                            teprovider = dummyTEProvider;
                        TypeExtensionTable[lastInterface.Name.UniqueIdKey] = teprovider;
                    }
                }

                if (teprovider == null) return null;
                return teprovider(nestedTypeProvider, attributeProvider, memberProvider, baseType, handle);
            }

            return null;
        }
#endif
        private static int GetInheritedTypeParameterCount(TypeNode type)
        {
            if (type == null) return 0;
            var n = 0;
            type = type.DeclaringType;
            while (type != null)
            {
                n += type.templateParameters == null ? 0 : type.templateParameters.Count;
                type = type.DeclaringType;
            }

            return n;
        }

        private TypeNode /*!*/ GetTypeGlobalMemberContainerTypeFromModule(int i)
        {
            var mr = tables.ModuleRefTable[i - 1];
            var mod = mr.Module;
            TypeNode result = null;
            if (mod != null && mod.Types != null && mod.Types.Count > 0)
                result = mod.Types[0];
            if (result != null) return result;
            result = GetDummyTypeNode(Identifier.Empty, Identifier.For("<Module>"), mod, null, false);
            if (mod != null) mod.Types = new TypeNodeList(result);
            return result;
        }

        internal void GetNamespaces()
            //^ ensures this.namespaceTable != null;
        {
            var typeDefs = tables.TypeDefTable;
            var n = typeDefs.Length;
            var nsT = namespaceTable = new TrivialHashtable(n);
            var nsFor = new TrivialHashtable(128);
            var nsL = namespaceList = new NamespaceList(n);
            for (var i = 0; i < n; i++)
            {
                var typeDef = typeDefs[i];
                var ns = (TrivialHashtable)nsT[typeDef.NamespaceKey];
                var nSpace = (Namespace)nsFor[typeDef.NamespaceKey];
                if (ns == null)
                {
                    nsT[typeDef.NamespaceKey] = ns = new TrivialHashtable();
                    nsFor[typeDef.NamespaceKey] = nSpace = new Namespace(typeDef.NamespaceId);
                    nsL.Add(nSpace);
                }

                Debug.Assert(nSpace != null);
                if ((typeDef.Flags & (int)TypeFlags.VisibilityMask) == 0)
                {
                    ns[typeDef.NameKey] = i + 1;
                }
                else if ((typeDef.Flags & (int)TypeFlags.VisibilityMask) == 1)
                {
                    nSpace.isPublic = true;
                    ns[typeDef.NameKey] = i + 1;
                }
            }
        }

        private TypeNode GetTypeFromName(Identifier /*!*/ Namespace, Identifier /*!*/ name)
        {
            try
            {
                if (namespaceTable == null) GetNamespaces();
                //^ assert this.namespaceTable != null;
                var nsTable = (TrivialHashtable)namespaceTable[Namespace.UniqueIdKey];
                if (nsTable == null) return GetForwardedTypeFromName(Namespace, name);
                var ti = nsTable[name.UniqueIdKey];
                if (ti == null) return GetForwardedTypeFromName(Namespace, name);
                var t = GetTypeFromDef((int)ti);
                return t;
#if !FxCop
            }
            catch (Exception e)
            {
                if (module == null) return null;
                if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                module.MetadataImportErrors.Add(e);
                return null;
            }
#else
      }finally{}
#endif
        }

        private TypeNode GetForwardedTypeFromName(Identifier /*!*/ Namespace, Identifier /*!*/ name)
        {
            var exportedTypes = tables.ExportedTypeTable;
            for (int i = 0, n = exportedTypes == null ? 0 : exportedTypes.Length; i < n; i++)
            {
                var etr = exportedTypes[i];
                if ((etr.Flags & (int)TypeFlags.Forwarder) == 0) continue;
                if (tables.GetString(etr.TypeNamespace) != Namespace.Name ||
                    tables.GetString(etr.TypeName) != name.Name) continue;
                var index = etr.Implementation >> 2;
                var arr = tables.AssemblyRefTable[index - 1];
                return arr.AssemblyReference.Assembly.GetType(Namespace, name);
            }

            return GetReplacedTypeFromName(Namespace, name);
        }

        private TypeNode GetReplacedTypeFromName(Identifier /*!*/ Namespace, Identifier /*!*/ name)
        {
#if !MinimalReader
            if (module.ContainingAssembly == null ||
                (module.ContainingAssembly.Flags & AssemblyFlags.ContainsForeignTypes) == 0) return null;
            if (!SystemTypes.Initialized || module.ContainingAssembly == null) return null;
            var nsKey = Namespace.UniqueIdKey;
            var nKey = name.UniqueIdKey;
            if (nsKey == StandardIds.WindowsFoundationMetadata.UniqueIdKey)
            {
                if (nKey == SystemTypes.AttributeUsageAttribute.Name.UniqueIdKey)
                    return SystemTypes.AttributeUsageAttribute;
                if (nKey == SystemTypes.AttributeTargets.Name.UniqueIdKey) return SystemTypes.AttributeTargets;
            }
            else if (nsKey == StandardIds.WindowsUI.UniqueIdKey)
            {
                if (nKey == SystemTypes.Color.Name.UniqueIdKey &&
                    SystemTypes.Color.DeclaringModule.Location != "unknown:location") return SystemTypes.Color;
            }
            else if (nsKey == StandardIds.WindowsFoundation.UniqueIdKey)
            {
                if (nKey == SystemTypes.DateTime.Name.UniqueIdKey) return SystemTypes.DateTimeOffset;
                if (SystemTypes.EventHandler1 != null && nKey == SystemTypes.EventHandler1.Name.UniqueIdKey)
                    return SystemTypes.EventHandler1;
                if (nKey == SystemTypes.EventRegistrationToken.Name.UniqueIdKey)
                    return SystemTypes.EventRegistrationToken;
                if (nKey == StandardIds.HResult.UniqueIdKey) return SystemTypes.Exception;
                if (nKey == StandardIds.IReference1.UniqueIdKey) return SystemTypes.GenericNullable;
                if (nKey == SystemTypes.Point.Name.UniqueIdKey) return SystemTypes.Point;
                if (nKey == SystemTypes.Rect.Name.UniqueIdKey) return SystemTypes.Rect;
                if (nKey == SystemTypes.Size.Name.UniqueIdKey) return SystemTypes.Size;
                if (nKey == SystemTypes.TimeSpan.Name.UniqueIdKey) return SystemTypes.TimeSpan;
                if (nKey == SystemTypes.Uri.Name.UniqueIdKey) return SystemTypes.Uri;
                if (nKey == StandardIds.IClosable.UniqueIdKey) return SystemTypes.IDisposable;
            }
            else if (nsKey == StandardIds.WindowsFoundationCollections.UniqueIdKey)
            {
                if (nKey == StandardIds.IIterable1.UniqueIdKey) return SystemTypes.GenericIEnumerable;
                if (nKey == StandardIds.IVector1.UniqueIdKey) return SystemTypes.GenericIList;
                if (nKey == StandardIds.IVectorView1.UniqueIdKey) return SystemTypes.GenericIReadOnlyList;
                if (nKey == StandardIds.IMap2.UniqueIdKey) return SystemTypes.GenericIDictionary;
                if (nKey == StandardIds.IMapView2.UniqueIdKey) return SystemTypes.GenericIReadOnlyDictionary;
                if (nKey == StandardIds.IKeyValuePair2.UniqueIdKey) return SystemTypes.GenericKeyValuePair;
            }
            else if (nsKey == StandardIds.WindowsUIXaml.UniqueIdKey)
            {
                if (nKey == SystemTypes.CornerRadius.Name.UniqueIdKey &&
                    SystemTypes.CornerRadius.DeclaringModule.Location != "unknown:location")
                    return SystemTypes.CornerRadius;
                if (nKey == SystemTypes.Duration.Name.UniqueIdKey &&
                    SystemTypes.Duration.DeclaringModule.Location != "unknown:location") return SystemTypes.Duration;
                if (SystemTypes.DurationType != null && nKey == SystemTypes.DurationType.Name.UniqueIdKey)
                    return SystemTypes.DurationType;
                if (nKey == SystemTypes.GridLength.Name.UniqueIdKey &&
                    SystemTypes.GridLength.DeclaringModule.Location != "unknown:location")
                    return SystemTypes.GridLength;
                if (SystemTypes.GridUnitType != null && nKey == SystemTypes.GridUnitType.Name.UniqueIdKey)
                    return SystemTypes.GridUnitType;
                if (nKey == SystemTypes.Thickness.Name.UniqueIdKey &&
                    SystemTypes.Thickness.DeclaringModule.Location != "unknown:location") return SystemTypes.Thickness;
            }
            else if (nsKey == StandardIds.WindowsUIXamlData.UniqueIdKey)
            {
                if (nKey == SystemTypes.INotifyPropertyChanged.Name.UniqueIdKey &&
                    SystemTypes.INotifyPropertyChanged.DeclaringModule.Location != "unknown:location")
                    return SystemTypes.INotifyPropertyChanged;
                if (nKey == SystemTypes.PropertyChangedEventHandler.Name.UniqueIdKey &&
                    SystemTypes.PropertyChangedEventHandler.DeclaringModule.Location != "unknown:location")
                    return SystemTypes.PropertyChangedEventHandler;
                if (nKey == SystemTypes.PropertyChangedEventArgs.Name.UniqueIdKey &&
                    SystemTypes.PropertyChangedEventArgs.DeclaringModule.Location != "unknown:location")
                    return SystemTypes.PropertyChangedEventArgs;
            }
            else if (nsKey == StandardIds.WindowsUIXamlInput.UniqueIdKey)
            {
                if (nKey == StandardIds.ICommand.UniqueIdKey) return SystemTypes.ICommand;
            }
            else if (nsKey == StandardIds.WindowsUIXamlInterop.UniqueIdKey)
            {
                if (nKey == StandardIds.IBindableIterable.UniqueIdKey) return SystemTypes.IBindableIterable;
                if (nKey == StandardIds.IBindableVector.UniqueIdKey) return SystemTypes.IBindableVector;
                if (nKey == StandardIds.INotifyCollectionChanged.UniqueIdKey)
                    return SystemTypes.INotifyCollectionChanged;
                if (nKey == StandardIds.NotifyCollectionChangedAction.UniqueIdKey)
                    return SystemTypes.NotifyCollectionChangedAction;
                if (nKey == StandardIds.NotifyCollectionChangedEventArgs.UniqueIdKey)
                    return SystemTypes.NotifyCollectionChangedEventArgs;
                if (nKey == StandardIds.NotifyCollectionChangedEventHandler.UniqueIdKey)
                    return SystemTypes.NotifyCollectionChangedEventHandler;
                if (nKey == StandardIds.TypeName.UniqueIdKey) return SystemTypes.Type;
            }
            else if (nsKey == StandardIds.WindowsUIXamlControlsPrimitives.UniqueIdKey)
            {
                if (nKey == SystemTypes.GeneratorPosition.Name.UniqueIdKey &&
                    SystemTypes.GeneratorPosition.DeclaringModule.Location != "unknown:location")
                    return SystemTypes.GeneratorPosition;
            }
            else if (nsKey == StandardIds.WindowsUIXamlMedia.UniqueIdKey)
            {
                if (nKey == SystemTypes.Matrix.Name.UniqueIdKey &&
                    SystemTypes.Matrix.DeclaringModule.Location != "unknown:location") return SystemTypes.Matrix;
            }
            else if (nsKey == StandardIds.WindowsUIXamlMediaAnimation.UniqueIdKey)
            {
                if (nKey == SystemTypes.KeyTime.Name.UniqueIdKey &&
                    SystemTypes.KeyTime.DeclaringModule.Location != "unknown:location") return SystemTypes.KeyTime;
                if (nKey == SystemTypes.RepeatBehavior.Name.UniqueIdKey &&
                    SystemTypes.KeyTime.DeclaringModule.Location != "unknown:location")
                    return SystemTypes.RepeatBehavior;
                if (SystemTypes.RepeatBehaviorType != null && nKey == SystemTypes.RepeatBehaviorType.Name.UniqueIdKey)
                    return SystemTypes.RepeatBehaviorType;
            }
            else if (nsKey == StandardIds.WindowsUIXamlMediaMedia3D.UniqueIdKey)
            {
                if (nKey == SystemTypes.Matrix3D.Name.UniqueIdKey &&
                    SystemTypes.Matrix3D.DeclaringModule.Location != "unknown:location") return SystemTypes.Matrix3D;
            }
#endif
            return null;
        }

        internal bool IsValidTypeName(Identifier /*!*/ Namespace, Identifier /*!*/ name)
        {
            try
            {
                if (namespaceTable == null) GetNamespaces();
                //^ assert this.namespaceTable != null;
                var nsTable = (TrivialHashtable)namespaceTable[Namespace.UniqueIdKey];
                if (nsTable == null) return false;
                return nsTable[name.UniqueIdKey] != null;
#if !FxCop
            }
            catch (Exception e)
            {
                if (module == null) return false;
                if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                module.MetadataImportErrors.Add(e);
                return false;
            }
#else
      }finally{}
#endif
        }

        internal TypeNode /*!*/ GetTypeFromRef(int i)
        {
            return GetTypeFromRef(i, false);
        }

        internal TypeNode /*!*/ GetTypeFromRef(int i, bool expectStruct)
        {
            var trtable = tables.TypeRefTable;
            var trr = trtable[i - 1];
            var result = trr.Type;
            if (result != null) return result;
            var name = tables.GetIdentifier(trr.Name);
            var namesp = tables.GetIdentifier(trr.Namespace);
            var resolutionScope = trr.ResolutionScope;
            Module declaringModule = null;
            TypeNode declaringType = null;
            var index = resolutionScope >> 2;
            switch (resolutionScope & 0x3)
            {
                case 0:
                    declaringModule = module;
                    //^ assume declaringModule != null;
                    result = declaringModule.GetType(namesp, name);
                    //REVIEW: deal with case where ref is in same (multi-module) assembly, but not the current module? index == 0
                    break;
                case 1:
                    declaringModule = tables.ModuleRefTable[index - 1].Module;
                    if (declaringModule != null)
                        result = declaringModule.GetType(namesp, name);
                    break;
                case 2:
                    if (index > 0)
                        declaringModule = tables.AssemblyRefTable[index - 1].AssemblyReference.Assembly;
                    if (declaringModule != null)
                        result = declaringModule.GetType(namesp, name);
                    break;
                case 3:
                    declaringType = GetTypeFromRef(index);
                    declaringModule = declaringType.DeclaringModule;
                    if (namesp == null || namesp.length == 0)
                        result = (TypeNode)declaringType.GetMembersNamed(name)[0];
                    else
                        result =
                            (TypeNode)declaringType.GetMembersNamed(Identifier.For(namesp.Name + "." + name.Name))[0];
                    break;
                default:
                    declaringModule = module;
                    break;
            }

            if (result == null)
                result = GetDummyTypeNode(namesp, name, declaringModule, declaringType, expectStruct);
            trtable[i - 1].Type = result;
            if (!CanCacheTypeNode(result))
                trtable[i - 1].Type = null;
            return result;
        }

        private TypeNode /*!*/ GetDummyTypeNode(Identifier namesp, Identifier name, Module declaringModule,
            TypeNode declaringType, bool expectStruct)
        {
            TypeNode result = null;
            if (module != null)
            {
                var modName = declaringModule == null ? "" : declaringModule.Name == null ? "" : declaringModule.Name;
                var context = declaringType != null ? declaringType.FullName : namesp != null ? namesp.Name : null;
                HandleError(module, string.Format(CultureInfo.CurrentCulture,
                    ExceptionStrings.CouldNotResolveTypeReference,
                    "[" + modName + "]" + context + "." + name));
            }

            result = expectStruct ? new Struct() : (TypeNode)new Class();
            if (name != null && name.ToString().StartsWith("I") && name.ToString().Length > 1 &&
                char.IsUpper(name.ToString()[1]))
                result = new Interface();
            result.Flags |= TypeFlags.Public;
            result.Name = name;
            result.Namespace = namesp;
            if (declaringType != null)
            {
                result.DeclaringType = declaringType;
                result.DeclaringType.DeclaringModule = declaringType.DeclaringModule;
                declaringType.Members.Add(result);
            }
            else
            {
                if (declaringModule == null) declaringModule = module;
                //^ assume declaringModule != null;
                result.DeclaringModule = declaringModule;
                if (declaringModule.types != null)
                    declaringModule.types.Add(result);
            }

            return result;
        }

        private bool TypeSpecIsClass(int i)
        {
            var tsr = tables.TypeSpecTable[i - 1];
            if (tsr.Type != null) return tsr.Type is Class;
            tables.GetSignatureLength(tsr.Signature);
            return TypeSignatureIsClass(tables.GetNewCursor());
        }

        internal TypeNode /*!*/ GetTypeFromSpec(int i)
        {
            var tsr = tables.TypeSpecTable[i - 1];
            if (tsr.Type != null) return tsr.Type;
            tables.GetSignatureLength(tsr.Signature);
            var pinned = false;
            var isTypeArgument = false;
            var result = ParseTypeSignature(tables.GetNewCursor(), ref pinned, ref isTypeArgument);
            if (result == null) result = new Class();
            //Get custom attributes
            var attributes = GetCustomAttributesFor((i << 5) | 13);
            if (attributes != null && attributes.Count > 0)
            {
                //Append attributes "inherited" from template to metadata attributes
                var templAttributes = result.Attributes;
                for (int j = 0, n = templAttributes == null ? 0 : templAttributes.Count; j < n; j++)
                {
                    var attr = result.Attributes[j];
                    if (attr == null) continue;
                    attributes.Add(attr);
                }

                result.Attributes = attributes;
            }
#if ExtendedRuntime
      for (int j = 0, n = attributes.Count; j < n; j++) {
        if (attributes[j].Type == SystemTypes.NotNullGenericArgumentsAttribute) {
          Literal l = (Literal)attributes[j].Expressions[0];
          string s = (string)l.Value;
          TypeNodeList ts = new TypeNodeList(s.Length);
          for (int k = 0, m = s.Length; k < m; k++) {
            if (s[k] == '!')
              ts.Add(OptionalModifier.For(ExtendedRuntimeTypes.NonNullType, result.ConsolidatedTemplateArguments[k]));
            else
              ts.Add(result.ConsolidatedTemplateArguments[k]);
          }
          result = result.Template.GetGenericTemplateInstance(this.module, ts);
          //^ assume result != null;
        }
      }
#endif
            if (!isTypeArgument && CanCacheTypeNode(result))
                tables.TypeSpecTable[i - 1].Type = result;
            return result;
        }

        private static bool CanCacheTypeNode(TypeNode /*!*/ type)
        {
#if WHIDBEY
            if (!type.IsGeneric && (type.Template == null || !type.IsNotFullySpecialized) &&
                type.NodeType != NodeType.TypeParameter && type.NodeType != NodeType.ClassParameter &&
                type.NodeType != NodeType.InterfaceExpression)
            {
                var elementTypes = type.StructuralElementTypes;
                for (int i = 0, n = elementTypes == null ? 0 : elementTypes.Count; i < n; i++)
                    if (!CanCacheTypeNode(type.StructuralElementTypes[i]))
                        return false;
                return true;
            }

            return false;
#else
      return true;
#endif
        }

        private static Module GetNestedModule(Module module, string modName, ref string modLocation)
        {
            if (module == null || modName == null)
            {
                Debug.Assert(false);
                return null;
            }

            var mod = module.GetNestedModule(modName);
            if (mod == null)
            {
                if (module.Location != null)
                    modLocation = Path.Combine(Path.GetDirectoryName(module.Location), modName);
                if (modLocation != null && File.Exists(modLocation))
                {
                    mod = Module.GetModule(modLocation);
                    if (mod != null)
                    {
                        mod.ContainingAssembly = module.ContainingAssembly;
                        module.ModuleReferences.Add(new ModuleReference(modName, mod));
                    }
                }
            }

            if (mod == null)
            {
                HandleError(module, string.Format(CultureInfo.CurrentCulture,
                    ExceptionStrings.CouldNotFindReferencedModule, modLocation));
                mod = new Module();
                mod.Name = modName;
                mod.ContainingAssembly = module.ContainingAssembly;
                mod.Kind = ModuleKindFlags.DynamicallyLinkedLibrary;
            }

            return mod;
        }

        private void GetTypeList(Module /*!*/ module)
        {
            var types = new TypeNodeList();
            var typeDefs = tables.TypeDefTable;
            for (int i = 0, n = typeDefs.Length; i < n; i++)
            {
                var t = GetTypeFromDef(i + 1);
                if (t != null && t.DeclaringType == null) types.Add(t);
            }

            module.Types = types;
            var assem = module as AssemblyNode;
            if (assem == null) return;
            types = new TypeNodeList();
            var exportedTypes = tables.ExportedTypeTable;
            for (int i = 0, n = exportedTypes.Length; i < n; i++)
            {
                var etr = exportedTypes[i];
                var nameSpace = Identifier.For(tables.GetString(etr.TypeNamespace));
                var typeName = Identifier.For(tables.GetString(etr.TypeName));
                TypeNode exportedType = null;
                switch (etr.Implementation & 0x3)
                {
                    case 0:
                        var modName = tables.GetString(tables.FileTable[(etr.Implementation >> 2) - 1].Name);
                        var modLocation = modName;
                        var mod = GetNestedModule(assem, modName, ref modLocation);
                        if (mod == null)
                        {
                            Debug.Assert(false);
                            break;
                        }

                        exportedType = mod.GetType(nameSpace, typeName);
                        if (exportedType == null)
                        {
                            HandleError(assem, string.Format(CultureInfo.CurrentCulture,
                                ExceptionStrings.CouldNotFindExportedTypeInModule, nameSpace + "." + typeName,
                                modLocation));
                            exportedType = new Class();
                            exportedType.Name = typeName;
                            exportedType.Namespace = nameSpace;
                            exportedType.Flags = TypeFlags.Class | TypeFlags.Public;
                            exportedType.DeclaringModule = mod;
                        }

                        break;
                    case 1:
                        var aref = tables.AssemblyRefTable[(etr.Implementation >> 2) - 1].AssemblyReference;
                        if (aref == null)
                        {
                            HandleError(assem, ExceptionStrings.BadMetadataInExportTypeTableNoSuchAssemblyReference);
                            aref = new AssemblyReference("dummy assembly for bad reference");
                        }

                        var a = aref.Assembly;
                        if (a == null)
                        {
                            Debug.Assert(false);
                            continue;
                        }

                        exportedType = a.GetType(nameSpace, typeName);
                        if (exportedType == null)
                        {
                            HandleError(assem, string.Format(CultureInfo.CurrentCulture,
                                ExceptionStrings.CouldNotFindExportedTypeInAssembly, nameSpace + "." + typeName,
                                a.StrongName));
                            exportedType = new Class();
                            exportedType.Name = typeName;
                            exportedType.Namespace = nameSpace;
                            exportedType.Flags = TypeFlags.Class | TypeFlags.Public;
                            exportedType.DeclaringModule = a;
                        }

                        break;
                    case 2:
                        var parentType = types[(etr.Implementation >> 2) - 1];
                        if (parentType == null)
                        {
                            HandleError(assem, ExceptionStrings.BadMetadataInExportTypeTableNoSuchParentType);
                            parentType = new Class();
                            parentType.DeclaringModule = this.module;
                            parentType.Name = Identifier.For("Missing parent type");
                        }

                        exportedType = parentType.GetNestedType(typeName);
                        if (exportedType == null)
                        {
                            HandleError(assem, string.Format(CultureInfo.CurrentCulture,
                                ExceptionStrings.CouldNotFindExportedNestedTypeInType, typeName, parentType.FullName));
                            exportedType = new Class();
                            exportedType.Name = typeName;
                            exportedType.Flags = TypeFlags.Class | TypeFlags.NestedPublic;
                            exportedType.DeclaringType = parentType;
                            exportedType.DeclaringModule = parentType.DeclaringModule;
                        }

                        break;
                }

                types.Add(exportedType);
            }

            assem.ExportedTypes = types;
        }

        private void GetNestedTypes(TypeNode /*!*/ type, object /*!*/ handle)
        {
            type.nestedTypes = null;
            var result = new TypeNodeList();
#if !FxCop
            var savedCurrentTypeParameters = currentTypeParameters;
            var savedCurrentType = currentType;
#endif
            try
            {
                if (type.IsGeneric)
                {
                    if (type.templateParameters == null) type.templateParameters = new TypeNodeList(0);
                    currentTypeParameters = type.ConsolidatedTemplateParameters;
                }

                currentType = type;
                var declaringType = type.DeclaringType;
                while (currentTypeParameters == null && declaringType != null)
                {
                    if (declaringType.IsGeneric)
                    {
                        if (declaringType.templateParameters == null)
                            declaringType.templateParameters = new TypeNodeList(0);
                        currentTypeParameters = declaringType.ConsolidatedTemplateParameters;
                    }

                    declaringType = declaringType.DeclaringType;
                }

                var tables = this.tables;
                var typeTableIndex = (int)handle;
                var typeDefs = tables.TypeDefTable;
                var n = typeDefs.Length;
                if (typeTableIndex < 1 || typeTableIndex > n)
                    throw new ArgumentOutOfRangeException("handle", ExceptionStrings.InvalidTypeTableIndex);
                var nestedClasses = tables.NestedClassTable;
                n = nestedClasses.Length;
                for (var i = 0; i < n; i++)
                {
                    //TODO: binary lookup
                    var ncr = nestedClasses[i];
                    if (ncr.EnclosingClass != typeTableIndex) continue;
                    var t = GetTypeFromDef(ncr.NestedClass);
                    if (t != null)
                    {
                        if (type.nestedTypes != null)
                            return; //A recursive call to GetNestedTypes has already done the deed
                        t.DeclaringType = type;
                        if ((t.Flags & TypeFlags.RTSpecialName) == 0 ||
                            t.Name.UniqueIdKey != StandardIds._Deleted.UniqueIdKey)
                            result.Add(t);
                    }
                    else
                    {
                        throw new InvalidMetadataException("Invalid nested class row");
                    }
                }

                type.nestedTypes = result;
#if !FxCop
            }
            catch (Exception e)
            {
                if (module != null)
                {
                    if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                    module.MetadataImportErrors.Add(e);
                }
            }
            finally
            {
                currentTypeParameters = savedCurrentTypeParameters;
                currentType = savedCurrentType;
            }
#else
      }finally{}
#endif
        }

        private void GetTypeMembers(TypeNode /*!*/ type, object /*!*/ handle)
        {
            var savedCurrentTypeParameters = currentTypeParameters;
            var savedCurrentType = currentType;
            try
            {
                var tables = this.tables;
                var typeTableIndex = (int)handle;
                var typeDefs = tables.TypeDefTable;
                var fieldDefs = tables.FieldTable;
                var fieldPtrs = tables.FieldPtrTable;
                var methodDefs = tables.MethodTable;
                var methodPtrs = tables.MethodPtrTable;
                var eventMaps = tables.EventMapTable;
                var eventDefs = tables.EventTable;
                var eventPtrs = tables.EventPtrTable;
                var methodImpls = tables.MethodImplTable;
                var propertyMaps = tables.PropertyMapTable;
                var propertyPtrs = tables.PropertyPtrTable;
                var propertyDefs = this.tables.PropertyTable;
                var nestedClasses = tables.NestedClassTable;
                var n = typeDefs.Length;
                if (typeTableIndex < 1 || typeTableIndex > n)
                    throw new ArgumentOutOfRangeException("handle", ExceptionStrings.InvalidTypeTableIndex);
                var td = typeDefs[typeTableIndex - 1];
                if (type != td.Type)
                    throw new ArgumentOutOfRangeException("handle", ExceptionStrings.InvalidTypeTableIndex);
                //Get type members
                if (type.IsGeneric)
                {
                    if (type.templateParameters == null) type.templateParameters = new TypeNodeList(0);
                    currentTypeParameters = type.ConsolidatedTemplateParameters;
                }

                currentType = type;
                var declaringType = type.DeclaringType;
                while (currentTypeParameters == null && declaringType != null)
                {
                    if (declaringType.IsGeneric)
                    {
                        if (declaringType.templateParameters == null)
                            declaringType.templateParameters = new TypeNodeList(0);
                        currentTypeParameters = declaringType.ConsolidatedTemplateParameters;
                    }

                    declaringType = declaringType.DeclaringType;
                }

                type.members = new MemberList();
                n = nestedClasses.Length;
                for (var i = 0; i < n; i++)
                {
                    var ncr = nestedClasses[i];
                    if (ncr.EnclosingClass != typeTableIndex) continue;
                    var t = GetTypeFromDef(ncr.NestedClass);
                    if (t != null)
                    {
                        t.DeclaringType = type;
                        if ((t.Flags & TypeFlags.RTSpecialName) == 0 ||
                            t.Name.UniqueIdKey != StandardIds._Deleted.UniqueIdKey)
                            type.Members.Add(t);
                    }
                }

                n = typeDefs.Length;
                var m = fieldDefs.Length;
                var start = td.FieldList;
                var end = m + 1;
                if (typeTableIndex < n) end = typeDefs[typeTableIndex].FieldList;
                if (type is EnumNode) GetUnderlyingTypeOfEnumNode((EnumNode)type, fieldDefs, fieldPtrs, start, end);
                AddFieldsToType(type, fieldDefs, fieldPtrs, start, end);
                m = methodDefs.Length;
                start = td.MethodList;
                end = m + 1;
                if (typeTableIndex < n) end = typeDefs[typeTableIndex].MethodList;
                AddMethodsToType(type, methodPtrs, start, end);
                n = propertyMaps.Length;
                m = propertyDefs.Length;
                for (var i = 0; i < n; i++)
                {
                    //TODO: binary search
                    var pm = propertyMaps[i];
                    if (pm.Parent != typeTableIndex) continue;
                    start = pm.PropertyList;
                    end = m + 1;
                    if (i < n - 1) end = propertyMaps[i + 1].PropertyList;
                    AddPropertiesToType(type, propertyDefs, propertyPtrs, start, end);
                }

                n = eventMaps.Length;
                m = eventDefs.Length;
                for (var i = 0; i < n; i++)
                {
                    //TODO: binary search
                    var em = eventMaps[i];
                    if (em.Parent != typeTableIndex) continue;
                    start = em.EventList;
                    end = m + 1;
                    if (i < n - 1) end = eventMaps[i + 1].EventList;
                    AddEventsToType(type, eventDefs, eventPtrs, start, end);
                }

                n = methodImpls.Length;
                for (var i = 0; i < n; i++)
                {
                    //TODO: binary search
                    var mir = methodImpls[i];
                    if (mir.Class != typeTableIndex) continue;
                    var implementer = GetMethodDefOrRef(mir.MethodBody);
                    if (implementer == null) continue;
                    var implementedInterfaceMethods = implementer.ImplementedInterfaceMethods;
                    if (implementedInterfaceMethods == null)
                        implementedInterfaceMethods = implementer.ImplementedInterfaceMethods = new MethodList();
                    var savedMethodTypeParameters = currentMethodTypeParameters;
                    currentMethodTypeParameters = implementer.TemplateParameters;
                    implementedInterfaceMethods.Add(GetMethodDefOrRef(mir.MethodDeclaration));
                    currentMethodTypeParameters = savedMethodTypeParameters;
                }

                currentTypeParameters = savedCurrentTypeParameters;
#if !FxCop
            }
            catch (Exception e)
            {
                if (module != null)
                {
                    if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                    module.MetadataImportErrors.Add(e);
                }

                type.Members = new MemberList(0);
            }
            finally
            {
                currentTypeParameters = savedCurrentTypeParameters;
                currentType = savedCurrentType;
            }
#else
      }finally{}
#endif
        }

        private void GetTypeAttributes(TypeNode /*!*/ type, object /*!*/ handle)
        {
            Debug.Assert(TypeNode.IsCompleteTemplate(type));
            var savedCurrentTypeParameters = currentTypeParameters;
            try
            {
                var tables = this.tables;
                var typeTableIndex = (int)handle;
                var typeDefs = tables.TypeDefTable;
                var n = typeDefs.Length;
                if (typeTableIndex < 1 || typeTableIndex > n)
                    throw new ArgumentOutOfRangeException("handle", ExceptionStrings.InvalidTypeTableIndex);
                var td = typeDefs[typeTableIndex - 1];
                if (type != td.Type)
                    throw new ArgumentOutOfRangeException("handle", ExceptionStrings.InvalidTypeTableIndex);
                //Get custom attributes   
                type.Attributes = GetCustomAttributesNonNullFor((typeTableIndex << 5) | 3);
                currentTypeParameters = savedCurrentTypeParameters;
                //Get security attributes
                if ((type.Flags & TypeFlags.HasSecurity) != 0)
                    type.SecurityAttributes = GetSecurityAttributesFor((typeTableIndex << 2) | 0);
#if !FxCop
            }
            catch (Exception e)
            {
                if (module != null)
                {
                    if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                    module.MetadataImportErrors.Add(e);
                }

                type.Attributes = new AttributeList(0);
                currentTypeParameters = savedCurrentTypeParameters;
            }
#else
      }finally{}
#endif
        }

        private void GetTypeParameterAttributes(TypeNode /*!*/ type, object /*!*/ handle)
        {
            var savedCurrentTypeParameters = currentTypeParameters;
            try
            {
                var tables = this.tables;
                var genericParamIndex = (int)handle;
                var genParDefs = tables.GenericParamTable;
                var n = genParDefs.Length;
                if (genericParamIndex < 1 || genericParamIndex > n)
                    throw new ArgumentOutOfRangeException("handle", ExceptionStrings.InvalidTypeTableIndex);
                var td = genParDefs[genericParamIndex - 1];
                //Get custom attributes   
                type.Attributes = GetCustomAttributesNonNullFor((genericParamIndex << 5) | 19);
                currentTypeParameters = savedCurrentTypeParameters;
#if !FxCop
            }
            catch (Exception e)
            {
                if (module != null)
                {
                    if (module.MetadataImportErrors == null) module.MetadataImportErrors = new ArrayList();
                    module.MetadataImportErrors.Add(e);
                }

                type.Attributes = new AttributeList(0);
                currentTypeParameters = savedCurrentTypeParameters;
            }
#else
      }finally{}
#endif
        }

        private TypeNodeList /*!*/ ParseTypeList(MemoryCursor /*!*/ sigReader)
        {
            var n = sigReader.ReadCompressedInt();
            var result = new TypeNodeList(n);
            for (var i = 0; i < n; i++)
            {
                var t = ParseTypeSignature(sigReader);
                if (t == null || t == Struct.Dummy)
                {
                    //Can happen when dealing with a primitive type that implements an interface that references the primitive type.
                    //For example, System.String implements IComparable<System.String>.
                    if (currentType != null && !CoreSystemTypes.Initialized)
                    {
                        t = currentType;
                    }
                    else
                    {
                        Debug.Assert(false);
                        t = new TypeParameter();
                        t.Name = Identifier.For("Bad type parameter in position " + i);
                        t.DeclaringModule = module;
                    }
                }

                result.Add(t);
            }

            return result;
        }

        private bool TypeSignatureIsClass(MemoryCursor /*!*/ sigReader)
        {
            var tok = (ElementType)sigReader.ReadCompressedInt();
            switch (tok)
            {
                case ElementType.Pinned:
                case ElementType.Pointer:
                case ElementType.Reference:
                    return TypeSignatureIsClass(sigReader);
                case ElementType.OptionalModifier:
                case ElementType.RequiredModifier:
                    sigReader.ReadCompressedInt();
                    return TypeSignatureIsClass(sigReader);
                case ElementType.Class:
                    return
                        true; // [maf] counter intuitive, but this is used to determine if a type parameter is constrained to be a class. If the type parameter derives from an interface type, then it is not constrained, but the interface type has that Elementtype Class
                case ElementType.GenericTypeInstance:
                    return TypeSignatureIsClass(sigReader);
                case ElementType.TypeParameter:
                {
                    var pnum = sigReader.ReadCompressedInt();
                    if (currentTypeParameters != null && currentTypeParameters.Count > pnum)
                    {
                        var tPar = currentTypeParameters[pnum];
                        return tPar != null && tPar is Class;
                    }

                    return false;
                }
                case ElementType.MethodParameter:
                {
                    var pnum = sigReader.ReadCompressedInt();
                    if (currentMethodTypeParameters != null && currentMethodTypeParameters.Count > pnum)
                    {
                        var tPar = currentMethodTypeParameters[pnum];
                        return tPar != null && tPar is Class;
                    }

                    return false;
                }
                default:
                    return false;
            }
        }

        private TypeNode ParseTypeSignature(MemoryCursor /*!*/ sigReader)
        {
            var junk = false;
            return ParseTypeSignature(sigReader, ref junk, ref junk);
        }

        private TypeNode ParseTypeSignature(MemoryCursor /*!*/ sigReader, ref bool pinned)
        {
            var junk = false;
            return ParseTypeSignature(sigReader, ref pinned, ref junk);
        }

        private TypeNode ParseTypeSignature(MemoryCursor /*!*/ sigReader, ref bool pinned, ref bool isTypeArgument)
        {
            TypeNode elementType;
            var tok = (ElementType)sigReader.ReadCompressedInt();
            if (tok == ElementType.Pinned)
            {
                pinned = true;
                tok = (ElementType)sigReader.ReadCompressedInt();
            }

            switch (tok)
            {
                case ElementType.Boolean: return CoreSystemTypes.Boolean;
                case ElementType.Char: return CoreSystemTypes.Char;
                case ElementType.Double: return CoreSystemTypes.Double;
                case ElementType.Int16: return CoreSystemTypes.Int16;
                case ElementType.Int32: return CoreSystemTypes.Int32;
                case ElementType.Int64: return CoreSystemTypes.Int64;
                case ElementType.Int8: return CoreSystemTypes.Int8;
                case ElementType.IntPtr: return CoreSystemTypes.IntPtr;
                case ElementType.BoxedEnum:
                case ElementType.Object: return CoreSystemTypes.Object;
                case ElementType.Single: return CoreSystemTypes.Single;
                case ElementType.String: return CoreSystemTypes.String;
                case ElementType.DynamicallyTypedReference: return CoreSystemTypes.DynamicallyTypedReference;
                case ElementType.UInt16: return CoreSystemTypes.UInt16;
                case ElementType.UInt32: return CoreSystemTypes.UInt32;
                case ElementType.UInt64: return CoreSystemTypes.UInt64;
                case ElementType.UInt8: return CoreSystemTypes.UInt8;
                case ElementType.UIntPtr: return CoreSystemTypes.UIntPtr;
                case ElementType.Void: return CoreSystemTypes.Void;
                case ElementType.Pointer:
                    elementType = ParseTypeSignature(sigReader, ref pinned);
                    if (elementType == null) elementType = CoreSystemTypes.Object;
                    if (elementType == null) return null;
                    return elementType.GetPointerType();
                case ElementType.Reference:
                    elementType = ParseTypeSignature(sigReader, ref pinned);
                    if (elementType == null) elementType = CoreSystemTypes.Object;
                    return elementType.GetReferenceType();
                case ElementType.FunctionPointer:
                    return ParseFunctionPointer(sigReader);
                case ElementType.OptionalModifier:
                case ElementType.RequiredModifier:
                    var modifier = DecodeAndGetTypeDefOrRefOrSpec(sigReader.ReadCompressedInt());
                    if (modifier == null) modifier = CoreSystemTypes.Object;
                    var modified = ParseTypeSignature(sigReader, ref pinned);
                    if (modified == null) modified = CoreSystemTypes.Object;
                    if (modified == null || modified == null) return null;
                    if (tok == ElementType.RequiredModifier)
                        return RequiredModifier.For(modifier, modified);
                    return OptionalModifier.For(modifier, modified);
                case ElementType.Class:
                    return DecodeAndGetTypeDefOrRefOrSpec(sigReader.ReadCompressedInt());
                case ElementType.ValueType:
                    return DecodeAndGetTypeDefOrRefOrSpec(sigReader.ReadCompressedInt(), true);
                case ElementType.TypeParameter:
                    TypeNode tPar = null;
                    var pnum = sigReader.ReadCompressedInt();
                    if (currentTypeParameters != null && currentTypeParameters.Count > pnum)
                        tPar = currentTypeParameters[pnum];
                    if (tPar == null)
                    {
                        HandleError(module, string.Format(CultureInfo.CurrentCulture,
                            ExceptionStrings.BadTypeParameterInPositionForType, pnum,
                            currentType == null ? "" : currentType.FullName));
                        tPar = new TypeParameter();
                        tPar.Name = Identifier.For("Bad type parameter in position " + pnum);
                        tPar.DeclaringModule = module;
                    }

                    isTypeArgument = true;
                    return tPar;
                case ElementType.MethodParameter:
                    TypeNode mTPar = null;
                    pnum = sigReader.ReadCompressedInt();
                    if (currentMethodTypeParameters != null && currentMethodTypeParameters.Count > pnum)
                        mTPar = currentMethodTypeParameters[pnum];
                    if (mTPar == null)
                    {
                        HandleError(module, string.Format(CultureInfo.CurrentCulture,
                            ExceptionStrings.BadMethodTypeParameterInPosition, pnum));
                        mTPar = new MethodTypeParameter();
                        mTPar.Name = Identifier.For("Bad method type parameter in position " + pnum);
                    }

                    isTypeArgument = true;
                    return mTPar;
                case ElementType.GenericTypeInstance:
                    var savedCurrentTypeParameters = currentTypeParameters;
                    var template = ParseTypeSignature(sigReader, ref pinned);
                    currentTypeParameters = savedCurrentTypeParameters;
                    if (template == null) return null;
                    if (template.ConsolidatedTemplateParameters == null)
                    {
                        //The template could not be resolved, so we have a dummy type that has not yet been instantiated
                        //Now that it is being instantiated we know how many template parameters it has, we can dummy up some template parameters
                        template.IsGeneric = true;
                        template.templateParameters = new TypeNodeList();
                        for (int i = 0, n = sigReader.Byte(0); i < n; i++)
                            template.templateParameters.Add(new TypeParameter());
                    }

                    if (CoreSystemTypes.Initialized)
                    {
                        if (currentTypeParameters == null || currentTypeParameters.Count == 0)
                            currentTypeParameters = template.ConsolidatedTemplateParameters;
                        var genArgs = ParseTypeList(sigReader);
                        if (module == null) return null;
                        var genInst = template.GetGenericTemplateInstance(module, genArgs);
                        currentTypeParameters = savedCurrentTypeParameters;
                        return genInst;
                    }

                    var ifaceExpr = new InterfaceExpression(null);
                    ifaceExpr.Template = template;
                    ifaceExpr.Namespace = template.Namespace;
                    ifaceExpr.Name = template.Name;
                    ifaceExpr.TemplateArguments = ParseTypeList(sigReader);
                    currentTypeParameters = savedCurrentTypeParameters;
                    return ifaceExpr;
                case ElementType.SzArray:
                    elementType = ParseTypeSignature(sigReader, ref pinned);
                    if (elementType == null) elementType = CoreSystemTypes.Object;
                    if (elementType == null) return null;
                    return elementType.GetArrayType(1);
                case ElementType.Array:
                    elementType = ParseTypeSignature(sigReader, ref pinned);
                    if (elementType == null) elementType = CoreSystemTypes.Object;
                    if (elementType == null) return null;
                    var rank = sigReader.ReadCompressedInt();
                    var numSizes = sigReader.ReadCompressedInt();
                    var sizes = new int[numSizes];
                    for (var i = 0; i < numSizes; i++) sizes[i] = sigReader.ReadCompressedInt();
                    var numLoBounds = sigReader.ReadCompressedInt();
                    var loBounds = new int[numLoBounds];
                    for (var i = 0; i < numLoBounds; i++) loBounds[i] = sigReader.ReadCompressedInt();
                    return elementType.GetArrayType(rank, numSizes, numLoBounds, sizes, loBounds);
                case ElementType.Sentinel: return null;
                case ElementType.Type: return CoreSystemTypes.Type;
                case ElementType.Enum: return GetTypeFromSerializedName(ReadSerString(sigReader));
            }

            throw new InvalidMetadataException(ExceptionStrings.MalformedSignature);
        }

        private FunctionPointer /*!*/ ParseFunctionPointer(MemoryCursor /*!*/ sigReader)
        {
            var convention = (CallingConventionFlags)sigReader.ReadByte();
            var n = sigReader.ReadCompressedInt();
            var returnType = ParseTypeSignature(sigReader);
            if (returnType == null) returnType = CoreSystemTypes.Object;
            var parameterTypes = new TypeNodeList(n);
            var m = n;
            for (var i = 0; i < n; i++)
            {
                var t = ParseTypeSignature(sigReader);
                if (t == null)
                    m = i--;
                else
                    parameterTypes.Add(t);
            }

            var fp = FunctionPointer.For(parameterTypes, returnType);
            fp.CallingConvention = convention;
            fp.VarArgStart = m;
            return fp;
        }

        private StatementList ParseMethodBody(Method /*!*/ method, int methodIndex, int RVA)
        {
            var savedCurrentTypeParameters = currentTypeParameters;
            if (method.DeclaringType.Template != null)
                currentTypeParameters = method.DeclaringType.ConsolidatedTemplateArguments;
            else
                currentTypeParameters = method.DeclaringType.ConsolidatedTemplateParameters;
            var parser = new BodyParser(this, method, methodIndex, RVA);
            var result = parser.ParseStatements();
            currentTypeParameters = savedCurrentTypeParameters;
            return result;
        }

        private InstructionList ParseMethodInstructions(Method /*!*/ method, int methodIndex, int RVA)
        {
            var savedCurrentTypeParameters = currentTypeParameters;
            if (method.DeclaringType.Template != null)
                currentTypeParameters = method.DeclaringType.ConsolidatedTemplateArguments;
            else
                currentTypeParameters = method.DeclaringType.ConsolidatedTemplateParameters;
            var parser = new InstructionParser(this, method, methodIndex, RVA);
            var result = parser.ParseInstructions();
            currentTypeParameters = savedCurrentTypeParameters;
            return result;
        }
    }

    internal struct LocalInfo
    {
        public LocalInfo(string name, uint attributes)
        {
            Name = name;
            Attributes = attributes;
        }

        public readonly string Name;
        public readonly uint Attributes;
    }

    internal abstract class ILParser
    {
        protected MemoryCursor /*!*/
            bodyReader;

        internal int counter;

        protected LocalList /*!*/
            locals = new LocalList();

        protected Method /*!*/
            method;

        protected int methodIndex;

        protected Reader /*!*/
            reader;

        protected int RVA;
        internal int size;

        internal ILParser(Reader /*!*/ reader, Method /*!*/ method, int methodIndex, int RVA)
        {
            this.reader = reader;
            bodyReader = reader.tables.GetNewCursor();
            this.method = method;
#if !FxCop
            this.method.LocalList = locals;
#else
      this.method.Locals = this.locals;
#endif
            this.methodIndex = methodIndex;
            this.RVA = RVA;
            //^ base();
        }

        protected Expression Parameters(int i)
        {
            if (method.IsStatic) return method.Parameters[i];
            if (i == 0) return method.ThisParameter;
            return method.Parameters[i - 1];
        }

        protected void ParseHeader()
        {
            var header = reader.tables.GetMethodBodyHeaderByte(RVA);
            if ((header & 0x3) == 2)
            {
                size = header >> 2;
                bodyReader = reader.tables.GetNewCursor();
                reader.tables.Skip(size);
            }
            else
            {
                method.InitLocals = (header & 0x10) != 0;
                var header2 = reader.tables.GetByte();
                var fatHeaderSize = header2 >> 4;
                if (fatHeaderSize == 2) return;
                if (fatHeaderSize != 3) throw new InvalidMetadataException(ExceptionStrings.InvalidFatMethodHeader);
                reader.tables.Skip(2); //Skip over maxstack. No need to remember it.
                size = reader.tables.GetInt32();
                var localIndex = reader.tables.GetInt32();
                bodyReader = reader.tables.GetNewCursor();
                reader.tables.Skip(size);
                reader.tables.AlignTo32BitBoundary();
                while ((header & 0x8) != 0)
                {
                    header = reader.tables.GetByte();
                    if ((header & 3) != 1) throw new InvalidMetadataException(ExceptionStrings.BadMethodHeaderSection);
                    if ((header & 0x80) != 0)
                        throw new InvalidMetadataException(ExceptionStrings.TooManyMethodHeaderSections);
                    ParseExceptionHandlerEntry((header & 0x40) == 0);
                }

                var localSourceNames = new Dictionary<int, LocalInfo>();
#if !ROTOR
                if (reader.getDebugSymbols && reader.debugReader != null)
                {
                    ISymUnmanagedMethod methodInfo = null;
                    try
                    {
                        try
                        {
                            reader.debugReader.GetMethod(0x6000000 | (uint)methodIndex, ref methodInfo);
                            if (methodInfo != null)
                            {
                                var rootScope = methodInfo.GetRootScope();
                                try
                                {
                                    reader.GetLocalSourceNames(rootScope, localSourceNames);
#if CodeContracts
                                    method.ExtraDebugInfo = ExtraPDBInfo.Parse(methodInfo.GetToken(), method,
                                        reader.debugReader, reader);
#endif
                                }
                                finally
                                {
                                    if (rootScope != null)
                                        Marshal.ReleaseComObject(rootScope);
                                }
                            }
                        }
                        catch (COMException)
                        {
                        }
                        catch (InvalidCastException)
                        {
                        }
                        catch (InvalidComObjectException)
                        {
                        }
                    }
                    finally
                    {
                        if (methodInfo != null)
                            Marshal.ReleaseComObject(methodInfo);
                    }
                }
#endif
                reader.GetLocals(localIndex, locals, localSourceNames);
            }
        }


        protected abstract void ParseExceptionHandlerEntry(bool smallSection);

        protected byte GetByte()
        {
            counter += 1;
            return bodyReader.ReadByte();
        }

        protected sbyte GetSByte()
        {
            counter += 1;
            return bodyReader.ReadSByte();
        }

        protected short GetInt16()
        {
            counter += 2;
            return bodyReader.ReadInt16();
        }

        protected int GetInt32()
        {
            counter += 4;
            return bodyReader.ReadInt32();
        }

        protected long GetInt64()
        {
            counter += 8;
            return bodyReader.ReadInt64();
        }

        protected float GetSingle()
        {
            counter += 4;
            return bodyReader.ReadSingle();
        }

        protected double GetDouble()
        {
            counter += 8;
            return bodyReader.ReadDouble();
        }

        protected Member /*!*/ GetMemberFromToken(object memberInfo = null)
        {
            return reader.GetMemberFromToken(GetInt32(), memberInfo);
        }

        protected Member /*!*/ GetMemberFromToken(out TypeNodeList varArgTypes)
        {
            return reader.GetMemberFromToken(GetInt32(), out varArgTypes);
        }

        protected string /*!*/ GetStringFromToken()
        {
            var tok = GetInt32();
            return reader.tables.GetUserString(tok & 0xFFFFFF);
        }

        protected OpCode GetOpCode()
        {
            int result = GetByte();
            if (result == (int)OpCode.Prefix1)
                result = (result << 8) | GetByte();
            return (OpCode)result;
        }
    }

    internal sealed class BodyParser : ILParser
    {
        private readonly ExpressionStack /*!*/
            operandStack = new ExpressionStack();

        private readonly TrivialHashtable /*!*/
            blockMap = new TrivialHashtable(32);

        private int alignment = -1;
        private bool isReadOnly;
        private bool isTailCall;
        private bool isVolatile;
        private TypeNode constraint;

        internal BodyParser(Reader /*!*/ reader, Method /*!*/ method, int methodIndex, int RVA)
            : base(reader, method, methodIndex, RVA)
        {
            //^ base;
        }
#if !FxCop
        protected override void ParseExceptionHandlerEntry(bool smallSection)
        {
            int dataSize = reader.tables.GetByte();
            int n = (ushort)reader.tables.GetInt16();
            if (smallSection)
                n = dataSize / 12;
            else
                n = (dataSize + (n << 8)) / 24;
            if (n < 0) n = 0;
            method.ExceptionHandlers = new ExceptionHandlerList(n);
            for (var i = 0; i < n; i++)
            {
                int flags, tryOffset, tryLength, handlerOffset, handlerLength, tokenOrOffset;
                if (smallSection)
                {
                    flags = reader.tables.GetInt16();
                    tryOffset = reader.tables.GetUInt16();
                    tryLength = reader.tables.GetByte();
                    handlerOffset = reader.tables.GetUInt16();
                    handlerLength = reader.tables.GetByte();
                }
                else
                {
                    flags = reader.tables.GetInt32();
                    tryOffset = reader.tables.GetInt32();
                    tryLength = reader.tables.GetInt32();
                    handlerOffset = reader.tables.GetInt32();
                    handlerLength = reader.tables.GetInt32();
                }

                tokenOrOffset = reader.tables.GetInt32();
                var eh = new ExceptionHandler();
                switch (flags)
                {
                    case 0x00:
                        eh.HandlerType = NodeType.Catch;
                        var pos = reader.tables.GetCurrentPosition();
                        eh.FilterType = (TypeNode)reader.GetMemberFromToken(tokenOrOffset);
                        reader.tables.SetCurrentPosition(pos);
                        break;
                    case 0x01:
                        eh.HandlerType = NodeType.Filter;
                        eh.FilterExpression = Reader.GetOrCreateBlock(blockMap, tokenOrOffset);
                        break;
                    case 0x02:
                        eh.HandlerType = NodeType.Finally;
                        break;
                    case 0x04:
                        eh.HandlerType = NodeType.FaultHandler;
                        break;
                    default: throw new InvalidMetadataException(ExceptionStrings.BadExceptionHandlerType);
                }

                eh.TryStartBlock = Reader.GetOrCreateBlock(blockMap, tryOffset);
                eh.BlockAfterTryEnd = Reader.GetOrCreateBlock(blockMap, tryOffset + tryLength);
                eh.HandlerStartBlock = Reader.GetOrCreateBlock(blockMap, handlerOffset);
                eh.BlockAfterHandlerEnd = Reader.GetOrCreateBlock(blockMap, handlerOffset + handlerLength);
                method.ExceptionHandlers.Add(eh);
            }
        }
#endif
        private AssignmentStatement /*!*/ ParseArrayElementAssignment(OpCode opCode)
        {
            var rhvalue = PopOperand();
            var indexers = new ExpressionList(1);
            indexers.Add(PopOperand());
            var array = PopOperand();
            var indexer = new Indexer(array, indexers);
            TypeNode t = CoreSystemTypes.Object;
            switch (opCode)
            {
                case OpCode.Stelem_I:
                    t = CoreSystemTypes.IntPtr;
                    break;
                case OpCode.Stelem_I1:
                    t = CoreSystemTypes.Int8;
                    break;
                case OpCode.Stelem_I2:
                    t = CoreSystemTypes.Int16;
                    break;
                case OpCode.Stelem_I4:
                    t = CoreSystemTypes.Int32;
                    break;
                case OpCode.Stelem_I8:
                    t = CoreSystemTypes.Int64;
                    break;
                case OpCode.Stelem_R4:
                    t = CoreSystemTypes.Single;
                    break;
                case OpCode.Stelem_R8:
                    t = CoreSystemTypes.Double;
                    break;
                case OpCode.Stelem:
                    t = (TypeNode)GetMemberFromToken();
                    break;
                default:
                    var arrT = array.Type as ArrayType;
                    if (arrT != null) t = arrT.ElementType;
                    break;
            }

            indexer.ElementType = indexer.Type = t;
            return new AssignmentStatement(indexer, rhvalue);
        }

        private Indexer /*!*/ ParseArrayElementLoad(OpCode opCode, TypeNode elementType)
        {
            var indexers = new ExpressionList(1);
            indexers.Add(PopOperand());
            var array = PopOperand();
            var indexer = new Indexer(array, indexers);
            var t = elementType;
            switch (opCode)
            {
                case OpCode.Ldelem_I1:
                    t = CoreSystemTypes.Int8;
                    break;
                case OpCode.Ldelem_U1:
                    t = CoreSystemTypes.UInt8;
                    break;
                case OpCode.Ldelem_I2:
                    t = CoreSystemTypes.Int16;
                    break;
                case OpCode.Ldelem_U2:
                    t = CoreSystemTypes.UInt16;
                    break;
                case OpCode.Ldelem_I4:
                    t = CoreSystemTypes.Int32;
                    break;
                case OpCode.Ldelem_U4:
                    t = CoreSystemTypes.UInt32;
                    break;
                case OpCode.Ldelem_I8:
                    t = CoreSystemTypes.Int64;
                    break;
                case OpCode.Ldelem_I:
                    t = CoreSystemTypes.IntPtr;
                    break;
                case OpCode.Ldelem_R4:
                    t = CoreSystemTypes.Single;
                    break;
                case OpCode.Ldelem_R8:
                    t = CoreSystemTypes.Double;
                    break;
                case OpCode.Ldelem:
                    t = (TypeNode)GetMemberFromToken();
                    break;
                default:
                    if (t != null) break;
                    t = CoreSystemTypes.Object;
                    var arrT = array.Type as ArrayType;
                    if (arrT != null) t = arrT.ElementType;
                    break;
            }

            indexer.ElementType = indexer.Type = t;
            return indexer;
        }

        private UnaryExpression /*!*/ ParseArrayElementLoadAddress()
        {
            var elemType = (TypeNode)GetMemberFromToken();
            return new UnaryExpression(ParseArrayElementLoad(0, elemType),
                isReadOnly ? NodeType.ReadOnlyAddressOf : NodeType.AddressOf, elemType.GetReferenceType());
        }

        private static UnaryExpression /*!*/ SetType(UnaryExpression /*!*/ uex)
        {
            if (uex == null || uex.Operand == null) return uex;
            var elemType = uex.Operand.Type;
            if (elemType == null) return uex;
            uex.Type = elemType.GetReferenceType();
            return uex;
        }

        private BinaryExpression /*!*/ ParseBinaryComparison(NodeType oper)
        {
            var op2 = PopOperand();
            var op1 = PopOperand();
            var result = new BinaryExpression(op1, op2, oper);
            result.Type = CoreSystemTypes.Int32;
            return result;
        }

        private BinaryExpression /*!*/ ParseBinaryOperation(NodeType oper)
        {
            var op2 = PopOperand();
            var op1 = PopOperand();
            var result = new BinaryExpression(op1, op2, oper);
            result.Type = op1.Type;
            if (result.Type == null) result.Type = op2.Type;
            return result;
        }

        private UnaryExpression /*!*/ ParseUnaryOperation(NodeType oper)
        {
            var op = PopOperand();
            return new UnaryExpression(op, oper, op.Type);
        }

        private Branch /*!*/ ParseBranch(NodeType operatorType, int operandCount, bool shortOffset, bool unordered)
        {
            return ParseBranch(operatorType, operandCount, shortOffset, unordered, false);
        }

        private Branch /*!*/ ParseBranch(NodeType operatorType, int operandCount, bool shortOffset, bool unordered,
            bool leavesExceptionBlock)
        {
            var operand2 = operandCount > 1 ? PopOperand() : null;
            var operand1 = operandCount > 0 ? PopOperand() : null;
            var condition = operandCount > 1
                ? new BinaryExpression(operand1, operand2, operatorType)
                :
                operandCount > 0
                    ?
                    operatorType == NodeType.Nop ? operand1 : new UnaryExpression(operand1, operatorType)
                    : null;
            var targetAddress = shortOffset ? GetSByte() : GetInt32();
            var targetBlock = (Block)blockMap[targetAddress + counter + 1];
            Debug.Assert(targetBlock != null);
            if (targetAddress >= 0 && !reader.preserveShortBranches) shortOffset = false;
            return new Branch(condition, targetBlock, shortOffset, unordered, leavesExceptionBlock);
        }

        private MethodCall /*!*/ ParseCall(NodeType typeOfCall, out bool isStatement)
        {
            TypeNodeList varArgTypes;
            var meth = (Method)GetMemberFromToken(out varArgTypes);
            var numVarArgs = varArgTypes == null ? 0 : varArgTypes.Count;
            isStatement = TypeIsVoid(meth.ReturnType);
            var n = meth.Parameters == null ? 0 : meth.Parameters.Count;
            if (typeOfCall == NodeType.Jmp) n = 0;
            else n += numVarArgs;
            var args = new Expression[n];
            var arguments = new ExpressionList(n);
            for (var i = n - 1; i >= 0; i--) args[i] = PopOperand();
            for (var i = 0; i < n; i++) arguments.Add(args[i]);
            if (varArgTypes != null)
                for (int i = n - 1, j = numVarArgs; j > 0; j--, i--)
                {
                    var e = arguments[i];
                    var t = varArgTypes[j - 1];
                    if (e != null && t != null) e.Type = t;
                }

            var thisob = meth.IsStatic ? null : PopOperand();
            var methBinding = new MemberBinding(thisob, meth);
            var result = new MethodCall(methBinding, arguments, typeOfCall);
            result.Type = meth.ReturnType;
            result.IsTailCall = isTailCall;
            if (constraint != null)
            {
                result.Constraint = constraint;
                constraint = null;
            }

            return result;
        }

        private static bool TypeIsVoid(TypeNode t)
        {
            if (t == null) return false;
            for (;;)
                switch (t.NodeType)
                {
                    case NodeType.OptionalModifier:
                    case NodeType.RequiredModifier:
                        t = ((TypeModifier)t).ModifiedType;
                        break;
                    default:
                        return t == CoreSystemTypes.Void;
                }
        }

        private MethodCall /*!*/ ParseCalli(out bool isStatement)
        {
            var fp = reader.GetCalliSignature(GetInt32());
            if (fp == null) throw new InvalidMetadataException(ExceptionStrings.BaddCalliSignature);
            isStatement = TypeIsVoid(fp.ReturnType);
            var n = fp.ParameterTypes.Count;
            var args = new Expression[n + 1];
            var arguments = new ExpressionList(n + 1);
            for (var i = n; i >= 0; i--) args[i] = PopOperand();
            for (var i = 0; i <= n; i++) arguments.Add(args[i]);
            var thisob = fp.IsStatic ? null : PopOperand();
            var methBinding = new MemberBinding(thisob, fp);
            var result = new MethodCall(methBinding, arguments, NodeType.Calli);
            result.Type = fp.ReturnType;
            result.IsTailCall = isTailCall;
            return result;
        }

        private static Expression /*!*/ ParseTypeCheck(Expression operand, TypeNode type, NodeType typeOfCheck)
        {
            var etype = type;
            if (typeOfCheck == NodeType.Unbox) etype = type.GetReferenceType();
            Expression expr =
                new BinaryExpression(operand, new Literal(type, CoreSystemTypes.Type), typeOfCheck, etype);
            return expr;
        }

        private Construct /*!*/ ParseConstruct()
        {
            TypeNodeList varArgTypes;
            var meth = (Method)GetMemberFromToken(out varArgTypes);
            var n = meth.Parameters.Count;
            var args = new Expression[n];
            var arguments = new ExpressionList(n);
            for (var i = n - 1; i >= 0; i--) args[i] = PopOperand();
            for (var i = 0; i < n; i++) arguments.Add(args[i]);
            var result = new Construct(new MemberBinding(null, meth), arguments);
            result.Type = meth.DeclaringType;
            return result;
        }

        private AssignmentStatement /*!*/ ParseCopyObject()
        {
            var type = (TypeNode)GetMemberFromToken();
            var rhaddr = PopOperand();
            var lhaddr = PopOperand();
            return new AssignmentStatement(new AddressDereference(lhaddr, type, isVolatile, alignment),
                new AddressDereference(rhaddr, type));
        }

        private UnaryExpression /*!*/ ParseLoadRuntimeMetadataToken()
        {
            Expression expr = null;
            TypeNode exprType = null;
            var member = GetMemberFromToken();
            var t = member as TypeNode;
            if (t == null)
            {
                exprType = member.NodeType == NodeType.Field
                    ? CoreSystemTypes.RuntimeFieldHandle
                    : CoreSystemTypes.RuntimeMethodHandle;
                expr = new MemberBinding(null, member);
            }
            else
            {
                exprType = CoreSystemTypes.RuntimeTypeHandle;
                expr = new Literal(t, CoreSystemTypes.Type);
            }

            return new UnaryExpression(expr, NodeType.Ldtoken, exprType);
        }

        private AssignmentStatement /*!*/ ParseInitObject()
        {
            var type = (TypeNode)GetMemberFromToken();
            var lhaddr = PopOperand();
            return new AssignmentStatement(new AddressDereference(lhaddr, type, isVolatile, alignment),
                new Literal(null, CoreSystemTypes.Object));
        }

        private ConstructArray /*!*/ ParseNewArray()
        {
            var type = (TypeNode)GetMemberFromToken();
            var sizes = new ExpressionList(1);
            sizes.Add(PopOperand());
            var result = new ConstructArray(type, sizes, null);
            result.Type = type.GetArrayType(1);
            return result;
        }
#if !FxCop
        internal StatementList /*!*/ ParseStatements()
        {
            ParseHeader();
            if (size == 0) return new StatementList(0);
            CreateBlocksForBranchTargets();
            var result = new StatementList();
            Block currentBlock = null;
            while (counter < size)
            {
                if (currentBlock == null)
                {
                    currentBlock = Reader.GetOrCreateBlock(blockMap, counter);
#if ILOFFSETS
                    currentBlock.SourceContext = lastSourceContext;
#endif
                    result.Add(currentBlock);
                }

                var endOfBasicBlock = ParseStatement(currentBlock);
                if (endOfBasicBlock) currentBlock = null;
            }

            result.Add(Reader.GetOrCreateBlock(blockMap, counter));
            return result;
        }
#endif
        private bool ParseStatement(Block /*!*/ block)
        {
            //parse instructions and put in expression tree until an assignment, void call, branch target, or branch is encountered
            var statementList = block.Statements;
            Expression expr = null;
            Statement statement = null;
            var transferStatement = false;
            var startingAddress = 0;
#if !FxCop
            var sourceContext = new SourceContext();
#if !CodeContracts
      sourceContext.StartPos = this.counter;
#endif
#endif
#if !ROTOR
            if (method.contextForOffset != null)
            {
                var sctx = method.contextForOffset[counter + 1];
                if (sctx != null) sourceContext = (SourceContext)sctx;
#if ILOFFSETS
                else
                    sourceContext = lastSourceContext;
#endif
            }
#endif
            while (true)
            {
                var isStatement = false;
                startingAddress =
                    counter + 1; //Add one so that it is never zero (the latter means no entry to the TrivialHashtable)
#if FxCop || ILOFFSETS
                ilOffset = counter;
                opCode = GetOpCode();
#else
        OpCode opCode = this.GetOpCode();
#endif
#if FxCop
        if (this.handlerMap.TryGetValue(this.ilOffset, out expr)){
          expr.sourceContext = sourceContext;
          expr.ILOffset = this.ilOffset;
          this.operandStack.Push(expr);
        }
#endif
                switch (opCode)
                {
                    case OpCode.Nop:
                        statement = new Statement(NodeType.Nop);
                        goto done;
                    case OpCode.Break:
                        statement = new Statement(NodeType.DebugBreak);
                        goto done;
                    case OpCode.Ldarg_0:
                        expr = Parameters(0);
                        break;
                    case OpCode.Ldarg_1:
                        expr = Parameters(1);
                        break;
                    case OpCode.Ldarg_2:
                        expr = Parameters(2);
                        break;
                    case OpCode.Ldarg_3:
                        expr = Parameters(3);
                        break;
                    case OpCode.Ldloc_0:
                        expr = locals[0];
                        break;
                    case OpCode.Ldloc_1:
                        expr = locals[1];
                        break;
                    case OpCode.Ldloc_2:
                        expr = locals[2];
                        break;
                    case OpCode.Ldloc_3:
                        expr = locals[3];
                        break;
                    case OpCode.Stloc_0:
                        statement = new AssignmentStatement(locals[0], PopOperand());
                        goto done;
                    case OpCode.Stloc_1:
                        statement = new AssignmentStatement(locals[1], PopOperand());
                        goto done;
                    case OpCode.Stloc_2:
                        statement = new AssignmentStatement(locals[2], PopOperand());
                        goto done;
                    case OpCode.Stloc_3:
                        statement = new AssignmentStatement(locals[3], PopOperand());
                        goto done;
                    case OpCode.Ldarg_S:
                        expr = Parameters(GetByte());
                        break;
                    case OpCode.Ldarga_S:
                        expr = SetType(new UnaryExpression(Parameters(GetByte()), NodeType.AddressOf));
                        break;
                    case OpCode.Starg_S:
                        statement = new AssignmentStatement(Parameters(GetByte()), PopOperand());
                        goto done;
                    case OpCode.Ldloc_S:
                        expr = locals[GetByte()];
                        break;
                    case OpCode.Ldloca_S:
                        expr = SetType(new UnaryExpression(locals[GetByte()], NodeType.AddressOf));
                        break;
                    case OpCode.Stloc_S:
                        statement = new AssignmentStatement(locals[GetByte()], PopOperand());
                        goto done;
#if true || CodeContracts
                    case OpCode.Ldnull:
                        expr = Literal.Null;
                        break;
                    case OpCode.Ldc_I4_M1:
                        expr = Literal.Int32MinusOne;
                        break;
                    case OpCode.Ldc_I4_0:
                        expr = Literal.Int32Zero;
                        break;
                    case OpCode.Ldc_I4_1:
                        expr = Literal.Int32One;
                        break;
                    case OpCode.Ldc_I4_2:
                        expr = Literal.Int32Two;
                        break;
#else
          case OpCode.Ldnull: expr = new Literal(null, CoreSystemTypes.Object); break;
          case OpCode.Ldc_I4_M1: expr = new Literal(-1, CoreSystemTypes.Int32); break;
          case OpCode.Ldc_I4_0: expr = new Literal(0, CoreSystemTypes.Int32); break;
          case OpCode.Ldc_I4_1: expr = new Literal(1, CoreSystemTypes.Int32); break;
          case OpCode.Ldc_I4_2: expr = new Literal(2, CoreSystemTypes.Int32); break;
#endif
                    case OpCode.Ldc_I4_3:
                        expr = new Literal(3, CoreSystemTypes.Int32);
                        break;
                    case OpCode.Ldc_I4_4:
                        expr = new Literal(4, CoreSystemTypes.Int32);
                        break;
                    case OpCode.Ldc_I4_5:
                        expr = new Literal(5, CoreSystemTypes.Int32);
                        break;
                    case OpCode.Ldc_I4_6:
                        expr = new Literal(6, CoreSystemTypes.Int32);
                        break;
                    case OpCode.Ldc_I4_7:
                        expr = new Literal(7, CoreSystemTypes.Int32);
                        break;
                    case OpCode.Ldc_I4_8:
                        expr = new Literal(8, CoreSystemTypes.Int32);
                        break;
                    case OpCode.Ldc_I4_S:
                        expr = new Literal((int)GetSByte(), CoreSystemTypes.Int32);
                        break;
                    case OpCode.Ldc_I4:
                        expr = new Literal(GetInt32(), CoreSystemTypes.Int32);
                        break;
                    case OpCode.Ldc_I8:
                        expr = new Literal(GetInt64(), CoreSystemTypes.Int64);
                        break;
                    case OpCode.Ldc_R4:
                        expr = new Literal(GetSingle(), CoreSystemTypes.Single);
                        break;
                    case OpCode.Ldc_R8:
                        expr = new Literal(GetDouble(), CoreSystemTypes.Double);
                        break;
                    case OpCode.Dup:
                        statement = new ExpressionStatement(new Expression(NodeType.Dup));
                        goto done;
                    case OpCode.Pop:
                        statement = new ExpressionStatement(new UnaryExpression(PopOperand(), NodeType.Pop));
                        goto done;
                    case OpCode.Jmp:
                        expr = ParseCall(NodeType.Jmp, out isStatement);
                        if (isStatement) goto done;
                        break;
                    case OpCode.Call:
                        expr = ParseCall(NodeType.Call, out isStatement);
                        if (isStatement) goto done;
                        break;
                    case OpCode.Calli:
                        expr = ParseCalli(out isStatement);
                        if (isStatement) goto done;
                        break;
                    case OpCode.Ret:
                        var retVal = TypeIsVoid(method.ReturnType) ? null : PopOperand();
                        statement = new Return(retVal);
                        transferStatement = true;
                        goto done;
                    case OpCode.Br_S:
                        statement = ParseBranch(NodeType.Nop, 0, true, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Brfalse_S:
                        statement = ParseBranch(NodeType.LogicalNot, 1, true, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Brtrue_S:
                        statement = ParseBranch(NodeType.Nop, 1, true, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Beq_S:
                        statement = ParseBranch(NodeType.Eq, 2, true, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Bge_S:
                        statement = ParseBranch(NodeType.Ge, 2, true, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Bgt_S:
                        statement = ParseBranch(NodeType.Gt, 2, true, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Ble_S:
                        statement = ParseBranch(NodeType.Le, 2, true, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Blt_S:
                        statement = ParseBranch(NodeType.Lt, 2, true, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Bne_Un_S:
                        statement = ParseBranch(NodeType.Ne, 2, true, true);
                        transferStatement = true;
                        goto done;
                    case OpCode.Bge_Un_S:
                        statement = ParseBranch(NodeType.Ge, 2, true, true);
                        transferStatement = true;
                        goto done;
                    case OpCode.Bgt_Un_S:
                        statement = ParseBranch(NodeType.Gt, 2, true, true);
                        transferStatement = true;
                        goto done;
                    case OpCode.Ble_Un_S:
                        statement = ParseBranch(NodeType.Le, 2, true, true);
                        transferStatement = true;
                        goto done;
                    case OpCode.Blt_Un_S:
                        statement = ParseBranch(NodeType.Lt, 2, true, true);
                        transferStatement = true;
                        goto done;
                    case OpCode.Br:
                        statement = ParseBranch(NodeType.Nop, 0, false, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Brfalse:
                        statement = ParseBranch(NodeType.LogicalNot, 1, false, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Brtrue:
                        statement = ParseBranch(NodeType.Nop, 1, false, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Beq:
                        statement = ParseBranch(NodeType.Eq, 2, false, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Bge:
                        statement = ParseBranch(NodeType.Ge, 2, false, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Bgt:
                        statement = ParseBranch(NodeType.Gt, 2, false, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Ble:
                        statement = ParseBranch(NodeType.Le, 2, false, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Blt:
                        statement = ParseBranch(NodeType.Lt, 2, false, false);
                        transferStatement = true;
                        goto done;
                    case OpCode.Bne_Un:
                        statement = ParseBranch(NodeType.Ne, 2, false, true);
                        transferStatement = true;
                        goto done;
                    case OpCode.Bge_Un:
                        statement = ParseBranch(NodeType.Ge, 2, false, true);
                        transferStatement = true;
                        goto done;
                    case OpCode.Bgt_Un:
                        statement = ParseBranch(NodeType.Gt, 2, false, true);
                        transferStatement = true;
                        goto done;
                    case OpCode.Ble_Un:
                        statement = ParseBranch(NodeType.Le, 2, false, true);
                        transferStatement = true;
                        goto done;
                    case OpCode.Blt_Un:
                        statement = ParseBranch(NodeType.Lt, 2, false, true);
                        transferStatement = true;
                        goto done;
                    case OpCode.Switch:
                        statement = ParseSwitchInstruction();
                        transferStatement = true;
                        goto done;
                    case OpCode.Ldind_I1:
                        expr = new AddressDereference(PopOperand(), CoreSystemTypes.Int8, isVolatile, alignment);
                        break;
                    case OpCode.Ldind_U1:
                        expr = new AddressDereference(PopOperand(), CoreSystemTypes.UInt8, isVolatile, alignment);
                        break;
                    case OpCode.Ldind_I2:
                        expr = new AddressDereference(PopOperand(), CoreSystemTypes.Int16, isVolatile, alignment);
                        break;
                    case OpCode.Ldind_U2:
                        expr = new AddressDereference(PopOperand(), CoreSystemTypes.UInt16, isVolatile, alignment);
                        break;
                    case OpCode.Ldind_I4:
                        expr = new AddressDereference(PopOperand(), CoreSystemTypes.Int32, isVolatile, alignment);
                        break;
                    case OpCode.Ldind_U4:
                        expr = new AddressDereference(PopOperand(), CoreSystemTypes.UInt32, isVolatile, alignment);
                        break;
                    case OpCode.Ldind_I8:
                        expr = new AddressDereference(PopOperand(), CoreSystemTypes.Int64, isVolatile, alignment);
                        break;
                    case OpCode.Ldind_I:
                        expr = new AddressDereference(PopOperand(), CoreSystemTypes.IntPtr, isVolatile, alignment);
                        break;
                    case OpCode.Ldind_R4:
                        expr = new AddressDereference(PopOperand(), CoreSystemTypes.Single, isVolatile, alignment);
                        break;
                    case OpCode.Ldind_R8:
                        expr = new AddressDereference(PopOperand(), CoreSystemTypes.Double, isVolatile, alignment);
                        break;
                    case OpCode.Ldind_Ref:
                        expr = new AddressDereference(PopOperand(), CoreSystemTypes.Object, isVolatile, alignment);
                        break;
                    case OpCode.Stind_Ref:
                        statement = ParseStoreIndirect(CoreSystemTypes.Object);
                        goto done;
                    case OpCode.Stind_I1:
                        statement = ParseStoreIndirect(CoreSystemTypes.Int8);
                        goto done;
                    case OpCode.Stind_I2:
                        statement = ParseStoreIndirect(CoreSystemTypes.Int16);
                        goto done;
                    case OpCode.Stind_I4:
                        statement = ParseStoreIndirect(CoreSystemTypes.Int32);
                        goto done;
                    case OpCode.Stind_I8:
                        statement = ParseStoreIndirect(CoreSystemTypes.Int64);
                        goto done;
                    case OpCode.Stind_R4:
                        statement = ParseStoreIndirect(CoreSystemTypes.Single);
                        goto done;
                    case OpCode.Stind_R8:
                        statement = ParseStoreIndirect(CoreSystemTypes.Double);
                        goto done;
                    case OpCode.Add:
                        expr = ParseBinaryOperation(NodeType.Add);
                        break;
                    case OpCode.Sub:
                        expr = ParseBinaryOperation(NodeType.Sub);
                        break;
                    case OpCode.Mul:
                        expr = ParseBinaryOperation(NodeType.Mul);
                        break;
                    case OpCode.Div:
                        expr = ParseBinaryOperation(NodeType.Div);
                        break;
                    case OpCode.Div_Un:
                        expr = ParseBinaryOperation(NodeType.Div_Un);
                        break;
                    case OpCode.Rem:
                        expr = ParseBinaryOperation(NodeType.Rem);
                        break;
                    case OpCode.Rem_Un:
                        expr = ParseBinaryOperation(NodeType.Rem_Un);
                        break;
                    case OpCode.And:
                        expr = ParseBinaryOperation(NodeType.And);
                        break;
                    case OpCode.Or:
                        expr = ParseBinaryOperation(NodeType.Or);
                        break;
                    case OpCode.Xor:
                        expr = ParseBinaryOperation(NodeType.Xor);
                        break;
                    case OpCode.Shl:
                        expr = ParseBinaryOperation(NodeType.Shl);
                        break;
                    case OpCode.Shr:
                        expr = ParseBinaryOperation(NodeType.Shr);
                        break;
                    case OpCode.Shr_Un:
                        expr = ParseBinaryOperation(NodeType.Shr_Un);
                        break;
                    case OpCode.Neg:
                        expr = ParseUnaryOperation(NodeType.Neg);
                        break;
                    case OpCode.Not:
                        expr = ParseUnaryOperation(NodeType.Not);
                        break;
                    case OpCode.Conv_I1:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_I1, CoreSystemTypes.Int8);
                        break;
                    case OpCode.Conv_I2:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_I2, CoreSystemTypes.Int16);
                        break;
                    case OpCode.Conv_I4:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_I4, CoreSystemTypes.Int32);
                        break;
                    case OpCode.Conv_I8:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_I8, CoreSystemTypes.Int64);
                        break;
                    case OpCode.Conv_R4:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_R4, CoreSystemTypes.Single);
                        break;
                    case OpCode.Conv_R8:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_R8, CoreSystemTypes.Double);
                        break;
                    case OpCode.Conv_U4:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_U4, CoreSystemTypes.UInt32);
                        break;
                    case OpCode.Conv_U8:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_U8, CoreSystemTypes.UInt64);
                        break;
                    case OpCode.Callvirt:
                        expr = ParseCall(NodeType.Callvirt, out isStatement);
                        if (isStatement) goto done;
                        break;
                    case OpCode.Cpobj:
                        statement = ParseCopyObject();
                        goto done;
                    case OpCode.Ldobj:
                        expr = new AddressDereference(PopOperand(), (TypeNode)GetMemberFromToken(), isVolatile,
                            alignment);
                        break;
                    case OpCode.Ldstr:
                        expr = new Literal(GetStringFromToken(), CoreSystemTypes.String);
                        break;
                    case OpCode.Newobj:
                        expr = ParseConstruct();
                        break;
                    case OpCode.Castclass:
                        expr = ParseTypeCheck(PopOperand(), (TypeNode)GetMemberFromToken(), NodeType.Castclass);
                        break;
                    case OpCode.Isinst:
                        expr = ParseTypeCheck(PopOperand(), (TypeNode)GetMemberFromToken(), NodeType.Isinst);
                        break;
                    case OpCode.Conv_R_Un:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_R_Un, CoreSystemTypes.Double);
                        break;
                    case OpCode.Unbox:
                        expr = ParseTypeCheck(PopOperand(), (TypeNode)GetMemberFromToken(), NodeType.Unbox);
                        break;
                    case OpCode.Throw:
                        statement = new Throw(PopOperand());
                        transferStatement = true;
                        goto done;
                    case OpCode.Ldfld:
                        expr = new MemberBinding(PopOperand(), GetMemberFromToken(), isVolatile, alignment);
                        break;
                    case OpCode.Ldflda:
                        expr = SetType(new UnaryExpression(
                            new MemberBinding(PopOperand(), GetMemberFromToken(), isVolatile, alignment),
                            NodeType.AddressOf));
                        break;
                    case OpCode.Stfld:
                        statement = ParseStoreField();
                        goto done;
                    case OpCode.Ldsfld:
                        expr = new MemberBinding(null, GetMemberFromToken(new FieldInfo { IsStatic = true }),
                            isVolatile, alignment);
                        break;
                    case OpCode.Ldsflda:
                        expr = SetType(new UnaryExpression(
                            new MemberBinding(null, GetMemberFromToken(new FieldInfo { IsStatic = true }), isVolatile,
                                alignment), NodeType.AddressOf));
                        break;
                    case OpCode.Stsfld:
                        statement = new AssignmentStatement(
                            new MemberBinding(null, GetMemberFromToken(), isVolatile, alignment), PopOperand());
                        goto done;
                    case OpCode.Stobj:
                        statement = ParseStoreIndirect((TypeNode)GetMemberFromToken());
                        goto done;
                    case OpCode.Conv_Ovf_I1_Un:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_I1_Un, CoreSystemTypes.Int8);
                        break;
                    case OpCode.Conv_Ovf_I2_Un:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_I2_Un, CoreSystemTypes.Int16);
                        break;
                    case OpCode.Conv_Ovf_I4_Un:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_I4_Un, CoreSystemTypes.Int32);
                        break;
                    case OpCode.Conv_Ovf_I8_Un:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_I8_Un, CoreSystemTypes.Int64);
                        break;
                    case OpCode.Conv_Ovf_U1_Un:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_U1_Un, CoreSystemTypes.UInt8);
                        break;
                    case OpCode.Conv_Ovf_U2_Un:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_U2_Un, CoreSystemTypes.UInt16);
                        break;
                    case OpCode.Conv_Ovf_U4_Un:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_U4_Un, CoreSystemTypes.UInt32);
                        break;
                    case OpCode.Conv_Ovf_U8_Un:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_U8_Un, CoreSystemTypes.UInt64);
                        break;
                    case OpCode.Conv_Ovf_I_Un:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_I_Un, CoreSystemTypes.IntPtr);
                        break;
                    case OpCode.Conv_Ovf_U_Un:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_U_Un, CoreSystemTypes.UIntPtr);
                        break;
                    case OpCode.Box:
                        var t = (TypeNode)GetMemberFromToken();
                        TypeNode bt = t is EnumNode ? CoreSystemTypes.Enum : CoreSystemTypes.ValueType;
                        expr = new BinaryExpression(PopOperand(), new Literal(t, CoreSystemTypes.Type), NodeType.Box,
                            bt);
                        break;
                    case OpCode.Newarr:
                        expr = ParseNewArray();
                        break;
                    case OpCode.Ldlen:
                        expr = new UnaryExpression(PopOperand(), NodeType.Ldlen, CoreSystemTypes.UIntPtr);
                        break;
                    case OpCode.Ldelema:
                        expr = ParseArrayElementLoadAddress();
                        break;
                    case OpCode.Ldelem_I1:
                    case OpCode.Ldelem_U1:
                    case OpCode.Ldelem_I2:
                    case OpCode.Ldelem_U2:
                    case OpCode.Ldelem_I4:
                    case OpCode.Ldelem_U4:
                    case OpCode.Ldelem_I8:
                    case OpCode.Ldelem_I:
                    case OpCode.Ldelem_R4:
                    case OpCode.Ldelem_R8:
                    case OpCode.Ldelem_Ref:
                        expr = ParseArrayElementLoad(opCode, null);
                        break;
                    case OpCode.Stelem_I:
                    case OpCode.Stelem_I1:
                    case OpCode.Stelem_I2:
                    case OpCode.Stelem_I4:
                    case OpCode.Stelem_I8:
                    case OpCode.Stelem_R4:
                    case OpCode.Stelem_R8:
                    case OpCode.Stelem_Ref:
                        statement = ParseArrayElementAssignment(opCode);
                        goto done;
                    case OpCode.Ldelem:
                        expr = ParseArrayElementLoad(opCode, null);
                        break;
                    case OpCode.Stelem:
                        statement = ParseArrayElementAssignment(opCode);
                        goto done;
                    case OpCode.Unbox_Any:
                        expr = ParseTypeCheck(PopOperand(), (TypeNode)GetMemberFromToken(), NodeType.UnboxAny);
                        break;
                    case OpCode.Conv_Ovf_I1:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_I1, CoreSystemTypes.Int8);
                        break;
                    case OpCode.Conv_Ovf_U1:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_U1, CoreSystemTypes.UInt8);
                        break;
                    case OpCode.Conv_Ovf_I2:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_I2, CoreSystemTypes.Int16);
                        break;
                    case OpCode.Conv_Ovf_U2:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_U2, CoreSystemTypes.UInt16);
                        break;
                    case OpCode.Conv_Ovf_I4:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_I4, CoreSystemTypes.Int32);
                        break;
                    case OpCode.Conv_Ovf_U4:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_U4, CoreSystemTypes.UInt32);
                        break;
                    case OpCode.Conv_Ovf_I8:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_I8, CoreSystemTypes.Int64);
                        break;
                    case OpCode.Conv_Ovf_U8:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_U8, CoreSystemTypes.UInt64);
                        break;
                    case OpCode.Refanyval:
                        expr = new BinaryExpression(PopOperand(),
                            new Literal(GetMemberFromToken(), CoreSystemTypes.Type), NodeType.Refanyval,
                            CoreSystemTypes.IntPtr);
                        break;
                    case OpCode.Ckfinite:
                        expr = ParseUnaryOperation(NodeType.Ckfinite);
                        break;
                    case OpCode.Mkrefany:
                        expr = new BinaryExpression(PopOperand(),
                            new Literal(GetMemberFromToken(), CoreSystemTypes.Type), NodeType.Mkrefany,
                            CoreSystemTypes.DynamicallyTypedReference);
                        break;
                    case OpCode.Ldtoken:
                        expr = ParseLoadRuntimeMetadataToken();
                        break;
                    case OpCode.Conv_U2:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_U2, CoreSystemTypes.UInt16);
                        break;
                    case OpCode.Conv_U1:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_U1, CoreSystemTypes.UInt8);
                        break;
                    case OpCode.Conv_I:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_I, CoreSystemTypes.IntPtr);
                        break;
                    case OpCode.Conv_Ovf_I:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_I, CoreSystemTypes.IntPtr);
                        break;
                    case OpCode.Conv_Ovf_U:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_Ovf_U, CoreSystemTypes.UIntPtr);
                        break;
                    case OpCode.Add_Ovf:
                        expr = ParseBinaryOperation(NodeType.Add_Ovf);
                        break;
                    case OpCode.Add_Ovf_Un:
                        expr = ParseBinaryOperation(NodeType.Add_Ovf_Un);
                        break;
                    case OpCode.Mul_Ovf:
                        expr = ParseBinaryOperation(NodeType.Mul_Ovf);
                        break;
                    case OpCode.Mul_Ovf_Un:
                        expr = ParseBinaryOperation(NodeType.Mul_Ovf_Un);
                        break;
                    case OpCode.Sub_Ovf:
                        expr = ParseBinaryOperation(NodeType.Sub_Ovf);
                        break;
                    case OpCode.Sub_Ovf_Un:
                        expr = ParseBinaryOperation(NodeType.Sub_Ovf_Un);
                        break;
                    case OpCode.Endfinally:
                        statement = new EndFinally();
                        transferStatement = true;
                        goto done;
                    case OpCode.Leave:
                        statement = ParseBranch(NodeType.Nop, 0, false, false, true);
                        transferStatement = true;
                        goto done;
                    case OpCode.Leave_S:
                        statement = ParseBranch(NodeType.Nop, 0, true, false, true);
                        transferStatement = true;
                        goto done;
                    case OpCode.Stind_I:
                        statement = ParseStoreIndirect(CoreSystemTypes.IntPtr);
                        goto done;
                    case OpCode.Conv_U:
                        expr = new UnaryExpression(PopOperand(), NodeType.Conv_U, CoreSystemTypes.UIntPtr);
                        break;
                    case OpCode.Arglist:
                        expr = new Expression(NodeType.Arglist, CoreSystemTypes.ArgIterator);
                        break;
                    case OpCode.Ceq:
                        expr = ParseBinaryComparison(NodeType.Ceq);
                        break;
                    case OpCode.Cgt:
                        expr = ParseBinaryComparison(NodeType.Cgt);
                        break;
                    case OpCode.Cgt_Un:
                        expr = ParseBinaryComparison(NodeType.Cgt_Un);
                        break;
                    case OpCode.Clt:
                        expr = ParseBinaryComparison(NodeType.Clt);
                        break;
                    case OpCode.Clt_Un:
                        expr = ParseBinaryComparison(NodeType.Clt_Un);
                        break;
                    case OpCode.Ldftn:
                        expr = new UnaryExpression(new MemberBinding(null, GetMemberFromToken()), NodeType.Ldftn,
                            CoreSystemTypes.IntPtr);
                        break;
                    case OpCode.Ldvirtftn:
                        expr = new BinaryExpression(PopOperand(), new MemberBinding(null, GetMemberFromToken()),
                            NodeType.Ldvirtftn, CoreSystemTypes.IntPtr);
                        break;
                    case OpCode.Ldarg:
                        expr = Parameters((ushort)GetInt16());
                        break;
                    case OpCode.Ldarga:
                        expr = SetType(new UnaryExpression(Parameters((ushort)GetInt16()), NodeType.AddressOf));
                        break;
                    case OpCode.Starg:
                        statement = new AssignmentStatement(Parameters((ushort)GetInt16()), PopOperand());
                        goto done;
                    case OpCode.Ldloc:
                        expr = locals[(ushort)GetInt16()];
                        break;
                    case OpCode.Ldloca:
                        expr = SetType(new UnaryExpression(locals[(ushort)GetInt16()], NodeType.AddressOf));
                        break;
                    case OpCode.Stloc:
                        statement = new AssignmentStatement(locals[(ushort)GetInt16()], PopOperand());
                        goto done;
                    case OpCode.Localloc:
                        expr = new UnaryExpression(PopOperand(), NodeType.Localloc, CoreSystemTypes.Void);
                        break;
                    case OpCode.Endfilter:
                        statement = new EndFilter(PopOperand());
                        transferStatement = true;
                        goto done;
                    case OpCode.Unaligned_:
                        alignment = GetByte();
                        continue;
                    case OpCode.Volatile_:
                        isVolatile = true;
                        continue;
                    case OpCode.Tail_:
                        isTailCall = true;
                        continue;
                    case OpCode.Initobj:
                        statement = ParseInitObject();
                        goto done;
                    case OpCode.Constrained_:
                        constraint = GetMemberFromToken() as TypeNode;
                        continue;
                    case OpCode.Cpblk:
                        expr = ParseTernaryOperation(NodeType.Cpblk);
                        goto done;
                    case OpCode.Initblk:
                        expr = ParseTernaryOperation(NodeType.Initblk);
                        goto done;
                    case OpCode.Rethrow:
                        statement = new Throw(null);
                        statement.NodeType = NodeType.Rethrow;
                        transferStatement = true;
                        goto done;
                    case OpCode.Sizeof:
                        expr = new UnaryExpression(new Literal(GetMemberFromToken(), CoreSystemTypes.Type),
                            NodeType.Sizeof, CoreSystemTypes.Int32);
                        break;
                    case OpCode.Refanytype:
                        expr = new UnaryExpression(PopOperand(), NodeType.Refanytype,
                            CoreSystemTypes.RuntimeTypeHandle);
                        break;
                    case OpCode.Readonly_:
                        isReadOnly = true;
                        continue;
                    default: throw new InvalidMetadataException(ExceptionStrings.UnknownOpCode);
                }

                if (blockMap[counter + 1] != null)
                {
                    transferStatement =
                        true; //Falls through to the next basic block, so implicitly a "transfer" statement
                    goto done;
                }
                //^ assume expr != null;
#if FxCop
        expr.sourceContext = sourceContext;
#endif
#if FxCop || ILOFFSETS
                expr.ILOffset = ilOffset;
#endif
                operandStack.Push(expr);
                isReadOnly = false;
                isVolatile = false;
                isTailCall = false;
                alignment = -1;
            }

            done:
            for (var i = 0; i <= operandStack.top; i++)
            {
                var e = operandStack.elements[i];
                //^ assume e != null;
                Statement s = new ExpressionStatement(e);
#if FxCop
        s.SourceContext = this.sourceContext;
        s.ILOffset = this.ilOffset;
#endif
#if ILOFFSETS
                s.ILOffset = ilOffset;
                s.SourceContext = sourceContext;
#endif
                statementList.Add(s);
            }

            operandStack.top = -1;
            if (statement == null)
            {
                statement = new ExpressionStatement(expr);
#if FxCop
        expr.sourceContext = this.sourceContext;
#endif
#if FxCop || ILOFFSETS
                expr.ILOffset = ilOffset;
#endif
            }

            statement.SourceContext = sourceContext;
#if FxCop || ILOFFSETS
            statement.ILOffset = ilOffset;
#endif
#if ILOFFSETS
            lastSourceContext = sourceContext;
#endif
            statementList.Add(statement);
            if (transferStatement) return true;
            return blockMap[counter + 1] != null;
        }

        private AssignmentStatement ParseStoreField()
        {
            var rhvalue = PopOperand();
            var thisob = PopOperand();
            var s = new AssignmentStatement(new MemberBinding(thisob, GetMemberFromToken(), isVolatile, alignment),
                rhvalue);
            return s;
        }

        private AssignmentStatement ParseStoreIndirect(TypeNode type)
        {
            var rhvalue = PopOperand();
            var lhaddr = PopOperand();
            return new AssignmentStatement(new AddressDereference(lhaddr, type, isVolatile, alignment), rhvalue);
        }

        private SwitchInstruction ParseSwitchInstruction()
        {
            var numTargets = GetInt32();
            var offset = counter + numTargets * 4;
            var targetList = new BlockList(numTargets);
            for (var i = 0; i < numTargets; i++)
            {
                var targetAddress = GetInt32() + offset;
                targetList.Add(Reader.GetOrCreateBlock(blockMap, targetAddress));
            }

            return new SwitchInstruction(PopOperand(), targetList);
        }

        private TernaryExpression ParseTernaryOperation(NodeType oper)
        {
            var op3 = PopOperand();
            var op2 = PopOperand();
            var op1 = PopOperand();
            return new TernaryExpression(op1, op2, op3, oper, null);
        }

        private void CreateBlocksForBranchTargets()
        {
            var savedPosition = bodyReader.Position;
            while (counter < size)
                ProcessOneILInstruction();
            counter = 0;
            bodyReader.Position = savedPosition;
        }

        private void ProcessOneILInstruction()
        {
            var opc = GetOpCode();
            switch (opc)
            {
                case OpCode.Ldarg_S:
                case OpCode.Ldarga_S:
                case OpCode.Starg_S:
                case OpCode.Ldloc_S:
                case OpCode.Ldloca_S:
                case OpCode.Stloc_S:
                case OpCode.Ldc_I4_S:
                    GetByte();
                    return;
                case OpCode.Ldc_I4:
                case OpCode.Jmp:
                case OpCode.Call:
                case OpCode.Calli:
                case OpCode.Callvirt:
                case OpCode.Cpobj:
                case OpCode.Ldobj:
                case OpCode.Ldstr:
                case OpCode.Newobj:
                case OpCode.Castclass:
                case OpCode.Isinst:
                case OpCode.Unbox:
                case OpCode.Ldfld:
                case OpCode.Ldflda:
                case OpCode.Stfld:
                case OpCode.Ldsfld:
                case OpCode.Ldsflda:
                case OpCode.Stsfld:
                case OpCode.Stobj:
                case OpCode.Box:
                case OpCode.Newarr:
                case OpCode.Ldelema:
                case OpCode.Ldelem:
                case OpCode.Stelem:
                case OpCode.Unbox_Any:
                case OpCode.Refanyval:
                case OpCode.Mkrefany:
                case OpCode.Ldtoken:
                    GetInt32();
                    return;
                case OpCode.Ldc_I8:
                    GetInt64();
                    return;
                case OpCode.Ldc_R4:
                    GetSingle();
                    return;
                case OpCode.Ldc_R8:
                    GetDouble();
                    return;
                case OpCode.Br_S:
                case OpCode.Brfalse_S:
                case OpCode.Brtrue_S:
                case OpCode.Beq_S:
                case OpCode.Bge_S:
                case OpCode.Bgt_S:
                case OpCode.Ble_S:
                case OpCode.Blt_S:
                case OpCode.Bne_Un_S:
                case OpCode.Bge_Un_S:
                case OpCode.Bgt_Un_S:
                case OpCode.Ble_Un_S:
                case OpCode.Blt_Un_S:
                case OpCode.Leave_S:
                    SkipBranch(true);
                    return;
                case OpCode.Br:
                case OpCode.Brfalse:
                case OpCode.Brtrue:
                case OpCode.Beq:
                case OpCode.Bge:
                case OpCode.Bgt:
                case OpCode.Ble:
                case OpCode.Blt:
                case OpCode.Bne_Un:
                case OpCode.Bge_Un:
                case OpCode.Bgt_Un:
                case OpCode.Ble_Un:
                case OpCode.Blt_Un:
                case OpCode.Leave:
                    SkipBranch(false);
                    return;
                case OpCode.Switch:
                    SkipSwitch();
                    return;
                case OpCode.Ldftn:
                case OpCode.Ldvirtftn:
                case OpCode.Initobj:
                case OpCode.Constrained_:
                case OpCode.Sizeof:
                    GetInt32();
                    return;
                case OpCode.Ldarg:
                case OpCode.Ldarga:
                case OpCode.Ldloc:
                case OpCode.Ldloca:
                case OpCode.Starg:
                case OpCode.Stloc:
                    GetInt16();
                    return;
                case OpCode.Unaligned_:
                    GetByte();
                    return;
                default:
                    return;
            }
        }

        private void SkipBranch(bool shortOffset)
        {
            var offset = shortOffset ? GetSByte() : GetInt32();
            Reader.GetOrCreateBlock(blockMap, counter + offset);
        }

        private void SkipSwitch()
        {
            var numCases = GetInt32();
            var offset = counter + numCases * 4;
            for (var i = 0; i < numCases; i++)
            {
                var targetAddress = GetInt32() + offset;
                Reader.GetOrCreateBlock(blockMap, targetAddress);
            }
        }

        private Expression PopOperand()
        {
            return operandStack.Pop();
        }
#if FxCop || ILOFFSETS
        private OpCode opCode;
        private int ilOffset;
#endif
#if ILOFFSETS
        private SourceContext lastSourceContext;
#endif
#if FxCop
    private SourceContext sourceContext;
    private Block currentBlock;
    private Dictionary<Block, List<TryNode>> tryMap;
    private Dictionary<int, Expression> handlerMap;
    internal StatementList/*!*/ ParseStatements() {
      this.tryMap = new Dictionary<Block, List<TryNode>>();
      this.handlerMap = new Dictionary<int, Expression>();
      this.ParseHeader();
      this.CreateBlocksForBranchTargets();
      currentBlock = null;
      this.sourceContext = new SourceContext();
      while (this.counter < size) {
        if (currentBlock == null) {
          currentBlock = Reader.GetOrCreateBlock(this.blockMap, this.counter);
        }
        bool endOfBasicBlock = this.ParseStatement(currentBlock);
        if (endOfBasicBlock) {
          currentBlock.SourceContext = currentBlock.Statements[0].SourceContext;
          currentBlock = null;
        }
      }
      Reader.GetOrCreateBlock(this.blockMap, this.counter);
      int counter = 0;
      Block block = new Block();
      block.Statements = new StatementList();
      ProcessBlock(block, ref counter, this.size, null);
      return block.Statements;
    }
    override protected void ParseExceptionHandlerEntry(bool smallSection) {
      int dataSize = this.reader.tables.GetByte();
      int n = (int)(ushort)this.reader.tables.GetInt16();
      if (smallSection)
        n = dataSize / 12;
      else
        n = (dataSize + (n << 8)) / 24;
      for (int i = 0; i < n; i++) {
        int flags, tryOffset, tryLength, handlerOffset, handlerLength, tokenOrOffset;
        if (smallSection) {
          flags = this.reader.tables.GetInt16();
          tryOffset = this.reader.tables.GetUInt16();
          tryLength = this.reader.tables.GetByte();
          handlerOffset = this.reader.tables.GetUInt16();
          handlerLength = this.reader.tables.GetByte();
        }
        else {
          flags = this.reader.tables.GetInt32();
          tryOffset = this.reader.tables.GetInt32();
          tryLength = this.reader.tables.GetInt32();
          handlerOffset = this.reader.tables.GetInt32();
          handlerLength = this.reader.tables.GetInt32();
        }
        tokenOrOffset = this.reader.tables.GetInt32();
        Block tryStartBlock = Reader.GetOrCreateBlock(this.blockMap, tryOffset);
        Block blockAfterTryEnd = Reader.GetOrCreateBlock(this.blockMap, tryOffset + tryLength);
        Block handlerStartBlock = Reader.GetOrCreateBlock(this.blockMap, handlerOffset);
        Block blockAfterHandlerEnd = Reader.GetOrCreateBlock(this.blockMap, handlerOffset + handlerLength);
        List<TryNode> tryList = null;
        if (!this.tryMap.TryGetValue(tryStartBlock, out tryList)) {
          this.tryMap[tryStartBlock] = tryList = new List<TryNode>();
        }
        TryNode currentTry = null;
        int tryEnd = tryOffset + tryLength;
        foreach (TryNode t in tryList) {
          if (t.tryEnd == tryEnd) {
            currentTry = t;
            break;
          }
        }
        if (currentTry == null) {
          currentTry = new TryNode();
          currentTry.tryEnd = tryEnd;
          tryList.Add(currentTry);
        }
        int handlerEnd = handlerOffset + handlerLength;
        if (currentTry.handlersEnd < handlerEnd)
          currentTry.handlersEnd = handlerEnd;

        Debug.Assert((int)flags != 3);
        Debug.Assert((int)flags < 5);

        switch (flags) {
          case 0x00:
            // for a catch handler, tokenOrOffset represents
            // the metadata token of the handler type. handlerOffset
            // is the literal offset for the catch block
            int pos = this.reader.tables.GetCurrentPosition();
            TypeNode filterType = (TypeNode)this.reader.GetMemberFromToken(tokenOrOffset);
            this.reader.tables.SetCurrentPosition(pos);
            string variableName = "$exception" + this.handlerMap.Count.ToString(CultureInfo.InvariantCulture);
            StackVariable exception = new StackVariable(filterType, variableName);
            CatchNode c = new CatchNode(handlerStartBlock, exception, filterType);
            c.handlerEnd = handlerEnd;
            currentTry.Catchers.Add(c);
            this.handlerMap[handlerOffset] = exception;
            break;
          case 0x01:
            // for a filter, tokenOrOffset represents the IL offset
            // of the filter block. handlerOffset represents
            // the IL offset of the associated catch handler
            Block filterExpression = Reader.GetOrCreateBlock(blockMap, tokenOrOffset);
            variableName = "$exception" + this.handlerMap.Count.ToString(CultureInfo.InvariantCulture);
            exception = new StackVariable(CoreSystemTypes.Object, variableName);
            Filter filter = new Filter(filterExpression, exception);
            filter.handlerEnd = handlerOffset;
            c = new CatchNode(handlerStartBlock, exception, null, filter);
            c.handlerEnd = handlerEnd;
            currentTry.Catchers.Add(c);
            // note that handlerOffset would not be correct here!
            this.handlerMap[tokenOrOffset] = exception;
            break;
          case 0x02:
            FinallyNode f = new FinallyNode(handlerStartBlock);
            f.handlerEnd = handlerEnd;
            currentTry.Finally = f;
            break;
          case 0x04:
            FaultHandler fh = new FaultHandler(handlerStartBlock);
            fh.handlerEnd = handlerEnd;
            currentTry.FaultHandler = fh;
            break;
        }
      }
    }
    private void ProcessBlock(Block currentBlock, ref int counter, int blockEnd, Node blockNode) {
      while (true) {
        int lastCounter = counter;
        Block block = GetNextBlock(ref counter);
        if (block == null || block.ILOffset >= blockEnd) {
          counter = lastCounter;
          if (blockNode != null)
            blockNode.SourceContext = currentBlock.Statements[0].SourceContext;
          return;
        }
        if (this.tryMap.ContainsKey(block)) {
          ProcessTryBlock(currentBlock, block, ref counter);
        }
        else {
          if (currentBlock.Statements.Count == 0)
            currentBlock.SourceContext = block.SourceContext;
          currentBlock.Statements.Add(block);
        }
      }
    }
    private void ProcessTryBlock(Block outerBlock, Block currentBlock, ref int counter) {
      List<TryNode> tryList = this.tryMap[currentBlock];
      TryNode outerTry = tryList[tryList.Count - 1];
      outerBlock.Statements.Add(outerTry);
      tryList.Remove(outerTry);
      if (tryList.Count > 0) {
        outerTry.Block = new Block();
        outerTry.Block.Statements = new StatementList();
        ProcessTryBlock(outerTry.Block, currentBlock, ref counter);
      }
      else {
        outerTry.Block = currentBlock;
      }
      this.tryMap.Remove(currentBlock);
      ProcessBlock(outerTry.Block, ref counter, outerTry.tryEnd, outerTry);
      while (true) {
        int lastCounter = counter;
        Block block = GetNextBlock(ref counter);
        if (counter >= outerTry.handlersEnd) {
          counter = lastCounter;
          return;
        }
        int handlerEnd;
        Node handlerNode;
        GetHandlerEnd(outerTry, block, out handlerEnd, out handlerNode);
        ProcessBlock(block, ref counter, handlerEnd, handlerNode);
      }
    }
    private Block GetNextBlock(ref int counter) {
      while (true) {
        Block result = this.blockMap[counter + 1] as Block;
        ++counter;
        if (result != null || counter >= this.size)
          return result;
      }
    }
    private void GetHandlerEnd(TryNode t, Block block, out int handlerEnd, out Node handlerNode) {
      handlerEnd = Int32.MaxValue;
      handlerNode = null;
      int startPos = block.ILOffset;
      if (t.Finally != null
        && t.Finally.handlerEnd > startPos
        && t.Finally.handlerEnd < handlerEnd) {
        handlerEnd = t.Finally.handlerEnd;
        handlerNode = t.Finally;
      }
      foreach (CatchNode c in t.Catchers) {
        if (c.handlerEnd > startPos && c.handlerEnd < handlerEnd) {
          handlerEnd = c.handlerEnd;
          handlerNode = c;
        }
        if (c.Filter != null && c.Filter.handlerEnd > startPos && c.Filter.handlerEnd < handlerEnd) {
          handlerEnd = c.Filter.handlerEnd;
          handlerNode = c.Filter;
        }
      }
      if (t.FaultHandler != null
        && t.FaultHandler.handlerEnd > startPos
        && t.FaultHandler.handlerEnd < handlerEnd) {
        handlerEnd = t.FaultHandler.handlerEnd;
        handlerNode = t.FaultHandler;
      }
    }
#endif
    }

    internal class InstructionParser : ILParser
    {
        private readonly TrivialHashtable /*!*/
            ehMap;

        private SourceContext sourceContext;

        internal InstructionParser(Reader /*!*/ reader, Method /*!*/ method, int methodIndex, int RVA)
            : base(reader, method, methodIndex, RVA)
        {
            ehMap = new TrivialHashtable();
        }

        protected override void ParseExceptionHandlerEntry(bool smallSection)
        {
            var tryMap = new TrivialHashtable();
            int dataSize = reader.tables.GetByte();
            int n = (ushort)reader.tables.GetInt16();
            if (smallSection)
                n = dataSize / 12;
            else
                n = (dataSize + (n << 8)) / 24;
            for (var i = 0; i < n; i++)
            {
                Instruction matchingInstruction;
                int flags, tryOffset, tryLength, handlerOffset, handlerLength, tokenOrOffset;
                if (smallSection)
                {
                    flags = reader.tables.GetInt16();
                    tryOffset = reader.tables.GetUInt16();
                    tryLength = reader.tables.GetByte();
                    handlerOffset = reader.tables.GetUInt16();
                    handlerLength = reader.tables.GetByte();
                }
                else
                {
                    flags = reader.tables.GetInt32();
                    tryOffset = reader.tables.GetInt32();
                    tryLength = reader.tables.GetInt32();
                    handlerOffset = reader.tables.GetInt32();
                    handlerLength = reader.tables.GetInt32();
                }

                tokenOrOffset = reader.tables.GetInt32();
                if (tryMap[tryOffset + tryLength] == null)
                {
                    matchingInstruction = AddInstruction(OpCode._Try, tryOffset);
                    AddInstruction(OpCode._EndTry, tryOffset + tryLength, matchingInstruction);
                    tryMap[tryOffset + tryLength] = string.Empty;
                }

                switch (flags)
                {
                    case 0x00:
                        var pos = reader.tables.GetCurrentPosition();
                        var catchType = (TypeNode)reader.GetMemberFromToken(tokenOrOffset);
                        reader.tables.SetCurrentPosition(pos);
                        matchingInstruction = AddInstruction(OpCode._Catch, handlerOffset, catchType);
                        AddInstruction(OpCode._EndHandler, handlerOffset + handlerLength, matchingInstruction);
                        break;
                    case 0x01:
                        matchingInstruction = AddInstruction(OpCode._Filter, tokenOrOffset);
                        AddInstruction(OpCode._EndFilter, handlerOffset, matchingInstruction);
                        matchingInstruction = AddInstruction(OpCode._Catch, handlerOffset);
                        AddInstruction(OpCode._EndHandler, handlerOffset + handlerLength, matchingInstruction);
                        break;
                    case 0x02:
                        matchingInstruction = AddInstruction(OpCode._Finally, handlerOffset);
                        AddInstruction(OpCode._EndHandler, handlerOffset + handlerLength, matchingInstruction);
                        break;
                    case 0x04:
                        matchingInstruction = AddInstruction(OpCode._Fault, handlerOffset);
                        AddInstruction(OpCode._EndHandler, handlerOffset + handlerLength, matchingInstruction);
                        break;
                    default: throw new InvalidMetadataException(ExceptionStrings.BadExceptionHandlerType);
                }
            }
        }

        private Instruction AddInstruction(OpCode opCode, int offset)
        {
            return AddInstruction(opCode, offset, null);
        }

        private Instruction AddInstruction(OpCode opCode, int offset, object value)
        {
            var instruction = new Instruction(opCode, offset, value);
            var instructions = (InstructionList)ehMap[offset + 1];
            if (instructions == null) ehMap[offset + 1] = instructions = new InstructionList(2);
            instructions.Add(instruction);
#if !ROTOR
            if (method.contextForOffset != null)
            {
                var sctx = method.contextForOffset[offset + 1];
                if (sctx != null) instruction.SourceContext = (SourceContext)sctx;
            }
#endif
            return instruction;
        }

        private Int32List ParseSwitchInstruction()
        {
            var numTargets = GetInt32();
            var result = new Int32List(numTargets);
            var offset = counter + numTargets * 4;
            for (var i = 0; i < numTargets; i++)
            {
                var targetAddress = GetInt32() + offset;
                result.Add(targetAddress);
            }

            return result;
        }

        internal InstructionList ParseInstructions()
        {
            ParseHeader();
            if (size == 0) return new InstructionList(0);
            var result = new InstructionList();
            result.Add(new Instruction(OpCode._Locals, 0, locals));
            while (counter <= size)
            {
                var instructions = (InstructionList)ehMap[counter + 1];
                if (instructions != null)
                    for (var i = 0; i < instructions.Count; i++)
                        result.Add(instructions[i]);
                if (counter < size)
                    result.Add(ParseInstruction());
                else
                    break;
            }

            return result;
        }

        internal Instruction ParseInstruction()
        {
            if (counter >= size)
                return null;
            var offset = counter;
#if !ROTOR
            if (method.contextForOffset != null)
            {
                var sctx = method.contextForOffset[offset + 1];
                if (sctx != null) sourceContext = (SourceContext)sctx;
            }
#endif
            object value = null;
            var opCode = GetOpCode();
            switch (opCode)
            {
                case OpCode.Nop:
                case OpCode.Break:
                    break;
                case OpCode.Ldarg_0:
                    value = Parameters(0);
                    break;
                case OpCode.Ldarg_1:
                    value = Parameters(1);
                    break;
                case OpCode.Ldarg_2:
                    value = Parameters(2);
                    break;
                case OpCode.Ldarg_3:
                    value = Parameters(3);
                    break;
                case OpCode.Ldloc_0:
                    value = locals[0];
                    break;
                case OpCode.Ldloc_1:
                    value = locals[1];
                    break;
                case OpCode.Ldloc_2:
                    value = locals[2];
                    break;
                case OpCode.Ldloc_3:
                    value = locals[3];
                    break;
                case OpCode.Stloc_0:
                    value = locals[0];
                    break;
                case OpCode.Stloc_1:
                    value = locals[1];
                    break;
                case OpCode.Stloc_2:
                    value = locals[2];
                    break;
                case OpCode.Stloc_3:
                    value = locals[3];
                    break;
                case OpCode.Ldarg_S:
                case OpCode.Ldarga_S:
                case OpCode.Starg_S:
                    value = Parameters(GetByte());
                    break;
                case OpCode.Ldloc_S:
                case OpCode.Ldloca_S:
                case OpCode.Stloc_S:
                    value = locals[GetByte()];
                    break;
                case OpCode.Ldnull:
                    break;
                case OpCode.Ldc_I4_M1:
                    value = -1;
                    break;
                case OpCode.Ldc_I4_0:
                    value = 0;
                    break;
                case OpCode.Ldc_I4_1:
                    value = 1;
                    break;
                case OpCode.Ldc_I4_2:
                    value = 2;
                    break;
                case OpCode.Ldc_I4_3:
                    value = 3;
                    break;
                case OpCode.Ldc_I4_4:
                    value = 4;
                    break;
                case OpCode.Ldc_I4_5:
                    value = 5;
                    break;
                case OpCode.Ldc_I4_6:
                    value = 6;
                    break;
                case OpCode.Ldc_I4_7:
                    value = 7;
                    break;
                case OpCode.Ldc_I4_8:
                    value = 8;
                    break;
                case OpCode.Ldc_I4_S:
                    value = (int)GetSByte();
                    break;
                case OpCode.Ldc_I4:
                    value = GetInt32();
                    break;
                case OpCode.Ldc_I8:
                    value = GetInt64();
                    break;
                case OpCode.Ldc_R4:
                    value = GetSingle();
                    break;
                case OpCode.Ldc_R8:
                    value = GetDouble();
                    break;
                case OpCode.Dup:
                case OpCode.Pop:
                    break;
                case OpCode.Jmp:
                case OpCode.Call:
                    value = (Method)GetMemberFromToken();
                    break;
                case OpCode.Calli:
                    value = reader.GetCalliSignature(GetInt32());
                    break;
                case OpCode.Ret: break;
                case OpCode.Br_S:
                case OpCode.Brfalse_S:
                case OpCode.Brtrue_S:
                case OpCode.Beq_S:
                case OpCode.Bge_S:
                case OpCode.Bgt_S:
                case OpCode.Ble_S:
                case OpCode.Blt_S:
                case OpCode.Bne_Un_S:
                case OpCode.Bge_Un_S:
                case OpCode.Bgt_Un_S:
                case OpCode.Ble_Un_S:
                case OpCode.Blt_Un_S:
                    value = counter + 1 + GetSByte();
                    break;
                case OpCode.Br:
                case OpCode.Brfalse:
                case OpCode.Brtrue:
                case OpCode.Beq:
                case OpCode.Bge:
                case OpCode.Bgt:
                case OpCode.Ble:
                case OpCode.Blt:
                case OpCode.Bne_Un:
                case OpCode.Bge_Un:
                case OpCode.Bgt_Un:
                case OpCode.Ble_Un:
                case OpCode.Blt_Un:
                    value = counter + 4 + GetInt32();
                    break;
                case OpCode.Switch:
                    value = ParseSwitchInstruction();
                    break;
                case OpCode.Ldind_I1:
                case OpCode.Ldind_U1:
                case OpCode.Ldind_I2:
                case OpCode.Ldind_U2:
                case OpCode.Ldind_I4:
                case OpCode.Ldind_U4:
                case OpCode.Ldind_I8:
                case OpCode.Ldind_I:
                case OpCode.Ldind_R4:
                case OpCode.Ldind_R8:
                case OpCode.Ldind_Ref:
                case OpCode.Stind_Ref:
                case OpCode.Stind_I1:
                case OpCode.Stind_I2:
                case OpCode.Stind_I4:
                case OpCode.Stind_I8:
                case OpCode.Stind_R4:
                case OpCode.Stind_R8:
                case OpCode.Add:
                case OpCode.Sub:
                case OpCode.Mul:
                case OpCode.Div:
                case OpCode.Div_Un:
                case OpCode.Rem:
                case OpCode.Rem_Un:
                case OpCode.And:
                case OpCode.Or:
                case OpCode.Xor:
                case OpCode.Shl:
                case OpCode.Shr:
                case OpCode.Shr_Un:
                case OpCode.Neg:
                case OpCode.Not:
                case OpCode.Conv_I1:
                case OpCode.Conv_I2:
                case OpCode.Conv_I4:
                case OpCode.Conv_I8:
                case OpCode.Conv_R4:
                case OpCode.Conv_R8:
                case OpCode.Conv_U4:
                case OpCode.Conv_U8:
                    break;
                case OpCode.Callvirt:
                    value = (Method)GetMemberFromToken();
                    break;
                case OpCode.Cpobj:
                case OpCode.Ldobj:
                    value = (TypeNode)GetMemberFromToken();
                    break;
                case OpCode.Ldstr:
                    value = GetStringFromToken();
                    break;
                case OpCode.Newobj:
                    value = (Method)GetMemberFromToken();
                    break;
                case OpCode.Castclass:
                case OpCode.Isinst:
                    value = (TypeNode)GetMemberFromToken();
                    break;
                case OpCode.Conv_R_Un: break;
                case OpCode.Unbox:
                    value = (TypeNode)GetMemberFromToken();
                    break;
                case OpCode.Throw: break;
                case OpCode.Ldfld:
                case OpCode.Ldflda:
                case OpCode.Stfld:
                case OpCode.Ldsfld:
                case OpCode.Ldsflda:
                case OpCode.Stsfld:
                case OpCode.Stobj:
                    value = GetMemberFromToken();
                    break;
                case OpCode.Conv_Ovf_I1_Un:
                case OpCode.Conv_Ovf_I2_Un:
                case OpCode.Conv_Ovf_I4_Un:
                case OpCode.Conv_Ovf_I8_Un:
                case OpCode.Conv_Ovf_U1_Un:
                case OpCode.Conv_Ovf_U2_Un:
                case OpCode.Conv_Ovf_U4_Un:
                case OpCode.Conv_Ovf_U8_Un:
                case OpCode.Conv_Ovf_I_Un:
                case OpCode.Conv_Ovf_U_Un:
                    break;
                case OpCode.Box:
                case OpCode.Newarr:
                    value = (TypeNode)GetMemberFromToken();
                    break;
                case OpCode.Ldlen: break;
                case OpCode.Ldelema:
                    value = (TypeNode)GetMemberFromToken();
                    break;
                case OpCode.Ldelem_I1:
                case OpCode.Ldelem_U1:
                case OpCode.Ldelem_I2:
                case OpCode.Ldelem_U2:
                case OpCode.Ldelem_I4:
                case OpCode.Ldelem_U4:
                case OpCode.Ldelem_I8:
                case OpCode.Ldelem_I:
                case OpCode.Ldelem_R4:
                case OpCode.Ldelem_R8:
                case OpCode.Ldelem_Ref:
                case OpCode.Stelem_I:
                case OpCode.Stelem_I1:
                case OpCode.Stelem_I2:
                case OpCode.Stelem_I4:
                case OpCode.Stelem_I8:
                case OpCode.Stelem_R4:
                case OpCode.Stelem_R8:
                case OpCode.Stelem_Ref:
                    break;
                case OpCode.Ldelem:
                    value = (TypeNode)GetMemberFromToken();
                    break;
                case OpCode.Stelem:
                    value = (TypeNode)GetMemberFromToken();
                    break;
                case OpCode.Unbox_Any:
                    value = GetMemberFromToken();
                    break;
                case OpCode.Conv_Ovf_I1:
                case OpCode.Conv_Ovf_U1:
                case OpCode.Conv_Ovf_I2:
                case OpCode.Conv_Ovf_U2:
                case OpCode.Conv_Ovf_I4:
                case OpCode.Conv_Ovf_U4:
                case OpCode.Conv_Ovf_I8:
                case OpCode.Conv_Ovf_U8:
                    break;
                case OpCode.Refanyval:
                    value = GetMemberFromToken();
                    break;
                case OpCode.Ckfinite: break;
                case OpCode.Mkrefany:
                    value = GetMemberFromToken();
                    break;
                case OpCode.Ldtoken:
                    value = GetMemberFromToken();
                    break;
                case OpCode.Conv_U2:
                case OpCode.Conv_U1:
                case OpCode.Conv_I:
                case OpCode.Conv_Ovf_I:
                case OpCode.Conv_Ovf_U:
                case OpCode.Add_Ovf:
                case OpCode.Add_Ovf_Un:
                case OpCode.Mul_Ovf:
                case OpCode.Mul_Ovf_Un:
                case OpCode.Sub_Ovf:
                case OpCode.Sub_Ovf_Un:
                case OpCode.Endfinally:
                    break;
                case OpCode.Leave:
                    value = counter + 4 + GetInt32();
                    break;
                case OpCode.Leave_S:
                    value = counter + 1 + GetSByte();
                    break;
                case OpCode.Stind_I:
                case OpCode.Conv_U:
                case OpCode.Prefix7:
                case OpCode.Prefix6:
                case OpCode.Prefix5:
                case OpCode.Prefix4:
                case OpCode.Prefix3:
                case OpCode.Prefix2:
                case OpCode.Prefix1:
                case OpCode.Arglist:
                case OpCode.Ceq:
                case OpCode.Cgt:
                case OpCode.Cgt_Un:
                case OpCode.Clt:
                case OpCode.Clt_Un:
                    break;
                case OpCode.Ldftn:
                case OpCode.Ldvirtftn:
                    value = GetMemberFromToken();
                    break;
                case OpCode.Ldarg:
                case OpCode.Ldarga:
                case OpCode.Starg:
                    value = Parameters(GetInt16());
                    break;
                case OpCode.Ldloc:
                case OpCode.Ldloca:
                case OpCode.Stloc:
                    value = locals[GetInt16()];
                    break;
                case OpCode.Localloc:
                case OpCode.Endfilter:
                    break;
                case OpCode.Unaligned_:
                    value = GetByte();
                    break;
                case OpCode.Volatile_:
                case OpCode.Tail_:
                    break;
                case OpCode.Initobj:
                    value = (TypeNode)GetMemberFromToken();
                    break;
                case OpCode.Constrained_:
                    value = GetMemberFromToken() as TypeNode;
                    break;
                case OpCode.Cpblk:
                case OpCode.Initblk:
                    break;
                case OpCode.Rethrow:
                    break;
                case OpCode.Sizeof:
                    value = GetMemberFromToken();
                    break;
                case OpCode.Refanytype:
                case OpCode.Readonly_:
                    break;
                default:
                    throw new InvalidMetadataException(string.Format(CultureInfo.CurrentCulture,
                        ExceptionStrings.UnknownOpCodeEncountered, opCode.ToString("x")));
            }

            var instruction = new Instruction(opCode, offset, value);
            instruction.SourceContext = sourceContext;
            return instruction;
        }
    }

    internal class ExpressionStack
    {
        internal Expression[] /*!*/
            elements = new Expression[16];

        internal int top = -1;

        private void Grow()
        {
            var n = elements.Length;
            var newElements = new Expression[n + 64];
            for (var i = 0; i < n; i++) newElements[i] = elements[i];
            elements = newElements;
        }

        internal Expression /*!*/ Pop()
        {
            if (top < 0) return new Expression(NodeType.Pop);
            var e = elements[top--];
            //^ assume e != null;
            return e;
        }

        internal void Push(Expression /*!*/ e)
        {
            if (++top >= elements.Length) Grow();
            elements[top] = e;
        }
    }

    /// <summary>
    ///     A thin wrapper for a synchronized System.Collections.Hashtable that inserts and strips WeakReference wrappers for
    ///     the values stored in the table.
    /// </summary>
    internal class SynchronizedWeakDictionary : IDictionary
    {
        private readonly Hashtable /*!*/
            Hashtable = Hashtable.Synchronized(new Hashtable());

        public void Add(object /*!*/ key, object value)
        {
            Hashtable.Add(key, new WeakReference(value));
        }

        public void Clear()
        {
            Hashtable.Clear();
        }

        public bool Contains(object /*!*/ key)
        {
            return Hashtable.Contains(key);
        }

        public IDictionaryEnumerator /*!*/ GetEnumerator()
        {
            return Hashtable.GetEnumerator();
        }

        public bool IsFixedSize => false;

        public bool IsReadOnly => false;

        public ICollection /*!*/ Keys => Hashtable.Keys;

        public void Remove(object /*!*/ key)
        {
            Hashtable.Remove(key);
        }

        public ICollection /*!*/ Values => new WeakValuesCollection(Hashtable.Values);

        public object this[object /*!*/ key]
        {
            get
            {
                var wref = (WeakReference)Hashtable[key];
                if (wref == null) return null;
                return wref.Target;
            }
            set { Hashtable[key] = new WeakReference(value); }
        }

        public void CopyTo(Array /*!*/ array, int index)
        {
            IEnumerator enumerator = GetEnumerator();
            for (var i = 0; enumerator.MoveNext(); i++)
                array.SetValue(enumerator.Current, index + i);
        }

        public int Count => Hashtable.Count;

        public bool IsSynchronized => false;

        public object /*!*/ SyncRoot => Hashtable.SyncRoot;

        IEnumerator /*!*/ IEnumerable.GetEnumerator()
        {
            return new WeakValuesEnumerator(Hashtable.GetEnumerator());
        }
    }

    internal class WeakValuesCollection : ICollection
    {
        private readonly ICollection /*!*/
            collection;

        internal WeakValuesCollection(ICollection /*!*/ collection)
        {
            this.collection = collection;
            //^ base();
        }

        public void CopyTo(Array /*!*/ array, int index)
        {
            var enumerator = GetEnumerator();
            for (var i = 0; enumerator.MoveNext(); i++)
                array.SetValue(enumerator.Current, index + i);
        }

        public int Count => collection.Count;

        public bool IsSynchronized => collection.IsSynchronized;

        public object /*!*/ SyncRoot => collection.SyncRoot;

        public IEnumerator /*!*/ GetEnumerator()
        {
            return new WeakValuesEnumerator(collection.GetEnumerator());
        }
    }

    internal class WeakValuesEnumerator : IEnumerator
    {
        private readonly IEnumerator /*!*/
            enumerator;

        internal WeakValuesEnumerator(IEnumerator /*!*/ enumerator)
        {
            this.enumerator = enumerator;
            //^ base();
        }

        public object Current
        {
            get
            {
                var curr = enumerator.Current;
                if (curr is DictionaryEntry)
                {
                    var dicEntry = (DictionaryEntry)curr;
                    curr = dicEntry.Value;
                }

                var wref = curr as WeakReference;
                if (wref != null) return wref.Target;
                return null;
            }
        }

        public bool MoveNext()
        {
            return enumerator.MoveNext();
        }

        public void Reset()
        {
            enumerator.Reset();
        }
    }
#if !ROTOR && NoWriter
  [ComVisible(true), ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
  internal interface IMetaDataImport{}
  internal class EmptyImporter : IMetaDataImport{}
#endif
#if FxCop
  class StackVariable : Local {
    internal StackVariable(TypeNode type, string name)
      : base(type) {
      this.NodeType = NodeType.StackVariable;
      this.Name = Identifier.For(name);
    }
    internal StackVariable(TypeNode type, int index)
      : base(type) {
      this.NodeType = NodeType.StackVariable;
      this.Name = Identifier.For("stack$" + index.ToString(CultureInfo.InvariantCulture));
    }
  }
#endif
}