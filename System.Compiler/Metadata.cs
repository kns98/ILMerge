// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
#if FxCop
using Win32ResourceList = Microsoft.Cci.Win32ResourceCollection;
using TypeNodeList = Microsoft.Cci.TypeNodeCollection;
#endif
#if CCINamespace
using Microsoft.Cci;
#else
#endif

//^ using Microsoft.Contracts;

/* These classes help with parsing and producing PE files. They are best understood in conjunction with the ECMA 335 Specification
 * (Common Language Infrastructure), particularly Partition II. Also see "Inside Microsoft .NET IL Assembler" by Serge Lidin. */

#if CCINamespace
namespace Microsoft.Cci.Metadata{
#else
namespace System.Compiler.Metadata
{
#endif
    internal struct AssemblyRow
    {
        internal int HashAlgId;
        internal int MajorVersion;
        internal int MinorVersion;
        internal int BuildNumber;
        internal int RevisionNumber;
        internal int Flags;
        internal int PublicKey;
        internal int Name;
        internal int Culture;
    }

    internal struct AssemblyRefRow
    {
        internal int MajorVersion;
        internal int MinorVersion;
        internal int BuildNumber;
        internal int RevisionNumber;
        internal int Flags;
        internal int PublicKeyOrToken;
        internal int Name;
        internal int Culture;
        internal int HashValue;
        internal AssemblyReference AssemblyReference;
    }

    internal struct ClassLayoutRow
    {
        internal int PackingSize;
        internal int ClassSize;
        internal int Parent;
    }

    internal struct ConstantRow
    {
        internal int Type;
        internal int Parent;
        internal int Value;
    }

    internal struct CustomAttributeRow
    {
        internal int Parent;
        internal int Constructor;
        internal int Value;
    }

    internal struct DeclSecurityRow
    {
        internal int Action;
        internal int Parent;
        internal int PermissionSet;
    }

    internal struct EventMapRow
    {
        internal int Parent;
        internal int EventList;
    }

    internal struct EventPtrRow
    {
        internal int Event;
    }

    internal struct EventRow
    {
        internal int Flags;
        internal int Name;
        internal int EventType;
    }

    internal struct ExportedTypeRow
    {
        internal int Flags;
        internal int TypeDefId;
        internal int TypeName;
        internal int TypeNamespace;
        internal int Implementation;
    }

    internal struct FieldRow
    {
        internal int Flags;
        internal int Name;
        internal int Signature;
        internal Field Field;
    }

    internal struct FieldLayoutRow
    {
        internal int Offset;
        internal int Field;
    }

    internal struct FieldMarshalRow
    {
        internal int Parent;
        internal int NativeType;
    }

    internal struct FieldPtrRow
    {
        internal int Field;
    }

    internal struct FieldRvaRow
    {
        internal int RVA;
        internal int Field;
        internal PESection TargetSection;
    }

    internal struct FileRow
    {
        internal int Flags;
        internal int Name;
        internal int HashValue;
    }

    internal struct GenericParamRow
    {
        internal int Number;
        internal int Flags;
        internal int Owner;
        internal int Name;
        internal Member GenericParameter;
    }

    internal struct GenericParamConstraintRow
    {
        internal int Param;
        internal int Constraint;
    }

    internal struct ImplMapRow
    {
        internal int MappingFlags;
        internal int MemberForwarded;
        internal int ImportName;
        internal int ImportScope;
    }

    internal struct InterfaceImplRow
    {
        internal int Class;
        internal int Interface;
    }

    internal struct ManifestResourceRow
    {
        internal int Offset;
        internal int Flags;
        internal int Name;
        internal int Implementation;
    }

    internal struct MemberRefRow
    {
        internal int Class;
        internal int Name;
        internal int Signature;
        internal Member Member;
        internal TypeNodeList VarargTypes;
    }

    internal struct MethodRow
    {
        internal int RVA;
        internal int ImplFlags;
        internal int Flags;
        internal int Name;
        internal int Signature;
        internal int ParamList;
        internal Method Method;
    }

    internal struct MethodImplRow
    {
        internal int Class;
        internal int MethodBody;
        internal int MethodDeclaration;
    }

    internal struct MethodPtrRow
    {
        internal int Method;
    }

    internal struct MethodSemanticsRow
    {
        internal int Semantics;
        internal int Method;
        internal int Association;
    }

    internal struct MethodSpecRow
    {
        internal int Method;
        internal int Instantiation;
        internal Method InstantiatedMethod;
    }

    internal struct ModuleRow
    {
        internal int Generation;
        internal int Name;
        internal int Mvid;
        internal int EncId;
        internal int EncBaseId;
    }

    internal struct ModuleRefRow
    {
        internal int Name;
#if FxCop
    internal ModuleNode Module;
#else
        internal Module Module;
#endif
    }

    internal struct NestedClassRow
    {
        internal int NestedClass;
        internal int EnclosingClass;
    }

    internal struct ParamRow
    {
        internal int Flags;
        internal int Sequence;
        internal int Name;
    }

    internal struct ParamPtrRow
    {
        internal int Param;
    }

    internal struct PropertyRow
    {
        internal int Flags;
        internal int Name;
        internal int Signature;
    }

    internal struct PropertyPtrRow
    {
        internal int Property;
    }

    internal struct PropertyMapRow
    {
        internal int Parent;
        internal int PropertyList;
    }

    internal struct StandAloneSigRow
    {
        internal int Signature;
    }

    internal struct TypeDefRow
    {
        internal int Flags;
        internal int Name;
        internal int Namespace;
        internal int Extends;
        internal int FieldList;
        internal int MethodList;
        internal TypeNode Type;
        internal Identifier NamespaceId;
        internal int NamespaceKey;
        internal int NameKey;
    }

    internal struct TypeRefRow
    {
        internal int ResolutionScope;
        internal int Name;
        internal int Namespace;
        internal TypeNode Type;
    }

    internal struct TypeSpecRow
    {
        internal int Signature;
        internal TypeNode Type;
    }

    [Serializable]
    public sealed class InvalidMetadataException : Exception
    {
        public InvalidMetadataException()
        {
        }

        public InvalidMetadataException(string message)
            : base(message)
        {
        }

        public InvalidMetadataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private InvalidMetadataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    internal class CLIHeader
    {
        internal int cb;
        internal DirectoryEntry codeManagerTable;
        internal int entryPointToken;
        internal DirectoryEntry exportAddressTableJumps;
        internal int flags;
        internal ushort majorRuntimeVersion;
        internal DirectoryEntry metaData;
        internal ushort minorRuntimeVersion;
        internal DirectoryEntry resources;
        internal DirectoryEntry strongNameSignature;
        internal DirectoryEntry vtableFixups;

        internal CLIHeader()
        {
            cb = 72;
            majorRuntimeVersion = 2;
            minorRuntimeVersion = 5;
            // initialization provided by runtime
            //this.flags = 0;
            //this.entryPointToken = 0;
        }
    }

    internal struct DirectoryEntry
    {
        internal int virtualAddress;
        internal int size;
    }

    internal class MetadataHeader
    {
        internal int flags;
        internal ushort majorVersion;
        internal ushort minorVersion;
        internal int reserved;
        internal int signature;
        internal StreamHeader[] streamHeaders;
        internal string versionString;
    }

    internal class NTHeader
    {
        internal int addressOfEntryPoint;
        internal int baseOfCode;
        internal int baseOfData;
        internal DirectoryEntry baseRelocationTable;
        internal DirectoryEntry boundImportTable;
        internal DirectoryEntry certificateTable;
        internal ushort characteristics;
        internal int checkSum;
        internal DirectoryEntry cliHeaderTable;
        internal DirectoryEntry copyrightTable;
        internal DirectoryEntry debugTable;
        internal DirectoryEntry delayImportTable;
        internal ushort dllCharacteristics;
        internal DirectoryEntry exceptionTable;
        internal DirectoryEntry exportTable;
        internal int fileAlignment;
        internal DirectoryEntry globalPointerTable;
        internal long imageBase;
        internal DirectoryEntry importAddressTable;
        internal DirectoryEntry importTable;
        internal DirectoryEntry loadConfigTable;
        internal int loaderFlags;
        internal ushort machine;
        internal ushort magic;
        internal ushort majorImageVersion;
        internal byte majorLinkerVersion;
        internal ushort majorOperatingSystemVersion;
        internal ushort majorSubsystemVersion;
        internal ushort minorImageVersion;
        internal byte minorLinkerVersion;
        internal ushort minorOperatingSystemVersion;
        internal ushort minorSubsystemVersion;
        internal int numberOfDataDirectories;
        internal ushort numberOfSections;
        internal int numberOfSymbols;
        internal int pointerToSymbolTable;
        internal DirectoryEntry reserved;
        internal DirectoryEntry resourceTable;
        internal int sectionAlignment;
        internal int signature;
        internal int sizeOfCode;
        internal int sizeOfHeaders;
        internal long sizeOfHeapCommit;
        internal long sizeOfHeapReserve;
        internal int sizeOfImage;
        internal int sizeOfInitializedData;
        internal ushort sizeOfOptionalHeader;
        internal long sizeOfStackCommit;
        internal long sizeOfStackReserve;
        internal int sizeOfUninitializedData;
        internal ushort subsystem;
        internal DirectoryEntry threadLocalStorageTable;
        internal int timeDateStamp;
        internal int win32VersionValue;

        internal NTHeader()
        {
            signature = 0x00004550; /* "PE\0\0" */
            machine = 0x14c;
            sizeOfOptionalHeader = 224;
            //this.characteristics = 0x0002 | 0x0004 | 0x008 | 0x0100; //executable | no COFF line nums | no COFF symbols | 32-bit machine (as required by the standard)
            characteristics = 0x0002; //executable (as required by the Linker team).
            magic = 0x10B;
            majorLinkerVersion = 6;
            baseOfCode = 0x2000;
            imageBase = 0x400000; //TODO: make this settable
            sectionAlignment = 8192;
            fileAlignment = 512;
            majorOperatingSystemVersion = 4;
            majorSubsystemVersion = 4;
            dllCharacteristics = 0x400;
            sizeOfStackReserve = 1048576;
            sizeOfStackCommit = 4096;
            sizeOfHeapReserve = 1048576;
            sizeOfHeapCommit = 4096;
            numberOfDataDirectories = 16;

            // initialization provided by runtime
            //this.numberOfSections = 0;
            //this.timeDateStamp = 0;
            //this.pointerToSymbolTable = 0;
            //this.numberOfSymbols = 0;
            //this.minorLinkerVersion = 0;
            //this.sizeOfCode = 0;
            //this.sizeOfInitializedData = 0;
            //this.sizeOfUninitializedData = 0;
            //this.addressOfEntryPoint = 0;
            //this.baseOfData = 0;
            //this.minorOperatingSystemVersion = 0;
            //this.majorImageVersion = 0;
            //this.minorImageVersion = 0;
            //this.minorSubsystemVersion = 0;
            //this.win32VersionValue = 0;
            //this.sizeOfImage = 0;
            //this.sizeOfHeaders = 0;
            //this.checkSum = 0;
            //this.subsystem = 0;
            //this.loaderFlags = 0x0;
        }
    }

    internal struct SectionHeader
    {
        internal string name;
        internal int virtualSize;
        internal int virtualAddress;
        internal int sizeOfRawData;
        internal int pointerToRawData;
        internal int pointerToRelocations;
        internal int pointerToLinenumbers;
        internal ushort numberOfRelocations;
        internal ushort numberOfLinenumbers;
        internal int characteristics;
    }

    internal class StreamHeader
    {
        internal string name;
        internal int offset;
        internal int size;
    }

    internal class TablesHeader
    {
        internal int[] countArray;
        internal byte heapSizes;
        internal byte majorVersion;
        internal long maskSorted;
        internal long maskValid;
        internal byte minorVersion;
        internal int reserved;
        internal byte rowId;
    }

    internal enum TableIndices
    {
        Module = 0x00,
        TypeRef = 0x01,
        TypeDef = 0x02,
        FieldPtr = 0x03,
        Field = 0x04,
        MethodPtr = 0x05,
        Method = 0x06,
        ParamPtr = 0x07,
        Param = 0x08,
        InterfaceImpl = 0x09,
        MemberRef = 0x0A,
        Constant = 0x0B,
        CustomAttribute = 0x0C,
        FieldMarshal = 0x0D,
        DeclSecurity = 0x0E,
        ClassLayout = 0x0F,
        FieldLayout = 0x10,
        StandAloneSig = 0x11,
        EventMap = 0x12,
        EventPtr = 0x13,
        Event = 0x14,
        PropertyMap = 0x15,
        PropertyPtr = 0x16,
        Property = 0x17,
        MethodSemantics = 0x18,
        MethodImpl = 0x19,
        ModuleRef = 0x1A,
        TypeSpec = 0x1B,
        ImplMap = 0x1C,
        FieldRva = 0x1D,
        EncLog = 0x1E,
        EncMap = 0x1F,
        Assembly = 0x20,
        AssemblyProcessor = 0x21,
        AssemblyOS = 0x22,
        AssemblyRef = 0x23,
        AssemblyRefProcessor = 0x24,
        AssemblyRefOS = 0x25,
        File = 0x26,
        ExportedType = 0x27,
        ManifestResource = 0x28,
        NestedClass = 0x29,
        GenericParam = 0x2a,
        MethodSpec = 0x2b,
        GenericParamConstraint = 0x2c,
        Count
    }

    internal enum ElementType
    {
        End = 0x00,
        Void = 0x01,
        Boolean = 0x02,
        Char = 0x03,
        Int8 = 0x04,
        UInt8 = 0x05,
        Int16 = 0x06,
        UInt16 = 0x07,
        Int32 = 0x08,
        UInt32 = 0x09,
        Int64 = 0x0a,
        UInt64 = 0x0b,
        Single = 0x0c,
        Double = 0x0d,
        String = 0x0e,
        Pointer = 0x0f,
        Reference = 0x10,
        ValueType = 0x11,
        Class = 0x12,
        TypeParameter = 0x13,
        Array = 0x14,
        GenericTypeInstance = 0x15,
        DynamicallyTypedReference = 0x16,
        IntPtr = 0x18,
        UIntPtr = 0x19,
        FunctionPointer = 0x1b,
        Object = 0x1c,
        SzArray = 0x1d,
        MethodParameter = 0x1e,
        RequiredModifier = 0x1f,
        OptionalModifier = 0x20,
        Internal = 0x21,
        Modifier = 0x40,
        Sentinel = 0x41,
        Pinned = 0x45,
        Type = 0x50,
        BoxedEnum = 0x51,
        Enum = 0x55
    }

    internal unsafe class MetadataReader : IDisposable
    {
        private readonly MemoryCursor /*!*/
            cursor;

        private AssemblyRefRow[] assemblyRefTable;

        private AssemblyRow[] assemblyTable;
        private StreamHeader blobHeap;
        private int blobRefSize;
        private ClassLayoutRow[] classLayoutTable;
        private int constantParentRefSize;
        private ConstantRow[] constantTable;
        private int customAttributeConstructorRefSize;
        private int customAttributeParentRefSize;
        private CustomAttributeRow[] customAttributeTable;
        private int declSecurityParentRefSize;
        private DeclSecurityRow[] declSecurityTable;
        internal ushort dllCharacteristics;
        internal byte[] emptyBlob;
        internal int entryPointToken;
        private EventMapRow[] eventMapTable;
        private EventPtrRow[] eventPtrTable;
        private EventRow[] eventTable;
        private ExportedTypeRow[] exportedTypeTable;
        private FieldLayoutRow[] fieldLayoutTable;
        private int fieldMarshalParentRefSize;
        private FieldMarshalRow[] fieldMarshalTable;
        private FieldPtrRow[] fieldPtrTable;
        private FieldRvaRow[] fieldRvaTable;
        private FieldRow[] fieldTable;
        internal int fileAlignment;

        private FileRow[] fileTable;

        //^ [SpecPublic]
        private StreamHeader generalStringHeap;
        private GenericParamConstraintRow[] genericParamConstraintTable;

        private GenericParamRow[] genericParamTable;

        //^ [SpecPublic]
        private StreamHeader guidHeap;
        private int guidRefSize;
        internal byte[] HashValue;

        private int hasSemanticRefSize;

        //^ [SpecPublic]
        private StreamHeader identifierStringHeap;
        private int implementationRefSize;
        private ImplMapRow[] implMapTable;
        private InterfaceImplRow[] interfaceImplTable;
        internal int linkerMajorVersion;
        internal int linkerMinorVersion;
        private ManifestResourceRow[] manifestResourceTable;
        private int mdOffset;
        private int memberForwardedRefSize;
        private int memberRefParentSize;
        private MemberRefRow[] memberRefTable;
#if !ROTOR
        private MemoryMappedFile memmap;
#endif
        internal int metadataFormatMajorVersion;
        internal int metadataFormatMinorVersion;
        private int methodDefOrRefSize;
        private MethodImplRow[] methodImplTable;
        private MethodPtrRow[] methodPtrTable;
        private MethodSemanticsRow[] methodSemanticsTable;
        private MethodSpecRow[] methodSpecTable;
        private MethodRow[] methodTable;
        internal ModuleKindFlags moduleKind;
        private ModuleRefRow[] moduleRefTable;
        private ModuleRow[] moduleTable;
        private NestedClassRow[] nestedClassTable;
        private ParamPtrRow[] paramPtrTable;
        private ParamRow[] paramTable;
        internal PEKindFlags peKind;
        private PropertyMapRow[] propertyMapTable;
        private PropertyPtrRow[] propertyPtrTable;
        private PropertyRow[] propertyTable;
        private int resolutionScopeRefSize;
        private int resourcesOffset;
        private SectionHeader[] sectionHeaders;
        private StandAloneSigRow[] standAloneSigTable;
        private int stringRefSize;
        private int[] tableOffset;
        private int[] tableRefSize;
        private StreamHeader tables;
        internal TablesHeader tablesHeader;
        private int[] tableSize;
        internal string targetRuntimeVersion;
        internal bool TrackDebugData;
        private int typeDefOrMethodDefSize;
        private int typeDefOrRefOrSpecSize;
        private TypeDefRow[] typeDefTable;
        private TypeRefRow[] typeRefTable;
        private TypeSpecRow[] typeSpecTable;
        private int win32ResourcesOffset;

#if !ROTOR
        internal MetadataReader(string path)
        {
            var memmap = this.memmap = new MemoryMappedFile(path);
            try
            {
                cursor = new MemoryCursor(memmap);
                //^ base();
                ReadHeader();
            }
            catch
            {
                Dispose();
                throw;
            }
        }
#endif

        internal MetadataReader(byte* buffer, int length)
        {
            cursor = new MemoryCursor(buffer, length);
            //^ base();
            ReadHeader();
        }

        internal AssemblyRow[] /*!*/ AssemblyTable
        {
            get
            {
                if (assemblyTable == null) ReadAssemblyTable();
                return assemblyTable;
            }
        }

        internal AssemblyRefRow[] /*!*/ AssemblyRefTable
        {
            get
            {
                if (assemblyRefTable == null) ReadAssemblyRefTable();
                return assemblyRefTable;
            }
        }

        internal ClassLayoutRow[] /*!*/ ClassLayoutTable
        {
            get
            {
                if (classLayoutTable == null) ReadClassLayoutTable();
                return classLayoutTable;
            }
        }

        internal ConstantRow[] /*!*/ ConstantTable
        {
            get
            {
                if (constantTable == null) ReadConstantTable();
                return constantTable;
            }
        }

        internal CustomAttributeRow[] /*!*/ CustomAttributeTable
        {
            get
            {
                if (customAttributeTable == null) ReadCustomAttributeTable();
                return customAttributeTable;
            }
        }

