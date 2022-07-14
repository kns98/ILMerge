// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

#if !ROTOR
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

#if CCINamespace
namespace Microsoft.Cci{
#else
namespace System.Compiler
{
#endif
#if !FxCop
    public
#endif
        class GlobalAssemblyCache
    {
        private static readonly object Lock = new object();
        private static bool FusionLoaded;
#if CodeContracts
        public static bool probeGAC = true;
#endif
        private GlobalAssemblyCache()
        {
        }

        /// <param name="codeBaseUri">Uri pointing to the assembly</param>
        public static bool Contains(Uri codeBaseUri)
        {
            if (codeBaseUri == null)
            {
                Debug.Fail("codeBaseUri == null");
                return false;
            }

            lock (Lock)
            {
                if (!FusionLoaded)
                {
                    FusionLoaded = true;
                    var systemAssembly = typeof(object).Assembly;
                    //^ assume systemAssembly != null && systemAssembly.Location != null;
                    var dir = Path.GetDirectoryName(systemAssembly.Location);
                    //^ assume dir != null;
                    LoadLibrary(Path.Combine(dir, "fusion.dll"));
                }

                IAssemblyEnum assemblyEnum;
                var rc = CreateAssemblyEnum(out assemblyEnum, null, null, ASM_CACHE.GAC, 0);
                if (rc < 0 || assemblyEnum == null) return false;
                IApplicationContext applicationContext;
                IAssemblyName currentName;
                while (assemblyEnum.GetNextAssembly(out applicationContext, out currentName, 0) == 0)
                {
                    //^ assume currentName != null;
                    var assemblyName = new AssemblyName(currentName);
                    var scheme = codeBaseUri.Scheme;
                    if (scheme != null && assemblyName.CodeBase.StartsWith(scheme))
                        try
                        {
                            var foundUri = new Uri(assemblyName.CodeBase);
                            if (codeBaseUri.Equals(foundUri)) return true;
#if !FxCop
                        }
                        catch (Exception)
                        {
#else
            }finally{
#endif
                        }
                }

                return false;
            }
        }