        internal DeclSecurityRow[] /*!*/ DeclSecurityTable
        {
            get
            {
                if (declSecurityTable == null) ReadDeclSecurityTable();
                return declSecurityTable;
            }
        }

        internal EventMapRow[] /*!*/ EventMapTable
        {
            get
            {
                if (eventMapTable == null) ReadEventMapTable();
                return eventMapTable;
            }
        }

        internal EventPtrRow[] /*!*/ EventPtrTable
        {
            get
            {
                if (eventPtrTable == null) ReadEventPtrTable();
                return eventPtrTable;
            }
        }

        internal EventRow[] /*!*/ EventTable
        {
            get
            {
                if (eventTable == null) ReadEventTable();
                return eventTable;
            }
        }

        internal ExportedTypeRow[] /*!*/ ExportedTypeTable
        {
            get
            {
                if (exportedTypeTable == null) ReadExportedTypeTable();
                return exportedTypeTable;
            }
        }

        internal FieldRow[] /*!*/ FieldTable
        {
            get
            {
                if (fieldTable == null) ReadFieldTable();
                return fieldTable;
            }
        }

        internal FieldLayoutRow[] /*!*/ FieldLayoutTable
        {
            get
            {
                if (fieldLayoutTable == null) ReadFieldLayoutTable();
                return fieldLayoutTable;
            }
        }

        internal FieldMarshalRow[] /*!*/ FieldMarshalTable
        {
            get
            {
                if (fieldMarshalTable == null) ReadFieldMarshalTable();
                return fieldMarshalTable;
            }
        }

        internal FieldPtrRow[] /*!*/ FieldPtrTable
        {
            get
            {
                if (fieldPtrTable == null) ReadFieldPtrTable();
                return fieldPtrTable;
            }
        }

        internal FieldRvaRow[] /*!*/ FieldRvaTable
        {
            get
            {
                if (fieldRvaTable == null) ReadFieldRvaTable();
                return fieldRvaTable;
            }
        }

        internal FileRow[] /*!*/ FileTable
        {
            get
            {
                if (fileTable == null) ReadFileTable();
                return fileTable;
            }
        }

        internal GenericParamRow[] /*!*/ GenericParamTable
        {
            get
            {
                if (genericParamTable == null) ReadGenericParamTable();
                return genericParamTable;
            }
        }

        internal GenericParamConstraintRow[] /*!*/ GenericParamConstraintTable
        {
            get
            {
                if (genericParamConstraintTable == null) ReadGenericParamConstraintTable();
                return genericParamConstraintTable;
            }
        }

        internal ImplMapRow[] /*!*/ ImplMapTable
        {
            get
            {
                if (implMapTable == null) ReadImplMapTable();
                return implMapTable;
            }
        }

        internal InterfaceImplRow[] /*!*/ InterfaceImplTable
        {
            get
            {
                if (interfaceImplTable == null) ReadInterfaceImplTable();
                return interfaceImplTable;
            }
        }

        internal ManifestResourceRow[] /*!*/ ManifestResourceTable
        {
            get
            {
                if (manifestResourceTable == null) ReadManifestResourceTable();
                return manifestResourceTable;
            }
        }

        internal MemberRefRow[] /*!*/ MemberRefTable
        {
            get
            {
                if (memberRefTable == null) ReadMemberRefTable();
                return memberRefTable;
            }
        }

        internal MethodRow[] /*!*/ MethodTable
        {
            get
            {
                if (methodTable == null) ReadMethodTable();
                return methodTable;
            }
        }

        internal MethodImplRow[] /*!*/ MethodImplTable
        {
            get
            {
                if (methodImplTable == null) ReadMethodImplTable();
                return methodImplTable;
            }
        }

        internal MethodPtrRow[] /*!*/ MethodPtrTable
        {
            get
            {
                if (methodPtrTable == null) ReadMethodPtrTable();
                return methodPtrTable;
            }
        }

        internal MethodSemanticsRow[] /*!*/ MethodSemanticsTable
        {
            get
            {
                if (methodSemanticsTable == null) ReadMethodSemanticsTable();
                return methodSemanticsTable;
            }
        }

        internal MethodSpecRow[] /*!*/ MethodSpecTable
        {
            get
            {
                if (methodSpecTable == null) ReadMethodSpecTable();
                return methodSpecTable;
            }
        }

        internal ModuleRow[] /*!*/ ModuleTable
        {
            get
            {
                if (moduleTable == null) ReadModuleTable();
                return moduleTable;
            }
        }

        internal ModuleRefRow[] /*!*/ ModuleRefTable
        {
            get
            {
                if (moduleRefTable == null) ReadModuleRefTable();
                return moduleRefTable;
            }
        }

        internal NestedClassRow[] /*!*/ NestedClassTable
        {
            get
            {
                if (nestedClassTable == null) ReadNestedClassTable();
                return nestedClassTable;
            }
        }

        internal ParamRow[] /*!*/ ParamTable
        {
            get
            {
                if (paramTable == null) ReadParamTable();
                return paramTable;
            }
        }

        internal ParamPtrRow[] /*!*/ ParamPtrTable
        {
            get
            {
                if (paramPtrTable == null) ReadParamPtrTable();
                return paramPtrTable;
            }
        }

        internal PropertyRow[] /*!*/ PropertyTable
        {
            get
            {
                if (propertyTable == null) ReadPropertyTable();
                return propertyTable;
            }
        }

        internal PropertyMapRow[] /*!*/ PropertyMapTable
        {
            get
            {
                if (propertyMapTable == null) ReadPropertyMapTable();
                return propertyMapTable;
            }
        }

        internal PropertyPtrRow[] /*!*/ PropertyPtrTable
        {
            get
            {
                if (propertyPtrTable == null) ReadPropertyPtrTable();
                return propertyPtrTable;
            }
        }

        internal StandAloneSigRow[] /*!*/ StandAloneSigTable
        {
            get
            {
                if (standAloneSigTable == null) ReadStandAloneSigTable();
                return standAloneSigTable;
            }
        }

        internal TypeDefRow[] /*!*/ TypeDefTable
        {
            get
            {
                if (typeDefTable == null) ReadTypeDefTable();
                return typeDefTable;
            }
        }

        internal TypeRefRow[] /*!*/ TypeRefTable
        {
            get
            {
                if (typeRefTable == null) ReadTypeRefTable();
                return typeRefTable;
            }
        }

        internal TypeSpecRow[] /*!*/ TypeSpecTable
        {
            get
            {
                if (typeSpecTable == null) ReadTypeSpecTable();
                return typeSpecTable;
            }
        }

        internal byte[] /*!*/ EmptyBlob
        {
            get
            {
                if (emptyBlob == null) emptyBlob = new byte[0];
                return emptyBlob;
            }
        }

        public void Dispose()
        {
#if !ROTOR
            if (memmap != null) memmap.Dispose();
            memmap = null;
#endif
            //this.cursor = null;
            sectionHeaders = null;
            identifierStringHeap = null;
            generalStringHeap = null;
            blobHeap = null;
            guidHeap = null;
            tables = null;
            tablesHeader = null;
            targetRuntimeVersion = null;
            tableSize = null;
            tableRefSize = null;
            tableOffset = null;
            HashValue = null;
        }

        internal void SetCurrentPosition(int pos)
        {
            cursor.Position = pos;
        }

        internal void AlignTo32BitBoundary()
        {
            cursor.Align(4);
        }

        internal void Skip(int bytes)
        {
            cursor.SkipByte(bytes);
        }

        internal byte[] /*!*/ GetBlob(int blobIndex)
        {
            // special case absence of blob. Index 0 denotes empty blob
            if (blobHeap == null && blobIndex == 0) return EmptyBlob;
            var c = cursor;
            c.Position = PositionOfBlob(blobIndex);
            return c.ReadBytes(c.ReadCompressedInt());
        }

        internal MemoryCursor /*!*/ GetBlobCursor(int blobIndex)
        {
            var c = cursor;
            c.Position = PositionOfBlob(blobIndex);
            c.ReadCompressedInt();
            return new MemoryCursor(c);
        }

        internal MemoryCursor /*!*/ GetBlobCursor(int blobIndex, out int blobLength)
        {
            var c = cursor;
            c.Position = PositionOfBlob(blobIndex);
            blobLength = c.ReadCompressedInt();
            return new MemoryCursor(c);
        }

        internal Guid GetGuid(int guidIndex)
            //^ requires this.guidHeap != null;
        {
            var guidOffset = guidIndex * 16;
            if (guidOffset < 16 || guidHeap.size < guidOffset)
                throw new ArgumentOutOfRangeException("guidIndex", ExceptionStrings.BadGuidHeapIndex);
            var c = cursor;
            c.Position = mdOffset + guidHeap.offset + guidOffset - 16;
            return new Guid(c.ReadBytes(16));
        }

        internal Identifier /*!*/ GetIdentifier(int stringHeapIndex)
            //^ requires this.identifierStringHeap != null;
        {
            var position = mdOffset + identifierStringHeap.offset + stringHeapIndex;
            var c = cursor;
            return Identifier.For(c.GetBuffer(), position /*, c.KeepAlive*/);
        }

        internal byte GetMethodBodyHeaderByte(int RVA)
        {
            var c = cursor;
            c.Position = RvaToOffset(RVA);
            return c.ReadByte();
        }

        internal MemoryCursor /*!*/ GetNewCursor()
        {
            return new MemoryCursor(cursor);
        }

        internal MemoryCursor /*!*/ GetNewCursor(int RVA, out PESection targetSection)
        {
            var c = new MemoryCursor(cursor);
            c.Position = RvaToOffset(RVA, out targetSection);
            return c;
        }

        internal byte GetByte()
        {
            var c = cursor;
            return c.ReadByte();
        }

        internal int GetCurrentPosition()
        {
            return cursor.Position;
        }

        internal int GetInt32()
        {
            var c = cursor;
            return c.ReadInt32();
        }

        internal short GetInt16()
        {
            var c = cursor;
            return c.ReadInt16();
        }

        internal ushort GetUInt16()
        {
            var c = cursor;
            return c.ReadUInt16();
        }

        internal int GetSignatureLength(int blobIndex)
        {
            var c = cursor;
            c.Position = PositionOfBlob(blobIndex);
            return c.ReadCompressedInt();
        }

        internal string /*!*/ GetString(int stringHeapIndex)
            //^ requires this.identifierStringHeap != null;
        {
            if (stringHeapIndex < 0 || identifierStringHeap.size <= stringHeapIndex)
                throw new ArgumentOutOfRangeException("stringHeapIndex", ExceptionStrings.BadStringHeapIndex);
            var c = cursor;
            c.Position = mdOffset + identifierStringHeap.offset + stringHeapIndex;
            return c.ReadUTF8();
        }

        internal string /*!*/ GetUserString(int stringHeapIndex)
            //^ requires this.generalStringHeap != null;
        {
            if (stringHeapIndex < 0 || generalStringHeap.size <= stringHeapIndex)
                throw new ArgumentOutOfRangeException("stringHeapIndex", ExceptionStrings.BadUserStringHeapIndex);
            var c = cursor;
            c.Position = mdOffset + generalStringHeap.offset + stringHeapIndex;
            var strLength = c.ReadCompressedInt();
            return c.ReadUTF16(strLength / 2);
        }

        internal string /*!*/ GetBlobString(int blobIndex)
        {
            var c = cursor;
            c.Position = PositionOfBlob(blobIndex);
            var blobLength = c.ReadCompressedInt();
            return c.ReadUTF16(blobLength / 2);
        }

        internal object GetValueFromBlob(int type, int blobIndex)
        {
            var c = cursor;
            c.Position = PositionOfBlob(blobIndex);
            var blobLength = c.ReadCompressedInt();
            switch ((ElementType)type)
            {
                case ElementType.Boolean: return c.ReadBoolean();
                case ElementType.Char: return (char)c.ReadUInt16();
                case ElementType.Double: return c.ReadDouble();
                case ElementType.Single: return c.ReadSingle();
                case ElementType.Int16: return c.ReadInt16();
                case ElementType.Int32: return c.ReadInt32();
                case ElementType.Int64: return c.ReadInt64();
                case ElementType.Int8: return c.ReadSByte();
                case ElementType.UInt16: return c.ReadUInt16();
                case ElementType.UInt32: return c.ReadUInt32();
                case ElementType.UInt64: return c.ReadUInt64();
                case ElementType.UInt8: return c.ReadByte();
                case ElementType.Class: return null;
                case ElementType.String: return c.ReadUTF16(blobLength / 2);
            }

            throw new InvalidMetadataException(ExceptionStrings.UnknownConstantType);
        }

        internal byte[] GetResourceData(int resourceOffset)
        {
            cursor.Position = resourcesOffset + resourceOffset;
            var length = cursor.ReadInt32();
            return cursor.ReadBytes(length);
        }

        private int PositionOfBlob(int blobIndex)
            //^ requires this.blobHeap != null;
        {
            if (blobIndex < 0 || blobHeap.size <= blobIndex)
                throw new ArgumentOutOfRangeException("blobIndex", ExceptionStrings.BadBlobHeapIndex);
            return mdOffset + blobHeap.offset + blobIndex;
        }

        private void ReadHeader()
        {
            //TODO: break up this method
            var c = cursor;
            c.Position = 0;

            ReadDOSHeader(c);
            var ntHeader = ReadNTHeader(c);
            dllCharacteristics = ntHeader.dllCharacteristics;
            linkerMajorVersion = ntHeader.majorLinkerVersion;
            linkerMinorVersion = ntHeader.minorLinkerVersion;
            fileAlignment = ntHeader.fileAlignment;
            if ((ntHeader.characteristics & 0x2000) != 0)
                moduleKind = ModuleKindFlags.DynamicallyLinkedLibrary;
            else
                moduleKind = ntHeader.subsystem == 0x3
                    ? ModuleKindFlags.ConsoleApplication
                    : ModuleKindFlags.WindowsApplication;

            int sectionCount = ntHeader.numberOfSections;
            var sectionHeaders = this.sectionHeaders = new SectionHeader[sectionCount];
            var resourceSectionIndex = -1;
            for (var i = 0; i < sectionCount; i++)
            {
                sectionHeaders[i] = ReadSectionHeader(c);
                if (sectionHeaders[i].name == ".rsrc") resourceSectionIndex = i;
            }

            if (resourceSectionIndex >= 0)
                win32ResourcesOffset = sectionHeaders[resourceSectionIndex].pointerToRawData;
            else
                win32ResourcesOffset = -1;

            var de = ntHeader.cliHeaderTable;
            var cliHeaderOffset = RvaToOffset(de.virtualAddress);
            c.Position = cliHeaderOffset;

            var cliHeader = ReadCLIHeader(c);
            entryPointToken = cliHeader.entryPointToken;
            if ((cliHeader.flags & 1) != 0)
                peKind = PEKindFlags.ILonly;
            if ((cliHeader.flags & 0x10) != 0)
                entryPointToken = 0; //Native entry point. Ignore.
            switch (ntHeader.machine)
            {
                case 0x0200:
                    peKind |= PEKindFlags.Requires64bits;
                    break;
                case 0x8664:
                    peKind |= PEKindFlags.Requires64bits | PEKindFlags.AMD;
                    break;
                default:
                    if (ntHeader.magic == 0x20B) //Optional header magic for PE32+
                    {
                        peKind |= PEKindFlags.Requires64bits;
                    }
                    else
                    {
                        if ((cliHeader.flags & 2) != 0)
                            peKind |= PEKindFlags.Requires32bits;
                        if ((cliHeader.flags & 0x00020000) != 0)
                            peKind |= PEKindFlags.Prefers32bits;
                    }

                    break;
            }

            TrackDebugData = (cliHeader.flags & 0x10000) != 0;
            if (cliHeader.resources.size > 0)
                resourcesOffset = RvaToOffset(cliHeader.resources.virtualAddress);

            var snSize = cliHeader.strongNameSignature.size;
            if (snSize > 0)
            {
                long hashOffset = RvaToOffset(cliHeader.strongNameSignature.virtualAddress);
                c.Position = (int)hashOffset;
                HashValue = c.ReadBytes(snSize);
                var zeroHash = true;
                for (var i = 0; i < snSize; i++)
                    if (HashValue[i] != 0)
                        zeroHash = false;
                if (zeroHash) HashValue = null; //partially signed assembly
            }

            long mdOffset = this.mdOffset = RvaToOffset(cliHeader.metaData.virtualAddress);
            c.Position = (int)mdOffset;
            var mdHeader = ReadMetadataHeader(c);
            targetRuntimeVersion = mdHeader.versionString;

            foreach (var sheader in mdHeader.streamHeaders) //^ assume sheader != null;
                switch (sheader.name)
                {
                    case "#Strings":
                        identifierStringHeap = sheader;
                        continue;
                    case "#US":
                        generalStringHeap = sheader;
                        continue;
                    case "#Blob":
                        blobHeap = sheader;
                        continue;
                    case "#GUID":
                        guidHeap = sheader;
                        continue;
                    case "#~":
                        tables = sheader;
                        continue;
                    case "#-":
                        tables = sheader;
                        continue;
                    default: continue;
                }

            if (tables == null) throw new InvalidMetadataException(ExceptionStrings.NoMetadataStream);
            c.Position = (int)(mdOffset + tables.offset);
            var tablesHeader = this.tablesHeader = ReadTablesHeader(c);
            metadataFormatMajorVersion = tablesHeader.majorVersion;
            metadataFormatMinorVersion = tablesHeader.minorVersion;

            var tableSize = this.tableSize = new int[(int)TableIndices.Count];
            var tableRefSize = this.tableRefSize = new int[(int)TableIndices.Count];
            var valid = tablesHeader.maskValid;
            var countArray = tablesHeader.countArray;
            //^ assume countArray != null;
            for (int i = 0, j = 0; i < (int)TableIndices.Count; i++)
            {
                if (valid % 2 == 1)
                {
                    var m = tableSize[i] = countArray[j++];
                    tableRefSize[i] = m < 0x10000 ? 2 : 4;
                }
                else
                {
                    tableRefSize[i] = 2;
                }

                valid /= 2;
            }

            var blobRefSize = this.blobRefSize = (tablesHeader.heapSizes & 0x04) == 0 ? 2 : 4;
            var constantParentRefSize = this.constantParentRefSize =
                tableSize[(int)TableIndices.Param] < 0x4000 &&
                tableSize[(int)TableIndices.Field] < 0x4000 &&
                tableSize[(int)TableIndices.Property] < 0x4000
                    ? 2
                    : 4;
            var customAttributeParentRefSize = 0;
            if (metadataFormatMajorVersion > 1 || metadataFormatMinorVersion > 0)
                customAttributeParentRefSize = this.customAttributeParentRefSize =
                    tableSize[(int)TableIndices.Method] < 0x0800 &&
                    tableSize[(int)TableIndices.Field] < 0x0800 &&
                    tableSize[(int)TableIndices.TypeRef] < 0x0800 &&
                    tableSize[(int)TableIndices.TypeDef] < 0x0800 &&
                    tableSize[(int)TableIndices.Param] < 0x0800 &&
                    tableSize[(int)TableIndices.InterfaceImpl] < 0x0800 &&
                    tableSize[(int)TableIndices.MemberRef] < 0x0800 &&
                    tableSize[(int)TableIndices.Module] < 0x0800 &&
                    tableSize[(int)TableIndices.DeclSecurity] < 0x0800 &&
                    tableSize[(int)TableIndices.Property] < 0x0800 &&
                    tableSize[(int)TableIndices.Event] < 0x0800 &&
                    tableSize[(int)TableIndices.StandAloneSig] < 0x0800 &&
                    tableSize[(int)TableIndices.ModuleRef] < 0x0800 &&
                    tableSize[(int)TableIndices.TypeSpec] < 0x0800 &&
                    tableSize[(int)TableIndices.Assembly] < 0x0800 &&
                    tableSize[(int)TableIndices.File] < 0x0800 &&
                    tableSize[(int)TableIndices.ExportedType] < 0x0800 &&
                    tableSize[(int)TableIndices.ManifestResource] < 0x0800 &&
                    tableSize[(int)TableIndices.GenericParam] < 0x0800 &&
                    tableSize[(int)TableIndices.MethodSpec] < 0x0800 &&
                    tableSize[(int)TableIndices.GenericParamConstraint] < 0x0800
                        ? 2
                        : 4;
            else
                customAttributeParentRefSize = this.customAttributeParentRefSize =
                    tableSize[(int)TableIndices.Method] < 0x0800 &&
                    tableSize[(int)TableIndices.Field] < 0x0800 &&
                    tableSize[(int)TableIndices.TypeRef] < 0x0800 &&
                    tableSize[(int)TableIndices.TypeDef] < 0x0800 &&
                    tableSize[(int)TableIndices.Param] < 0x0800 &&
                    tableSize[(int)TableIndices.InterfaceImpl] < 0x0800 &&
                    tableSize[(int)TableIndices.MemberRef] < 0x0800 &&
                    tableSize[(int)TableIndices.Module] < 0x0800 &&
                    tableSize[(int)TableIndices.DeclSecurity] < 0x0800 &&
                    tableSize[(int)TableIndices.Property] < 0x0800 &&
                    tableSize[(int)TableIndices.Event] < 0x0800 &&
                    tableSize[(int)TableIndices.StandAloneSig] < 0x0800 &&
                    tableSize[(int)TableIndices.ModuleRef] < 0x0800 &&
                    tableSize[(int)TableIndices.TypeSpec] < 0x0800 &&
                    tableSize[(int)TableIndices.Assembly] < 0x0800 &&
                    tableSize[(int)TableIndices.File] < 0x0800 &&
                    tableSize[(int)TableIndices.ExportedType] < 0x0800 &&
                    tableSize[(int)TableIndices.ManifestResource] < 0x0800
                        ? 2
                        : 4;
            var customAttributeConstructorRefSize = this.customAttributeConstructorRefSize =
                tableSize[(int)TableIndices.Method] < 0x2000 &&
                tableSize[(int)TableIndices.MemberRef] < 0x2000
                    ? 2
                    : 4;
            var declSecurityParentRefSize = this.declSecurityParentRefSize =
                tableSize[(int)TableIndices.TypeDef] < 0x4000 &&
                tableSize[(int)TableIndices.Method] < 0x4000 &&
                tableSize[(int)TableIndices.Assembly] < 0x4000
                    ? 2
                    : 4;
            var fieldMarshalParentRefSize = this.fieldMarshalParentRefSize =
                tableSize[(int)TableIndices.Field] < 0x8000 &&
                tableSize[(int)TableIndices.Param] < 0x8000
                    ? 2
                    : 4;
            var guidRefSize = this.guidRefSize = (tablesHeader.heapSizes & 0x02) == 0 ? 2 : 4;
            var hasSemanticRefSize = this.hasSemanticRefSize =
                tableSize[(int)TableIndices.Event] < 0x8000 &&
                tableSize[(int)TableIndices.Property] < 0x8000
                    ? 2
                    : 4;
            var implementationRefSize = this.implementationRefSize =
                tableSize[(int)TableIndices.File] < 0x4000 &&
                tableSize[(int)TableIndices.AssemblyRef] < 0x4000 &&
                tableSize[(int)TableIndices.ExportedType] < 0x4000
                    ? 2
                    : 4;
            var methodDefOrRefSize = this.methodDefOrRefSize =
                tableSize[(int)TableIndices.Method] < 0x8000 &&
                tableSize[(int)TableIndices.MemberRef] < 0x8000
                    ? 2
                    : 4;
            var memberRefParentSize = this.memberRefParentSize =
                tableSize[(int)TableIndices.TypeDef] < 0x2000 &&
                tableSize[(int)TableIndices.TypeRef] < 0x2000 &&
                tableSize[(int)TableIndices.ModuleRef] < 0x2000 &&
                tableSize[(int)TableIndices.Method] < 0x2000 &&
                tableSize[(int)TableIndices.TypeSpec] < 0x2000
                    ? 2
                    : 4;
            var memberForwardedRefSize = this.memberForwardedRefSize =
                tableSize[(int)TableIndices.Field] < 0x8000 &&
                tableSize[(int)TableIndices.Method] < 0x8000
                    ? 2
                    : 4;
            var typeDefOrMethodDefSize = this.typeDefOrMethodDefSize =
                tableSize[(int)TableIndices.TypeDef] < 0x8000 &&
                tableSize[(int)TableIndices.Method] < 0x8000
                    ? 2
                    : 4;
            var typeDefOrRefOrSpecSize = this.typeDefOrRefOrSpecSize =
                tableSize[(int)TableIndices.TypeDef] < 0x4000 &&
                tableSize[(int)TableIndices.TypeRef] < 0x4000 &&
                tableSize[(int)TableIndices.TypeSpec] < 0x4000
                    ? 2
                    : 4;
            var resolutionScopeRefSize = this.resolutionScopeRefSize =
                tableSize[(int)TableIndices.Module] < 0x4000 &&
                tableSize[(int)TableIndices.ModuleRef] < 0x4000 &&
                tableSize[(int)TableIndices.AssemblyRef] < 0x4000 &&
                tableSize[(int)TableIndices.TypeRef] < 0x4000
                    ? 2
                    : 4;
            var stringRefSize = this.stringRefSize = (tablesHeader.heapSizes & 0x01) == 0 ? 2 : 4;

            var tableOffset = this.tableOffset = new int[(int)TableIndices.Count];
            var offset = this.mdOffset + tables.offset + 24 + countArray.Length * 4;
            for (var i = 0; i < (int)TableIndices.Count; i++)
            {
                var m = tableSize[i];
                if (m == 0) continue;
                tableOffset[i] = offset;
                switch ((TableIndices)i)
                {
                    case TableIndices.Module:
                        offset += m * (2 + stringRefSize + 3 * guidRefSize);
                        break;
                    case TableIndices.TypeRef:
                        offset += m * (resolutionScopeRefSize + 2 * stringRefSize);
                        break;
                    case TableIndices.TypeDef:
                        offset += m * (4 + 2 * stringRefSize + typeDefOrRefOrSpecSize +
                                       tableRefSize[(int)TableIndices.Field] + tableRefSize[(int)TableIndices.Method]);
                        break;
                    case TableIndices.FieldPtr:
                        offset += m * tableRefSize[(int)TableIndices.Field];
                        break;
                    case TableIndices.Field:
                        offset += m * (2 + stringRefSize + blobRefSize);
                        break;
                    case TableIndices.MethodPtr:
                        offset += m * tableRefSize[(int)TableIndices.Method];
                        break;
                    case TableIndices.Method:
                        offset += m * (8 + stringRefSize + blobRefSize + tableRefSize[(int)TableIndices.Param]);
                        break;
                    case TableIndices.ParamPtr:
                        offset += m * tableRefSize[(int)TableIndices.Param];
                        break;
                    case TableIndices.Param:
                        offset += m * (4 + stringRefSize);
                        break;
                    case TableIndices.InterfaceImpl:
                        offset += m * (tableRefSize[(int)TableIndices.TypeDef] + typeDefOrRefOrSpecSize);
                        break;
                    case TableIndices.MemberRef:
                        offset += m * (memberRefParentSize + stringRefSize + blobRefSize);
                        break;
                    case TableIndices.Constant:
                        offset += m * (2 + constantParentRefSize + blobRefSize);
                        break;
                    case TableIndices.CustomAttribute:
                        offset += m * (customAttributeParentRefSize + customAttributeConstructorRefSize + blobRefSize);
                        break;
                    case TableIndices.FieldMarshal:
                        offset += m * (fieldMarshalParentRefSize + blobRefSize);
                        break;
                    case TableIndices.DeclSecurity:
                        offset += m * (2 + declSecurityParentRefSize + blobRefSize);
                        break;
                    case TableIndices.ClassLayout:
                        offset += m * (6 + tableRefSize[(int)TableIndices.TypeDef]);
                        break;
                    case TableIndices.FieldLayout:
                        offset += m * (4 + tableRefSize[(int)TableIndices.Field]);
                        break;
                    case TableIndices.StandAloneSig:
                        offset += m * blobRefSize;
                        break;
                    case TableIndices.EventMap:
                        offset += m * (tableRefSize[(int)TableIndices.TypeDef] + tableRefSize[(int)TableIndices.Event]);
                        break;
                    case TableIndices.EventPtr:
                        offset += m * tableRefSize[(int)TableIndices.Event];
                        break;
                    case TableIndices.Event:
                        offset += m * (2 + stringRefSize + typeDefOrRefOrSpecSize);
                        break;
                    case TableIndices.PropertyMap:
                        offset += m * (tableRefSize[(int)TableIndices.TypeDef] +
                                       tableRefSize[(int)TableIndices.Property]);
                        break;
                    case TableIndices.PropertyPtr:
                        offset += m * tableRefSize[(int)TableIndices.Property];
                        break;
                    case TableIndices.Property:
                        offset += m * (2 + stringRefSize + blobRefSize);
                        break;
                    case TableIndices.MethodSemantics:
                        offset += m * (2 + tableRefSize[(int)TableIndices.Method] + hasSemanticRefSize);
                        break;
                    case TableIndices.MethodImpl:
                        offset += m * (tableRefSize[(int)TableIndices.TypeDef] + 2 * methodDefOrRefSize);
                        break;
                    case TableIndices.ModuleRef:
                        offset += m * stringRefSize;
                        break;
                    case TableIndices.TypeSpec:
                        offset += m * blobRefSize;
                        break;
                    case TableIndices.ImplMap:
                        offset += m * (2 + memberForwardedRefSize + stringRefSize +
                                       tableRefSize[(int)TableIndices.ModuleRef]);
                        break;
                    case TableIndices.FieldRva:
                        offset += m * (4 + tableRefSize[(int)TableIndices.Field]);
                        break;
                    case TableIndices.EncLog:
                        throw new InvalidMetadataException(ExceptionStrings.ENCLogTableEncountered);
                    case TableIndices.EncMap:
                        throw new InvalidMetadataException(ExceptionStrings.ENCMapTableEncountered);
                    case TableIndices.Assembly:
                        offset += m * (16 + blobRefSize + 2 * stringRefSize);
                        break;
                    case TableIndices.AssemblyProcessor:
                        offset += m * 4;
                        break;
                    case TableIndices.AssemblyOS:
                        offset += m * 12;
                        break;
                    case TableIndices.AssemblyRef:
                        offset += m * (12 + 2 * blobRefSize + 2 * stringRefSize);
                        break;
                    case TableIndices.AssemblyRefProcessor:
                        offset += m * (4 + tableRefSize[(int)TableIndices.AssemblyRef]);
                        break;
                    case TableIndices.AssemblyRefOS:
                        offset += m * (12 + tableRefSize[(int)TableIndices.AssemblyRef]);
                        break;
                    case TableIndices.File:
                        offset += m * (4 + stringRefSize + blobRefSize);
                        break;
                    case TableIndices.ExportedType:
                        offset += m * (8 + 2 * stringRefSize + implementationRefSize);
                        break;
                    case TableIndices.ManifestResource:
                        offset += m * (8 + stringRefSize + implementationRefSize);
                        break;
                    case TableIndices.NestedClass:
                        offset += m * 2 * tableRefSize[(int)TableIndices.TypeDef];
                        break;
                    case TableIndices.GenericParam:
                        if (metadataFormatMajorVersion == 1 && metadataFormatMinorVersion == 0)
                            offset += m * (6 + typeDefOrMethodDefSize + stringRefSize + typeDefOrRefOrSpecSize);
                        else if (metadataFormatMajorVersion == 1 && metadataFormatMinorVersion == 1)
                            offset += m * (4 + typeDefOrMethodDefSize + stringRefSize + typeDefOrRefOrSpecSize);
                        else
                            offset += m * (4 + typeDefOrMethodDefSize + stringRefSize);
                        break;
                    case TableIndices.MethodSpec:
                        offset += m * (methodDefOrRefSize + blobRefSize);
                        break;
                    case TableIndices.GenericParamConstraint:
                        offset += m * (tableRefSize[(int)TableIndices.GenericParam] + typeDefOrRefOrSpecSize);
                        break;
                    default: throw new InvalidMetadataException(ExceptionStrings.UnsupportedTableEncountered);
                }
            }
        }

        internal Win32ResourceList ReadWin32Resources()
        {
            var rs = new Win32ResourceList();
            var startPos = win32ResourcesOffset;
            if (startPos < 0) return rs;
            var c = cursor;
            c.Position = startPos;
            var sizeOfTypeDirectory = ReadWin32ResourceDirectoryHeader(c);
            for (var i = 0; i < sizeOfTypeDirectory; i++)
            {
                string TypeName = null;
                var TypeID = c.ReadInt32();
                if (TypeID < 0)
                {
                    var nac = new MemoryCursor(c);
                    nac.Position = startPos + (TypeID & 0x7FFFFFFF);
                    int strLength = nac.ReadUInt16();
                    TypeName = nac.ReadUTF16(strLength);
                }

                var offset = c.ReadInt32();
                if (offset >= 0)
                {
                    rs.Add(ReadWin32ResourceDataEntry(c, startPos + offset, TypeName, TypeID, null, 0, 0));
                }
                else
                {
                    var nc = new MemoryCursor(c);
                    nc.Position = startPos + (offset & 0x7FFFFFFF);
                    var sizeOfNameDirectory = ReadWin32ResourceDirectoryHeader(nc);
                    for (var j = 0; j < sizeOfNameDirectory; j++)
                    {
                        string Name = null;
                        var ID = nc.ReadInt32();
                        if (ID < 0)
                        {
                            var nac = new MemoryCursor(c);
                            int strLength = nac.ReadUInt16();
                            Name = nac.ReadUTF16(strLength);
                        }

                        offset = nc.ReadInt32();
                        if (offset >= 0)
                        {
                            rs.Add(ReadWin32ResourceDataEntry(c, startPos + offset, TypeName, TypeID, Name, ID, 0));
                        }
                        else
                        {
                            var lc = new MemoryCursor(c);
                            lc.Position = startPos + (offset & 0x7FFFFFFF);
                            var sizeOfLanguageDirectory = ReadWin32ResourceDirectoryHeader(lc);
                            for (var k = 0; k < sizeOfLanguageDirectory; k++)
                            {
                                var LanguageID = lc.ReadInt32();
                                offset = lc.ReadInt32();
                                rs.Add(ReadWin32ResourceDataEntry(c, startPos + offset, TypeName, TypeID, Name, ID,
                                    LanguageID));
                            }
                        }
                    }
                }
            }

            return rs;
        }

        private static int ReadWin32ResourceDirectoryHeader(MemoryCursor /*!*/ c)
        {
            c.ReadInt32(); //Characteristics
            c.ReadInt32(); //TimeDate stamp
            c.ReadInt32(); //Version
            int numberOfNamedEntries = c.ReadUInt16();
            int numberOfIdEntries = c.ReadUInt16();
            return numberOfNamedEntries + numberOfIdEntries;
        }

        private Win32Resource ReadWin32ResourceDataEntry(MemoryCursor /*!*/ c, int position,
            string TypeName, int TypeID, string Name, int ID, int LanguageID)
        {
            var rsrc = new Win32Resource();
            rsrc.TypeName = TypeName;
            rsrc.TypeId = TypeID;
            rsrc.Name = Name;
            rsrc.Id = ID;
            rsrc.LanguageId = LanguageID;
            c = new MemoryCursor(c);
            c.Position = position;
            var dataRVA = c.ReadInt32();
            var dataSize = c.ReadInt32();
            rsrc.CodePage = c.ReadInt32();
            c.Position = RvaToOffset(dataRVA);
            rsrc.Data = c.ReadBytes(dataSize);
            return rsrc;
        }

        private void ReadAssemblyTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.Assembly];
            var result = assemblyTable = new AssemblyRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.Assembly];
            for (var i = 0; i < n; i++)
            {
                AssemblyRow row;
                row.HashAlgId = c.ReadInt32();
                row.MajorVersion = c.ReadUInt16();
                row.MinorVersion = c.ReadUInt16();
                row.BuildNumber = c.ReadUInt16();
                row.RevisionNumber = c.ReadUInt16();
                row.Flags = c.ReadInt32();
                row.PublicKey = c.ReadReference(blobRefSize);
                row.Name = c.ReadReference(stringRefSize);
                row.Culture = c.ReadReference(stringRefSize);
                result[i] = row;
            }
        }