        /// <summary>
        ///     Returns the original location of the corresponding assembly if available, otherwise returns the location of the
        ///     shadow copy.
        ///     If the corresponding assembly is not in the GAC, null is returned.
        /// </summary>
        public static string GetLocation(AssemblyReference assemblyReference)
        {
#if CodeContracts
            if (!probeGAC) return null;
#endif
            if (assemblyReference == null)
            {
                Debug.Fail("assemblyReference == null");
                return null;
            }

            lock (Lock)
            {
                if (!FusionLoaded)
                {
                    FusionLoaded = true;
                    var systemAssembly = typeof(object).Assembly;
                    //^ assume systemAssembly != null && systemAssembly.Location != null;
                    var dir = Path.GetDirectoryName(systemAssembly.Location);
                    //^ assume dir != null;
                    LoadLibrary(Path.Combine(dir, "fusion.dll"));
                }

                IAssemblyEnum assemblyEnum;
                CreateAssemblyEnum(out assemblyEnum, null, null, ASM_CACHE.GAC, 0);
                if (assemblyEnum == null) return null;
                IApplicationContext applicationContext;
                IAssemblyName currentName;
                while (assemblyEnum.GetNextAssembly(out applicationContext, out currentName, 0) == 0)
                {
                    //^ assume currentName != null;
                    var aName = new AssemblyName(currentName);
                    if (assemblyReference.Matches(aName.Name, aName.Version, aName.Culture, aName.PublicKeyToken))
                    {
                        var codeBase = aName.CodeBase;
                        if (codeBase != null && codeBase.StartsWith("file:///"))
                            return codeBase.Substring(8);
                        return aName.GetLocation();
                    }
                }

                return null;
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("fusion.dll", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern int CreateAssemblyEnum(out IAssemblyEnum ppEnum, IApplicationContext pAppCtx,
            IAssemblyName pName, uint dwFlags, int pvReserved);

        private class ASM_CACHE
        {
            public const uint ZAP = 1;
            public const uint GAC = 2;
            public const uint DOWNLOAD = 4;

            private ASM_CACHE()
            {
            }
        }
    }

    internal class AssemblyName
    {
        private readonly IAssemblyName /*!*/
            assemblyName;

        internal AssemblyName(IAssemblyName /*!*/ assemblyName)
        {
            this.assemblyName = assemblyName;
            //^ base();
        }

        internal string /*!*/ Name =>
            //set {this.WriteString(ASM_NAME.NAME, value);}
            ReadString(ASM_NAME.NAME);

        internal Version Version
        {
            //set{
            //  if (value == null) throw new ArgumentNullException();
            //  this.WriteUInt16(ASM_NAME.MAJOR_VERSION, (ushort)value.Major);
            //  this.WriteUInt16(ASM_NAME.MINOR_VERSION, (ushort)value.Minor);
            //  this.WriteUInt16(ASM_NAME.BUILD_NUMBER, (ushort)value.Build);
            //  this.WriteUInt16(ASM_NAME.REVISION_NUMBER, (ushort)value.Revision);
            //}
            get
            {
                int major = ReadUInt16(ASM_NAME.MAJOR_VERSION);
                int minor = ReadUInt16(ASM_NAME.MINOR_VERSION);
                int build = ReadUInt16(ASM_NAME.BUILD_NUMBER);
                int revision = ReadUInt16(ASM_NAME.REVISION_NUMBER);
                return new Version(major, minor, build, revision);
            }
        }

        internal string /*!*/ Culture =>
            //set {this.WriteString(ASM_NAME.CULTURE, value);}
            ReadString(ASM_NAME.CULTURE);

        internal byte[] /*!*/ PublicKeyToken =>
            //set {this.WriteBytes(ASM_NAME.PUBLIC_KEY_TOKEN, value); }
            ReadBytes(ASM_NAME.PUBLIC_KEY_TOKEN);

        internal string StrongName
        {
            get
            {
                uint size = 0;
                assemblyName.GetDisplayName(null, ref size, (uint)AssemblyNameDisplayFlags.ALL);
                if (size == 0) return "";
                var strongName = new StringBuilder((int)size);
                assemblyName.GetDisplayName(strongName, ref size, (uint)AssemblyNameDisplayFlags.ALL);
                return strongName.ToString();
            }
        }

        internal string /*!*/ CodeBase =>
            //set {this.WriteString(ASM_NAME.CODEBASE_URL, value);}
            ReadString(ASM_NAME.CODEBASE_URL);

        public override string ToString()
        {
            return StrongName;
        }

        internal string GetLocation()
        {
            IAssemblyCache assemblyCache;
            CreateAssemblyCache(out assemblyCache, 0);
            if (assemblyCache == null) return null;
            var assemblyInfo = new ASSEMBLY_INFO();
            assemblyInfo.cbAssemblyInfo = (uint)Marshal.SizeOf(typeof(ASSEMBLY_INFO));
            assemblyCache.QueryAssemblyInfo(ASSEMBLYINFO_FLAG.VALIDATE | ASSEMBLYINFO_FLAG.GETSIZE, StrongName,
                ref assemblyInfo);
            if (assemblyInfo.cbAssemblyInfo == 0) return null;
            assemblyInfo.pszCurrentAssemblyPathBuf = new string(new char[assemblyInfo.cchBuf]);
            assemblyCache.QueryAssemblyInfo(ASSEMBLYINFO_FLAG.VALIDATE | ASSEMBLYINFO_FLAG.GETSIZE, StrongName,
                ref assemblyInfo);
            var value = assemblyInfo.pszCurrentAssemblyPathBuf;
            return value;
        }

        private string /*!*/ ReadString(uint assemblyNameProperty)
        {
            uint size = 0;
            assemblyName.GetProperty(assemblyNameProperty, IntPtr.Zero, ref size);
            if (size == 0 || size > short.MaxValue) return string.Empty;
            var ptr = Marshal.AllocHGlobal((int)size);
            assemblyName.GetProperty(assemblyNameProperty, ptr, ref size);
            var str = Marshal.PtrToStringUni(ptr);
            //^ assume str != null;
            Marshal.FreeHGlobal(ptr);
            return str;
        }

        private ushort ReadUInt16(uint assemblyNameProperty)
        {
            uint size = 0;
            assemblyName.GetProperty(assemblyNameProperty, IntPtr.Zero, ref size);
            var ptr = Marshal.AllocHGlobal((int)size);
            assemblyName.GetProperty(assemblyNameProperty, ptr, ref size);
            var value = (ushort)Marshal.ReadInt16(ptr);
            Marshal.FreeHGlobal(ptr);
            return value;
        }

        private byte[] /*!*/ ReadBytes(uint assemblyNameProperty)
        {
            uint size = 0;
            assemblyName.GetProperty(assemblyNameProperty, IntPtr.Zero, ref size);
            var ptr = Marshal.AllocHGlobal((int)size);
            assemblyName.GetProperty(assemblyNameProperty, ptr, ref size);
            var value = new byte[(int)size];
            Marshal.Copy(ptr, value, 0, (int)size);
            Marshal.FreeHGlobal(ptr);
            return value;
        }
        //private void WriteString(uint assemblyNameProperty, string/*!*/ value){
        //  IntPtr ptr = Marshal.StringToHGlobalUni(value);
        //  this.assemblyName.SetProperty(assemblyNameProperty, ptr, (uint)((value.Length + 1) * 2));
        //  Marshal.FreeHGlobal(ptr);
        //}
        //private void WriteUInt16(uint assemblyNameProperty, ushort value){
        //  IntPtr ptr = Marshal.AllocHGlobal(2);
        //  Marshal.WriteInt16(ptr, (short)value);
        //  this.assemblyName.SetProperty(assemblyNameProperty, ptr, 2);
        //  Marshal.FreeHGlobal(ptr);
        //}
        //private void WriteBytes(uint assemblyNameProperty, Byte[]/*!*/ value) {
        //  int size = value.Length;
        //  IntPtr ptr = Marshal.AllocHGlobal(size);
        //  Marshal.Copy(value, 0, ptr, size);
        //  this.assemblyName.SetProperty(assemblyNameProperty, ptr, (uint)size);
        //  Marshal.FreeHGlobal(ptr);
        //}

        [DllImport("fusion.dll", CharSet = CharSet.Auto)]
        private static extern int CreateAssemblyCache(out IAssemblyCache ppAsmCache, uint dwReserved);

        private class CREATE_ASM_NAME_OBJ_FLAGS
        {
            public const uint CANOF_PARSE_DISPLAY_NAME = 0x1;
            public const uint CANOF_SET_DEFAULT_VALUES = 0x2;

            private CREATE_ASM_NAME_OBJ_FLAGS()
            {
            }
        }

        private class ASM_NAME
        {
            public const uint PUBLIC_KEY = 0;
            public const uint PUBLIC_KEY_TOKEN = 1;
            public const uint HASH_VALUE = 2;
            public const uint NAME = 3;
            public const uint MAJOR_VERSION = 4;
            public const uint MINOR_VERSION = 5;
            public const uint BUILD_NUMBER = 6;
            public const uint REVISION_NUMBER = 7;
            public const uint CULTURE = 8;
            public const uint PROCESSOR_ID_ARRAY = 9;
            public const uint OSINFO_ARRAY = 10;
            public const uint HASH_ALGID = 11;
            public const uint ALIAS = 12;
            public const uint CODEBASE_URL = 13;
            public const uint CODEBASE_LASTMOD = 14;
            public const uint NULL_PUBLIC_KEY = 15;
            public const uint NULL_PUBLIC_KEY_TOKEN = 16;
            public const uint CUSTOM = 17;
            public const uint NULL_CUSTOM = 18;
            public const uint MVID = 19;
            public const uint _32_BIT_ONLY = 20;

            private ASM_NAME()
            {
            }
        }

        [Flags]
        internal enum AssemblyNameDisplayFlags
        {
            VERSION = 0x01,
            CULTURE = 0x02,
            PUBLIC_KEY_TOKEN = 0x04,
            PROCESSORARCHITECTURE = 0x20,
            RETARGETABLE = 0x80,
            ALL = VERSION | CULTURE | PUBLIC_KEY_TOKEN | PROCESSORARCHITECTURE | RETARGETABLE
        }

        private class ASSEMBLYINFO_FLAG
        {
            public const uint VALIDATE = 1;
            public const uint GETSIZE = 2;

            private ASSEMBLYINFO_FLAG()
            {
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ASSEMBLY_INFO
        {
            public uint cbAssemblyInfo;
            public readonly uint dwAssemblyFlags;
            public readonly ulong uliAssemblySizeInKB;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszCurrentAssemblyPathBuf;
            public readonly uint cchBuf;
        }

        [ComImport]
        [Guid("E707DCDE-D1CD-11D2-BAB9-00C04F8ECEAE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAssemblyCache
        {
            [PreserveSig]
            int UninstallAssembly(uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName,
                IntPtr pvReserved, int pulDisposition);

            [PreserveSig]
            int QueryAssemblyInfo(uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName,
                ref ASSEMBLY_INFO pAsmInfo);

            [PreserveSig]
            int CreateAssemblyCacheItem(uint dwFlags, IntPtr pvReserved, out object ppAsmItem,
                [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName);

            [PreserveSig]
            int CreateAssemblyScavenger(out object ppAsmScavenger);

            [PreserveSig]
            int InstallAssembly(uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pszManifestFilePath,
                IntPtr pvReserved);
        }
    }

    [ComImport]
    [Guid("CD193BC0-B4BC-11D2-9833-00C04FC31D2E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAssemblyName
    {
        [PreserveSig]
        int SetProperty(uint PropertyId, IntPtr pvProperty, uint cbProperty);

        [PreserveSig]
        int GetProperty(uint PropertyId, IntPtr pvProperty, ref uint pcbProperty);

        [PreserveSig]
        int Finalize();

        [PreserveSig]
        int GetDisplayName(StringBuilder szDisplayName, ref uint pccDisplayName, uint dwDisplayFlags);

        [PreserveSig]
        int BindToObject(object refIID, object pAsmBindSink, IApplicationContext pApplicationContext,
            [MarshalAs(UnmanagedType.LPWStr)] string szCodeBase, long llFlags, int pvReserved, uint cbReserved,
            out int ppv);

        [PreserveSig]
        int GetName(out uint lpcwBuffer, out int pwzName);

        [PreserveSig]
        int GetVersion(out uint pdwVersionHi, out uint pdwVersionLow);

        [PreserveSig]
        int IsEqual(IAssemblyName pName, uint dwCmpFlags);

        [PreserveSig]
        int Clone(out IAssemblyName pName);
    }

    [ComImport]
    [Guid("7C23FF90-33AF-11D3-95DA-00A024A85B51")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IApplicationContext
    {
        void SetContextNameObject(IAssemblyName pName);
        void GetContextNameObject(out IAssemblyName ppName);
        void Set([MarshalAs(UnmanagedType.LPWStr)] string szName, int pvValue, uint cbValue, uint dwFlags);
        void Get([MarshalAs(UnmanagedType.LPWStr)] string szName, out int pvValue, ref uint pcbValue, uint dwFlags);
        void GetDynamicDirectory(out int wzDynamicDir, ref uint pdwSize);
    }

    [ComImport]
    [Guid("21B8916C-F28E-11D2-A473-00C04F8EF448")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAssemblyEnum
    {
        [PreserveSig]
        int GetNextAssembly(out IApplicationContext ppAppCtx, out IAssemblyName ppName, uint dwFlags);

        [PreserveSig]
        int Reset();

        [PreserveSig]
        int Clone(out IAssemblyEnum ppEnum);
    }
}
#else
//TODO: provide a way to query ROTOR GAC
#endif