        private void ReadAssemblyRefTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.AssemblyRef];
            var result = assemblyRefTable = new AssemblyRefRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.AssemblyRef];
            for (var i = 0; i < n; i++)
            {
                AssemblyRefRow row;
                row.MajorVersion = c.ReadUInt16();
                row.MinorVersion = c.ReadUInt16();
                row.BuildNumber = c.ReadUInt16();
                row.RevisionNumber = c.ReadUInt16();
                row.Flags = c.ReadInt32();
                row.PublicKeyOrToken = c.ReadReference(blobRefSize);
                row.Name = c.ReadReference(stringRefSize);
                row.Culture = c.ReadReference(stringRefSize);
                row.HashValue = c.ReadReference(blobRefSize);
                row.AssemblyReference = null;
                result[i] = row;
            }
        }

        private void ReadClassLayoutTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.ClassLayout];
            var result = classLayoutTable = new ClassLayoutRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.ClassLayout];
            for (var i = 0; i < n; i++)
            {
                ClassLayoutRow row;
                row.PackingSize = c.ReadUInt16();
                row.ClassSize = c.ReadInt32();
                row.Parent = c.ReadReference(tableRefSize[(int)TableIndices.TypeDef]);
                result[i] = row;
            }
        }

        private void ReadConstantTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.Constant];
            var result = constantTable = new ConstantRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.Constant];
            for (var i = 0; i < n; i++)
            {
                ConstantRow row;
                row.Type = c.ReadByte();
                c.ReadByte();
                row.Parent = c.ReadReference(constantParentRefSize);
                row.Value = c.ReadReference(blobRefSize);
                result[i] = row;
            }
        }

        private void ReadCustomAttributeTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.CustomAttribute];
            var result = customAttributeTable = new CustomAttributeRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.CustomAttribute];
            for (var i = 0; i < n; i++)
            {
                CustomAttributeRow row;
                row.Parent = c.ReadReference(customAttributeParentRefSize);
                row.Constructor = c.ReadReference(customAttributeConstructorRefSize);
                row.Value = c.ReadReference(blobRefSize);
                result[i] = row;
            }
        }

        private void ReadDeclSecurityTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.DeclSecurity];
            var result = declSecurityTable = new DeclSecurityRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.DeclSecurity];
            for (var i = 0; i < n; i++)
            {
                DeclSecurityRow row;
                row.Action = c.ReadUInt16();
                row.Parent = c.ReadReference(declSecurityParentRefSize);
                row.PermissionSet = c.ReadReference(blobRefSize);
                result[i] = row;
            }
        }

        private void ReadEventMapTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.EventMap];
            var result = eventMapTable = new EventMapRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.EventMap];
            for (var i = 0; i < n; i++)
            {
                EventMapRow row;
                row.Parent = c.ReadReference(tableRefSize[(int)TableIndices.TypeDef]);
                row.EventList = c.ReadReference(tableRefSize[(int)TableIndices.Event]);
                result[i] = row;
            }
        }

        private void ReadEventPtrTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.EventPtr];
            var result = eventPtrTable = new EventPtrRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.EventPtr];
            for (var i = 0; i < n; i++)
            {
                EventPtrRow row;
                row.Event = c.ReadReference(tableRefSize[(int)TableIndices.Event]);
                result[i] = row;
            }
        }

        private void ReadEventTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.Event];
            var result = eventTable = new EventRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.Event];
            for (var i = 0; i < n; i++)
            {
                EventRow row;
                row.Flags = c.ReadUInt16();
                row.Name = c.ReadReference(stringRefSize);
                row.EventType = c.ReadReference(typeDefOrRefOrSpecSize);
                result[i] = row;
            }
        }

        private void ReadExportedTypeTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.ExportedType];
            var result = exportedTypeTable = new ExportedTypeRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.ExportedType];
            for (var i = 0; i < n; i++)
            {
                ExportedTypeRow row;
                row.Flags = c.ReadInt32();
                row.TypeDefId = c.ReadInt32();
                row.TypeName = c.ReadReference(stringRefSize);
                row.TypeNamespace = c.ReadReference(stringRefSize);
                row.Implementation = c.ReadReference(implementationRefSize);
                result[i] = row;
            }
        }

        private void ReadFieldTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.Field];
            var result = fieldTable = new FieldRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.Field];
            for (var i = 0; i < n; i++)
            {
                FieldRow row;
                row.Flags = c.ReadUInt16();
                row.Name = c.ReadReference(stringRefSize);
                row.Signature = c.ReadReference(blobRefSize);
                row.Field = null;
                result[i] = row;
            }
        }

        private void ReadFieldLayoutTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.FieldLayout];
            var result = fieldLayoutTable = new FieldLayoutRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.FieldLayout];
            for (var i = 0; i < n; i++)
            {
                FieldLayoutRow row;
                row.Offset = c.ReadInt32();
                row.Field = c.ReadReference(tableRefSize[(int)TableIndices.Field]);
                result[i] = row;
            }
        }

        private void ReadFieldMarshalTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.FieldMarshal];
            var result = fieldMarshalTable = new FieldMarshalRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.FieldMarshal];
            for (var i = 0; i < n; i++)
            {
                FieldMarshalRow row;
                row.Parent = c.ReadReference(fieldMarshalParentRefSize);
                row.NativeType = c.ReadReference(blobRefSize);
                result[i] = row;
            }
        }

        private void ReadFieldPtrTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.FieldPtr];
            var result = fieldPtrTable = new FieldPtrRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.FieldPtr];
            for (var i = 0; i < n; i++)
            {
                FieldPtrRow row;
                row.Field = c.ReadReference(tableRefSize[(int)TableIndices.Field]);
                result[i] = row;
            }
        }

        private void ReadFieldRvaTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.FieldRva];
            var result = fieldRvaTable = new FieldRvaRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.FieldRva];
            for (var i = 0; i < n; i++)
            {
                FieldRvaRow row;
                row.RVA = c.ReadInt32();
                row.Field = c.ReadReference(tableRefSize[(int)TableIndices.Field]);
                row.TargetSection = 0; //Ignored on reading
                result[i] = row;
            }
        }

        private void ReadFileTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.File];
            var result = fileTable = new FileRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.File];
            for (var i = 0; i < n; i++)
            {
                FileRow row;
                row.Flags = c.ReadInt32();
                row.Name = c.ReadReference(stringRefSize);
                row.HashValue = c.ReadReference(blobRefSize);
                result[i] = row;
            }
        }

        private void ReadGenericParamTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.GenericParam];
            var result = genericParamTable = new GenericParamRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.GenericParam];
            var reallyOldGenericsFileFormat = metadataFormatMajorVersion == 1 && metadataFormatMinorVersion == 0;
            var oldGenericsFileFormat = metadataFormatMajorVersion == 1 && metadataFormatMinorVersion == 1;
            for (var i = 0; i < n; i++)
            {
                GenericParamRow row;
                row.Number = c.ReadUInt16();
                row.Flags = c.ReadUInt16();
                row.Owner = c.ReadReference(typeDefOrMethodDefSize);
                row.Name = c.ReadReference(stringRefSize);
                row.GenericParameter = null;
                if (oldGenericsFileFormat) c.ReadReference(typeDefOrRefOrSpecSize);
                if (reallyOldGenericsFileFormat) c.ReadInt16();
                result[i] = row;
            }
        }

        private void ReadGenericParamConstraintTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.GenericParamConstraint];
            var result = genericParamConstraintTable = new GenericParamConstraintRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.GenericParamConstraint];
            for (var i = 0; i < n; i++)
            {
                GenericParamConstraintRow row;
                row.Param = c.ReadReference(tableRefSize[(int)TableIndices.GenericParam]);
                row.Constraint = c.ReadReference(typeDefOrRefOrSpecSize);
                result[i] = row;
            }
        }

        private void ReadImplMapTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.ImplMap];
            var result = implMapTable = new ImplMapRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.ImplMap];
            for (var i = 0; i < n; i++)
            {
                ImplMapRow row;
                row.MappingFlags = c.ReadUInt16();
                row.MemberForwarded = c.ReadReference(memberForwardedRefSize);
                row.ImportName = c.ReadReference(stringRefSize);
                row.ImportScope = c.ReadReference(tableRefSize[(int)TableIndices.ModuleRef]);
                result[i] = row;
            }
        }

        private void ReadInterfaceImplTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.InterfaceImpl];
            var result = interfaceImplTable = new InterfaceImplRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.InterfaceImpl];
            for (var i = 0; i < n; i++)
            {
                InterfaceImplRow row;
                row.Class = c.ReadReference(tableRefSize[(int)TableIndices.TypeDef]);
                row.Interface = c.ReadReference(typeDefOrRefOrSpecSize);
                result[i] = row;
            }
        }

        private void ReadManifestResourceTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.ManifestResource];
            var result = manifestResourceTable = new ManifestResourceRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.ManifestResource];
            for (var i = 0; i < n; i++)
            {
                ManifestResourceRow row;
                row.Offset = c.ReadInt32();
                row.Flags = c.ReadInt32();
                row.Name = c.ReadReference(stringRefSize);
                row.Implementation = c.ReadReference(implementationRefSize);
                result[i] = row;
            }
        }

        private void ReadMemberRefTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.MemberRef];
            var result = memberRefTable = new MemberRefRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.MemberRef];
            for (var i = 0; i < n; i++)
            {
                MemberRefRow row;
                row.Class = c.ReadReference(memberRefParentSize);
                row.Name = c.ReadReference(stringRefSize);
                row.Signature = c.ReadReference(blobRefSize);
                row.Member = null;
                row.VarargTypes = null;
                result[i] = row;
            }
        }

        private void ReadMethodTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.Method];
            var result = methodTable = new MethodRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.Method];
            for (var i = 0; i < n; i++)
            {
                MethodRow row;
                row.RVA = c.ReadInt32();
                row.ImplFlags = c.ReadUInt16();
                row.Flags = c.ReadUInt16();
                row.Name = c.ReadReference(stringRefSize);
                row.Signature = c.ReadReference(blobRefSize);
                row.ParamList = c.ReadReference(tableRefSize[(int)TableIndices.Param]);
                row.Method = null;
                result[i] = row;
            }
        }

        private void ReadMethodImplTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.MethodImpl];
            var result = methodImplTable = new MethodImplRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.MethodImpl];
            for (var i = 0; i < n; i++)
            {
                MethodImplRow row;
                row.Class = c.ReadReference(tableRefSize[(int)TableIndices.TypeDef]);
                row.MethodBody = c.ReadReference(methodDefOrRefSize);
                row.MethodDeclaration = c.ReadReference(methodDefOrRefSize);
                result[i] = row;
            }
        }

        private void ReadMethodPtrTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.MethodPtr];
            var result = methodPtrTable = new MethodPtrRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.MethodPtr];
            for (var i = 0; i < n; i++)
            {
                MethodPtrRow row;
                row.Method = c.ReadReference(tableRefSize[(int)TableIndices.Method]);
                result[i] = row;
            }
        }

        private void ReadMethodSemanticsTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.MethodSemantics];
            var result = methodSemanticsTable = new MethodSemanticsRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.MethodSemantics];
            for (var i = 0; i < n; i++)
            {
                MethodSemanticsRow row;
                row.Semantics = c.ReadUInt16();
                row.Method = c.ReadReference(tableRefSize[(int)TableIndices.Method]);
                row.Association = c.ReadReference(hasSemanticRefSize);
                result[i] = row;
            }
        }

        private void ReadMethodSpecTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.MethodSpec];
            var result = methodSpecTable = new MethodSpecRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.MethodSpec];
            for (var i = 0; i < n; i++)
            {
                MethodSpecRow row;
                row.Method = c.ReadReference(methodDefOrRefSize);
                row.Instantiation = c.ReadReference(blobRefSize);
                row.InstantiatedMethod = null;
                result[i] = row;
            }
        }

        private void ReadModuleTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.Module];
            var result = moduleTable = new ModuleRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.Module];
            for (var i = 0; i < n; i++)
            {
                ModuleRow row;
                row.Generation = c.ReadUInt16();
                row.Name = c.ReadReference(stringRefSize);
                row.Mvid = c.ReadReference(guidRefSize);
                row.EncId = c.ReadReference(guidRefSize);
                row.EncBaseId = c.ReadReference(guidRefSize);
                result[i] = row;
            }
        }

        private void ReadModuleRefTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.ModuleRef];
            var result = moduleRefTable = new ModuleRefRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.ModuleRef];
            for (var i = 0; i < n; i++)
            {
                ModuleRefRow row;
                row.Name = c.ReadReference(stringRefSize);
                row.Module = null;
                result[i] = row;
            }
        }

        private void ReadNestedClassTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.NestedClass];
            var result = nestedClassTable = new NestedClassRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.NestedClass];
            for (var i = 0; i < n; i++)
            {
                NestedClassRow row;
                row.NestedClass = c.ReadReference(tableRefSize[(int)TableIndices.TypeDef]);
                row.EnclosingClass = c.ReadReference(tableRefSize[(int)TableIndices.TypeDef]);
                result[i] = row;
            }
        }

        private void ReadParamTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.Param];
            var result = paramTable = new ParamRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.Param];
            for (var i = 0; i < n; i++)
            {
                ParamRow row;
                row.Flags = c.ReadUInt16();
                row.Sequence = c.ReadUInt16();
                row.Name = c.ReadReference(stringRefSize);
                result[i] = row;
            }
        }

        private void ReadParamPtrTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.ParamPtr];
            var result = paramPtrTable = new ParamPtrRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.ParamPtr];
            for (var i = 0; i < n; i++)
            {
                ParamPtrRow row;
                row.Param = c.ReadReference(tableRefSize[(int)TableIndices.Param]);
                result[i] = row;
            }
        }

        private void ReadPropertyTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.Property];
            var result = propertyTable = new PropertyRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.Property];
            for (var i = 0; i < n; i++)
            {
                PropertyRow row;
                row.Flags = c.ReadUInt16();
                row.Name = c.ReadReference(stringRefSize);
                row.Signature = c.ReadReference(blobRefSize);
                result[i] = row;
            }
        }

        private void ReadPropertyMapTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.PropertyMap];
            var result = propertyMapTable = new PropertyMapRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.PropertyMap];
            for (var i = 0; i < n; i++)
            {
                PropertyMapRow row;
                row.Parent = c.ReadReference(tableRefSize[(int)TableIndices.TypeDef]);
                row.PropertyList = c.ReadReference(tableRefSize[(int)TableIndices.Property]);
                result[i] = row;
            }
        }

        private void ReadPropertyPtrTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.PropertyPtr];
            var result = propertyPtrTable = new PropertyPtrRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.PropertyPtr];
            for (var i = 0; i < n; i++)
            {
                PropertyPtrRow row;
                row.Property = c.ReadReference(tableRefSize[(int)TableIndices.Property]);
                result[i] = row;
            }
        }

        private void ReadStandAloneSigTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.StandAloneSig];
            var result = standAloneSigTable = new StandAloneSigRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.StandAloneSig];
            for (var i = 0; i < n; i++)
            {
                StandAloneSigRow row;
                row.Signature = c.ReadReference(blobRefSize);
                result[i] = row;
            }
        }

        private void ReadTypeDefTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.TypeDef];
            var result = typeDefTable = new TypeDefRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.TypeDef];
            for (var i = 0; i < n; i++)
            {
                TypeDefRow row;
                row.Flags = c.ReadInt32();
                row.Name = c.ReadReference(stringRefSize);
                row.Namespace = c.ReadReference(stringRefSize);
                row.Extends = c.ReadReference(typeDefOrRefOrSpecSize);
                row.FieldList = c.ReadReference(tableRefSize[(int)TableIndices.Field]);
                row.MethodList = c.ReadReference(tableRefSize[(int)TableIndices.Method]);
                row.Type = null;
                row.NameKey = 0;
                row.NamespaceId = null;
                row.NamespaceKey = 0;
                result[i] = row;
            }

            for (var i = 0; i < n; i++)
            {
                result[i].NameKey = GetIdentifier(result[i].Name).UniqueIdKey;
                result[i].NamespaceId = GetIdentifier(result[i].Namespace);
                //^ assume result[i].NamespaceId != null;
                result[i].NamespaceKey = result[i].NamespaceId.UniqueIdKey;
            }
        }

        private void ReadTypeRefTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.TypeRef];
            var result = typeRefTable = new TypeRefRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.TypeRef];
            for (var i = 0; i < n; i++)
            {
                TypeRefRow row;
                row.ResolutionScope = c.ReadReference(resolutionScopeRefSize);
                row.Name = c.ReadReference(stringRefSize);
                row.Namespace = c.ReadReference(stringRefSize);
                row.Type = null;
                result[i] = row;
            }
        }

        private void ReadTypeSpecTable()
            //^ requires this.tableSize != null;
            //^ requires this.tableOffset != null;
        {
            var n = tableSize[(int)TableIndices.TypeSpec];
            var result = typeSpecTable = new TypeSpecRow[n];
            if (n == 0) return;
            var c = cursor;
            c.Position = tableOffset[(int)TableIndices.TypeSpec];
            for (var i = 0; i < n; i++)
            {
                TypeSpecRow row;
                row.Signature = c.ReadReference(blobRefSize);
                row.Type = null;
                result[i] = row;
            }
        }

        internal int GetOffsetToEndOfSection(int virtualAddress)
        {
            foreach (var section in sectionHeaders)
                if (virtualAddress >= section.virtualAddress &&
                    virtualAddress < section.virtualAddress + section.sizeOfRawData)
                    return section.sizeOfRawData - (virtualAddress - section.virtualAddress);
            return -1;
        }

        internal bool NoOffsetFor(int virtualAddress)
        {
            foreach (var section in sectionHeaders)
                if (virtualAddress >= section.virtualAddress &&
                    virtualAddress < section.virtualAddress + section.sizeOfRawData)
                    return false;
            return true;
        }

        private int RvaToOffset(int virtualAddress)
        {
            foreach (var section in sectionHeaders)
                if (virtualAddress >= section.virtualAddress &&
                    virtualAddress < section.virtualAddress + section.sizeOfRawData)
                    return virtualAddress - section.virtualAddress + section.pointerToRawData;
            throw new InvalidMetadataException(string.Format(CultureInfo.CurrentCulture,
                ExceptionStrings.UnknownVirtualAddress, virtualAddress));
        }

        private int RvaToOffset(int virtualAddress, out PESection targetSection)
        {
            foreach (var section in sectionHeaders)
                if (virtualAddress >= section.virtualAddress &&
                    virtualAddress < section.virtualAddress + section.sizeOfRawData)
                {
                    if (section.name == ".tls") targetSection = PESection.TLS;
                    else if (section.name == ".sdata") targetSection = PESection.SData;
                    else targetSection = PESection.Text;
                    return virtualAddress - section.virtualAddress + section.pointerToRawData;
                }

            throw new InvalidMetadataException(string.Format(
                CultureInfo.CurrentCulture, ExceptionStrings.UnknownVirtualAddress, +virtualAddress));
        }

        private static CLIHeader /*!*/ ReadCLIHeader(MemoryCursor /*!*/ c)
        {
            var header = new CLIHeader();
            header.cb = c.Int32(0);
            c.SkipInt32(1);
            header.majorRuntimeVersion = c.UInt16(0);
            header.minorRuntimeVersion = c.UInt16(1);
            c.SkipUInt16(2);
            header.metaData = ReadDirectoryEntry(c);
            header.flags = c.Int32(0);
            header.entryPointToken = c.Int32(1);
            c.SkipInt32(2);
            header.resources = ReadDirectoryEntry(c);
            header.strongNameSignature = ReadDirectoryEntry(c);
            header.codeManagerTable = ReadDirectoryEntry(c);
            header.vtableFixups = ReadDirectoryEntry(c);
            header.exportAddressTableJumps = ReadDirectoryEntry(c);
            if (header.majorRuntimeVersion < 2)
                throw new InvalidMetadataException(ExceptionStrings.BadCLIHeader);
            return header;
        }

        private static DirectoryEntry ReadDirectoryEntry(MemoryCursor /*!*/ c)
        {
            var entry = new DirectoryEntry();
            entry.virtualAddress = c.Int32(0);
            entry.size = c.Int32(1);
            c.SkipInt32(2);
            return entry;
        }

        internal static void ReadDOSHeader(MemoryCursor /*!*/ c)
        {
            c.Position = 0;
            int magicNumber = c.UInt16(0);
            if (magicNumber != 0x5a4d) throw new InvalidMetadataException(ExceptionStrings.BadMagicNumber);
            c.Position = 0x3c;
            var ntHeaderOffset = c.Int32(0);
            c.Position = ntHeaderOffset;
        }

        private static MetadataHeader /*!*/ ReadMetadataHeader(MemoryCursor /*!*/ c)
        {
            var header = new MetadataHeader();
            header.signature = c.ReadInt32();
            if (header.signature != 0x424a5342)
                throw new InvalidMetadataException(ExceptionStrings.BadMetadataHeaderSignature);
            header.majorVersion = c.ReadUInt16();
            header.minorVersion = c.ReadUInt16();
            header.reserved = c.ReadInt32();
            var len = c.ReadInt32();
            header.versionString = c.ReadASCII(len);
            while (len++ % 4 != 0) c.ReadByte();
            header.flags = c.ReadUInt16();
            int n = c.ReadUInt16();
            var streamHeaders = header.streamHeaders = new StreamHeader[n];
            for (var i = 0; i < n; i++)
                streamHeaders[i] = ReadStreamHeader(c);
            return header;
        }

        internal static NTHeader /*!*/ ReadNTHeader(MemoryCursor /*!*/ c)
        {
            var header = new NTHeader();
            header.signature = c.ReadInt32();
            header.machine = c.ReadUInt16();
            header.numberOfSections = c.ReadUInt16();
            header.timeDateStamp = c.ReadInt32();
            header.pointerToSymbolTable = c.ReadInt32();
            header.numberOfSymbols = c.ReadInt32();
            header.sizeOfOptionalHeader = c.ReadUInt16();
            header.characteristics = c.ReadUInt16();
            header.magic = c.ReadUInt16();
            header.majorLinkerVersion = c.ReadByte();
            header.minorLinkerVersion = c.ReadByte();
            header.sizeOfCode = c.ReadInt32();
            header.sizeOfInitializedData = c.ReadInt32();
            header.sizeOfUninitializedData = c.ReadInt32();
            header.addressOfEntryPoint = c.ReadInt32();
            header.baseOfCode = c.ReadInt32();
            if (header.magic == 0x10B)
            {
                header.baseOfData = c.ReadInt32();
                header.imageBase = c.ReadInt32();
            }
            else
            {
                header.baseOfData = 0;
                header.imageBase = c.ReadInt64();
            }

            header.sectionAlignment = c.ReadInt32();
            header.fileAlignment = c.ReadInt32();
            header.majorOperatingSystemVersion = c.ReadUInt16();
            header.minorOperatingSystemVersion = c.ReadUInt16();
            header.majorImageVersion = c.ReadUInt16();
            header.minorImageVersion = c.ReadUInt16();
            header.majorSubsystemVersion = c.ReadUInt16();
            header.minorSubsystemVersion = c.ReadUInt16();
            header.win32VersionValue = c.ReadInt32();
            header.sizeOfImage = c.ReadInt32();
            header.sizeOfHeaders = c.ReadInt32();
            header.checkSum = c.ReadInt32();
            header.subsystem = c.ReadUInt16();
            header.dllCharacteristics = c.ReadUInt16();
            if (header.magic == 0x10B)
            {
                header.sizeOfStackReserve = c.ReadInt32();
                header.sizeOfStackCommit = c.ReadInt32();
                header.sizeOfHeapReserve = c.ReadInt32();
                header.sizeOfHeapCommit = c.ReadInt32();
            }
            else
            {
                header.sizeOfStackReserve = c.ReadInt64();
                header.sizeOfStackCommit = c.ReadInt64();
                header.sizeOfHeapReserve = c.ReadInt64();
                header.sizeOfHeapCommit = c.ReadInt64();
            }

            header.loaderFlags = c.ReadInt32();
            header.numberOfDataDirectories = c.ReadInt32();

            // Verify that the header signature and magic number are valid
            if (header.signature != 0x00004550 /* "PE\0\0" */)
                throw new InvalidMetadataException(ExceptionStrings.BadCOFFHeaderSignature);
            if (header.magic != 0x010B && header.magic != 0x020B)
                throw new InvalidMetadataException(ExceptionStrings.BadPEHeaderMagicNumber);

            //Read the data directories
            header.exportTable = ReadDirectoryEntry(c);
            header.importTable = ReadDirectoryEntry(c);
            header.resourceTable = ReadDirectoryEntry(c);
            header.exceptionTable = ReadDirectoryEntry(c);
            header.certificateTable = ReadDirectoryEntry(c);
            header.baseRelocationTable = ReadDirectoryEntry(c);
            header.debugTable = ReadDirectoryEntry(c);
            header.copyrightTable = ReadDirectoryEntry(c);
            header.globalPointerTable = ReadDirectoryEntry(c);
            header.threadLocalStorageTable = ReadDirectoryEntry(c);
            header.loadConfigTable = ReadDirectoryEntry(c);
            header.boundImportTable = ReadDirectoryEntry(c);
            header.importAddressTable = ReadDirectoryEntry(c);
            header.delayImportTable = ReadDirectoryEntry(c);
            header.cliHeaderTable = ReadDirectoryEntry(c);
            header.reserved = ReadDirectoryEntry(c);

            return header;
        }

        internal static SectionHeader ReadSectionHeader(MemoryCursor /*!*/ c)
        {
            var header = new SectionHeader();
            header.name = c.ReadASCII(8);
            header.virtualSize = c.Int32(0);
            header.virtualAddress = c.Int32(1);
            header.sizeOfRawData = c.Int32(2);
            header.pointerToRawData = c.Int32(3);
            header.pointerToRelocations = c.Int32(4);
            header.pointerToLinenumbers = c.Int32(5);
            c.SkipInt32(6);
            header.numberOfRelocations = c.UInt16(0);
            header.numberOfLinenumbers = c.UInt16(1);
            c.SkipInt16(2);
            header.characteristics = c.Int32(0);
            c.SkipInt32(1);
            return header;
        }

        private static StreamHeader ReadStreamHeader(MemoryCursor /*!*/ c)
        {
            var header = new StreamHeader();
            header.offset = c.ReadInt32();
            header.size = c.ReadInt32();
            header.name = c.ReadASCII();
            var n = header.name.Length + 1;
            c.Position += (4 - n % 4) % 4;
            return header;
        }

        private static TablesHeader /*!*/ ReadTablesHeader(MemoryCursor /*!*/ c)
        {
            var header = new TablesHeader();
            header.reserved = c.ReadInt32(); // Must be zero
            header.majorVersion = c.ReadByte(); // Must be one
            header.minorVersion = c.ReadByte(); // Must be zero
            header.heapSizes = c.ReadByte(); // Bits for heap sizes
            header.rowId = c.ReadByte(); // log-base-2 of largest rowId
            header.maskValid = c.ReadInt64(); // Present table counts
            header.maskSorted = c.ReadInt64(); // Sorted tables
            var n = 0;
            var mask = (ulong)header.maskValid;
            while (mask != 0)
            {
                if (mask % 2 == 1) n++;
                mask /= 2;
            }

            var countArray = header.countArray = new int[n];
            for (var i = 0; i < n; i++)
                countArray[i] = c.ReadInt32();
            return header;
        }
    }
#if !NoWriter
    internal class MetadataWriter
    {
        internal MemoryStream StringHeap;
        internal MemoryStream BlobHeap;
        internal MemoryStream UserstringHeap;
        internal MemoryStream ResourceDataHeap;
        internal MemoryStream SdataHeap;
        internal MemoryStream TlsHeap;
        internal Guid[] GuidHeap;
        internal MemoryStream MethodBodiesHeap;
        internal Win32ResourceList Win32Resources;
        internal AssemblyRow[] assemblyTable;
        internal AssemblyRefRow[] assemblyRefTable;
        internal ClassLayoutRow[] classLayoutTable;
        internal ConstantRow[] constantTable;
        internal CustomAttributeRow[] customAttributeTable;
        internal DeclSecurityRow[] declSecurityTable;
        internal EventMapRow[] eventMapTable;
        internal EventRow[] eventTable;
        internal ExportedTypeRow[] exportedTypeTable = null;
        internal FieldRow[] fieldTable;
        internal FieldLayoutRow[] fieldLayoutTable;
        internal FieldMarshalRow[] fieldMarshalTable = null;
        internal FieldRvaRow[] fieldRvaTable = null;
        internal FileRow[] fileTable;
        internal GenericParamRow[] genericParamTable;
        internal GenericParamConstraintRow[] genericParamConstraintTable;
        internal ImplMapRow[] implMapTable;
        internal InterfaceImplRow[] interfaceImplTable;
        internal ManifestResourceRow[] manifestResourceTable = null;
        internal MemberRefRow[] memberRefTable;
        internal MethodRow[] methodTable;
        internal MethodImplRow[] methodImplTable;
        internal MethodSemanticsRow[] methodSemanticsTable;
        internal MethodSpecRow[] methodSpecTable;
        internal ModuleRow[] moduleTable;
        internal ModuleRefRow[] moduleRefTable;
        internal NestedClassRow[] nestedClassTable;
        internal ParamRow[] paramTable;
        internal PropertyRow[] propertyTable;
        internal PropertyMapRow[] propertyMapTable;
        internal StandAloneSigRow[] standAloneSigTable;
        internal TypeDefRow[] typeDefTable;
        internal TypeRefRow[] typeRefTable;
        internal TypeSpecRow[] typeSpecTable;
        internal int entryPointToken;
        internal int fileAlignment;
        internal ModuleKindFlags moduleKind;
        internal ushort dllCharacteristics;
        internal PEKindFlags peKind;
        internal bool TrackDebugData;
        internal bool UseGenerics = false;
        internal byte[] PublicKey;
        internal int SignatureKeyLength;

        private int blobRefSize;
        private int constantParentRefSize;
        private int customAttributeParentRefSize;
        private int customAttributeConstructorRefSize;
        private int declSecurityParentRefSize;
        private int fieldMarshalParentRefSize;
        private int guidRefSize;
        private int hasSemanticRefSize;
        private int implementationRefSize;
        private int methodDefOrRefSize;
        private int memberRefParentSize;
        private int memberForwardedRefSize;
        private int typeDefOrMethodDefSize;
        private int typeDefOrRefOrSpecSize;
        private int resolutionScopeRefSize;
        private int stringRefSize;
#if !ROTOR
        private readonly ISymUnmanagedWriter symWriter;
#endif
        private int[] tableRefSize;
        private int[] tableSize;
        private long validMask;

#if !ROTOR
        internal MetadataWriter(ISymUnmanagedWriter symWriter)
        {
            this.symWriter = symWriter;
        }
#else
    internal MetadataWriter(){
    }
#endif

        private void SerializeMetadata(BinaryWriter /*!*/ writer, int virtualAddressBase, Fixup /*!*/ sdataFixup,
                Fixup /*!*/ tlsFixup)
            //^ requires this.MethodBodiesHeap != null;
            //^ requires this.ResourceDataHeap != null;
            //^ requires this.StringHeap != null;
            //^ requires this.UserstringHeap != null;
            //^ requires this.BlobHeap != null;
            //^ requires this.GuidHeap != null;
            //^ requires TargetPlatform.TargetRuntimeVersion != null;
        {
            var tableOffset = 0;
            tableOffset += MethodBodiesHeap.Length;
            MethodBodiesHeap.WriteTo(writer.BaseStream);
            while (tableOffset % 4 != 0)
            {
                writer.Write((byte)0);
                tableOffset++;
            }

            if (PublicKey != null && 0 < PublicKey.Length)
            {
                cliHeader.strongNameSignature.virtualAddress = virtualAddressBase + 72 + tableOffset;
                var keysize = ComputeStrongNameSignatureSize();
                cliHeader.strongNameSignature.size = keysize;
                tableOffset += keysize;
                writer.BaseStream.Position += keysize;
            }

            if (ResourceDataHeap.Length > 0)
            {
                cliHeader.resources.virtualAddress = virtualAddressBase + 72 + tableOffset;
                ResourceDataHeap.WriteTo(writer.BaseStream);
                var sizeOfResources = ResourceDataHeap.Length;
                while (sizeOfResources % 4 != 0)
                {
                    writer.Write((byte)0);
                    sizeOfResources++;
                }

                cliHeader.resources.size = sizeOfResources;
                tableOffset += sizeOfResources;
            }

            cliHeader.metaData.virtualAddress = virtualAddressBase + 72 + tableOffset;
            var startPos = writer.BaseStream.Position;
            writer.Write(0x424a5342); //Magic signature
            writer.Write((short)1); //Major version
            writer.Write((short)1); //Minor version
            writer.Write(0); //Reserved
            writer.Write(12); // version must be 12 chars
            var version = new char[12];
            var aversion = TargetPlatform.TargetRuntimeVersion.ToCharArray();
            Array.Copy(aversion, 0, version, 0, Math.Min(12, aversion.Length));
            writer.Write(version);
            writer.Write((short)0); //flags
            writer.Write((short)5); //number of streams
            var offsetFromStartOfMetadata = 108;
            writer.Write(offsetFromStartOfMetadata);
            var cbStringHeapPad = 0;
            offsetFromStartOfMetadata += StringHeap.Length;
            while (offsetFromStartOfMetadata % 4 != 0)
            {
                offsetFromStartOfMetadata++;
                cbStringHeapPad++;
            }

            writer.Write(StringHeap.Length + cbStringHeapPad);
            writer.Write(new[] { '#', 'S', 't', 'r', 'i', 'n', 'g', 's', '\0', '\0', '\0', '\0' });

            writer.Write(offsetFromStartOfMetadata);
            offsetFromStartOfMetadata += UserstringHeap.Length;
            var cbUserStringHeapPad = 0;
            while (offsetFromStartOfMetadata % 4 != 0)
            {
                offsetFromStartOfMetadata++;
                cbUserStringHeapPad++;
            }

            writer.Write(UserstringHeap.Length + cbUserStringHeapPad);
            writer.Write(new[] { '#', 'U', 'S', '\0' });

            writer.Write(offsetFromStartOfMetadata);
            writer.Write(BlobHeap.Length);
            writer.Write(new[] { '#', 'B', 'l', 'o', 'b', '\0', '\0', '\0' });
            offsetFromStartOfMetadata += BlobHeap.Length;
            while (offsetFromStartOfMetadata % 4 != 0) offsetFromStartOfMetadata++;
            writer.Write(offsetFromStartOfMetadata);
            writer.Write(GuidHeap.Length * 16);
            writer.Write(new[] { '#', 'G', 'U', 'I', 'D', '\0', '\0', '\0' });
            offsetFromStartOfMetadata += GuidHeap.Length * 16;
            writer.Write(offsetFromStartOfMetadata);
            var tabsL = TablesLength();
            writer.Write(tabsL);
            writer.Write(new[] { '#', '~', '\0', '\0' });
            StringHeap.WriteTo(writer.BaseStream);
            var p = StringHeap.Length; // +cbStringHeapPad;
            while (p % 4 != 0)
            {
                writer.Write((byte)0);
                p++;
            }

            UserstringHeap.WriteTo(writer.BaseStream);
            p = UserstringHeap.Length; // +cbUserStringHeapPad;
            while (p % 4 != 0)
            {
                writer.Write((byte)0);
                p++;
            }

            BlobHeap.WriteTo(writer.BaseStream);
            p = BlobHeap.Length;
            while (p % 4 != 0)
            {
                writer.Write((byte)0);
                p++;
            }

            for (int i = 0, n = GuidHeap.Length; i < n; i++)
                writer.Write(GuidHeap[i].ToByteArray());
            SerializeTables(writer, virtualAddressBase + 72, sdataFixup, tlsFixup);
            cliHeader.metaData.size = writer.BaseStream.Position - startPos;
        }
#if !ROTOR
        private unsafe void WriteReferenceToPDBFile(BinaryWriter /*!*/ writer, int virtualAddressBase, int fileBase)
            //^ requires this.symWriter != null;
        {
            var startPos = writer.BaseStream.Position;
            ntHeader.debugTable.virtualAddress = startPos - fileBase + virtualAddressBase;
            ntHeader.debugTable.size = 28;
            var debugDir = new ImageDebugDirectory(true);
            uint pcData = 0;
            symWriter.GetDebugInfo(ref debugDir, 0, out pcData, IntPtr.Zero);
            var data = new byte[pcData];
            fixed (byte* pb = data)
            {
                symWriter.GetDebugInfo(ref debugDir, pcData, out pcData, (IntPtr)pb);
            }

            writer.Write(debugDir.Characteristics);
            writer.Write(ntHeader.timeDateStamp);
            writer.Write((ushort)debugDir.MajorVersion);
            writer.Write((ushort)debugDir.MinorVersion);
            writer.Write(debugDir.Type);
            writer.Write(debugDir.SizeOfData);
            writer.Write(startPos + 28 - fileBase + virtualAddressBase); //AddressOfRawData
            writer.Write(startPos + 28); //PointerToRawData
            writer.Write(data);
        }
#endif
        private void SerializeTables(BinaryWriter /*!*/ writer, int mbRVAOffset, Fixup /*!*/ sdataFixup,
                Fixup /*!*/ tlsFixup)
            //^ requires this.StringHeap != null;
            //^ requires this.GuidHeap != null;
            //^ requires this.BlobHeap != null;
            //^ requires this.tableSize != null;
            //^ requires this.tableRefSize != null;
        {
            writer.Write(0); //Reserved
            if (UseGenerics)
            {
                writer.Write((byte)2);
                writer.Write((byte)0);
            }
            else
            {
                writer.Write((byte)1);
                writer.Write((byte)1);
            }

            byte heapSizes = 0;
            if (StringHeap.Length >= 0x10000) heapSizes |= 0x01;
            if (GuidHeap.Length >= 0x10000) heapSizes |= 0x02;
            if (BlobHeap.Length >= 0x10000) heapSizes |= 0x04;
            writer.Write(heapSizes);
            writer.Write((byte)0); //Reserved
            writer.Write(validMask); //Tables that are present
            if (UseGenerics)
                writer.Write(0x16003301fa00); //Tables that are sorted
            else
                writer.Write(0x02003301fa00); //Tables that are sorted
            var tableSize = this.tableSize;
            for (int i = 0, n = 0; i < (int)TableIndices.Count; i++)
                if ((n = tableSize[i]) > 0)
                    writer.Write(n);
            if (moduleTable != null) SerializeModuleTable(writer);
            if (typeRefTable != null) SerializeTypeRefTable(writer);
            if (typeDefTable != null) SerializeTypeDefTable(writer);
            if (fieldTable != null) SerializeFieldTable(writer);
            if (methodTable != null) SerializeMethodTable(writer, mbRVAOffset);
            if (paramTable != null) SerializeParamTable(writer);
            if (interfaceImplTable != null) SerializeInterfaceImplTable(writer);
            if (memberRefTable != null) SerializeMemberRefTable(writer);
            if (constantTable != null) SerializeConstantTable(writer);
            if (customAttributeTable != null) SerializeCustomAttributeTable(writer);
            if (fieldMarshalTable != null) SerializeFieldMarshalTable(writer);
            if (declSecurityTable != null) SerializeDeclSecurityTable(writer);
            if (classLayoutTable != null) SerializeClassLayoutTable(writer);
            if (fieldLayoutTable != null) SerializeFieldLayoutTable(writer);
            if (standAloneSigTable != null) SerializeStandAloneSigTable(writer);
            if (eventMapTable != null) SerializeEventMapTable(writer);
            if (eventTable != null) SerializeEventTable(writer);
            if (propertyMapTable != null) SerializePropertyMapTable(writer);
            if (propertyTable != null) SerializePropertyTable(writer);
            if (methodSemanticsTable != null) SerializeMethodSemanticsTable(writer);
            if (methodImplTable != null) SerializeMethodImplTable(writer);
            if (moduleRefTable != null) SerializeModuleRefTable(writer);
            if (typeSpecTable != null) SerializeTypeSpecTable(writer);
            if (implMapTable != null) SerializeImplMapTable(writer);
            if (fieldRvaTable != null) SerializeFieldRvaTable(writer, mbRVAOffset, sdataFixup, tlsFixup);
            if (assemblyTable != null) SerializeAssemblyTable(writer);
            if (assemblyRefTable != null) SerializeAssemblyRefTable(writer);
            if (fileTable != null) SerializeFileTable(writer);
            if (exportedTypeTable != null) SerializeExportedTypeTable(writer);
            if (manifestResourceTable != null) SerializeManifestResourceTable(writer);
            if (nestedClassTable != null) SerializeNestedClassTable(writer);
            if (genericParamTable != null) SerializeGenericParamTable(writer);
            if (methodSpecTable != null) SerializeMethodSpecTable(writer);
            if (genericParamConstraintTable != null) SerializeGenericParamConstraintTable(writer);
        }

        private void SerializeAssemblyTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.assemblyTable != null;
        {
            var n = tableSize[(int)TableIndices.Assembly];
            for (var i = 0; i < n; i++)
            {
                var row = assemblyTable[i];
                writer.Write(row.HashAlgId);
                writer.Write((short)row.MajorVersion);
                writer.Write((short)row.MinorVersion);
                writer.Write((short)row.BuildNumber);
                writer.Write((short)row.RevisionNumber);
                writer.Write(row.Flags);
                WriteReference(writer, row.PublicKey, blobRefSize);
                WriteReference(writer, row.Name, stringRefSize);
                WriteReference(writer, row.Culture, stringRefSize);
            }
        }

        private void SerializeAssemblyRefTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.assemblyRefTable != null;
        {
            var n = tableSize[(int)TableIndices.AssemblyRef];
            for (var i = 0; i < n; i++)
            {
                var row = assemblyRefTable[i];
                writer.Write((short)row.MajorVersion);
                writer.Write((short)row.MinorVersion);
                writer.Write((short)row.BuildNumber);
                writer.Write((short)row.RevisionNumber);
                writer.Write(row.Flags);
                WriteReference(writer, row.PublicKeyOrToken, blobRefSize);
                WriteReference(writer, row.Name, stringRefSize);
                WriteReference(writer, row.Culture, stringRefSize);
                WriteReference(writer, row.HashValue, blobRefSize);
            }
        }

        private void SerializeClassLayoutTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.classLayoutTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.ClassLayout];
            for (var i = 0; i < n; i++)
            {
                var row = classLayoutTable[i];
                writer.Write((short)row.PackingSize);
                writer.Write(row.ClassSize);
                WriteReference(writer, row.Parent, tableRefSize[(int)TableIndices.TypeDef]);
            }
        }

        private void SerializeConstantTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.constantTable != null;
        {
            var n = tableSize[(int)TableIndices.Constant];
            for (var i = 0; i < n; i++)
            {
                var row = constantTable[i];
                writer.Write((byte)row.Type);
                writer.Write((byte)0);
                WriteReference(writer, row.Parent, constantParentRefSize);
                WriteReference(writer, row.Value, blobRefSize);
            }
        }

        private void SerializeCustomAttributeTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.customAttributeTable != null;
        {
            var n = tableSize[(int)TableIndices.CustomAttribute];
            for (var i = 0; i < n; i++)
            {
                var row = customAttributeTable[i];
                WriteReference(writer, row.Parent, customAttributeParentRefSize);
                WriteReference(writer, row.Constructor, customAttributeConstructorRefSize);
                WriteReference(writer, row.Value, blobRefSize);
            }
        }

        private void SerializeDeclSecurityTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.declSecurityTable != null;
        {
            var n = tableSize[(int)TableIndices.DeclSecurity];
            for (var i = 0; i < n; i++)
            {
                var row = declSecurityTable[i];
                writer.Write((short)row.Action);
                WriteReference(writer, row.Parent, declSecurityParentRefSize);
                WriteReference(writer, row.PermissionSet, blobRefSize);
            }
        }

        private void SerializeEventMapTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.eventMapTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.EventMap];
            for (var i = 0; i < n; i++)
            {
                var row = eventMapTable[i];
                WriteReference(writer, row.Parent, tableRefSize[(int)TableIndices.TypeDef]);
                WriteReference(writer, row.EventList, tableRefSize[(int)TableIndices.Event]);
            }
        }

        private void SerializeEventTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.eventTable != null;
        {
            var n = tableSize[(int)TableIndices.Event];
            for (var i = 0; i < n; i++)
            {
                var row = eventTable[i];
                writer.Write((short)row.Flags);
                WriteReference(writer, row.Name, stringRefSize);
                WriteReference(writer, row.EventType, typeDefOrRefOrSpecSize);
            }
        }

        private void SerializeExportedTypeTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.exportedTypeTable != null;
        {
            var n = tableSize[(int)TableIndices.ExportedType];
            for (var i = 0; i < n; i++)
            {
                var row = exportedTypeTable[i];
                writer.Write(row.Flags);
                writer.Write(row.TypeDefId);
                WriteReference(writer, row.TypeName, stringRefSize);
                WriteReference(writer, row.TypeNamespace, stringRefSize);
                WriteReference(writer, row.Implementation, implementationRefSize);
            }
        }

        private void SerializeFieldTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.fieldTable != null;
        {
            var n = tableSize[(int)TableIndices.Field];
            for (var i = 0; i < n; i++)
            {
                var row = fieldTable[i];
                writer.Write((short)row.Flags);
                WriteReference(writer, row.Name, stringRefSize);
                WriteReference(writer, row.Signature, blobRefSize);
            }
        }

        private void SerializeFieldLayoutTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.fieldLayoutTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.FieldLayout];
            for (var i = 0; i < n; i++)
            {
                var row = fieldLayoutTable[i];
                writer.Write(row.Offset);
                WriteReference(writer, row.Field, tableRefSize[(int)TableIndices.Field]);
            }
        }

        private void SerializeFieldMarshalTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.fieldMarshalTable != null;
        {
            var n = tableSize[(int)TableIndices.FieldMarshal];
            for (var i = 0; i < n; i++)
            {
                var row = fieldMarshalTable[i];
                WriteReference(writer, row.Parent, fieldMarshalParentRefSize);
                WriteReference(writer, row.NativeType, blobRefSize);
            }
        }

        private void SerializeFieldRvaTable(BinaryWriter /*!*/ writer, int mbRVAOffset, Fixup /*!*/ sdataFixup,
                Fixup /*!*/ tlsFixup)
            //^ requires this.tableSize != null;
            //^ requires this.fieldRvaTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.FieldRva];
            for (var i = 0; i < n; i++)
            {
                var row = fieldRvaTable[i];
                switch (row.TargetSection)
                {
                    case PESection.SData:
                    case PESection.TLS:
                        var fixup = new Fixup();
                        fixup.fixupLocation = writer.BaseStream.Position;
                        fixup.addressOfNextInstruction = row.RVA;
                        if (row.TargetSection == PESection.SData)
                        {
                            sdataFixup.nextFixUp = fixup;
                            sdataFixup = fixup;
                        }
                        else
                        {
                            sdataFixup.nextFixUp = fixup;
                            sdataFixup = fixup;
                        }

                        writer.Write(0);
                        break;
                    case PESection.Text:
                        writer.Write(row.RVA + mbRVAOffset);
                        break;
                }

                WriteReference(writer, row.Field, tableRefSize[(int)TableIndices.Field]);
            }
        }

        private void SerializeFileTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.fileTable != null;
        {
            var n = tableSize[(int)TableIndices.File];
            for (var i = 0; i < n; i++)
            {
                var row = fileTable[i];
                writer.Write(row.Flags);
                WriteReference(writer, row.Name, stringRefSize);
                WriteReference(writer, row.HashValue, blobRefSize);
            }
        }

        private void SerializeGenericParamTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.genericParamTable != null;
        {
            var n = tableSize[(int)TableIndices.GenericParam];
            var reallyOldGenericsFileFormat = TargetPlatform.MajorVersion == 1 && TargetPlatform.MinorVersion == 0;
            var oldGenericsFileFormat = TargetPlatform.MajorVersion == 1 && TargetPlatform.MinorVersion == 1;
            for (var i = 0; i < n; i++)
            {
                var row = genericParamTable[i];
                writer.Write((short)row.Number);
                writer.Write((short)row.Flags);
                WriteReference(writer, row.Owner, typeDefOrMethodDefSize);
                WriteReference(writer, row.Name, stringRefSize);
                if (oldGenericsFileFormat) WriteReference(writer, 0, typeDefOrRefOrSpecSize);
                if (reallyOldGenericsFileFormat) writer.Write((short)0);
            }
        }

        private void SerializeGenericParamConstraintTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.genericParamConstraintTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.GenericParamConstraint];
            for (var i = 0; i < n; i++)
            {
                var row = genericParamConstraintTable[i];
                WriteReference(writer, row.Param, tableRefSize[(int)TableIndices.GenericParam]);
                WriteReference(writer, row.Constraint, typeDefOrRefOrSpecSize);
            }
        }

        private void SerializeImplMapTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.implMapTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.ImplMap];
            for (var i = 0; i < n; i++)
            {
                var row = implMapTable[i];
                writer.Write((short)row.MappingFlags);
                WriteReference(writer, row.MemberForwarded, memberForwardedRefSize);
                WriteReference(writer, row.ImportName, stringRefSize);
                WriteReference(writer, row.ImportScope, tableRefSize[(int)TableIndices.ModuleRef]);
            }
        }

        private void SerializeInterfaceImplTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.interfaceImplTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.InterfaceImpl];
            for (var i = 0; i < n; i++)
            {
                var row = interfaceImplTable[i];
                WriteReference(writer, row.Class, tableRefSize[(int)TableIndices.TypeDef]);
                WriteReference(writer, row.Interface, typeDefOrRefOrSpecSize);
            }
        }

        private void SerializeManifestResourceTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.manifestResourceTable != null;
        {
            var n = tableSize[(int)TableIndices.ManifestResource];
            for (var i = 0; i < n; i++)
            {
                var row = manifestResourceTable[i];
                writer.Write(row.Offset);
                writer.Write(row.Flags);
                WriteReference(writer, row.Name, stringRefSize);
                WriteReference(writer, row.Implementation, implementationRefSize);
            }
        }

        private void SerializeMemberRefTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.memberRefTable != null;
        {
            var n = tableSize[(int)TableIndices.MemberRef];
            for (var i = 0; i < n; i++)
            {
                var row = memberRefTable[i];
                WriteReference(writer, row.Class, memberRefParentSize);
                WriteReference(writer, row.Name, stringRefSize);
                WriteReference(writer, row.Signature, blobRefSize);
            }
        }

        private void SerializeMethodTable(BinaryWriter /*!*/ writer, int mbRVAOffset)
            //^ requires this.tableSize != null;
            //^ requires this.methodTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.Method];
            var pn = paramTable == null ? 1 : paramTable.Length + 1;
            for (var i = n - 1; i >= 0; i--)
            {
                var row = methodTable[i];
                if (row.ParamList != 0) pn = row.ParamList;
                else methodTable[i].ParamList = pn;
            }

            for (var i = 0; i < n; i++)
            {
                var row = methodTable[i];
                if (row.RVA < 0)
                    writer.Write(0);
                else
                    writer.Write(row.RVA + mbRVAOffset);
                writer.Write((short)row.ImplFlags);
                writer.Write((short)row.Flags);
                WriteReference(writer, row.Name, stringRefSize);
                WriteReference(writer, row.Signature, blobRefSize);
                WriteReference(writer, row.ParamList, tableRefSize[(int)TableIndices.Param]);
            }
        }

        private void SerializeMethodImplTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.methodImplTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.MethodImpl];
            for (var i = 0; i < n; i++)
            {
                var row = methodImplTable[i];
                WriteReference(writer, row.Class, tableRefSize[(int)TableIndices.TypeDef]);
                WriteReference(writer, row.MethodBody, methodDefOrRefSize);
                WriteReference(writer, row.MethodDeclaration, methodDefOrRefSize);
            }
        }

        private void SerializeMethodSemanticsTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.methodSemanticsTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.MethodSemantics];
            for (var i = 0; i < n; i++)
            {
                var row = methodSemanticsTable[i];
                writer.Write((short)row.Semantics);
                WriteReference(writer, row.Method, tableRefSize[(int)TableIndices.Method]);
                WriteReference(writer, row.Association, hasSemanticRefSize);
            }
        }

        private void SerializeMethodSpecTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.assemblyTable != null;
            //^ requires this.methodSpecTable != null;
        {
            var n = tableSize[(int)TableIndices.MethodSpec];
            for (var i = 0; i < n; i++)
            {
                var row = methodSpecTable[i];
                WriteReference(writer, row.Method, methodDefOrRefSize);
                WriteReference(writer, row.Instantiation, blobRefSize);
            }
        }

        private void SerializeModuleTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.moduleTable != null;
        {
            var n = tableSize[(int)TableIndices.Module];
            for (var i = 0; i < n; i++)
            {
                var row = moduleTable[i];
                writer.Write((short)row.Generation);
                WriteReference(writer, row.Name, stringRefSize);
                WriteReference(writer, row.Mvid, guidRefSize);
                WriteReference(writer, row.EncId, guidRefSize);
                WriteReference(writer, row.EncBaseId, guidRefSize);
            }
        }

        private void SerializeModuleRefTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.moduleRefTable != null;
        {
            var n = tableSize[(int)TableIndices.ModuleRef];
            for (var i = 0; i < n; i++)
            {
                var row = moduleRefTable[i];
                WriteReference(writer, row.Name, stringRefSize);
            }
        }

        private void SerializeNestedClassTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.nestedClassTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.NestedClass];
            for (var i = 0; i < n; i++)
            {
                var row = nestedClassTable[i];
                WriteReference(writer, row.NestedClass, tableRefSize[(int)TableIndices.TypeDef]);
                WriteReference(writer, row.EnclosingClass, tableRefSize[(int)TableIndices.TypeDef]);
            }
        }

        private void SerializeParamTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.paramTable != null;
        {
            var n = tableSize[(int)TableIndices.Param];
            for (var i = 0; i < n; i++)
            {
                var row = paramTable[i];
                writer.Write((short)row.Flags);
                writer.Write((short)row.Sequence);
                WriteReference(writer, row.Name, stringRefSize);
            }
        }

        private void SerializePropertyTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.propertyTable != null;
        {
            var n = tableSize[(int)TableIndices.Property];
            for (var i = 0; i < n; i++)
            {
                var row = propertyTable[i];
                writer.Write((short)row.Flags);
                WriteReference(writer, row.Name, stringRefSize);
                WriteReference(writer, row.Signature, blobRefSize);
            }
        }

        private void SerializePropertyMapTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.propertyMapTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.PropertyMap];
            for (var i = 0; i < n; i++)
            {
                var row = propertyMapTable[i];
                WriteReference(writer, row.Parent, tableRefSize[(int)TableIndices.TypeDef]);
                WriteReference(writer, row.PropertyList, tableRefSize[(int)TableIndices.Property]);
            }
        }

        private void SerializeStandAloneSigTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.assemblyTable != null;
            //^ requires this.standAloneSigTable != null;
        {
            var n = tableSize[(int)TableIndices.StandAloneSig];
            for (var i = 0; i < n; i++)
            {
                var row = standAloneSigTable[i];
                WriteReference(writer, row.Signature, blobRefSize);
            }
        }

        private void SerializeTypeDefTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.typeDefTable != null;
            //^ requires this.tableRefSize != null;
        {
            var n = tableSize[(int)TableIndices.TypeDef];
            var fn = fieldTable == null ? 1 : fieldTable.Length + 1;
            var mn = methodTable == null ? 1 : methodTable.Length + 1;
            for (var i = n - 1; i >= 0; i--)
            {
                var row = typeDefTable[i];
                if (row.FieldList != 0) fn = row.FieldList;
                else typeDefTable[i].FieldList = fn;
                if (row.MethodList != 0) mn = row.MethodList;
                else typeDefTable[i].MethodList = mn;
            }

            for (var i = 0; i < n; i++)
            {
                var row = typeDefTable[i];
                writer.Write(row.Flags);
                WriteReference(writer, row.Name, stringRefSize);
                WriteReference(writer, row.Namespace, stringRefSize);
                WriteReference(writer, row.Extends, typeDefOrRefOrSpecSize);
                WriteReference(writer, row.FieldList, tableRefSize[(int)TableIndices.Field]);
                WriteReference(writer, row.MethodList, tableRefSize[(int)TableIndices.Method]);
            }
        }

        private void SerializeTypeRefTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.typeRefTable != null;
        {
            var n = tableSize[(int)TableIndices.TypeRef];
            for (var i = 0; i < n; i++)
            {
                var row = typeRefTable[i];
                WriteReference(writer, row.ResolutionScope, resolutionScopeRefSize);
                WriteReference(writer, row.Name, stringRefSize);
                WriteReference(writer, row.Namespace, stringRefSize);
            }
        }

        private void SerializeTypeSpecTable(BinaryWriter /*!*/ writer)
            //^ requires this.tableSize != null;
            //^ requires this.typeSpecTable != null;
        {
            var n = tableSize[(int)TableIndices.TypeSpec];
            for (var i = 0; i < n; i++)
            {
                var row = typeSpecTable[i];
                WriteReference(writer, row.Signature, blobRefSize);
            }
        }

        private int TablesLength()
            //^ requires this.BlobHeap != null;
            //^ requires this.GuidHeap != null;
            //^ requires this.StringHeap != null;
        {
            var tableSize = this.tableSize = new int[(int)TableIndices.Count];
            var tableRefSize = this.tableRefSize = new int[(int)TableIndices.Count];
            var tableCount = 0;
            long validMask = 0;
            for (var i = 0; i < (int)TableIndices.Count; i++)
            {
                var j = 0;
                switch ((TableIndices)i)
                {
                    case TableIndices.Module:
                        if (moduleTable != null) j = moduleTable.Length;
                        break;
                    case TableIndices.TypeRef:
                        if (typeRefTable != null) j = typeRefTable.Length;
                        break;
                    case TableIndices.TypeDef:
                        if (typeDefTable != null) j = typeDefTable.Length;
                        break;
                    case TableIndices.Field:
                        if (fieldTable != null) j = fieldTable.Length;
                        break;
                    case TableIndices.Method:
                        if (methodTable != null) j = methodTable.Length;
                        break;
                    case TableIndices.Param:
                        if (paramTable != null) j = paramTable.Length;
                        break;
                    case TableIndices.InterfaceImpl:
                        if (interfaceImplTable != null) j = interfaceImplTable.Length;
                        break;
                    case TableIndices.MemberRef:
                        if (memberRefTable != null) j = memberRefTable.Length;
                        break;
                    case TableIndices.Constant:
                        if (constantTable != null) j = constantTable.Length;
                        break;
                    case TableIndices.CustomAttribute:
                        if (customAttributeTable != null) j = customAttributeTable.Length;
                        break;
                    case TableIndices.FieldMarshal:
                        if (fieldMarshalTable != null) j = fieldMarshalTable.Length;
                        break;
                    case TableIndices.DeclSecurity:
                        if (declSecurityTable != null) j = declSecurityTable.Length;
                        break;
                    case TableIndices.ClassLayout:
                        if (classLayoutTable != null) j = classLayoutTable.Length;
                        break;
                    case TableIndices.FieldLayout:
                        if (fieldLayoutTable != null) j = fieldLayoutTable.Length;
                        break;
                    case TableIndices.StandAloneSig:
                        if (standAloneSigTable != null) j = standAloneSigTable.Length;
                        break;
                    case TableIndices.EventMap:
                        if (eventMapTable != null) j = eventMapTable.Length;
                        break;
                    case TableIndices.Event:
                        if (eventTable != null) j = eventTable.Length;
                        break;
                    case TableIndices.PropertyMap:
                        if (propertyMapTable != null) j = propertyMapTable.Length;
                        break;
                    case TableIndices.Property:
                        if (propertyTable != null) j = propertyTable.Length;
                        break;
                    case TableIndices.MethodSemantics:
                        if (methodSemanticsTable != null) j = methodSemanticsTable.Length;
                        break;
                    case TableIndices.MethodImpl:
                        if (methodImplTable != null) j = methodImplTable.Length;
                        break;
                    case TableIndices.ModuleRef:
                        if (moduleRefTable != null) j = moduleRefTable.Length;
                        break;
                    case TableIndices.TypeSpec:
                        if (typeSpecTable != null) j = typeSpecTable.Length;
                        break;
                    case TableIndices.ImplMap:
                        if (implMapTable != null) j = implMapTable.Length;
                        break;
                    case TableIndices.FieldRva:
                        if (fieldRvaTable != null) j = fieldRvaTable.Length;
                        break;
                    case TableIndices.Assembly:
                        if (assemblyTable != null) j = assemblyTable.Length;
                        break;
                    case TableIndices.AssemblyRef:
                        if (assemblyRefTable != null) j = assemblyRefTable.Length;
                        break;
                    case TableIndices.File:
                        if (fileTable != null) j = fileTable.Length;
                        break;
                    case TableIndices.ExportedType:
                        if (exportedTypeTable != null) j = exportedTypeTable.Length;
                        break;
                    case TableIndices.ManifestResource:
                        if (manifestResourceTable != null) j = manifestResourceTable.Length;
                        break;
                    case TableIndices.NestedClass:
                        if (nestedClassTable != null) j = nestedClassTable.Length;
                        break;
                    case TableIndices.GenericParam:
                        if (genericParamTable != null) j = genericParamTable.Length;
                        break;
                    case TableIndices.MethodSpec:
                        if (methodSpecTable != null) j = methodSpecTable.Length;
                        break;
                    case TableIndices.GenericParamConstraint:
                        if (genericParamConstraintTable != null) j = genericParamConstraintTable.Length;
                        break;
                }

                tableSize[i] = j;
                if (j > 0)
                {
                    tableCount++;
                    validMask |= 1L << i;
                }
            }

            this.validMask = validMask;

            for (var i = 0; i < (int)TableIndices.Count; i++)
                tableRefSize[i] = tableSize[i] < 0x10000 ? 2 : 4;
            var blobRefSize = this.blobRefSize = BlobHeap.Length < 0x10000 ? 2 : 4;
            var constantParentRefSize = this.constantParentRefSize =
                tableSize[(int)TableIndices.Param] < 0x4000 &&
                tableSize[(int)TableIndices.Field] < 0x4000 &&
                tableSize[(int)TableIndices.Property] < 0x4000
                    ? 2
                    : 4;
            var customAttributeParentRefSize = this.customAttributeParentRefSize =
                tableSize[(int)TableIndices.Method] < 0x0800 &&
                tableSize[(int)TableIndices.Field] < 0x0800 &&
                tableSize[(int)TableIndices.TypeRef] < 0x0800 &&
                tableSize[(int)TableIndices.TypeDef] < 0x0800 &&
                tableSize[(int)TableIndices.Param] < 0x0800 &&
                tableSize[(int)TableIndices.InterfaceImpl] < 0x0800 &&
                tableSize[(int)TableIndices.MemberRef] < 0x0800 &&
                tableSize[(int)TableIndices.Module] < 0x0800 &&
                tableSize[(int)TableIndices.DeclSecurity] < 0x0800 &&
                tableSize[(int)TableIndices.Property] < 0x0800 &&
                tableSize[(int)TableIndices.Event] < 0x0800 &&
                tableSize[(int)TableIndices.StandAloneSig] < 0x0800 &&
                tableSize[(int)TableIndices.ModuleRef] < 0x0800 &&
                tableSize[(int)TableIndices.TypeSpec] < 0x0800 &&
                tableSize[(int)TableIndices.Assembly] < 0x0800 &&
                tableSize[(int)TableIndices.File] < 0x0800 &&
                tableSize[(int)TableIndices.ExportedType] < 0x0800 &&
                tableSize[(int)TableIndices.ManifestResource] < 0x0800 &&
                tableSize[(int)TableIndices.GenericParam] < 0x0800 &&
                tableSize[(int)TableIndices.MethodSpec] < 0x0800 &&
                tableSize[(int)TableIndices.GenericParamConstraint] < 0x0800
                    ? 2
                    : 4;
            var customAttributeConstructorRefSize = this.customAttributeConstructorRefSize =
                tableSize[(int)TableIndices.Method] < 0x2000 &&
                tableSize[(int)TableIndices.MemberRef] < 0x2000
                    ? 2
                    : 4;
            var declSecurityParentRefSize = this.declSecurityParentRefSize =
                tableSize[(int)TableIndices.TypeDef] < 0x4000 &&
                tableSize[(int)TableIndices.Method] < 0x4000 &&
                tableSize[(int)TableIndices.Assembly] < 0x4000
                    ? 2
                    : 4;
            var fieldMarshalParentRefSize = this.fieldMarshalParentRefSize =
                tableSize[(int)TableIndices.Field] < 0x8000 &&
                tableSize[(int)TableIndices.Param] < 0x8000
                    ? 2
                    : 4;
            var guidRefSize = this.guidRefSize = GuidHeap.Length < 0x10000 ? 2 : 4;
            var hasSemanticRefSize = this.hasSemanticRefSize =
                tableSize[(int)TableIndices.Event] < 0x8000 &&
                tableSize[(int)TableIndices.Property] < 0x8000
                    ? 2
                    : 4;
            var implementationRefSize = this.implementationRefSize =
                tableSize[(int)TableIndices.File] < 0x4000 &&
                tableSize[(int)TableIndices.AssemblyRef] < 0x4000 &&
                tableSize[(int)TableIndices.ExportedType] < 0x4000
                    ? 2
                    : 4;
            var methodDefOrRefSize = this.methodDefOrRefSize =
                tableSize[(int)TableIndices.Method] < 0x8000 &&
                tableSize[(int)TableIndices.MemberRef] < 0x8000
                    ? 2
                    : 4;
            var memberRefParentSize = this.memberRefParentSize =
                tableSize[(int)TableIndices.TypeDef] < 0x2000 &&
                tableSize[(int)TableIndices.TypeRef] < 0x2000 &&
                tableSize[(int)TableIndices.ModuleRef] < 0x2000 &&
                tableSize[(int)TableIndices.Method] < 0x2000 &&
                tableSize[(int)TableIndices.TypeSpec] < 0x2000
                    ? 2
                    : 4;
            var memberForwardedRefSize = this.memberForwardedRefSize =
                tableSize[(int)TableIndices.Field] < 0x8000 &&
                tableSize[(int)TableIndices.Method] < 0x8000
                    ? 2
                    : 4;
            var typeDefOrMethodDefSize = this.typeDefOrMethodDefSize =
                tableSize[(int)TableIndices.TypeDef] < 0x8000 &&
                tableSize[(int)TableIndices.Method] < 0x8000
                    ? 2
                    : 4;
            var typeDefOrRefOrSpecSize = this.typeDefOrRefOrSpecSize =
                tableSize[(int)TableIndices.TypeDef] < 0x4000 &&
                tableSize[(int)TableIndices.TypeRef] < 0x4000 &&
                tableSize[(int)TableIndices.TypeSpec] < 0x4000
                    ? 2
                    : 4;
            var resolutionScopeRefSize = this.resolutionScopeRefSize =
                tableSize[(int)TableIndices.Module] < 0x4000 &&
                tableSize[(int)TableIndices.ModuleRef] < 0x4000 &&
                tableSize[(int)TableIndices.AssemblyRef] < 0x4000 &&
                tableSize[(int)TableIndices.TypeRef] < 0x4000
                    ? 2
                    : 4;
            var stringRefSize = this.stringRefSize = StringHeap.Length < 0x10000 ? 2 : 4;

            var length = 0;
            for (var i = 0; i < (int)TableIndices.Count; i++)
            {
                var m = tableSize[i];
                if (m == 0) continue;
                switch ((TableIndices)i)
                {
                    case TableIndices.Module:
                        length += m * (2 + stringRefSize + 3 * guidRefSize);
                        break;
                    case TableIndices.TypeRef:
                        length += m * (resolutionScopeRefSize + 2 * stringRefSize);
                        break;
                    case TableIndices.TypeDef:
                        length += m * (4 + 2 * stringRefSize + typeDefOrRefOrSpecSize +
                                       tableRefSize[(int)TableIndices.Field] + tableRefSize[(int)TableIndices.Method]);
                        break;
                    case TableIndices.Field:
                        length += m * (2 + stringRefSize + blobRefSize);
                        break;
                    case TableIndices.Method:
                        length += m * (8 + stringRefSize + blobRefSize + tableRefSize[(int)TableIndices.Param]);
                        break;
                    case TableIndices.Param:
                        length += m * (4 + stringRefSize);
                        break;
                    case TableIndices.InterfaceImpl:
                        length += m * (tableRefSize[(int)TableIndices.TypeDef] + typeDefOrRefOrSpecSize);
                        break;
                    case TableIndices.MemberRef:
                        length += m * (memberRefParentSize + stringRefSize + blobRefSize);
                        break;
                    case TableIndices.Constant:
                        length += m * (2 + constantParentRefSize + blobRefSize);
                        break;
                    case TableIndices.CustomAttribute:
                        length += m * (customAttributeParentRefSize + customAttributeConstructorRefSize + blobRefSize);
                        break;
                    case TableIndices.FieldMarshal:
                        length += m * (fieldMarshalParentRefSize + blobRefSize);
                        break;
                    case TableIndices.DeclSecurity:
                        length += m * (2 + declSecurityParentRefSize + blobRefSize);
                        break;
                    case TableIndices.ClassLayout:
                        length += m * (6 + tableRefSize[(int)TableIndices.TypeDef]);
                        break;
                    case TableIndices.FieldLayout:
                        length += m * (4 + tableRefSize[(int)TableIndices.Field]);
                        break;
                    case TableIndices.StandAloneSig:
                        length += m * blobRefSize;
                        break;
                    case TableIndices.EventMap:
                        length += m * (tableRefSize[(int)TableIndices.TypeDef] + tableRefSize[(int)TableIndices.Event]);
                        break;
                    case TableIndices.Event:
                        length += m * (2 + stringRefSize + typeDefOrRefOrSpecSize);
                        break;
                    case TableIndices.PropertyMap:
                        length += m * (tableRefSize[(int)TableIndices.TypeDef] +
                                       tableRefSize[(int)TableIndices.Property]);
                        break;
                    case TableIndices.Property:
                        length += m * (2 + stringRefSize + blobRefSize);
                        break;
                    case TableIndices.MethodSemantics:
                        length += m * (2 + tableRefSize[(int)TableIndices.Method] + hasSemanticRefSize);
                        break;
                    case TableIndices.MethodImpl:
                        length += m * (tableRefSize[(int)TableIndices.TypeDef] + 2 * methodDefOrRefSize);
                        break;
                    case TableIndices.ModuleRef:
                        length += m * stringRefSize;
                        break;
                    case TableIndices.TypeSpec:
                        length += m * blobRefSize;
                        break;
                    case TableIndices.ImplMap:
                        length += m * (2 + memberForwardedRefSize + stringRefSize +
                                       tableRefSize[(int)TableIndices.ModuleRef]);
                        break;
                    case TableIndices.FieldRva:
                        length += m * (4 + tableRefSize[(int)TableIndices.Field]);
                        break;
                    case TableIndices.EncLog:
                        throw new InvalidMetadataException(ExceptionStrings.ENCLogTableEncountered);
                    case TableIndices.EncMap:
                        throw new InvalidMetadataException(ExceptionStrings.ENCMapTableEncountered);
                    case TableIndices.Assembly:
                        length += m * (16 + blobRefSize + 2 * stringRefSize);
                        break;
                    case TableIndices.AssemblyRef:
                        length += m * (12 + 2 * blobRefSize + 2 * stringRefSize);
                        break;
                    case TableIndices.File:
                        length += m * (4 + stringRefSize + blobRefSize);
                        break;
                    case TableIndices.ExportedType:
                        length += m * (8 + 2 * stringRefSize + implementationRefSize);
                        break;
                    case TableIndices.ManifestResource:
                        length += m * (8 + stringRefSize + implementationRefSize);
                        break;
                    case TableIndices.NestedClass:
                        length += m * 2 * tableRefSize[(int)TableIndices.TypeDef];
                        break;
                    case TableIndices.GenericParam:
                        if (TargetPlatform.MajorVersion == 1 && TargetPlatform.MinorVersion == 0)
                            length += m * (6 + typeDefOrMethodDefSize + stringRefSize + typeDefOrRefOrSpecSize);
                        else if (TargetPlatform.MajorVersion == 1 && TargetPlatform.MinorVersion == 1)
                            length += m * (4 + typeDefOrMethodDefSize + stringRefSize + typeDefOrRefOrSpecSize);
                        else
                            length += m * (4 + typeDefOrMethodDefSize + stringRefSize);
                        break;
                    case TableIndices.MethodSpec:
                        length += m * (methodDefOrRefSize + blobRefSize);
                        break;
                    case TableIndices.GenericParamConstraint:
                        length += m * (tableRefSize[(int)TableIndices.GenericParam] + typeDefOrRefOrSpecSize);
                        break;
                }
            }

            length += 24 + tableCount * 4;
            return length;
        }

        private readonly NTHeader /*!*/
            ntHeader = new NTHeader();

        private readonly CLIHeader /*!*/
            cliHeader = new CLIHeader();

        private SectionHeader[] sectionHeaders;

        internal void WritePE(BinaryWriter /*!*/ writer)
            //^ requires this.SdataHeap != null;
            //^ requires this.TlsHeap != null;
        {
            cliHeader.entryPointToken = entryPointToken;
            switch (moduleKind)
            {
                case ModuleKindFlags.ConsoleApplication:
                    ntHeader.subsystem = 3;
                    break;
                case ModuleKindFlags.DynamicallyLinkedLibrary:
                    ntHeader.characteristics |= 0x2000;
                    ntHeader.subsystem = 3;
                    break;
                case ModuleKindFlags.WindowsApplication:
                    ntHeader.subsystem = 2;
                    break;
            }

            ntHeader.dllCharacteristics = (ushort)(dllCharacteristics | 0x400);
            var numSectionHeaders = 2;
            if (SdataHeap.Length > 0) numSectionHeaders++;
            if (TlsHeap.Length > 0) numSectionHeaders++;
            if (Win32Resources != null && Win32Resources.Count > 0) numSectionHeaders++;
            sectionHeaders = new SectionHeader[numSectionHeaders];
            ntHeader.numberOfSections = (ushort)numSectionHeaders;
            ntHeader.timeDateStamp = (int)(DateTime.Now.ToUniversalTime() - NineteenSeventy).TotalSeconds;

            //Write out .text section for meta data tables, method bodies, address tables and entry point stub
            var sdataFixup = new Fixup();
            var tlsFixup = new Fixup();
            var textSection = new SectionHeader();
            textSection.name = ".text";
            textSection.virtualAddress = 8192;
            var sizeOfPeHeaders = 376 + 40 * numSectionHeaders;
            if ((peKind & PEKindFlags.Requires64bits) != 0) sizeOfPeHeaders += 16;
            textSection.pointerToRawData = (int)Math.Ceiling(sizeOfPeHeaders / (double)fileAlignment) * fileAlignment;
            textSection.characteristics = 0x60000020;
            writer.BaseStream.Position = textSection.pointerToRawData + 72; //Leave 72 bytes for CLI header
            SerializeMetadata(writer, textSection.virtualAddress, sdataFixup, tlsFixup);
            var RVAofEntryPointJumpTarget = WriteImportTableAndEntryPointStub(writer, ref textSection);
#if !ROTOR
            if (symWriter != null)
                WriteReferenceToPDBFile(writer, textSection.virtualAddress, textSection.pointerToRawData);
#endif
            var len = textSection.virtualSize = writer.BaseStream.Position - textSection.pointerToRawData;
            textSection.sizeOfRawData = (int)Math.Ceiling(len / (double)fileAlignment) * fileAlignment;
            sectionHeaders[0] = textSection;
            writer.BaseStream.Position = textSection.pointerToRawData;
            ntHeader.cliHeaderTable.virtualAddress = textSection.virtualAddress;
            ntHeader.cliHeaderTable.size = 72;
            WriteCLIHeader(writer); //Write CLI header last so that forward pointers can be filled in first

            var sectionHeaderIndex = 1;
            var previousSection = textSection;
            var n = ntHeader.sectionAlignment;
            var m = fileAlignment;

            if (SdataHeap.Length > 0)
            {
                var sdataSection = new SectionHeader();
                sdataSection.name = ".sdata";
                var vaddr = sdataSection.virtualAddress = previousSection.virtualAddress +
                                                          n * (int)Math.Ceiling(previousSection.sizeOfRawData /
                                                              (double)n);
                sdataSection.virtualSize = SdataHeap.Length;
                sdataSection.pointerToRawData = previousSection.pointerToRawData +
                                                m * (int)Math.Ceiling(previousSection.sizeOfRawData / (double)m);
                sdataSection.characteristics = unchecked((int)0xC0000040);
                writer.BaseStream.Position = sdataSection.pointerToRawData;
                SdataHeap.WriteTo(writer.BaseStream);
                len = sdataSection.virtualSize = writer.BaseStream.Position - sdataSection.pointerToRawData;
                writer.BaseStream.Position += m - len % fileAlignment - 1;
                writer.Write((byte)0);
                sdataSection.sizeOfRawData = (int)Math.Ceiling(len / (double)fileAlignment) * fileAlignment;
                sdataFixup = sdataFixup.nextFixUp; //Skip over dummy header
                while (sdataFixup != null)
                {
                    writer.BaseStream.Position = sdataFixup.fixupLocation;
                    writer.Write(vaddr + sdataFixup.addressOfNextInstruction);
                    sdataFixup = sdataFixup.nextFixUp;
                }

                sectionHeaders[sectionHeaderIndex++] = sdataSection;
                previousSection = sdataSection;
            }

            if (TlsHeap.Length > 0)
            {
                var tlsSection = new SectionHeader();
                tlsSection.name = ".tls";
                var vaddr = tlsSection.virtualAddress = previousSection.virtualAddress +
                                                        n * (int)Math.Ceiling(previousSection.sizeOfRawData /
                                                                              (double)n);
                tlsSection.virtualSize = SdataHeap.Length;
                tlsSection.pointerToRawData = previousSection.pointerToRawData +
                                              m * (int)Math.Ceiling(previousSection.sizeOfRawData / (double)m);
                tlsSection.characteristics = unchecked((int)0xC0000040);
                writer.BaseStream.Position = tlsSection.pointerToRawData;
                TlsHeap.WriteTo(writer.BaseStream);
                len = tlsSection.virtualSize = writer.BaseStream.Position - tlsSection.pointerToRawData;
                writer.BaseStream.Position += m - len % fileAlignment - 1;
                writer.Write((byte)0);
                tlsSection.sizeOfRawData = (int)Math.Ceiling(len / (double)fileAlignment) * fileAlignment;
                tlsFixup = tlsFixup.nextFixUp; //Skip over dummy header
                while (tlsFixup != null)
                {
                    writer.BaseStream.Position = tlsFixup.fixupLocation;
                    writer.Write(vaddr + tlsFixup.addressOfNextInstruction);
                    tlsFixup = tlsFixup.nextFixUp;
                }

                sectionHeaders[sectionHeaderIndex++] = tlsSection;
                previousSection = tlsSection;
            }

            if (Win32Resources != null && Win32Resources.Count > 0)
            {
                var rsrcSection = new SectionHeader();
                rsrcSection.name = ".rsrc";
                rsrcSection.virtualAddress = previousSection.virtualAddress +
                                             n * (int)Math.Ceiling(previousSection.sizeOfRawData / (double)n);
                rsrcSection.pointerToRawData = previousSection.pointerToRawData +
                                               m * (int)Math.Ceiling(previousSection.sizeOfRawData / (double)m);
                rsrcSection.characteristics = 0x40000040;
                writer.BaseStream.Position = rsrcSection.pointerToRawData;
                WriteWin32Resources(writer, rsrcSection.virtualAddress);
                len = rsrcSection.virtualSize = writer.BaseStream.Position - rsrcSection.pointerToRawData;
                writer.BaseStream.Position += m - len % fileAlignment - 1;
                writer.Write((byte)0);
                rsrcSection.sizeOfRawData = (int)Math.Ceiling(len / (double)fileAlignment) * fileAlignment;
                sectionHeaders[sectionHeaderIndex++] = rsrcSection;
                ntHeader.resourceTable.virtualAddress = rsrcSection.virtualAddress;
                ntHeader.resourceTable.size = rsrcSection.virtualSize;
                ntHeader.sizeOfInitializedData += rsrcSection.sizeOfRawData;
                previousSection = rsrcSection;
            }

            //Write out .reloc section for entry point stub relocation table
            var IA64 = (peKind & PEKindFlags.Requires64bits) != 0 && (peKind & PEKindFlags.AMD) == 0;
            var relocSection = new SectionHeader();
            relocSection.name = ".reloc";
            relocSection.virtualAddress = previousSection.virtualAddress +
                                          n * (int)Math.Ceiling(previousSection.sizeOfRawData / (double)n);
            relocSection.virtualSize = IA64 ? 14 : 12;
            relocSection.pointerToRawData = previousSection.pointerToRawData +
                                            m * (int)Math.Ceiling(previousSection.sizeOfRawData / (double)m);
            relocSection.sizeOfRawData = m;
            relocSection.characteristics = 0x42000040;
            writer.BaseStream.Position = relocSection.pointerToRawData;
            writer.Write(RVAofEntryPointJumpTarget / 4096 * 4096); //Page RVA
            writer.Write(IA64 ? 14 : 12);
            var offsetWithinPage = RVAofEntryPointJumpTarget % 4096;
            var relocType = (peKind & PEKindFlags.Requires64bits) != 0 ? 10 : 3;
            var s = (short)((relocType << 12) | offsetWithinPage);
            writer.Write(s);
            if (IA64) writer.Write((short)(relocType << 12));
            writer.Write((short)0); //next chunk's RVA
            writer.BaseStream.Position += m - 13;
            writer.Write((byte)0);
            sectionHeaders[sectionHeaderIndex] = relocSection;
            ntHeader.baseRelocationTable.virtualAddress = relocSection.virtualAddress;
            ntHeader.baseRelocationTable.size = relocSection.virtualSize;
            ntHeader.sizeOfInitializedData += relocSection.sizeOfRawData;

            //Write PE headers. Do this last because forward pointers are filled in by preceding code
            writer.BaseStream.Position = 0;
            writer.Write(dosHeader);
            WriteNTHeader(writer);
            WriteSectionHeaders(writer);
        }

        private class Directory
        {
            internal readonly ArrayList /*!*/
                Entries;

            internal readonly int ID;
            internal readonly string Name;
            internal int NumberOfIdEntries;
            internal int NumberOfNamedEntries;

            internal Directory(string Name, int ID)
            {
                this.Name = Name;
                this.ID = ID;
                Entries = new ArrayList();
                //^ base();
            }
        }

        private void WriteWin32Resources(BinaryWriter /*!*/ writer, int virtualAddressBase)
            //^ requires this.Win32Resources != null;
        {
            var rsrcs = Win32Resources;
            var dataHeap = new BinaryWriter(new MemoryStream(), Encoding.Unicode);
            //Construct a tree of array lists to represent the directory and make it easier to compute offsets
            var TypeDirectory = new Directory("", 0);
            Directory NameDirectory = null;
            Directory LanguageDirectory = null;
            var lastTypeID = int.MinValue;
            string lastTypeName = null;
            var lastID = int.MinValue;
            string lastName = null;
            var sizeOfDirectoryTree = 16;
            for (int i = 0, n = rsrcs.Count; i < n; i++)
            {
                var r = rsrcs[i];
                var typeDifferent = (r.TypeId < 0 && r.TypeName != lastTypeName) || r.TypeId > lastTypeID;
                if (typeDifferent)
                {
                    lastTypeID = r.TypeId;
                    lastTypeName = r.TypeName;
                    if (lastTypeID < 0) TypeDirectory.NumberOfNamedEntries++;
                    else TypeDirectory.NumberOfIdEntries++;
                    sizeOfDirectoryTree += 24;
                    TypeDirectory.Entries.Add(NameDirectory = new Directory(lastTypeName, lastTypeID));
                }

                //^ assume NameDirectory != null;
                if (typeDifferent || (r.Id < 0 && r.Name != lastName) || r.Id > lastID)
                {
                    lastID = r.Id;
                    lastName = r.Name;
                    if (lastID < 0) NameDirectory.NumberOfNamedEntries++;
                    else NameDirectory.NumberOfIdEntries++;
                    sizeOfDirectoryTree += 24;
                    NameDirectory.Entries.Add(LanguageDirectory = new Directory(lastName, lastID));
                }

                //^ assume LanguageDirectory != null;
                LanguageDirectory.NumberOfIdEntries++;
                sizeOfDirectoryTree += 8;
                LanguageDirectory.Entries.Add(r);
            }

            WriteDirectory(TypeDirectory, writer, 0, 0, sizeOfDirectoryTree, virtualAddressBase, dataHeap);
            dataHeap.BaseStream.WriteTo(writer.BaseStream);
        }

        private void WriteDirectory(Directory /*!*/ directory, BinaryWriter /*!*/ writer, int offset, int level,
            int sizeOfDirectoryTree,
            int virtualAddressBase, BinaryWriter /*!*/ dataHeap)
        {
            writer.Write(0); //Characteristics
            writer.Write(0); //Timestamp
            writer.Write(0); //Version
            writer.Write((short)directory.NumberOfNamedEntries);
            writer.Write((short)directory.NumberOfIdEntries);
            var n = directory.Entries.Count;
            var k = offset + 16 + n * 8;
            for (var i = 0; i < n; i++)
            {
                var id = int.MinValue;
                string name = null;
                var nOff = dataHeap.BaseStream.Position + sizeOfDirectoryTree;
                var dOff = k;
                var subDir = directory.Entries[i] as Directory;
                if (subDir != null)
                {
                    id = subDir.ID;
                    name = subDir.Name;
                    if (level == 0)
                        k += SizeOfDirectory(subDir);
                    else
                        k += 16 + 8 * subDir.Entries.Count;
                }
                else
                {
                    var r = (Win32Resource)directory.Entries[i];
                    id = level == 0 ? r.TypeId : level == 1 ? r.Id : r.LanguageId;
                    name = level == 0 ? r.TypeName : level == 1 ? r.Name : null;
                    dataHeap.Write(virtualAddressBase + sizeOfDirectoryTree + 16 + dataHeap.BaseStream.Position);
                    dataHeap.Write(r.Data.Length);
                    dataHeap.Write(r.CodePage);
                    dataHeap.Write(0);
                    dataHeap.Write(r.Data);
                }

                if (id >= 0)
                {
                    writer.Write(id);
                }
                else
                {
                    if (name == null) name = "";
                    writer.Write((uint)nOff | 0x80000000);
                    dataHeap.Write((ushort)name.Length);
                    dataHeap.Write(name
                        .ToCharArray()); //REVIEW: what happens if the name contains chars that do not fit into a single utf8 code point?
                }

                if (subDir != null)
                    writer.Write((uint)dOff | 0x80000000);
                else
                    writer.Write(nOff);
            }

            k = offset + 16 + n * 8;
            for (var i = 0; i < n; i++)
            {
                var subDir = directory.Entries[i] as Directory;
                if (subDir != null)
                {
                    WriteDirectory(subDir, writer, k, level + 1, sizeOfDirectoryTree, virtualAddressBase, dataHeap);
                    if (level == 0)
                        k += SizeOfDirectory(subDir);
                    else
                        k += 16 + 8 * subDir.Entries.Count;
                }
            }
        }

        private int SizeOfDirectory(Directory /*!*/ directory)
        {
            var n = directory.Entries.Count;
            var size = 16 + 8 * n;
            for (var i = 0; i < n; i++)
            {
                var subDir = directory.Entries[i] as Directory;
                if (subDir != null)
                    size += 16 + 8 * subDir.Entries.Count;
            }

            return size;
        }

        private static readonly byte[] dosHeader =
        {
            0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00,
            0xb8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
            0x0e, 0x1f, 0xba, 0x0e, 0x00, 0xb4, 0x09, 0xcd,
            0x21, 0xb8, 0x01, 0x4c, 0xcd, 0x21, 0x54, 0x68,
            0x69, 0x73, 0x20, 0x70, 0x72, 0x6f, 0x67, 0x72,
            0x61, 0x6d, 0x20, 0x63, 0x61, 0x6e, 0x6e, 0x6f,
            0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6e,
            0x20, 0x69, 0x6e, 0x20, 0x44, 0x4f, 0x53, 0x20,
            0x6d, 0x6f, 0x64, 0x65, 0x2e, 0x0d, 0x0d, 0x0a,
            0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private static readonly DateTime NineteenSeventy = new DateTime(1970, 1, 1);

        private void WriteNTHeader(BinaryWriter /*!*/ writer)
            //^ requires this.sectionHeaders != null;
        {
            var ntHeader = this.ntHeader;
            writer.Write(ntHeader.signature);
            if ((peKind & PEKindFlags.Requires64bits) == 0)
            {
                if ((peKind & PEKindFlags.Requires32bits) != 0)
                    ntHeader.characteristics |=
                        0x0100; //32 bit machine (The standard says to always set this, the linker team says otherwise)
                ntHeader.magic = 0x10B; //PE32
                ntHeader.machine = 0x014c; //I386
            }
            else
            {
                ntHeader.characteristics |= 0x0020; //Can handle >2gb addresses
                ntHeader.magic = 0x20B; //PE32+
                if ((peKind & PEKindFlags.AMD) != 0)
                    ntHeader.machine = 0x8664; //AMD64
                else
                    ntHeader.machine = 0x0200; //IA64
                ntHeader.sizeOfOptionalHeader += 16;
            }

            writer.Write(ntHeader.machine);
            writer.Write(ntHeader.numberOfSections);
            writer.Write(ntHeader.timeDateStamp);
            writer.Write(ntHeader.pointerToSymbolTable);
            writer.Write(ntHeader.numberOfSymbols);
            writer.Write(ntHeader.sizeOfOptionalHeader);
            writer.Write(ntHeader.characteristics);
            writer.Write(ntHeader.magic);
            writer.Write((byte)TargetPlatform.LinkerMajorVersion);
            writer.Write((byte)TargetPlatform.LinkerMinorVersion);
            writer.Write(sectionHeaders[0].sizeOfRawData); //sizeOfCode
            writer.Write(ntHeader.sizeOfInitializedData);
            writer.Write(ntHeader.sizeOfUninitializedData);
            writer.Write(ntHeader.addressOfEntryPoint);
            writer.Write(sectionHeaders[0].virtualAddress); //baseOfCode
            if (ntHeader.magic == 0x10B)
            {
                if (sectionHeaders.Length > 1)
                    writer.Write(sectionHeaders[1].virtualAddress); //baseOfData
                else
                    writer.Write(0);
                writer.Write((int)ntHeader.imageBase);
            }
            else
            {
                writer.Write(ntHeader.imageBase);
            }

            writer.Write(ntHeader.sectionAlignment);
            writer.Write(fileAlignment);
            writer.Write(ntHeader.majorOperatingSystemVersion);
            writer.Write(ntHeader.minorOperatingSystemVersion);
            writer.Write(ntHeader.majorImageVersion);
            writer.Write(ntHeader.minorImageVersion);
            writer.Write(ntHeader.majorSubsystemVersion);
            writer.Write(ntHeader.minorSubsystemVersion);
            writer.Write(ntHeader.win32VersionValue);
            var sectionPages = (int)(Math.Ceiling(sectionHeaders[sectionHeaders.Length - 1].virtualSize /
                                                  (double)ntHeader.sectionAlignment) * ntHeader.sectionAlignment);
            writer.Write(sectionHeaders[sectionHeaders.Length - 1].virtualAddress + sectionPages); //sizeOfImage
            writer.Write(sectionHeaders[0].pointerToRawData); //sizeOfHeaders
            writer.Write(ntHeader.checkSum);
            writer.Write(ntHeader.subsystem);
            writer.Write(ntHeader.dllCharacteristics);
            if (ntHeader.magic == 0x10B)
            {
                writer.Write((int)ntHeader.sizeOfStackReserve);
                writer.Write((int)ntHeader.sizeOfStackCommit);
                writer.Write((int)ntHeader.sizeOfHeapReserve);
                writer.Write((int)ntHeader.sizeOfHeapCommit);
            }
            else
            {
                writer.Write(ntHeader.sizeOfStackReserve);
                writer.Write(ntHeader.sizeOfStackCommit);
                writer.Write(ntHeader.sizeOfHeapReserve);
                writer.Write(ntHeader.sizeOfHeapCommit);
            }

            writer.Write(ntHeader.loaderFlags);
            writer.Write(ntHeader.numberOfDataDirectories);
            writer.Write(ntHeader.exportTable.virtualAddress);
            writer.Write(ntHeader.exportTable.size);
            writer.Write(ntHeader.importTable.virtualAddress);
            writer.Write(ntHeader.importTable.size);
            writer.Write(ntHeader.resourceTable.virtualAddress);
            writer.Write(ntHeader.resourceTable.size);
            writer.Write(ntHeader.exceptionTable.virtualAddress);
            writer.Write(ntHeader.exceptionTable.size);
            writer.Write(ntHeader.certificateTable.virtualAddress);
            writer.Write(ntHeader.certificateTable.size);
            writer.Write(ntHeader.baseRelocationTable.virtualAddress);
            writer.Write(ntHeader.baseRelocationTable.size);
            writer.Write(ntHeader.debugTable.virtualAddress);
            writer.Write(ntHeader.debugTable.size);
            writer.Write(ntHeader.copyrightTable.virtualAddress);
            writer.Write(ntHeader.copyrightTable.size);
            writer.Write(ntHeader.globalPointerTable.virtualAddress);
            writer.Write(ntHeader.globalPointerTable.size);
            writer.Write(ntHeader.threadLocalStorageTable.virtualAddress);
            writer.Write(ntHeader.threadLocalStorageTable.size);
            writer.Write(ntHeader.loadConfigTable.virtualAddress);
            writer.Write(ntHeader.loadConfigTable.size);
            writer.Write(ntHeader.boundImportTable.virtualAddress);
            writer.Write(ntHeader.boundImportTable.size);
            writer.Write(ntHeader.importAddressTable.virtualAddress);
            writer.Write(ntHeader.importAddressTable.size);
            writer.Write(ntHeader.delayImportTable.virtualAddress);
            writer.Write(ntHeader.delayImportTable.size);
            writer.Write(ntHeader.cliHeaderTable.virtualAddress);
            writer.Write(ntHeader.cliHeaderTable.size);
            writer.Write((long)0);
        }

        private void WriteSectionHeaders(BinaryWriter /*!*/ writer)
            //^ requires this.sectionHeaders != null;
        {
            var sectionHeaders = this.sectionHeaders;
            for (int i = 0, n = this.sectionHeaders.Length; i < n; i++)
            {
                var hdr = sectionHeaders[i];
                //^ assume hdr.name != null;
                for (int j = 0, m = hdr.name.Length; j < 8; j++)
                    if (j < m) writer.Write(hdr.name[j]);
                    else writer.Write((byte)0);
                writer.Write(hdr.virtualSize);
                writer.Write(hdr.virtualAddress);
                writer.Write(hdr.sizeOfRawData);
                writer.Write(hdr.pointerToRawData);
                writer.Write(hdr.pointerToRelocations);
                writer.Write(hdr.pointerToLinenumbers);
                writer.Write(hdr.numberOfRelocations);
                writer.Write(hdr.numberOfLinenumbers);
                writer.Write(hdr.characteristics);
            }
        }

        private void WriteCLIHeader(BinaryWriter /*!*/ writer)
        {
            var hdr = cliHeader;
            writer.Write(hdr.cb);
            writer.Write((ushort)2);
            if (UseGenerics)
                writer.Write((ushort)5); //Violates the ECMA standard (25.3.3 of Partition II), but apparently necessary
            else
                writer.Write((ushort)0);
            writer.Write(hdr.metaData.virtualAddress);
            writer.Write(hdr.metaData.size);
            if ((peKind & PEKindFlags.Requires32bits) != 0) hdr.flags |= 2;
            if ((peKind & PEKindFlags.Prefers32bits) != 0) hdr.flags |= 0x00020000;
            if ((peKind & PEKindFlags.ILonly) != 0) hdr.flags |= 1;
            if (TrackDebugData) hdr.flags |= 0x10000;
            writer.Write(hdr.flags);
            writer.Write(hdr.entryPointToken);
            writer.Write(hdr.resources.virtualAddress);
            writer.Write(hdr.resources.size);
            writer.Write(hdr.strongNameSignature.virtualAddress);
            writer.Write(hdr.strongNameSignature.size);
            writer.Write(hdr.codeManagerTable.virtualAddress);
            writer.Write(hdr.codeManagerTable.size);
            writer.Write(hdr.vtableFixups.virtualAddress);
            writer.Write(hdr.vtableFixups.size);
        }

        private int WriteImportTableAndEntryPointStub(BinaryWriter /*!*/ writer, ref SectionHeader textSection)
        {
            var use32bitAddresses = (peKind & PEKindFlags.Requires64bits) == 0;
            var pos = writer.BaseStream.Position;
            while (pos % 4 != 0)
            {
                pos++;
                writer.Write((byte)0);
            }

            var ITrva = textSection.virtualAddress + pos - textSection.pointerToRawData;
            var ITLrva = ITrva + 40;
            var rvasize = 12; // size of relocation sections
            var hintRva = ITLrva + (use32bitAddresses ? 8 : 16); //position of name of entry point in runtime dll
            var nameRva = hintRva + 14; // position of name of runtime dll
            var ITArva = nameRva + 14 + // size of name of runtime dll
                         4 + // size of entry point code 0000ff25 
                         rvasize; // size of relocation fixup

            ntHeader.addressOfEntryPoint = ITArva - rvasize - 2;
            ntHeader.importTable.virtualAddress = ITrva;
            ntHeader.importTable.size = ntHeader.addressOfEntryPoint - ITrva - 2;
            ntHeader.importAddressTable.virtualAddress = ITArva;
            ntHeader.importAddressTable.size = 8;
            //Import table
            writer.Write(ITLrva);
            writer.Write(0);
            writer.Write(0);
            writer.Write(nameRva);
            writer.Write(ITArva);
            writer.BaseStream.Position += 20;
            //Import Lookup table
            if (use32bitAddresses)
            {
                writer.Write(hintRva);
                writer.Write(0);
            }
            else
            {
                writer.Write((long)hintRva);
                writer.Write((long)0);
            }

            //Hint table
            writer.Write((short)0); //Hint
            var entryPointName = moduleKind == ModuleKindFlags.DynamicallyLinkedLibrary ? "_CorDllMain" : "_CorExeMain";
            foreach (var ch in entryPointName) writer.Write((byte)ch);
            writer.Write((byte)0);
            //name of CLR runtime dll
            foreach (var ch in "mscoree.dll") writer.Write((byte)ch);
            writer.Write((byte)0);
            writer.Write((short)0);
            //entry point code, consisting of a jump indirect to _CorXXXMain
            writer.Write((short)0); //padding so that address to replace is on a word boundary
            writer.Write((byte)0xff);
            writer.Write((byte)0x25);
            writer.Write(ITArva + (int)ntHeader.imageBase); //REVIEW: is this OK for 64 bit?
            writer.Write(0); // relocation fixup must be 12 bytes.
            writer.Write(0);
            //Import Address Table
            if (use32bitAddresses)
            {
                writer.Write(hintRva);
                writer.Write(0);
            }
            else
            {
                writer.Write((long)hintRva);
                writer.Write((long)0);
            }

            //Return RVA of the target address of the indirect jump at the entry point
            return ITArva - rvasize;
        }

        private void WriteReference(BinaryWriter /*!*/ writer, int index, int refSize)
        {
            if (refSize == 2) writer.Write((short)index);
            else writer.Write(index);
        }

        private int ComputeStrongNameSignatureSize()
        {
            var keySize = SignatureKeyLength;
            if (keySize == 0)
                keySize = PublicKey.Length;
            if (keySize == 0)
                return 0;
            return keySize < 128 + 32 ? 128 : keySize - 32;
        }
    }
#endif
}