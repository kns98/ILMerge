// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

#if !NoWriter
using System.CodeDom.Compiler;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
#if CCINamespace
using Microsoft.Cci.Metadata;
#else
using System.Compiler.Metadata;
#endif
#if !ROTOR
using System.Security.Cryptography;
using System.Text;
#endif

#if CCINamespace
namespace Microsoft.Cci{
#else
namespace System.Compiler
{
#endif
#if !ROTOR
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("B01FAFEB-C450-3A4D-BEEC-B4CEEC01E006")]
    [SuppressUnmanagedCodeSecurity]
    internal interface ISymUnmanagedDocumentWriter
    {
        void SetSource(uint sourceSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] source);

        void SetCheckSum(ref Guid algorithmId, uint checkSumSize,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] checkSum);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("2DE91396-3844-3B1D-8E91-41C24FD672EA")]
    [SuppressUnmanagedCodeSecurity]
    internal interface ISymUnmanagedWriter
    {
        ISymUnmanagedDocumentWriter DefineDocument(string url, ref Guid language, ref Guid languageVendor,
            ref Guid documentType);

        void SetUserEntryPoint(uint entryMethod);
        void OpenMethod(uint method);
        void CloseMethod();
        uint OpenScope(uint startOffset);
        void CloseScope(uint endOffset);
        void SetScopeRange(uint scopeID, uint startOffset, uint endOffset);

        void DefineLocalVariable(string name, uint attributes, uint cSig, IntPtr signature, uint addrKind, uint addr1,
            uint addr2, uint startOffset, uint endOffset);

        void DefineParameter(string name, uint attributes, uint sequence, uint addrKind, uint addr1, uint addr2,
            uint addr3);

        void DefineField(uint parent, string name, uint attributes, uint cSig, IntPtr signature, uint addrKind,
            uint addr1, uint addr2, uint addr3);

        void DefineGlobalVariable(string name, uint attributes, uint cSig, IntPtr signature, uint addrKind, uint addr1,
            uint addr2, uint addr3);

        void Close();
        void SetSymAttribute(uint parent, string name, uint cData, IntPtr signature);
        void OpenNamespace(string name);
        void CloseNamespace();
        void UsingNamespace(string fullName);

        void SetMethodSourceRange(ISymUnmanagedDocumentWriter startDoc, uint startLine, uint startColumn, object endDoc,
            uint endLine, uint endColumn);

        void Initialize([MarshalAs(UnmanagedType.IUnknown)] object emitter, string filename,
            [MarshalAs(UnmanagedType.IUnknown)] object pIStream, bool fFullBuild);

        void GetDebugInfo(ref ImageDebugDirectory pIDD, uint cData, out uint pcData, IntPtr data);

        void DefineSequencePoints(ISymUnmanagedDocumentWriter document, uint spCount,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            uint[] offsets,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            uint[] lines,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            uint[] columns,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            uint[] endLines,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            uint[] endColumns);

        void RemapToken(uint oldToken, uint newToken);

        void Initialize2([MarshalAs(UnmanagedType.IUnknown)] object emitter, string tempfilename,
            [MarshalAs(UnmanagedType.IUnknown)] object pIStream, bool fFullBuild, string finalfilename);

        void DefineConstant(string name, object value, uint cSig, IntPtr signature);
    }

    internal struct ImageDebugDirectory
    {
        internal int Characteristics;
        internal int TimeDateStamp;
        internal short MajorVersion;
        internal short MinorVersion;
        internal int Type;
        internal int SizeOfData;
        internal int AddressOfRawData;
        internal int PointerToRawData;

        public ImageDebugDirectory(bool zeroFill)
        {
            Characteristics = 0;
            TimeDateStamp = 0;
            MajorVersion = 0;
            MinorVersion = 0;
            Type = 0;
            SizeOfData = 0;
            AddressOfRawData = 0;
            PointerToRawData = 0;
        }
    }

    [ComVisible(true)]
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("BA3FEE4C-ECB9-4e41-83B7-183FA41CD859")]
    public unsafe interface IMetaDataEmit
    {
        void SetModuleProps(string szName);
        void Save(string szFile, uint dwSaveFlags);
        void SaveToStream(void* pIStream, uint dwSaveFlags);
        uint GetSaveSize(uint fSave);
        uint DefineTypeDef(char* szTypeDef, uint dwTypeDefFlags, uint tkExtends, uint* rtkImplements);

        uint DefineNestedType(char* szTypeDef, uint dwTypeDefFlags, uint tkExtends, uint* rtkImplements,
            uint tdEncloser);

        void SetHandler([MarshalAs(UnmanagedType.IUnknown)] [In] object pUnk);

        uint DefineMethod(uint td, char* zName, uint dwMethodFlags, byte* pvSigBlob, uint cbSigBlob, uint ulCodeRVA,
            uint dwImplFlags);

        void DefineMethodImpl(uint td, uint tkBody, uint tkDecl);
        uint DefineTypeRefByName(uint tkResolutionScope, char* szName);

        uint DefineImportType(IntPtr pAssemImport, void* pbHashValue, uint cbHashValue, IMetaDataImport pImport,
            uint tdImport, IntPtr pAssemEmit);

        uint DefineMemberRef(uint tkImport, string szName, byte* pvSigBlob, uint cbSigBlob);

        uint DefineImportMember(IntPtr pAssemImport, void* pbHashValue, uint cbHashValue,
            IMetaDataImport pImport, uint mbMember, IntPtr pAssemEmit, uint tkParent);

        uint DefineEvent(uint td, string szEvent, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn,
            uint mdFire, uint* rmdOtherMethods);

        void SetClassLayout(uint td, uint dwPackSize, COR_FIELD_OFFSET* rFieldOffsets, uint ulClassSize);
        void DeleteClassLayout(uint td);
        void SetFieldMarshal(uint tk, byte* pvNativeType, uint cbNativeType);
        void DeleteFieldMarshal(uint tk);
        uint DefinePermissionSet(uint tk, uint dwAction, void* pvPermission, uint cbPermission);
        void SetRVA(uint md, uint ulRVA);
        uint GetTokenFromSig(byte* pvSig, uint cbSig);
        uint DefineModuleRef(string szName);
        void SetParent(uint mr, uint tk);
        uint GetTokenFromTypeSpec(byte* pvSig, uint cbSig);
        void SaveToMemory(void* pbData, uint cbData);
        uint DefineUserString(string szString, uint cchString);
        void DeleteToken(uint tkObj);
        void SetMethodProps(uint md, uint dwMethodFlags, uint ulCodeRVA, uint dwImplFlags);
        void SetTypeDefProps(uint td, uint dwTypeDefFlags, uint tkExtends, uint* rtkImplements);

        void SetEventProps(uint ev, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire,
            uint* rmdOtherMethods);

        uint SetPermissionSetProps(uint tk, uint dwAction, void* pvPermission, uint cbPermission);
        void DefinePinvokeMap(uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL);
        void SetPinvokeMap(uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL);
        void DeletePinvokeMap(uint tk);
        uint DefineCustomAttribute(uint tkObj, uint tkType, void* pCustomAttribute, uint cbCustomAttribute);
        void SetCustomAttributeValue(uint pcv, void* pCustomAttribute, uint cbCustomAttribute);

        uint DefineField(uint td, string szName, uint dwFieldFlags, byte* pvSigBlob, uint cbSigBlob,
            uint dwCPlusTypeFlag, void* pValue, uint cchValue);

        uint DefineProperty(uint td, string szProperty, uint dwPropFlags, byte* pvSig, uint cbSig, uint dwCPlusTypeFlag,
            void* pValue, uint cchValue, uint mdSetter, uint mdGetter, uint* rmdOtherMethods);

        uint DefineParam(uint md, uint ulParamSeq, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, void* pValue,
            uint cchValue);

        void SetFieldProps(uint fd, uint dwFieldFlags, uint dwCPlusTypeFlag, void* pValue, uint cchValue);

        void SetPropertyProps(uint pr, uint dwPropFlags, uint dwCPlusTypeFlag, void* pValue, uint cchValue,
            uint mdSetter, uint mdGetter, uint* rmdOtherMethods);

        void SetParamProps(uint pd, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, void* pValue,
            uint cchValue);

        uint DefineSecurityAttributeSet(uint tkObj, IntPtr rSecAttrs, uint cSecAttrs);
        void ApplyEditAndContinue([MarshalAs(UnmanagedType.IUnknown)] object pImport);

        uint TranslateSigWithScope(IntPtr pAssemImport, void* pbHashValue, uint cbHashValue,
            IMetaDataImport import, byte* pbSigBlob, uint cbSigBlob, IntPtr pAssemEmit, IMetaDataEmit emit,
            byte* pvTranslatedSig, uint cbTranslatedSigMax);

        void SetMethodImplFlags(uint md, uint dwImplFlags);
        void SetFieldRVA(uint fd, uint ulRVA);
        void Merge(IMetaDataImport pImport, IntPtr pHostMapToken, [MarshalAs(UnmanagedType.IUnknown)] object pHandler);
        void MergeEnd();
    }

    public struct COR_FIELD_OFFSET
    {
        public uint ridOfField;
        public uint ulOffset;
    }

    [ComVisible(true)]
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
    public unsafe interface IMetaDataImport
    {
        [PreserveSig]
        void CloseEnum(uint hEnum);

        uint CountEnum(uint hEnum);
        void ResetEnum(uint hEnum, uint ulPos);

        uint EnumTypeDefs(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rTypeDefs,
            uint cMax);

        uint EnumInterfaceImpls(ref uint phEnum, uint td,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rImpls, uint cMax);

        uint EnumTypeRefs(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rTypeRefs,
            uint cMax);

        uint FindTypeDefByName(string szTypeDef, uint tkEnclosingClass);
        Guid GetScopeProps(StringBuilder szName, uint cchName, out uint pchName);
        uint GetModuleFromScope();
        uint GetTypeDefProps(uint td, IntPtr szTypeDef, uint cchTypeDef, out uint pchTypeDef, IntPtr pdwTypeDefFlags);
        uint GetInterfaceImplProps(uint iiImpl, out uint pClass);
        uint GetTypeRefProps(uint tr, out uint ptkResolutionScope, StringBuilder szName, uint cchName);
        uint ResolveTypeRef(uint tr, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppIScope);

        uint EnumMembers(ref uint phEnum, uint cl,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rMembers, uint cMax);

        uint EnumMembersWithName(ref uint phEnum, uint cl, string szName,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rMembers, uint cMax);

        uint EnumMethods(ref uint phEnum, uint cl, uint* rMethods, uint cMax);

        uint EnumMethodsWithName(ref uint phEnum, uint cl, string szName,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rMethods, uint cMax);

        uint EnumFields(ref uint phEnum, uint cl, uint* rFields, uint cMax);

        uint EnumFieldsWithName(ref uint phEnum, uint cl, string szName,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rFields, uint cMax);

        uint EnumParams(ref uint phEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rParams,
            uint cMax);

        uint EnumMemberRefs(ref uint phEnum, uint tkParent,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rMemberRefs, uint cMax);

        uint EnumMethodImpls(ref uint phEnum, uint td,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rMethodBody,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
            uint[] rMethodDecl, uint cMax);

        uint EnumPermissionSets(ref uint phEnum, uint tk, uint dwActions,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rPermission,
            uint cMax);

        uint FindMember(uint td, string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pvSigBlob,
            uint cbSigBlob);

        uint FindMethod(uint td, string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pvSigBlob,
            uint cbSigBlob);

        uint FindField(uint td, string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pvSigBlob,
            uint cbSigBlob);

        uint FindMemberRef(uint td, string szName,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pvSigBlob, uint cbSigBlob);

        uint GetMethodProps(uint mb, out uint pClass, IntPtr szMethod, uint cchMethod, out uint pchMethod,
            IntPtr pdwAttr,
            IntPtr ppvSigBlob, IntPtr pcbSigBlob, IntPtr pulCodeRVA);

        uint GetMemberRefProps(uint mr, ref uint ptk, StringBuilder szMember, uint cchMember, out uint pchMember,
            out byte* ppvSigBlob);

        uint EnumProperties(ref uint phEnum, uint td, uint* rProperties, uint cMax);
        uint EnumEvents(ref uint phEnum, uint td, uint* rEvents, uint cMax);

        uint GetEventProps(uint ev, out uint pClass, StringBuilder szEvent, uint cchEvent, out uint pchEvent,
            out uint pdwEventFlags,
            out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 11)]
            uint[] rmdOtherMethod, uint cMax);

        uint EnumMethodSemantics(ref uint phEnum, uint mb,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rEventProp, uint cMax);

        uint GetMethodSemantics(uint mb, uint tkEventProp);

        uint GetClassLayout(uint td, out uint pdwPackSize,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] COR_FIELD_OFFSET[] rFieldOffset, uint cMax,
            out uint pcFieldOffset);

        uint GetFieldMarshal(uint tk, out byte* ppvNativeType);
        uint GetRVA(uint tk, out uint pulCodeRVA);
        uint GetPermissionSetProps(uint pm, out uint pdwAction, out void* ppvPermission);
        uint GetSigFromToken(uint mdSig, out byte* ppvSig);
        uint GetModuleRefProps(uint mur, StringBuilder szName, uint cchName);

        uint EnumModuleRefs(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rModuleRefs,
            uint cmax);

        uint GetTypeSpecFromToken(uint typespec, out byte* ppvSig);
        uint GetNameFromToken(uint tk);

        uint EnumUnresolvedMethods(ref uint phEnum,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rMethods, uint cMax);

        uint GetUserString(uint stk, StringBuilder szString, uint cchString);

        uint GetPinvokeMap(uint tk, out uint pdwMappingFlags, StringBuilder szImportName, uint cchImportName,
            out uint pchImportName);

        uint EnumSignatures(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rSignatures,
            uint cmax);

        uint EnumTypeSpecs(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rTypeSpecs,
            uint cmax);

        uint EnumUserStrings(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rStrings,
            uint cmax);

        [PreserveSig]
        int GetParamForMethodIndex(uint md, uint ulParamSeq, out uint pParam);

        uint EnumCustomAttributes(ref uint phEnum, uint tk, uint tkType,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rCustomAttributes, uint cMax);

        uint GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out void* ppBlob);
        uint FindTypeRef(uint tkResolutionScope, string szName);

        uint GetMemberProps(uint mb, out uint pClass, StringBuilder szMember, uint cchMember, out uint pchMember,
            out uint pdwAttr,
            out byte* ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags,
            out uint pdwCPlusTypeFlag, out void* ppValue);

        uint GetFieldProps(uint mb, out uint pClass, StringBuilder szField, uint cchField, out uint pchField,
            out uint pdwAttr,
            out byte* ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out void* ppValue);

        uint GetPropertyProps(uint prop, out uint pClass, StringBuilder szProperty, uint cchProperty,
            out uint pchProperty, out uint pdwPropFlags,
            out byte* ppvSig, out uint pbSig, out uint pdwCPlusTypeFlag, out void* ppDefaultValue,
            out uint pcchDefaultValue, out uint pmdSetter,
            out uint pmdGetter, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 14)] uint[] rmdOtherMethod,
            uint cMax);

        uint GetParamProps(uint tk, out uint pmd, out uint pulSequence, StringBuilder szName, uint cchName,
            out uint pchName,
            out uint pdwAttr, out uint pdwCPlusTypeFlag, out void* ppValue);

        uint GetCustomAttributeByName(uint tkObj, string szName, out void* ppData);

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsValidToken(uint tk);

        uint GetNestedClassProps(uint tdNestedClass);
        uint GetNativeCallConvFromSig(void* pvSig, uint cbSig);
        int IsGlobal(uint pd);
    }

    [SuppressUnmanagedCodeSecurity]
    internal sealed class Ir2md : IMetaDataEmit, IMetaDataImport
    {
#else
  internal sealed class Ir2md{
#endif
        private AssemblyNode assembly;

        private Module /*!*/
            module;

        private MetadataWriter /*!*/
            writer;

        private bool UseGenerics;
        private bool StripOptionalModifiersFromLocals => module.StripOptionalModifiersFromLocals;
        private BinaryWriter /*!*/
            blobHeap = new BinaryWriter(new MemoryStream(), Encoding.Unicode);
#if WHIDBEYwithGenerics || WHIDBEYwithGenericsAndIEqualityComparer
        private Hashtable /*!*/
            blobHeapIndex = new Hashtable(new ByteArrayKeyComparer());
#else
    private Hashtable/*!*/ blobHeapIndex = new Hashtable(new ByteArrayHasher(), new ByteArrayComparer());
#endif
        private Hashtable /*!*/
            blobHeapStringIndex = new Hashtable();

        private NodeList /*!*/
            nodesWithCustomAttributes = new NodeList();

        private int customAttributeCount;

        private NodeList /*!*/
            nodesWithSecurityAttributes = new NodeList();

        private int securityAttributeCount;

        private NodeList /*!*/
            constantTableEntries = new NodeList();

        private TrivialHashtable /*!*/
            assemblyRefIndex = new TrivialHashtable();

        private AssemblyReferenceList /*!*/
            assemblyRefEntries = new AssemblyReferenceList();

        private TypeNodeList /*!*/
            classLayoutEntries = new TypeNodeList();

        private TrivialHashtable /*!*/
            documentMap = new TrivialHashtable();

        private TrivialHashtable /*!*/
            eventIndex = new TrivialHashtable();

        private EventList /*!*/
            eventEntries = new EventList();

        private TrivialHashtable /*!*/
            eventMapIndex = new TrivialHashtable();

        private EventList /*!*/
            eventMapEntries = new EventList();

        private TrivialHashtable /*!*/
            exceptionBlock = new TrivialHashtable();

        private TrivialHashtable /*!*/
            fieldIndex = new TrivialHashtable();

        private FieldList /*!*/
            fieldEntries = new FieldList();

        private FieldList /*!*/
            fieldLayoutEntries = new FieldList();

        private FieldList /*!*/
            fieldRvaEntries = new FieldList();

        private Hashtable /*!*/
            fileTableIndex = new Hashtable();

        private ModuleList /*!*/
            fileTableEntries = new ModuleList();

        private Hashtable /*!*/
            genericParamIndex = new Hashtable();

        private MemberList /*!*/
            genericParamEntries = new MemberList();

        private TypeNodeList /*!*/
            genericParameters = new TypeNodeList();

        private TypeNodeList /*!*/
            genericParamConstraintEntries = new TypeNodeList();

        private ArrayList /*!*/
            guidEntries = new ArrayList();

        private Hashtable /*!*/
            guidIndex = new Hashtable();

        private MethodList /*!*/
            implMapEntries = new MethodList();

        private TypeNodeList /*!*/
            interfaceEntries = new TypeNodeList();

        private NodeList /*!*/
            marshalEntries = new NodeList();

        private TrivialHashtable<int> /*!*/
            memberRefIndex = new TrivialHashtable<int>();

        private MemberList /*!*/
            memberRefEntries = new MemberList();

        private TrivialHashtable /*!*/
            methodBodiesHeapIndex = new TrivialHashtable();

        private BinaryWriter /*!*/
            methodBodiesHeap = new BinaryWriter(new MemoryStream());

        private BinaryWriter /*!*/
            methodBodyHeap;

        private MethodList /*!*/
            methodEntries = new MethodList();

        private TrivialHashtable<int> /*!*/
            methodIndex = new TrivialHashtable<int>();

        private MethodList /*!*/
            methodImplEntries = new MethodList();

        private MethodInfo /*!*/
            methodInfo;
#if !MinimalReader && !CodeContracts
    private Method currentMethod;
#endif
        private MemberList /*!*/
            methodSemanticsEntries = new MemberList();

        private MethodList /*!*/
            methodSpecEntries = new MethodList();

        private Hashtable /*!*/
            methodSpecIndex = new Hashtable();

        private ModuleReferenceList /*!*/
            moduleRefEntries = new ModuleReferenceList();

        private Hashtable /*!*/
            moduleRefIndex = new Hashtable();

        private TypeNodeList /*!*/
            nestedClassEntries = new TypeNodeList();

        private TrivialHashtable<int> /*!*/
            paramIndex = new TrivialHashtable<int>();

        private ParameterList /*!*/
            paramEntries = new ParameterList();

        private TrivialHashtable /*!*/
            propertyIndex = new TrivialHashtable();

        private PropertyList /*!*/
            propertyEntries = new PropertyList();

        private TrivialHashtable /*!*/
            propertyMapIndex = new TrivialHashtable();

        private PropertyList /*!*/
            propertyMapEntries = new PropertyList();

        private BinaryWriter /*!*/
            resourceDataHeap = new BinaryWriter(new MemoryStream());

        private BinaryWriter /*!*/
            sdataHeap = new BinaryWriter(new MemoryStream());
#if !ROTOR
        private ISymUnmanagedWriter symWriter;
#endif
        private int stackHeight;
        private int stackHeightMax;
        private int stackHeightExitTotal;

        private ArrayList /*!*/
            standAloneSignatureEntries = new ArrayList();

        private BinaryWriter /*!*/
            stringHeap = new BinaryWriter(new MemoryStream());

        private Hashtable /*!*/
            stringHeapIndex = new Hashtable();

        private BinaryWriter /*!*/
            tlsHeap = new BinaryWriter(new MemoryStream());

        private TrivialHashtable /*!*/
            typeDefIndex = new TrivialHashtable();

        private TypeNodeList /*!*/
            typeDefEntries = new TypeNodeList();

        private TrivialHashtable /*!*/
            typeRefIndex = new TrivialHashtable();

        private TypeNodeList /*!*/
            typeRefEntries = new TypeNodeList();

        private TrivialHashtable /*!*/
            typeSpecIndex = new TrivialHashtable();

        private readonly TrivialHashtable /*!*/
            structuralTypeSpecIndexFor = new TrivialHashtable();

        private TypeNodeList /*!*/
            typeSpecEntries = new TypeNodeList();

        private TrivialHashtable /*!*/
            typeParameterNumber = new TrivialHashtable();

        private BinaryWriter /*!*/
            userStringHeap = new BinaryWriter(new MemoryStream(), Encoding.Unicode);

        private Hashtable /*!*/
            userStringHeapIndex = new Hashtable();

        private byte[] PublicKey;
        private readonly int SignatureKeyLength;

        internal Ir2md(Module /*!*/ module)
        {
            assembly = module as AssemblyNode;
            this.module = module;
            //^ base();
            blobHeap.Write((byte)0);
            stringHeap.Write((byte)0);
            userStringHeap.Write((byte)0);
            if (assembly != null)
            {
                PublicKey = assembly.PublicKeyOrToken;
                SignatureKeyLength = 0;
                for (var j = 0; j < assembly.Attributes.Count; j++)
                {
                    var node = assembly.Attributes[j];
                    if (node == null) continue;
                    if (node.Type.ToString() == "System.Reflection.AssemblySignatureKeyAttribute")
                    {
                        var rawString = node.GetPositionalArgument(0).ToString();
                        SignatureKeyLength = rawString.Length / 2;
                    }
                }
            }
        }

        internal static void WritePE(Module /*!*/ module, string debugSymbolsLocation, BinaryWriter /*!*/ writer)
        {
            var ir2md = new Ir2md(module);
            try
            {
                ir2md.SetupMetadataWriter(debugSymbolsLocation);
                var mdWriter = ir2md.writer;
                mdWriter.WritePE(writer);
            }
            finally
            {
#if !ROTOR
                if (ir2md.symWriter != null)
                    ir2md.symWriter.Close();
#endif
                ir2md.assembly = null;
                ir2md.assemblyRefEntries = null;
                ir2md.assemblyRefIndex = null;
                ir2md.blobHeap = null;
                ir2md.blobHeapIndex = null;
                ir2md.blobHeapStringIndex = null;
                ir2md.classLayoutEntries = null;
                ir2md.constantTableEntries = null;
                ir2md.documentMap = null;
                ir2md.eventEntries = null;
                ir2md.eventIndex = null;
                ir2md.eventMapEntries = null;
                ir2md.eventMapIndex = null;
                ir2md.exceptionBlock = null;
                ir2md.fieldEntries = null;
                ir2md.fieldIndex = null;
                ir2md.fieldLayoutEntries = null;
                ir2md.fieldRvaEntries = null;
                ir2md.fileTableEntries = null;
                ir2md.fileTableIndex = null;
                ir2md.genericParamConstraintEntries = null;
                ir2md.genericParamEntries = null;
                ir2md.genericParameters = null;
                ir2md.genericParamIndex = null;
                ir2md.guidEntries = null;
                ir2md.guidIndex = null;
                ir2md.implMapEntries = null;
                ir2md.interfaceEntries = null;
                ir2md.marshalEntries = null;
                ir2md.memberRefEntries = null;
                ir2md.memberRefIndex = null;
                ir2md.methodBodiesHeap = null;
                ir2md.methodBodiesHeapIndex = null;
                ir2md.methodBodyHeap = null;
                ir2md.methodEntries = null;
                ir2md.methodImplEntries = null;
                ir2md.methodIndex = null;
                ir2md.methodInfo = null;
#if !MinimalReader && !CodeContracts
        ir2md.currentMethod = null;
#endif
                ir2md.methodSemanticsEntries = null;
                ir2md.methodSpecEntries = null;
                ir2md.methodSpecIndex = null;
                ir2md.module = null;
                ir2md.moduleRefEntries = null;
                ir2md.moduleRefIndex = null;
                ir2md.nestedClassEntries = null;
                ir2md.nodesWithCustomAttributes = null;
                ir2md.nodesWithSecurityAttributes = null;
                ir2md.paramEntries = null;
                ir2md.paramIndex = null;
                ir2md.propertyEntries = null;
                ir2md.propertyIndex = null;
                ir2md.propertyMapEntries = null;
                ir2md.propertyMapIndex = null;
                ir2md.PublicKey = null;
                ir2md.resourceDataHeap = null;
                ir2md.sdataHeap = null;
                ir2md.standAloneSignatureEntries = null;
                ir2md.stringHeap = null;
                ir2md.stringHeapIndex = null;
#if !ROTOR
                ir2md.symWriter = null;
#endif
                ir2md.tlsHeap = null;
                ir2md.typeDefEntries = null;
                ir2md.typeDefIndex = null;
                ir2md.typeParameterNumber = null;
                ir2md.typeRefEntries = null;
                ir2md.typeRefIndex = null;
                ir2md.typeSpecEntries = null;
                ir2md.typeSpecIndex = null;
                ir2md.unspecializedFieldFor = null;
                ir2md.unspecializedMethodFor = null;
                ir2md.userStringHeap = null;
                ir2md.userStringHeapIndex = null;
                ir2md.writer = null;
                ir2md = null;
            }
        }

        private static Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
        private static Guid IID_IClassFactory = new Guid("00000001-0000-0000-C000-000000000046");

        [ComImport]
        [Guid("00000001-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IClassFactory
        {
            int CreateInstance(
                [In] [MarshalAs(UnmanagedType.Interface)]
                object unused,
                [In] ref Guid refiid,
                [MarshalAs(UnmanagedType.Interface)] out object ppunk);

            int LockServer(
                int fLock);
        }

        private delegate int GetClassObjectDelegate([In] ref Guid refclsid,
            [In] ref Guid refiid,
            [MarshalAs(UnmanagedType.Interface)] out IClassFactory ppUnk);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern int LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern GetClassObjectDelegate GetProcAddress(int hModule, string lpProcName);

        private static object CrossCompileActivate(string server, Guid guid)
        {
            // Poor man's version of Activator.CreateInstance or CoCreate
            object o = null;
            var hmod = LoadLibrary(server);
            if (hmod != 0)
            {
                var del = GetProcAddress(hmod, "DllGetClassObject");
                if (del != null)
                {
                    IClassFactory icf;
                    var hr = del(ref guid, ref IID_IClassFactory, out icf);
                    if (hr == 0 && icf != null)
                    {
                        object temp = null;
                        hr = icf.CreateInstance(null, ref IID_IUnknown, out temp);
                        if (hr == 0) o = temp;
                    }
                }
            }

            return o;
        }

        private void SetupMetadataWriter(string debugSymbolsLocation)
        {
            var v = TargetPlatform.TargetVersion;
            UseGenerics = TargetPlatform.UseGenerics;
#if !ROTOR
            if (debugSymbolsLocation != null)
            {
                // If targeting RTM (Version.Major = 1 and Version.Minor = 0)
                // then use Symwriter.pdb as ProgID else use CorSymWriter_SxS
                // (Note that RTM version 1.0.3705 has Assembly version 1.0.3300,
                // hence the <= 3705 expression.  This also leaves room for RTM SP releases
                // with slightly different build numbers).
                Type t = null;
                if (v.Major == 1 && v.Minor == 0 && v.Build <= 3705)
                    try
                    {
                        t = Type.GetTypeFromProgID("Symwriter.pdb", false);
                        symWriter = (ISymUnmanagedWriter)Activator.CreateInstance(t);
                        if (symWriter != null)
                            symWriter.Initialize(this, debugSymbolsLocation, null, true);
                    }
                    catch (Exception)
                    {
                        t = null;
                        symWriter = null;
                    }

                if (t == null)
                {
                    Debug.Assert(symWriter == null);
                    t = Type.GetTypeFromProgID("CorSymWriter_SxS", false);
                    if (t != null)
                    {
                        var guid = t.GUID;

                        // If the compiler was built with Whidbey, then mscoree will pick a matching
                        // diasymreader.dll out of the Whidbey directory.  But if we are cross-
                        // compiling, this is *NOT* what we want.  Instead, we want to override
                        // the shim's logic and explicitly pick a diasymreader.dll from the place
                        // that matches the version of the output file we are emitting.  This is
                        // strictly illegal by the CLR's rules.  However, the CLR does not yet
                        // support cross-compilation, so we have little choice.
                        if (!UseGenerics)
                        {
                            var vcompiler = typeof(object).Assembly.GetName().Version;
                            if (vcompiler.Major >= 2)
                            {
                                // This is the only cross-compilation case we currently support.
                                var server = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location),
                                    "..\\v1.1.4322\\diasymreader.dll");
                                var o = CrossCompileActivate(server, guid);
                                symWriter = (ISymUnmanagedWriter)o;
                            }
                        }

                        if (symWriter == null) symWriter = (ISymUnmanagedWriter)Activator.CreateInstance(t);
                        if (symWriter != null)
                            symWriter.Initialize(this, debugSymbolsLocation, null, true);
                    }
                    else
                    {
                        throw new DebugSymbolsCouldNotBeWrittenException();
                    }
                }
            }
#endif
            //Visit the module, building lists etc.
            VisitModule(module);
            //Use the lists to populate the tables in the metadata writer
#if !ROTOR
            var writer = this.writer = new MetadataWriter(symWriter);
#else
      MetadataWriter writer = this.writer = new MetadataWriter();
#endif
            writer.UseGenerics = UseGenerics;
            if (module.EntryPoint != null)
            {
                writer.entryPointToken = GetMethodToken(module.EntryPoint);
#if !ROTOR
                if (symWriter != null) symWriter.SetUserEntryPoint((uint)writer.entryPointToken);
#endif
            }

            writer.dllCharacteristics = module.DllCharacteristics;
            writer.moduleKind = module.Kind;
            writer.peKind = module.PEKind;
            writer.TrackDebugData = module.TrackDebugData;
            writer.fileAlignment = module.FileAlignment;
            if (writer.fileAlignment < 512) writer.fileAlignment = 512;
            writer.PublicKey = PublicKey;
            writer.SignatureKeyLength = SignatureKeyLength;
            if (assembly != null) PopulateAssemblyTable();
            PopulateClassLayoutTable();
            PopulateConstantTable();
            PopulateGenericParamTable(); //Needs to happen before PopulateCustomAttributeTable since it the latter refers to indices in the sorted table
            PopulateCustomAttributeTable();
            PopulateDeclSecurityTable();
            PopulateEventMapTable();
            PopulateEventTable();
            PopulateExportedTypeTable();
            PopulateFieldTable();
            PopulateFieldLayoutTable();
            PopulateFieldRVATable();
            PopulateManifestResourceTable(); //This needs to happen before PopulateFileTable because resources are not visited separately
            PopulateFileTable();
            PopulateGenericParamConstraintTable();
            PopulateImplMapTable();
            PopulateInterfaceImplTable();
            PopulateMarshalTable();
            PopulateMethodTable();
            PopulateMethodImplTable();
            PopulateMemberRefTable();
            PopulateMethodSemanticsTable();
            PopulateMethodSpecTable();
            PopulateModuleTable();
            PopulateModuleRefTable();
            PopulateNestedClassTable();
            PopulateParamTable();
            PopulatePropertyTable();
            PopulatePropertyMapTable();
            PopulateStandAloneSigTable();
            PopulateTypeDefTable();
            PopulateTypeRefTable();
            PopulateTypeSpecTable();
            PopulateGuidTable();
            PopulateAssemblyRefTable();
            this.writer.BlobHeap = blobHeap.BaseStream; //this.blobHeap = null;
            this.writer.SdataHeap = sdataHeap.BaseStream; //this.sdataHeap = null;
            this.writer.TlsHeap = tlsHeap.BaseStream; //this.tlsHeap = null;
            this.writer.StringHeap = stringHeap.BaseStream; //this.stringHeap = null;
            this.writer.UserstringHeap = userStringHeap.BaseStream; //this.userStringHeap = null;
            this.writer.MethodBodiesHeap = methodBodiesHeap.BaseStream; //this.methodBodiesHeap = null;
            this.writer.ResourceDataHeap = resourceDataHeap.BaseStream; //this.resourceDataHeap = null;
            this.writer.Win32Resources = module.Win32Resources;
        }

        private int GetAssemblyRefIndex(AssemblyNode /*!*/ assembly)
        {
            if (assembly.Location == "unknown:location")
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    ExceptionStrings.UnresolvedAssemblyReferenceNotAllowed, assembly.Name));
            var index = assemblyRefIndex[assembly.UniqueKey];
            if (index == null)
            {
                index = assemblyRefEntries.Count + 1;
                var aref = new AssemblyReference(assembly);
                if (module.UsePublicKeyTokensForAssemblyReferences)
                {
                    aref.PublicKeyOrToken = aref.PublicKeyToken;
                    aref.HashValue = null;
                    aref.Flags = aref.Flags & ~AssemblyFlags.PublicKey;
                }

                assemblyRefEntries.Add(aref);
                assemblyRefIndex[assembly.UniqueKey] = index;
            }

            return (int)index;
        }

        private int GetBlobIndex(ExpressionList expressions, ParameterList parameters)
        {
            var sig = new MemoryStream();
            var signature = new BinaryWriter(sig);
            WriteCustomAttributeSignature(expressions, parameters, false, signature);
            var sigBytes = sig.ToArray();
            var length = sigBytes.Length;
            var index = blobHeap.BaseStream.Position;
            WriteCompressedInt(blobHeap, length);
            blobHeap.BaseStream.Write(sigBytes, 0, length);
            return index;
        }

        private void WriteCustomAttributeSignature(ExpressionList expressions, ParameterList parameters,
            bool onlyWriteNamedArguments, BinaryWriter signature)
        {
            var n = parameters == null ? 0 : parameters.Count;
            var m = expressions == null ? 0 : expressions.Count;
            Debug.Assert(m >= n);
            var numNamed = m > n ? m - n : 0;
            if (onlyWriteNamedArguments)
            {
                WriteCompressedInt(signature, numNamed);
            }
            else
            {
                signature.Write((short)1);
                if (parameters != null && expressions != null)
                    for (var i = 0; i < n; i++)
                    {
                        var p = parameters[i];
                        var e = expressions[i];
                        if (p == null || e == null) continue;
                        var l = e as Literal;
                        if (l == null)
                        {
                            Debug.Assert(false);
                            continue;
                        }

                        WriteCustomAttributeLiteral(signature, l, p.Type == CoreSystemTypes.Object);
                    }

                signature.Write((short)numNamed);
            }

            if (expressions != null)
                for (var i = n; i < m; i++)
                {
                    var e = expressions[i];
                    var narg = e as NamedArgument;
                    if (narg == null)
                    {
                        Debug.Assert(false);
                        continue;
                    }

                    signature.Write((byte)(narg.IsCustomAttributeProperty ? 0x54 : 0x53));
                    if (narg.ValueIsBoxed)
                    {
                        signature.Write((byte)ElementType.BoxedEnum);
                    }
                    else if (narg.Value.Type is EnumNode)
                    {
                        signature.Write((byte)ElementType.Enum);
                        WriteSerializedTypeName(signature, narg.Value.Type);
                    }
                    else if (narg.Value.Type == CoreSystemTypes.Type)
                    {
                        signature.Write((byte)ElementType.Type);
                    }
                    else if (narg.Value.Type is ArrayType)
                    {
                        var arrT = (ArrayType)narg.Value.Type;
                        if (arrT.ElementType == CoreSystemTypes.Type)
                        {
                            signature.Write((byte)ElementType.SzArray);
                            signature.Write((byte)ElementType.Type);
                        }
                        else
                        {
                            if (arrT.ElementType is EnumNode)
                            {
                                signature.Write((byte)ElementType.SzArray);
                                signature.Write((byte)ElementType.Enum);
                                WriteSerializedTypeName(signature, arrT.ElementType);
                            }
                            else
                            {
                                WriteTypeSignature(signature, narg.Value.Type);
                            }
                        }
                    }
                    else
                    {
                        WriteTypeSignature(signature, narg.Value.Type);
                    }

                    signature.Write(narg.Name.Name, false);
                    WriteCustomAttributeLiteral(signature, (Literal)narg.Value, narg.ValueIsBoxed);
                }
        }

        private int GetBlobIndex(byte[] /*!*/ blob)
        {
            var indexOb = blobHeapIndex[blob];
            if (indexOb != null) return (int)indexOb;
            var index = blobHeap.BaseStream.Position;
            var length = blob.Length;
            WriteCompressedInt(blobHeap, length);
            blobHeap.BaseStream.Write(blob, 0, length);
            blobHeapIndex[blob] = index;
            return index;
        }

        private int GetBlobIndex(string /*!*/ str)
        {
            var indexOb = blobHeapStringIndex[str];
            if (indexOb != null) return (int)indexOb;
            var index = blobHeap.BaseStream.Position;
            blobHeap.Write(str);
            blobHeapStringIndex[str] = index;
            return index;
        }

        private int GetBlobIndex(Field /*!*/ field)
        {
            if (field != null && field.DeclaringType != null && field.DeclaringType.Template != null &&
                field.DeclaringType.Template.IsGeneric)
                field = GetUnspecializedField(field);
            var sig = new MemoryStream();
            var signature = new BinaryWriter(sig);
            signature.Write((byte)0x6);
            var fieldType = field.Type;
            if (field.IsVolatile && !(fieldType is RequiredModifier) && SystemTypes.IsVolatile != null)
                fieldType = RequiredModifier.For(SystemTypes.IsVolatile, fieldType);
#if ExtendedRuntime
      if (field.HasOutOfBandContract) fieldType = TypeNode.DeepStripModifiers(fieldType, null, SystemTypes.NonNullType);
#endif
            if (fieldType == null)
            {
                Debug.Fail("");
                fieldType = SystemTypes.Object;
            }

            WriteTypeSignature(signature, fieldType, true);
            return GetBlobIndex(sig.ToArray());
        }

        private int GetBlobIndex(MarshallingInformation /*!*/ marshallingInformation)
        {
            var sig = new MemoryStream();
            var signature = new BinaryWriter(sig);
            signature.Write((byte)marshallingInformation.NativeType);
            switch (marshallingInformation.NativeType)
            {
                case NativeType.SafeArray:
                    signature.Write((byte)marshallingInformation.ElementType);
                    if (marshallingInformation.Class != null && marshallingInformation.Class.Length > 0)
                        signature.Write(marshallingInformation.Class, false);
                    break;
                case NativeType.LPArray:
                    signature.Write((byte)marshallingInformation.ElementType);
                    if (marshallingInformation.ParamIndex >= 0 || marshallingInformation.ElementSize > 0)
                    {
                        if (marshallingInformation.ParamIndex < 0)
                        {
                            Debug.Fail("MarshallingInformation.ElementSize > 0 should imply that ParamIndex >= 0");
                            marshallingInformation.ParamIndex = 0;
                        }

                        WriteCompressedInt(signature, marshallingInformation.ParamIndex);
                    }

                    if (marshallingInformation.ElementSize > 0)
                    {
                        WriteCompressedInt(signature, marshallingInformation.ElementSize);
                        if (marshallingInformation.NumberOfElements > 0)
                            WriteCompressedInt(signature, marshallingInformation.NumberOfElements);
                    }

                    break;
                case NativeType.ByValArray:
                    WriteCompressedInt(signature, marshallingInformation.Size);
                    if (marshallingInformation.ElementType != NativeType.NotSpecified)
                        signature.Write((byte)marshallingInformation.ElementType);
                    break;
                case NativeType.ByValTStr:
                    WriteCompressedInt(signature, marshallingInformation.Size);
                    break;
                case NativeType.Interface:
                    if (marshallingInformation.Size > 0)
                        WriteCompressedInt(signature, marshallingInformation.Size);
                    break;
                case NativeType.CustomMarshaler:
                    signature.Write((short)0);
                    signature.Write(marshallingInformation.Class);
                    signature.Write(marshallingInformation.Cookie);
                    break;
            }

            return GetBlobIndex(sig.ToArray());
        }

        private int GetBlobIndex(Literal /*!*/ literal)
        {
            var index = blobHeap.BaseStream.Position;
            var lType = literal.Type;
            var eType = lType as EnumNode;
            if (eType != null) lType = eType.UnderlyingType;
            var ic = literal.Value as IConvertible;
            if (ic == null) ic = "";
            switch (lType.typeCode)
            {
                case ElementType.Boolean:
                    blobHeap.Write((byte)1);
                    blobHeap.Write(ic.ToBoolean(null));
                    break;
                case ElementType.Char:
                    blobHeap.Write((byte)2);
                    blobHeap.Write(ic.ToChar(null));
                    break;
                case ElementType.Int8:
                    blobHeap.Write((byte)1);
                    blobHeap.Write(ic.ToSByte(null));
                    break;
                case ElementType.UInt8:
                    blobHeap.Write((byte)1);
                    blobHeap.Write(ic.ToByte(null));
                    break;
                case ElementType.Int16:
                    blobHeap.Write((byte)2);
                    blobHeap.Write(ic.ToInt16(null));
                    break;
                case ElementType.UInt16:
                    blobHeap.Write((byte)2);
                    blobHeap.Write(ic.ToUInt16(null));
                    break;
                case ElementType.Int32:
                    blobHeap.Write((byte)4);
                    blobHeap.Write(ic.ToInt32(null));
                    break;
                case ElementType.UInt32:
                    blobHeap.Write((byte)4);
                    blobHeap.Write(ic.ToUInt32(null));
                    break;
                case ElementType.Int64:
                    blobHeap.Write((byte)8);
                    blobHeap.Write(ic.ToInt64(null));
                    break;
                case ElementType.UInt64:
                    blobHeap.Write((byte)8);
                    blobHeap.Write(ic.ToUInt64(null));
                    break;
                case ElementType.Single:
                    blobHeap.Write((byte)4);
                    blobHeap.Write(ic.ToSingle(null));
                    break;
                case ElementType.Double:
                    blobHeap.Write((byte)8);
                    blobHeap.Write(ic.ToDouble(null));
                    break;
                case ElementType.String:
                    blobHeap.Write((string)literal.Value, false);
                    break;
                case ElementType.Array:
                case ElementType.Class:
                case ElementType.Object:
                case ElementType.Reference:
                case ElementType.SzArray:
                    blobHeap.Write((byte)4);
                    blobHeap.Write(0);
                    break; //REVIEW: standard implies this should be 0, peverify thinks otherwise.
                default:
                    Debug.Assert(false, "Unexpected Literal type");
                    return 0;
            }

            return index;
        }

        private int GetBlobIndex(FunctionPointer /*!*/ fp)
        {
            var sig = new MemoryStream();
            var signature = new BinaryWriter(sig);
            WriteMethodSignature(signature, fp);
            return GetBlobIndex(sig.ToArray());
        }

        private int GetBlobIndex(Method /*!*/ method, bool methodSpecSignature)
        {
            var sig = new MemoryStream();
            var signature = new BinaryWriter(sig);
            if (methodSpecSignature)
                WriteMethodSpecSignature(signature, method);
            else
                WriteMethodSignature(signature, method);
            return GetBlobIndex(sig.ToArray());
        }

        private int GetBlobIndex(AttributeList /*!*/ securityAttributes)
        {
            var sig = new MemoryStream();
            var signature = new BinaryWriter(sig);
            signature.Write((byte)'.');
            WriteCompressedInt(signature, securityAttributes.Count);
            foreach (var attr in securityAttributes)
                WriteSecurityAttribute(signature, attr);
            return GetBlobIndex(sig.ToArray());
        }

        private void WriteSecurityAttribute(BinaryWriter signature, AttributeNode attr)
        {
            var isAssemblyQualified = true;
            var attrTypeName = GetSerializedTypeName(attr.Type, ref isAssemblyQualified);
            if (!isAssemblyQualified) attrTypeName += ", " + attr.Type.DeclaringModule.ContainingAssembly.StrongName;
            signature.Write(attrTypeName);
            var sig = new MemoryStream();
            var casig = new BinaryWriter(sig);
            var mb = attr.Constructor as MemberBinding;
            if (mb == null) return;
            var constructor = mb.BoundMember as InstanceInitializer;
            if (constructor == null) return;
            WriteCustomAttributeSignature(attr.Expressions, constructor.Parameters, true, casig);
            var sigBytes = sig.ToArray();
            var length = sigBytes.Length;
            WriteCompressedInt(signature, length);
            signature.BaseStream.Write(sigBytes, 0, length);
        }

        private int GetBlobIndex(Property /*!*/ prop)
        {
            var sig = new MemoryStream();
            var signature = new BinaryWriter(sig);
            WritePropertySignature(signature, prop);
            return GetBlobIndex(sig.ToArray());
        }

        private int GetBlobIndex(TypeNode /*!*/ type)
        {
            var sig = new MemoryStream();
            var signature = new BinaryWriter(sig);
            WriteTypeSignature(signature, type, true);
            return GetBlobIndex(sig.ToArray());
        }

        private int GetCustomAttributeParentCodedIndex(Node /*!*/ node)
        {
            switch (node.NodeType)
            {
                case NodeType.InstanceInitializer:
                case NodeType.StaticInitializer:
                case NodeType.Method: return GetMethodIndex((Method)node) << 5;
                case NodeType.Field: return (GetFieldIndex((Field)node) << 5) | 1;
                case NodeType.Parameter: return (GetParamIndex((Parameter)node) << 5) | 4;
                case NodeType.Class:
                case NodeType.DelegateNode:
                case NodeType.EnumNode:
                case NodeType.Interface:
                case NodeType.Struct:
#if !MinimalReader
                case NodeType.TupleType:
                case NodeType.TypeAlias:
                case NodeType.TypeIntersection:
                case NodeType.TypeUnion:
#endif
                    var t = (TypeNode)node;
                    if (IsStructural(t) && (!t.IsGeneric || (t.Template != null &&
                                                             t.ConsolidatedTemplateArguments != null &&
                                                             t.ConsolidatedTemplateArguments.Count > 0)))
                        return (GetTypeSpecIndex(t) << 5) | 13;
                    return (GetTypeDefIndex(t) << 5) | 3;
                case NodeType.ClassParameter:
                case NodeType.TypeParameter:
                    if (!UseGenerics) goto case NodeType.Class;
                    return (GetGenericParamIndex((TypeNode)node) << 5) | 19;
                case NodeType.Property: return (GetPropertyIndex((Property)node) << 5) | 9;
                case NodeType.Event: return (GetEventIndex((Event)node) << 5) | 10;
                case NodeType.Module: return (1 << 5) | 7;
                case NodeType.Assembly: return (1 << 5) | 14;
                default:
                    Debug.Assert(false, "Unexpect custom attribute parent");
                    return 0;
            }
        }
#if !ROTOR
        private ISymUnmanagedDocumentWriter GetDocumentWriter(Document /*!*/ doc)
            //^ requires this.symWriter != null;
        {
            var key = Identifier.For(doc.Name).UniqueIdKey;
            var writer = documentMap[key];
            if (writer == null)
            {
                writer = symWriter.DefineDocument(doc.Name, ref doc.Language, ref doc.LanguageVendor,
                    ref doc.DocumentType);
                documentMap[key] = writer;
            }

            return (ISymUnmanagedDocumentWriter)writer;
        }

        private ISymUnmanagedDocumentWriter GetArbitraryDocWriter()
            //^ requires this.symWriter != null;
        {
            foreach (var writer in documentMap.Values) return (ISymUnmanagedDocumentWriter)writer;
            return null;
        }

#endif
        private int GetEventIndex(Event /*!*/ e)
        {
            return (int)eventIndex[e.UniqueKey];
        }

        private int GetFieldIndex(Field /*!*/ f)
        {
            var index = fieldIndex[f.UniqueKey];
            if (index == null)
            {
                if (fieldEntries == null) return 1;
                index = fieldEntries.Count + 1;
                fieldEntries.Add(f);
                fieldIndex[f.UniqueKey] = index;
                if (f.DefaultValue != null && !(f.DefaultValue.Value is Parameter))
                    constantTableEntries.Add(f);
                if (!f.IsStatic && f.DeclaringType != null && (f.DeclaringType.Flags & TypeFlags.ExplicitLayout) != 0)
                    fieldLayoutEntries.Add(f);
                if ((f.Flags & FieldFlags.HasFieldRVA) != 0)
                    fieldRvaEntries.Add(f);
                if (f.MarshallingInformation != null)
                    marshalEntries.Add(f);
            }

            return (int)index;
        }

        private int GetGenericParamIndex(TypeNode /*!*/ gp)
        {
            return (int)genericParamIndex[gp.UniqueKey];
        }

        private int GetFieldToken(Field /*!*/ f)
        {
            if (f.DeclaringType == null ||
                (f.DeclaringType.DeclaringModule == module && !IsStructural(f.DeclaringType)))
                return 0x04000000 | GetFieldIndex(f);
            return 0x0a000000 | GetMemberRefIndex(f);
        }

        private bool IsStructural(TypeNode type)
        {
            if (type == null) return false;
            if (UseGenerics && (type.IsGeneric || (type.Template != null && type.Template.IsGeneric))) return true;
            switch (type.NodeType)
            {
                case NodeType.ArrayType:
                case NodeType.Pointer:
                case NodeType.Reference:
                case NodeType.OptionalModifier:
                case NodeType.RequiredModifier:
                    return true;
                case NodeType.ClassParameter:
                case NodeType.TypeParameter:
                    return UseGenerics;
            }

            return false;
        }

        private int GetFileTableIndex(Module /*!*/ module)
        {
            var index = fileTableIndex[module];
            if (index == null)
            {
                index = fileTableEntries.Count + 1;
                fileTableEntries.Add(module);
                fileTableIndex[module] = index;
            }

            return (int)index;
        }

        private int GetGuidIndex(Guid guid)
        {
            var index = guidIndex[guid];
            if (index == null)
            {
                index = guidEntries.Count + 1;
                guidEntries.Add(guid);
                guidIndex[guid] = index;
            }

            return (int)index;
        }

        internal int GetLocalVarIndex(Local /*!*/ loc)
        {
#if !MinimalReader
            var lb = loc as LocalBinding;
            if (lb != null) loc = lb.BoundLocal;
#endif
            if (StripOptionalModifiersFromLocals)
                loc.Type = TypeNode.StripModifiers(loc.Type);
            var methInfo = methodInfo;

            if (methInfo.localVarSignature == null)
            {
                methInfo.localVarSignature = new BinaryWriter(new MemoryStream());
                methInfo.localVarSignature.Write((short)0);
                methInfo.localVarIndex = new TrivialHashtable<int>();
                methInfo.localVarSigTok = 0x11000000 | GetStandAloneSignatureIndex(methInfo.localVarSignature);
            }
#if true
            int index;
            if (!methInfo.localVarIndex.TryGetValue(loc.UniqueKey, out index))
            {
#else
      object index = methInfo.localVarIndex[loc.UniqueKey];
      if (index == null) {
#endif
                methInfo.localVarIndex[loc.UniqueKey] = index = methInfo.localVarIndex.Count;
#if !ROTOR
                var startPosition = 0;
                if (symWriter != null && loc.Name != null && loc.Name.UniqueIdKey != Identifier.Empty.UniqueIdKey)
                {
                    methInfo.debugLocals.Add(loc);
                    methInfo.signatureOffsets.Add(startPosition = methInfo.localVarSignature.BaseStream.Position);
                    if (loc.Pinned) methInfo.localVarSignature.Write((byte)ElementType.Pinned);
                    WriteTypeSignature(methInfo.localVarSignature, loc.Type, true);
                    methInfo.signatureLengths.Add(methInfo.localVarSignature.BaseStream.Position - startPosition);
                }
                else
                {
#endif
                    if (loc.Pinned) methInfo.localVarSignature.Write((byte)ElementType.Pinned);
                    WriteTypeSignature(methInfo.localVarSignature, loc.Type, true);
#if !ROTOR
                }
#endif
            }

            return index;
        }

        private int GetMemberRefParentEncoded(TypeNode type)
        {
            if (type == null) return 0;
            if (IsStructural(type)) return (GetTypeSpecIndex(type) << 3) | 4;
            if (type.DeclaringModule == module) return GetTypeDefIndex(type) << 3;
            if (type.DeclaringModule != null) return (GetTypeRefIndex(type) << 3) | 1;
            if (type.typeCode == ElementType.Class || type.typeCode == ElementType.ValueType)
                return GetTypeDefIndex(type) << 3; //REVIEW: when does this happen?
            Debug.Assert(false);
            return 0;
        }

        private int GetMemberRefIndex(Member /*!*/ m)
        {
            int index;
            if (!memberRefIndex.TryGetValue(m.UniqueKey, out index))
            {
                index = memberRefEntries.Count + 1;
                memberRefEntries.Add(m);
                memberRefIndex[m.UniqueKey] = index;
                var type = m.DeclaringType;
                VisitReferencedType(type);
            }

            return index;
        }

        private class VarargMethodCallSignature : FunctionPointer
        {
            internal readonly Method method;

            internal VarargMethodCallSignature(Method /*!*/ method, TypeNodeList /*!*/ parameterTypes)
                : base(parameterTypes, method.ReturnType, method.Name)
            {
                this.method = method;
                DeclaringType = method.DeclaringType;
            }
        }

        private int GetMemberRefToken(Method /*!*/ m, ExpressionList arguments)
        {
            var numArgs = arguments == null ? 0 : arguments.Count;
            var parTypes = new TypeNodeList(numArgs);
            var varArgStart = m.Parameters.Count;
            for (var i = 0; i < varArgStart; i++)
                parTypes.Add(m.Parameters[i].Type);
            for (var i = varArgStart; i < numArgs; i++) //^ assert arguments != null;
                parTypes.Add(arguments[i].Type);
            var sig = new VarargMethodCallSignature(m, parTypes);
            sig.VarArgStart = varArgStart;
            sig.CallingConvention = m.CallingConvention;
            return 0x0a000000 | GetMemberRefIndex(sig);
        }

        private int GetMethodDefOrRefEncoded(Method /*!*/ m)
        {
            if (m.DeclaringType.DeclaringModule == module && !IsStructural(m.DeclaringType))
                return GetMethodIndex(m) << 1;
            return (GetMemberRefIndex(m) << 1) | 0x1;
        }

        private int GetMethodIndex(Method /*!*/ m)
        {
            int index;
            if (!methodIndex.TryGetValue(m.UniqueKey, out index))
            {
                if (methodEntries == null) return 1;
                index = methodEntries.Count + 1;
                methodEntries.Add(m);
                methodIndex[m.UniqueKey] = index;
                if (m.ReturnTypeMarshallingInformation != null ||
                    (m.ReturnAttributes != null && m.ReturnAttributes.Count > 0))
                {
                    var p = new Parameter();
                    p.ParameterListIndex = -1;
                    p.Attributes = m.ReturnAttributes;
                    if (m.ReturnTypeMarshallingInformation != null)
                    {
                        p.MarshallingInformation = m.ReturnTypeMarshallingInformation;
                        p.Flags = ParameterFlags.HasFieldMarshal;
                        marshalEntries.Add(p);
                    }

                    paramEntries.Add(p);
                    paramIndex[m.UniqueKey] = paramEntries.Count;
                    paramIndex[p.UniqueKey] = paramEntries.Count;
                    VisitAttributeList(p.Attributes, p);
                }

                var offset = m.IsStatic ? 0 : 1;
                if (m.Parameters != null)
                    for (int i = 0, n = m.Parameters.Count; i < n; i++)
                    {
                        var p = m.Parameters[i];
                        if (p == null) continue;
                        if (p == null) continue;
                        if (p.DeclaringMethod == null) p.DeclaringMethod = m;
                        p.ParameterListIndex = i;
                        p.ArgumentListIndex = i + offset;
                        var j = paramEntries.Count + 1;
                        paramEntries
                            .Add(p); //TODO: provide a way to suppress the param table entries unless param has custom attributes or flags
                        paramIndex[p.UniqueKey] = j;
                        if (p.DefaultValue != null)
                            constantTableEntries.Add(p);
                        if (p.MarshallingInformation != null)
                            marshalEntries.Add(p);
                    }

                if (m.IsGeneric)
                    VisitGenericParameterList(m, m.TemplateParameters);
            }

            return index;
        }

        private int GetMethodSpecIndex(Method /*!*/ m)
        {
            var structuralKey = m.UniqueKey;
            var blobIndex = GetBlobIndex(m, true);
            if (m.Template != null)
                structuralKey = (m.Template.UniqueKey << 8) + blobIndex;
            else
                Debug.Assert(false);
            var index = methodSpecIndex[m.UniqueKey];
            if (index == null)
            {
                index = methodSpecIndex[structuralKey];
                if (index is int)
                {
                    var otherMethod = methodSpecEntries[(int)index - 1];
                    if (otherMethod != null && otherMethod.Template == m.Template &&
                        blobIndex == GetBlobIndex(otherMethod, true))
                        return (int)index;
                }

                index = methodSpecEntries.Count + 1;
                methodSpecEntries.Add(m);
                methodSpecIndex[m.UniqueKey] = index;
                methodSpecIndex[structuralKey] = index;
                GetMemberRefIndex(m.Template);
                var templ = m.Template;
                if (templ != null)
                {
                    while (templ.Template != null) templ = templ.Template;
                    var templParams = templ.TemplateParameters;
                    if (templParams != null)
                        for (int i = 0, n = templParams.Count; i < n; i++)
                        {
                            var templParam = templParams[i];
                            if (templParam == null) continue;
                            typeParameterNumber[templParam.UniqueKey] = -(i + 1);
                        }
                }
            }

            return (int)index;
        }

        private int GetMethodToken(Method /*!*/ m)
        {
            if (UseGenerics && m.Template != null && m.Template.IsGeneric)
                return 0x2b000000 | GetMethodSpecIndex(m);
            if (m.DeclaringType.DeclaringModule == module && !IsStructural(m.DeclaringType))
                return 0x06000000 | GetMethodIndex(m);
            return 0x0a000000 | GetMemberRefIndex(m);
        }

        internal int GetMethodDefToken(Method /*!*/ m)
        {
            if (m.DeclaringType.DeclaringModule == module)
                return 0x06000000 | GetMethodIndex(m);
            return 0x0a000000 | GetMemberRefIndex(m);
        }

        private int GetMethodBodiesHeapIndex(Method /*!*/ m)
        {
            return (int)methodBodiesHeapIndex[m.UniqueKey];
        }

        private int GetModuleRefIndex(Module /*!*/ module)
        {
            if (module.Location == "unknown:location")
                throw new InvalidOperationException(ExceptionStrings.UnresolvedModuleReferenceNotAllowed);
            var index = moduleRefIndex[module.Name];
            if (index == null)
            {
                index = moduleRefEntries.Count + 1;
                moduleRefEntries.Add(new ModuleReference(module.Name, module));
                moduleRefIndex[module.Name] = index;
                if (module.HashValue != null && module.HashValue.Length > 0)
                    GetFileTableIndex(module);
            }

            return (int)index;
        }

        private int GetOffset(Block target, int addressOfNextInstruction)
        {
            if (target == null) return 0;
            var fixupLocation = methodBodyHeap.BaseStream.Position;
            var ob = methodInfo.fixupIndex[target.UniqueKey];
            if (ob is int) return (int)ob - addressOfNextInstruction;
            var fixup = new Fixup();
            fixup.addressOfNextInstruction = addressOfNextInstruction;
            fixup.fixupLocation = fixupLocation;
            fixup.shortOffset = false;
            fixup.nextFixUp = (Fixup)ob;
            methodInfo.fixupIndex[target.UniqueKey] = fixup;
            return 0;
        }

        private int GetOffset(Block target, ref bool shortOffset)
        {
            if (target == null) return 0;
            var fixupLocation = methodBodyHeap.BaseStream.Position + 1;
            var ob = methodInfo.fixupIndex[target.UniqueKey];
            if (ob is int)
            {
                var targetAddress = (int)ob;
                var offset = targetAddress - (fixupLocation + 1);
                if (-128 > offset || offset > 127)
                {
                    offset = targetAddress - (fixupLocation + 4);
                    Debug.Assert(offset < -128, "Forward short branch out of range");
                    shortOffset = false;
                }
                else
                {
                    shortOffset = true;
                }

                return offset;
            }

            var fixup = new Fixup();
            fixup.fixupLocation = fixup.addressOfNextInstruction = fixupLocation;
            if (shortOffset) fixup.addressOfNextInstruction += 1;
            else fixup.addressOfNextInstruction += 4;
            fixup.shortOffset = shortOffset;
            fixup.nextFixUp = (Fixup)ob;
            methodInfo.fixupIndex[target.UniqueKey] = fixup;
            return 0;
        }

        private int GetParamIndex(Parameter p)
        {
            if (p == null) return 0;
#if !MinimalReader
            var pb = p as ParameterBinding;
            if (pb != null) p = pb.BoundParameter;
#endif
            return paramIndex[p.UniqueKey];
        }

        private int GetPropertyIndex(Property /*!*/ p)
        {
            return (int)propertyIndex[p.UniqueKey];
        }

        private int GetSecurityAttributeParentCodedIndex(Node /*!*/ node)
        {
            switch (node.NodeType)
            {
                case NodeType.InstanceInitializer:
                case NodeType.StaticInitializer:
                case NodeType.Method: return (GetMethodIndex((Method)node) << 2) | 1;
                case NodeType.Class:
                case NodeType.Interface:
                case NodeType.DelegateNode:
                case NodeType.EnumNode:
                case NodeType.Struct: return (GetTypeDefIndex((TypeNode)node) << 2) | 0;
                case NodeType.Assembly: return (1 << 2) | 2;
                default:
                    Debug.Assert(false, "Unexpected security attribute parent");
                    return 0;
            }
        }

        private int GetStandAloneSignatureIndex(BinaryWriter signatureWriter)
        {
            standAloneSignatureEntries.Add(signatureWriter);
            return standAloneSignatureEntries.Count;
        }

        private int GetStaticDataIndex(byte[] data, PESection targetSection)
        {
            var result = 0;
            switch (targetSection)
            {
                case PESection.SData:
                    result = sdataHeap.BaseStream.Position;
                    sdataHeap.Write(data);
                    break;
                case PESection.Text:
                    result = methodBodiesHeap.BaseStream.Position;
                    methodBodiesHeap.Write(data);
                    break;
                case PESection.TLS:
                    result = tlsHeap.BaseStream.Position;
                    tlsHeap.Write(data);
                    break;
            }

            return result;
        }

        private int GetResourceDataIndex(byte[] /*!*/ data)
        {
            var index = resourceDataHeap.BaseStream.Position;
            resourceDataHeap.Write(data.Length);
            resourceDataHeap.Write(data);
            return index;
        }

        private int GetStringIndex(string str)
        {
            if (str == null || str.Length == 0) return 0;
            var index = stringHeapIndex[str];
            if (index == null)
            {
                index = stringHeap.BaseStream.Position;
                stringHeap.Write(str, true);
                stringHeapIndex[str] = index;
            }

            return (int)index;
        }

        private int GetUserStringIndex(string /*!*/ str)
        {
            var index = userStringHeapIndex[str];
            if (index == null)
            {
                index = userStringHeap.BaseStream.Position;
                WriteCompressedInt(userStringHeap, str.Length * 2 + 1);
                userStringHeap.Write(str.ToCharArray());
                userStringHeapIndex[str] = index;
                //Write out a trailing byte indicating if the string is really quite simple
                ulong stringKind = 0; //no funny business
                foreach (var ch in str)
                    if (ch >= 0x7F) stringKind += 1;
                    else
                        switch ((int)ch)
                        {
                            case 0x1:
                            case 0x2:
                            case 0x3:
                            case 0x4:
                            case 0x5:
                            case 0x6:
                            case 0x7:
                            case 0x8:
                            case 0xE:
                            case 0xF:
                            case 0x10:
                            case 0x11:
                            case 0x12:
                            case 0x13:
                            case 0x14:
                            case 0x15:
                            case 0x16:
                            case 0x17:
                            case 0x18:
                            case 0x19:
                            case 0x1A:
                            case 0x1B:
                            case 0x1C:
                            case 0x1D:
                            case 0x1E:
                            case 0x1F:
                            case 0x27:
                            case 0x2D:
                                stringKind += 1;
                                break;
                        }

                if (stringKind > 0) stringKind = 1;
                userStringHeap.Write((byte)stringKind);
            }

            return (int)index;
        }

        private int GetTypeDefIndex(TypeNode /*!*/ type)
        {
            var index = typeDefIndex[type.UniqueKey];
            if (index == null)
            {
                if (typeDefEntries == null) return 0;
                index = typeDefEntries.Count + 1;
                typeDefEntries.Add(type);
                typeDefIndex[type.UniqueKey] = index;
                if (type.IsGeneric && type.Template == null)
                    VisitGenericParameterList(type, type.ConsolidatedTemplateParameters);
            }

            return (int)index;
        }

        private int GetTypeDefOrRefOrSpecEncoded(TypeNode type)
        {
            if (type == null) return 0;
            if (!UseGenerics)
            {
                var cp = type as ClassParameter;
                if (cp != null)
                {
                    Debug.Assert(!cp.IsGeneric);
                    return GetTypeDefOrRefOrSpecEncoded(cp.BaseClass);
                } //REVIEW: why???
            }

            if (IsStructural(type)) return (GetTypeSpecIndex(type) << 2) | 2;
            if (type.DeclaringModule == module) return GetTypeDefIndex(type) << 2;
            return (GetTypeRefIndex(type) << 2) | 1;
        }

        private int GetTypeToken(TypeNode /*!*/ type)
        {
            if (IsStructural(type) && (!type.IsGeneric || (type.ConsolidatedTemplateArguments != null &&
                                                           type.ConsolidatedTemplateArguments.Count > 0)))
                return 0x1b000000 | GetTypeSpecIndex(type);
            if (type.IsGeneric)
            {
                var foundType = type.GetTemplateInstance(type, type.TemplateParameters);
                Debug.Assert(foundType != type);
                return GetTypeToken(foundType);
            }

            if (type.DeclaringModule == module) return 0x02000000 | GetTypeDefIndex(type);

            if (type.DeclaringModule != null)
            {
                return 0x01000000 | GetTypeRefIndex(type);
            }

            if (type.typeCode == ElementType.ValueType || type.typeCode == ElementType.Class)
            {
                type.DeclaringModule = module;
                return 0x02000000 | GetTypeDefIndex(type);
            }

            Debug.Assert(false);
            return 0;
        }

        private int GetTypeDefToken(TypeNode /*!*/ type)
        {
            if (IsStructural(type) && (!type.IsGeneric || (type.Template != null &&
                                                           type.ConsolidatedTemplateArguments != null &&
                                                           type.ConsolidatedTemplateArguments.Count > 0)))
                return 0x1b000000 | GetTypeSpecIndex(type);
            if (type.DeclaringModule == module) return 0x02000000 | GetTypeDefIndex(type);

            if (type.DeclaringModule != null)
            {
                return 0x01000000 | GetTypeRefIndex(type);
            }

            if (type.typeCode == ElementType.ValueType || type.typeCode == ElementType.Class)
            {
                type.DeclaringModule = module;
                return 0x02000000 | GetTypeDefIndex(type);
            }

            Debug.Assert(false);
            return 0;
        }

        private int GetTypeRefIndex(TypeNode /*!*/ type)
        {
            var index = typeRefIndex[type.UniqueKey];
            if (index == null)
            {
                index = typeRefEntries.Count + 1;
                typeRefEntries.Add(type);
                typeRefIndex[type.UniqueKey] = index;
                var module = type.DeclaringModule;
                var assembly = module as AssemblyNode;
                if (assembly != null)
                    GetAssemblyRefIndex(assembly);
                else
                    GetModuleRefIndex(module);
                if (type.DeclaringType != null)
                    GetTypeRefIndex(type.DeclaringType);
            }

            return (int)index;
        }

        private int GetTypeSpecIndex(TypeNode /*!*/ type)
        {
            var structuralKey = type.UniqueKey;
            var blobIndex = 0;
            if (type.Template != null)
            {
                blobIndex = GetBlobIndex(type);
                structuralKey = ((type.Template.UniqueKey << 8) & int.MaxValue) + blobIndex;
            }

            var index = typeSpecIndex[type.UniqueKey];
            if (index == null)
            {
                if (type.Template != null)
                {
                    index = structuralTypeSpecIndexFor[structuralKey];
                    if (index is int)
                    {
                        var otherType = typeSpecEntries[(int)index - 1];
                        if (otherType != null && otherType.Template == type.Template &&
                            blobIndex == GetBlobIndex(otherType))
                            return (int)index;
                    }
                }

                index = typeSpecEntries.Count + 1;
                typeSpecEntries.Add(type);
                typeSpecIndex[type.UniqueKey] = index;
                if (type.Template != null)
                    structuralTypeSpecIndexFor[structuralKey] = index;
                if (type.Template != null)
                {
                    if (type.Template.DeclaringModule != module)
                        GetTypeRefIndex(type.Template);
                    var templArgs = type.ConsolidatedTemplateArguments;
                    for (int i = 0, n = templArgs == null ? 0 : templArgs.Count; i < n; i++)
                        VisitReferencedType(templArgs[i]);
                }
                else
                {
                    var telems = type.StructuralElementTypes;
                    for (int i = 0, n = telems == null ? 0 : telems.Count; i < n; i++)
                        VisitReferencedType(telems[i]);
                }
            }

            return (int)index;
        }

        private TrivialHashtable /*!*/
            unspecializedFieldFor = new TrivialHashtable();

        private Field /*!*/ GetUnspecializedField(Field /*!*/ field)
        {
            if (field == null || field.DeclaringType == null || !field.DeclaringType.IsGeneric)
            {
                Debug.Fail("");
                return field;
            }

            var unspecializedField = (Field)unspecializedFieldFor[field.UniqueKey];
            if (unspecializedField != null) return unspecializedField;
            var template = field.DeclaringType;
            if (template == null)
            {
                Debug.Assert(false);
                return field;
            }

            while (template.Template != null) template = template.Template;
            var specializedMembers = field.DeclaringType.Members;
            var unspecializedMembers = template.Members;
            for (int i = 0, n = specializedMembers.Count; i < n; i++)
            {
                if (specializedMembers[i] != field) continue;
                unspecializedField = (Field)unspecializedMembers[i];
                if (unspecializedField == null)
                {
                    Debug.Fail("");
                    unspecializedField = field;
                }

                unspecializedFieldFor[field.UniqueKey] = unspecializedField;
                VisitReferencedType(unspecializedField.DeclaringType);
                return unspecializedField;
            }

            Debug.Fail("");
            return field;
        }

        private TrivialHashtable /*!*/
            unspecializedMethodFor = new TrivialHashtable();

        private Method /*!*/ GetUnspecializedMethod(Method /*!*/ method)
        {
            Debug.Assert(method != null && method.DeclaringType != null && method.DeclaringType.IsGeneric);
            var unspecializedMethod = (Method)unspecializedMethodFor[method.UniqueKey];
            if (unspecializedMethod != null) return unspecializedMethod;
            var template = method.DeclaringType;
            if (template == null)
            {
                Debug.Assert(false);
                return method;
            }

            while (template.Template != null) template = template.Template;
            var specializedMembers = method.DeclaringType.Members;
            var unspecializedMembers = template.Members;
            for (int i = 0, n = specializedMembers.Count; i < n; i++)
            {
                if (specializedMembers[i] != method) continue;
                unspecializedMethod = unspecializedMembers[i] as Method;
                if (unspecializedMethod == null) break;
                goto FoundUnspecialized;
            }

            // try alternative
            unspecializedMethod = method;
            while (unspecializedMethod.Template != null) unspecializedMethod = unspecializedMethod.Template;

            if (unspecializedMethod.DeclaringType.Template == null) goto FoundUnspecialized;
            Debug.Assert(false);
            return method;
            FoundUnspecialized:
            unspecializedMethodFor[method.UniqueKey] = unspecializedMethod;
            template = unspecializedMethod.DeclaringType;
            while (template.Template != null) template = template.Template;
            VisitReferencedType(template);
            for (int j = 0,
                 m = unspecializedMethod.TemplateParameters == null ? 0 : unspecializedMethod.TemplateParameters.Count;
                 j < m;
                 j++)
            {
                var p = unspecializedMethod.TemplateParameters[j];
                if (p == null) continue;
                typeParameterNumber[p.UniqueKey] = -(j + 1);
            }

            return unspecializedMethod;
        }

        internal void IncrementStackHeight()
        {
            stackHeight++;
            if (stackHeight > stackHeightMax) stackHeightMax = stackHeight;
        }

        private void PopulateAssemblyTable()
            //^ requires this.assembly != null;
        {
            var assembly = this.assembly;
            var assemblyTable = writer.assemblyTable = new AssemblyRow[1];
            assemblyTable[0].HashAlgId = (int)AssemblyHashAlgorithm.SHA1;
            assemblyTable[0].Flags = (int)assembly.Flags;
            if (assembly.Version == null) assembly.Version = new Version(1, 0, 0, 0);
            assemblyTable[0].MajorVersion = assembly.Version.Major;
            assemblyTable[0].MinorVersion = assembly.Version.Minor;
            assemblyTable[0].RevisionNumber = assembly.Version.Revision;
            assemblyTable[0].BuildNumber = assembly.Version.Build;
            if (assembly.PublicKeyOrToken != null && 0 < assembly.PublicKeyOrToken.Length)
                assemblyTable[0].PublicKey = GetBlobIndex(assembly.PublicKeyOrToken);
            if (assembly.Name != null)
                assemblyTable[0].Name = GetStringIndex(assembly.Name);
            else
                Debug.Assert(false, "Assembly must have a name");
            if (assembly.Culture != null && assembly.Culture.Length > 0)
                assemblyTable[0].Culture = GetStringIndex(assembly.Culture);
            writer.assemblyTable = assemblyTable;
        }

        private void PopulateAssemblyRefTable()
        {
            var arList = module.AssemblyReferences = assemblyRefEntries;
            if (arList == null) return;
            var n = arList.Count;
            var arRows = writer.assemblyRefTable = new AssemblyRefRow[n];
            for (var i = 0; i < n; i++)
            {
                var ar = arList[i];
                if (ar.Version == null)
                {
                    Debug.Assert(false, "assembly reference without a version");
                }
                else
                {
                    arRows[i].MajorVersion = ar.Version.Major;
                    arRows[i].MinorVersion = ar.Version.Minor;
                    arRows[i].RevisionNumber = ar.Version.Revision;
                    arRows[i].BuildNumber = ar.Version.Build;
                    arRows[i].Flags = (int)ar.Flags;
                }

                if (ar.PublicKeyOrToken != null && 0 < ar.PublicKeyOrToken.Length)
                    arRows[i].PublicKeyOrToken = GetBlobIndex(ar.PublicKeyOrToken);
                if (ar.Name == null)
                    Debug.Assert(false, "assembly reference without a name");
                else
                    arRows[i].Name = GetStringIndex(ar.Name);
                if (ar.Culture != null && ar.Culture.Length > 0)
                    arRows[i].Culture = GetStringIndex(ar.Culture);
                if (ar.HashValue != null)
                    arRows[i].HashValue = GetBlobIndex(ar.HashValue);
            }
            //this.assemblyRefEntries = null;
        }

        private void PopulateClassLayoutTable()
        {
            var n = classLayoutEntries.Count;
            if (n == 0) return;
            var clr = writer.classLayoutTable = new ClassLayoutRow[n];
            for (var i = 0; i < n; i++)
            {
                var t = classLayoutEntries[i];
                clr[i].ClassSize = t.ClassSize;
                clr[i].PackingSize = t.PackingSize;
                clr[i].Parent = GetTypeDefIndex(t);
            }
            //this.classLayoutEntries = null;
        }

        private void PopulateConstantTable()
        {
            var n = constantTableEntries.Count;
            if (n == 0) return;
            var cr = writer.constantTable = new ConstantRow[n];
            for (var i = 0; i < n; i++)
            {
                var p = constantTableEntries[i] as Parameter;
                if (p != null)
                {
                    cr[i].Parent = (GetParamIndex(p) << 2) | 1;
                    SetConstantTableEntryValueAndTypeCode(cr, i, (Literal)p.DefaultValue);
                }
                else
                {
                    var f = (Field)constantTableEntries[i];
                    cr[i].Parent = GetFieldIndex(f) << 2;
                    SetConstantTableEntryValueAndTypeCode(cr, i, f.DefaultValue);
                }

                var temp = cr[i];
                var parent = temp.Parent;
                for (var j = i - 1; j >= 0; j--)
                    if (cr[j].Parent > parent)
                    {
                        cr[j + 1] = cr[j];
                        if (j == 0)
                        {
                            cr[0] = temp;
                            break;
                        }
                    }
                    else
                    {
                        if (j < i - 1) cr[j + 1] = temp;
                        break;
                    }
            }
            //TODO: more efficient sort
            //this.constantTableEntries = null;
        }

        private void SetConstantTableEntryValueAndTypeCode(ConstantRow[] cr, int i, Literal defaultValue)
        {
            cr[i].Value = GetBlobIndex(defaultValue);
            var t = defaultValue.Type;
            if (t.NodeType == NodeType.EnumNode) t = ((EnumNode)t).UnderlyingType;
            cr[i].Type = (int)t.typeCode;
            if (t is Reference || Literal.IsNullLiteral(defaultValue))
                cr[i].Type = (int)ElementType.Class;
        }

        private void PopulateCustomAttributeTable()
        {
            if (customAttributeCount == 0) return;
            var table = writer.customAttributeTable = new CustomAttributeRow[customAttributeCount];
            var k = 0;
            var prevCodedIndex = 0;
            for (int i = 0, n = nodesWithCustomAttributes.Count; i < n; i++)
            {
                AttributeList attrs = null;
                var node = nodesWithCustomAttributes[i];
                var codedIndex = 0;
                switch (node.NodeType)
                {
                    case NodeType.Method:
                    case NodeType.InstanceInitializer:
                    case NodeType.StaticInitializer:
                        var m = (Method)node;
                        codedIndex = GetMethodIndex(m) << 5;
                        attrs = m.Attributes;
                        break;
                    case NodeType.Field:
                        var f = (Field)node;
                        codedIndex = (GetFieldIndex(f) << 5) | 1;
                        attrs = f.Attributes;
                        break;
                    case NodeType.Parameter:
                        var par = (Parameter)node;
                        codedIndex = (GetParamIndex(par) << 5) | 4;
                        attrs = par.Attributes;
                        break;
                    case NodeType.Class:
                    case NodeType.DelegateNode:
                    case NodeType.EnumNode:
                    case NodeType.Interface:
                    case NodeType.Struct:
#if !MinimalReader
                    case NodeType.TupleType:
                    case NodeType.TypeAlias:
                    case NodeType.TypeIntersection:
                    case NodeType.TypeUnion:
#endif
                        var t = (TypeNode)node;
                        if (IsStructural(t) && (!t.IsGeneric || (t.Template != null &&
                                                                 t.ConsolidatedTemplateArguments != null &&
                                                                 t.ConsolidatedTemplateArguments.Count > 0)))
                            codedIndex = (GetTypeSpecIndex(t) << 5) | 13;
                        else
                            codedIndex = (GetTypeDefIndex(t) << 5) | 3;
                        attrs = t.Attributes;
                        break;
                    case NodeType.ClassParameter:
                    case NodeType.TypeParameter:
                        if (!UseGenerics) goto case NodeType.Class;
                        t = (TypeNode)node;
                        codedIndex = (GetGenericParamIndex(t) << 5) | 19;
                        attrs = t.Attributes;
                        break;
                    case NodeType.Property:
                        var p = (Property)node;
                        codedIndex = (GetPropertyIndex(p) << 5) | 9;
                        attrs = p.Attributes;
                        break;
                    case NodeType.Event:
                        var e = (Event)node;
                        codedIndex = (GetEventIndex(e) << 5) | 10;
                        attrs = e.Attributes;
                        break;
                    case NodeType.Module:
                    case NodeType.Assembly:
                        codedIndex = (1 << 5) | (node.NodeType == NodeType.Module ? 7 : 14);
                        attrs = ((Module)node).Attributes;
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }

                if (attrs == null) continue;
                if (UseGenerics) Debug.Assert(codedIndex > prevCodedIndex);
                prevCodedIndex = codedIndex;
                for (int j = 0, m = attrs.Count; j < m; j++)
                {
                    var a = attrs[j];
                    if (a == null) continue;
                    table[k].Parent = codedIndex;
                    Debug.Assert(a.Constructor is MemberBinding);
                    var cons = (Method)((MemberBinding)a.Constructor).BoundMember;
                    if (cons.DeclaringType.DeclaringModule == module && !IsStructural(cons.DeclaringType))
                        table[k].Constructor = (GetMethodIndex(cons) << 3) | 2;
                    else
                        table[k].Constructor = (GetMemberRefIndex(cons) << 3) | 3;
                    table[k].Value = GetBlobIndex(a.Expressions, cons.Parameters);
                    k++;
                }
            }
        }

        private void PopulateDeclSecurityTable()
        {
            if (securityAttributeCount == 0) return;
            var table = writer.declSecurityTable = new DeclSecurityRow[securityAttributeCount];
            var k = 0;
            var prevCodedIndex = 0;
            for (int i = 0, n = nodesWithSecurityAttributes.Count; i < n; i++)
            {
                SecurityAttributeList attrs = null;
                var node = nodesWithSecurityAttributes[i];
                var codedIndex = 0;
                switch (node.NodeType)
                {
                    case NodeType.Method:
                    case NodeType.InstanceInitializer:
                    case NodeType.StaticInitializer:
                        var m = (Method)node;
                        codedIndex = (GetMethodIndex(m) << 2) | 1;
                        attrs = m.SecurityAttributes;
                        break;
                    case NodeType.Class:
                    case NodeType.Interface:
                    case NodeType.DelegateNode:
                    case NodeType.EnumNode:
                    case NodeType.Struct:
                        var t = (TypeNode)node;
                        codedIndex = (GetTypeDefIndex(t) << 2) | 0;
                        attrs = t.SecurityAttributes;
                        break;
                    case NodeType.Assembly:
                        codedIndex = (1 << 2) | 2;
                        attrs = ((AssemblyNode)node).SecurityAttributes;
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }

                if (attrs == null) continue;
                Debug.Assert(codedIndex > prevCodedIndex);
                prevCodedIndex = codedIndex;
                for (int j = 0, m = attrs.Count; j < m; j++)
                {
                    var a = attrs[j];
                    if (a == null) continue;
                    VisitReferencedType(CoreSystemTypes.SecurityAction);
                    table[k].Action = (int)a.Action;
                    table[k].Parent = codedIndex;
                    if (CoreSystemTypes.SystemAssembly.MetadataFormatMajorVersion == 1 &&
                        CoreSystemTypes.SystemAssembly.MetadataFormatMinorVersion < 1)
                    {
                        table[k].PermissionSet = GetBlobIndex(a.SerializedPermissions);
                    }
                    else
                    {
                        if (a.PermissionAttributes != null)
                            table[k].PermissionSet = GetBlobIndex(a.PermissionAttributes);
                        else // Came across some assemblies that had a metadata version > 1.0, but still used
                            // serialized security attributes. So might as well try to see if this is the case
                            // if the PermissionAttributes are null.
                            table[k].PermissionSet = GetBlobIndex(a.SerializedPermissions);
                    }

                    k++;
                }
            }
        }

        private void PopulateEventMapTable()
        {
            var n = eventMapEntries.Count;
            if (n == 0) return;
            var emr = writer.eventMapTable = new EventMapRow[n];
            for (var i = 0; i < n; i++)
            {
                var e = eventMapEntries[i];
                emr[i].Parent = GetTypeDefIndex(e.DeclaringType);
                emr[i].EventList = GetEventIndex(e);
            }
            //this.eventMapEntries = null;
        }

        private void PopulateEventTable()
        {
            var n = eventEntries.Count;
            if (n == 0) return;
            var er = writer.eventTable = new EventRow[n];
            for (var i = 0; i < n; i++)
            {
                var e = eventEntries[i];
                if (e == null || e.Name == null) continue;
                er[i].Flags = (int)e.Flags;
                er[i].Name = GetStringIndex(e.Name.ToString());
                er[i].EventType = GetTypeDefOrRefOrSpecEncoded(e.HandlerType);
            }
            //this.eventEntries = null;
        }

        private void PopulateExportedTypeTable()
        {
            if (assembly == null) return;
            var exportedTypes = assembly.ExportedTypes;
            var n = exportedTypes == null ? 0 : exportedTypes.Count;
            if (n == 0) return;
            var ett = writer.exportedTypeTable = new ExportedTypeRow[n];
            for (var i = 0; i < n; i++)
            {
                var et = exportedTypes[i];
                if (et == null || et.Namespace == null || et.Name == null) continue;
                ett[i].TypeDefId = 0;
                ett[i].TypeNamespace = GetStringIndex(et.Namespace.ToString());
                ett[i].TypeName = GetStringIndex(et.Name.ToString());
                ett[i].Flags = (int)(et.Flags & TypeFlags.VisibilityMask);
                if (et.DeclaringType != null)
                {
                    for (var j = 0; j < i; j++)
                        if (exportedTypes[j] == et.DeclaringType)
                        {
                            ett[i].Implementation = ((j + 1) << 2) | 2;
                            break;
                        }
                }
                else if (et.DeclaringModule != module && et.DeclaringModule is AssemblyNode)
                {
                    ett[i].Implementation = (GetAssemblyRefIndex((AssemblyNode)et.DeclaringModule) << 2) | 1;
                    ett[i].Flags = (int)TypeFlags.Forwarder;
                }
                else
                {
                    ett[i].Implementation = (GetFileTableIndex(et.DeclaringModule) << 2) | 0;
                }
            }
        }

        private void PopulateFieldTable()
        {
            var n = fieldEntries.Count;
            if (n == 0) return;
            var fr = writer.fieldTable = new FieldRow[n];
            for (var i = 0; i < n; i++)
            {
                var f = fieldEntries[i];
                fr[i].Flags = (int)f.Flags;
                fr[i].Name = GetStringIndex(f.Name.Name); // we don't store prefixes in field names.
                fr[i].Signature = GetBlobIndex(f);
            }
            //this.fieldEntries = null;
        }

        private void PopulateFieldLayoutTable()
        {
            var n = fieldLayoutEntries.Count;
            if (n == 0) return;
            var flr = writer.fieldLayoutTable = new FieldLayoutRow[n];
            for (var i = 0; i < n; i++)
            {
                var f = fieldLayoutEntries[i];
                flr[i].Field = GetFieldIndex(f);
                flr[i].Offset = f.Offset;
            }
            //this.fieldLayoutEntries = null;
        }

        private void PopulateFieldRVATable()
        {
            var n = fieldRvaEntries.Count;
            if (n == 0) return;
            var frr = writer.fieldRvaTable = new FieldRvaRow[n];
            for (var i = 0; i < n; i++)
            {
                var f = fieldRvaEntries[i];
                frr[i].Field = GetFieldIndex(f);
                if (f.InitialData != null)
                    frr[i].RVA =
                        GetStaticDataIndex(f.InitialData, f.Section); //Fixed up to be an RVA inside MetadataWriter.
                else
                    frr[i].RVA = f.Offset;
                frr[i].TargetSection = f.Section;
            }
            //this.fieldRvaEntries = null;
        }

        private void PopulateFileTable()
        {
            var n = fileTableEntries.Count;
            if (n == 0) return;
            var readContents = false;
            var ftr = writer.fileTable = new FileRow[n];
            for (var i = 0; i < n; i++)
            {
                var module = fileTableEntries[i];
                switch (module.Kind)
                {
                    case ModuleKindFlags.ConsoleApplication:
                    case ModuleKindFlags.DynamicallyLinkedLibrary:
                    case ModuleKindFlags.WindowsApplication:
                        ftr[i].Flags = (int)FileFlags.ContainsMetaData;
                        break;
                    case ModuleKindFlags.ManifestResourceFile:
                        readContents = true;
                        ftr[i].Flags = (int)FileFlags.ContainsNoMetaData;
                        break;
                    case ModuleKindFlags.UnmanagedDynamicallyLinkedLibrary:
                        ftr[i].Flags = (int)FileFlags.ContainsNoMetaData;
                        break;
                }

                if (module.HashValue != null)
                    ftr[i].HashValue = GetBlobIndex(module.HashValue);
                else
                    ftr[i].HashValue = 0;
                ftr[i].Name = GetStringIndex(module.Name);
                if (readContents)
                    try
                    {
                        var fs = File.OpenRead(module.Location);
                        var size = fs.Length;
                        var buffer = new byte[size];
                        fs.Read(buffer, 0, (int)size);
                        var sha = new SHA1CryptoServiceProvider();
                        var hash = sha.ComputeHash(buffer);
                        ftr[i].HashValue = GetBlobIndex(hash);
                    }
                    catch
                    {
                    }
            }
            //this.fileTableEntries = null;
        }

        private void PopulateGuidTable()
        {
            var n = guidEntries.Count;
            var guids = writer.GuidHeap = new Guid[n];
            for (var i = 0; i < n; i++)
                guids[i] = (Guid)guidEntries[i];
            //this.guidEntries = null;
        }

        private void PopulateGenericParamTable()
        {
            var n = genericParamEntries.Count;
            if (n == 0) return;
            var gpr = writer.genericParamTable = new GenericParamRow[n];
            Member lastMember = null;
            var number = 0;
            for (var i = 0; i < n; i++)
            {
                var m = genericParamEntries[i];
                var paramType = genericParameters[i];
                if (paramType == null || paramType.Name == null) continue;
                var meth = m as Method;
                var type = m as TypeNode;
                if (m != lastMember) number = 0;
                gpr[i].GenericParameter = paramType;
                gpr[i].Number = number++;
                if (type != null)
                {
                    gpr[i].Name = GetStringIndex(paramType.Name.ToString());
                    gpr[i].Owner = (GetTypeDefIndex(type) << 1) | 0;
                }
                else
                {
                    //^ assert meth != null;
                    gpr[i].Name = GetStringIndex(paramType.Name.ToString());
                    gpr[i].Owner = (GetMethodIndex(meth) << 1) | 1;
                }

                var tp = paramType as ITypeParameter;
                if (tp != null)
                {
                    gpr[i].Flags = (int)tp.TypeParameterFlags;
                }
                else
                {
                    Debug.Assert(false);
                    gpr[i].Flags = 0;
                }

                lastMember = m;
                var temp = gpr[i];
                var owner = temp.Owner;
                for (var j = i - 1; j >= 0; j--)
                    if (gpr[j].Owner > owner)
                    {
                        gpr[j + 1] = gpr[j];
                        if (j == 0)
                        {
                            gpr[0] = temp;
                            break;
                        }
                    }
                    else
                    {
                        if (j < i - 1) gpr[j + 1] = temp;
                        break;
                    }
            }

            for (var i = 0; i < n; i++)
            {
                var genPar = gpr[i].GenericParameter;
                if (genPar == null) continue;
                genericParamIndex[genPar.UniqueKey] = i + 1;
            }

            for (var i = 0; i < n; i++)
            {
                var genPar = gpr[i].GenericParameter;
                if (genPar == null) continue;
                VisitAttributeList(genPar.Attributes, genPar);
            }
            //this.genericParamEntries = null;
            //this.genericParameters = null;
        }

        private void PopulateGenericParamConstraintTable()
        {
            var n = genericParamConstraintEntries.Count;
            if (n == 0) return;
            var gpcr = writer.genericParamConstraintTable = new GenericParamConstraintRow[n];
            TypeNode lastParameter = null;
            var paramIndex = 0;
            var constraintIndex = 0;
#if !CodeContracts
      int indexOffset = 0;
#endif
            for (var i = 0; i < n; i++)
            {
                var t = genericParamConstraintEntries[i];
                if (t != lastParameter)
                {
                    paramIndex = GetGenericParamIndex(t);
                    constraintIndex = 0;
#if !CodeContracts
          indexOffset = 0;
#endif
                }

                gpcr[i].Param = paramIndex;
                TypeNode constraint;
#if CodeContracts
                constraint = t.StructuralElementTypes[constraintIndex];
#else
        if (constraintIndex == 0 && t.BaseType != null && t.BaseType != CoreSystemTypes.Object){
          constraint = t.BaseType; indexOffset = 1;
        }else
          constraint = t.Interfaces[constraintIndex-indexOffset];
#endif
                gpcr[i].Constraint = GetTypeDefOrRefOrSpecEncoded(constraint);
                lastParameter = t;
                constraintIndex++;
                var temp = gpcr[i];
                var param = temp.Param;
                for (var j = i - 1; j >= 0; j--)
                    if (gpcr[j].Param > param)
                    {
                        gpcr[j + 1] = gpcr[j];
                        if (j == 0)
                        {
                            gpcr[0] = temp;
                            break;
                        }
                    }
                    else
                    {
                        if (j < i - 1) gpcr[j + 1] = temp;
                        break;
                    }
            }
            //this.genericParamConstraintEntries = null;
        }

        private void PopulateImplMapTable()
        {
            var n = implMapEntries.Count;
            if (n == 0) return;
            var imr = writer.implMapTable = new ImplMapRow[n];
            for (var i = 0; i < n; i++)
            {
                var m = implMapEntries[i];
                imr[i].ImportName = GetStringIndex(m.PInvokeImportName);
                imr[i].ImportScope = GetModuleRefIndex(m.PInvokeModule);
                imr[i].MappingFlags = (int)m.PInvokeFlags;
                imr[i].MemberForwarded = (GetMethodIndex(m) << 1) | 1;
            }
            //this.implMapEntries = null;
        }

        private void PopulateInterfaceImplTable()
        {
            var n = interfaceEntries.Count;
            if (n == 0) return;
            var iir = writer.interfaceImplTable = new InterfaceImplRow[n];
            TypeNode prevT = null;
            for (int i = 0, j = 0; i < n; i++)
            {
                var t = interfaceEntries[i];
                if (t == prevT)
                {
                    j++;
                }
                else
                {
                    j = 0;
                    prevT = t;
                }

                var ti = iir[i].Class = GetTypeDefIndex(t);
                Interface iface = null;
#if ExtendedRuntime
        if (t is ITypeParameter){
          int numIfaces = t.Interfaces == null ? 0 : t.Interfaces.Count;
          if (j == numIfaces)
            iface = SystemTypes.ITemplateParameter;
          else
            iface = t.Interfaces[j];
        }else
#endif
                iface = t.Interfaces[j];
                if (iface == null)
                {
                    i--;
                    continue;
                }

                var ii = iir[i].Interface = GetTypeDefOrRefOrSpecEncoded(iface);
                for (var k = 0; k < i; k++) //REVIEW: is a more efficient sort worthwhile?
                    if (iir[k].Class > ti)
                    {
                        for (var kk = i; kk > k; kk--)
                        {
                            iir[kk].Class = iir[kk - 1].Class;
                            iir[kk].Interface = iir[kk - 1].Interface;
                        }

                        iir[k].Class = ti;
                        iir[k].Interface = ii;
                        break;
                    }
            }
            //this.interfaceEntries = null;
        }

        private void PopulateManifestResourceTable()
        {
            var resources = module.Resources;
            var n = resources == null ? 0 : resources.Count;
            if (n == 0) return;
            var mresources = writer.manifestResourceTable = new ManifestResourceRow[n];
            for (var i = 0; i < n; i++)
            {
                var r = resources[i];
                mresources[i].Flags = r.IsPublic ? 1 : 2;
                mresources[i].Name = GetStringIndex(r.Name);
                if (r.Data != null)
                    mresources[i].Offset = GetResourceDataIndex(r.Data);
                else if (r.DefiningModule is AssemblyNode)
                    mresources[i].Implementation = (GetAssemblyRefIndex((AssemblyNode)r.DefiningModule) << 2) | 1;
                else
                    mresources[i].Implementation = (GetFileTableIndex(r.DefiningModule) << 2) | 0;
            }
        }

        private void PopulateMarshalTable()
        {
            var n = marshalEntries.Count;
            if (n == 0) return;
            var fmr = writer.fieldMarshalTable = new FieldMarshalRow[n];
            for (var i = 0; i < n; i++)
            {
                MarshallingInformation mi;
                var f = marshalEntries[i] as Field;
                if (f != null)
                {
                    fmr[i].Parent = (GetFieldIndex(f) << 1) | 0;
                    mi = f.MarshallingInformation;
                }
                else
                {
                    var p = (Parameter)marshalEntries[i];
                    fmr[i].Parent = (GetParamIndex(p) << 1) | 1;
                    mi = p.MarshallingInformation;
                }

                var nt = fmr[i].NativeType = GetBlobIndex(mi);
                var pi = fmr[i].Parent;
                for (var k = 0; k < i; k++) //REVIEW: is a more efficient sort worthwhile?
                    if (fmr[k].Parent > pi)
                    {
                        for (var kk = i; kk > k; kk--)
                        {
                            fmr[kk].Parent = fmr[kk - 1].Parent;
                            fmr[kk].NativeType = fmr[kk - 1].NativeType;
                        }

                        fmr[k].Parent = pi;
                        fmr[k].NativeType = nt;
                        break;
                    }
            }
            //this.marshalEntries = null;
        }

        private void PopulateMemberRefTable()
        {
            var n = memberRefEntries.Count;
            if (n == 0) return;
            var mr = writer.memberRefTable = new MemberRefRow[n];
            for (var i = 0; i < n; i++)
            {
                var member = memberRefEntries[i];
                if (member == null || member.Name == null) continue;
                mr[i].Name = GetStringIndex(member.Name.ToString());
                var f = member as Field;
                if (f != null)
                {
                    mr[i].Signature = GetBlobIndex(f);
                }
                else
                {
                    var fp = member as FunctionPointer;
                    if (fp != null)
                    {
                        mr[i].Signature = GetBlobIndex(fp);
                        if (fp is VarargMethodCallSignature)
                        {
                            var m = ((VarargMethodCallSignature)member).method;
                            if (m != null && m.DeclaringType.DeclaringModule == module &&
                                !IsStructural(m.DeclaringType))
                            {
                                mr[i].Class = (GetMethodIndex(m) << 3) | 3;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        var m = (Method)member;
                        if (m.IsGeneric && m.Template != null) m = GetUnspecializedMethod(m);
                        mr[i].Signature = GetBlobIndex(m, false);
                        if (m.DeclaringType.DeclaringModule == module && !IsStructural(m.DeclaringType) && !m.IsGeneric)
                        {
                            mr[i].Class = (GetMethodIndex(m) << 3) | 3;
                            continue;
                        }
                        //TODO: if the declaring type is the special global members type of another module, set class to a module ref
                    }
                }

                var j = mr[i].Class = GetMemberRefParentEncoded(member.DeclaringType);
                if ((j & 0x3) == 2) mr[i].Class = (j & ~0x3) | 4;
            }
            //this.memberRefEntries = null;
        }

        private void PopulateMethodTable()
        {
            var n = methodEntries.Count;
            if (n == 0) return;
            var mr = writer.methodTable = new MethodRow[n];
            for (var i = 0; i < n; i++)
            {
                var m = methodEntries[i];
                if (m == null || m.Name == null) continue;
                if (m.IsAbstract || m.Body == null || m.Body.Statements == null || m.Body.Statements.Count == 0)
                    mr[i].RVA = -1;
                else
                    mr[i].RVA = GetMethodBodiesHeapIndex(m); //Fixed up to be an RVA inside MetadataWriter.
                mr[i].Flags = (int)m.Flags;
                mr[i].ImplFlags = (int)m.ImplFlags;
                mr[i].Name = GetStringIndex(m.Name.ToString());
                mr[i].Signature = GetBlobIndex(m, false);
                if (m.ReturnTypeMarshallingInformation != null ||
                    (m.ReturnAttributes != null && m.ReturnAttributes.Count > 0))
                {
                    mr[i].ParamList = paramIndex[m.UniqueKey];
                }
                else
                {
                    var pars = m.Parameters;
                    if (pars != null && pars.Count > 0)
                    {
                        Debug.Assert(pars[0] != null && pars[0].DeclaringMethod == m);
                        mr[i].ParamList = GetParamIndex(pars[0]);
                    }
                    else
                    {
                        mr[i].ParamList = 0;
                    }
                }
            }
            //this.methodEntries = null;
        }

        private void PopulateMethodImplTable()
        {
            var n = methodImplEntries.Count;
            if (n == 0) return;
            var mir = writer.methodImplTable = new MethodImplRow[n];
            var j = 0;
            Method lastMethod = null;
            for (var i = 0; i < n; i++)
            {
                var m = methodImplEntries[i];
                if (lastMethod != m) j = 0;
                mir[i].Class = GetTypeDefIndex(m.DeclaringType);
                if (m.DeclaringType.DeclaringModule == module)
                    mir[i].MethodBody = GetMethodIndex(m) << 1;
                else
                    mir[i].MethodBody = (GetMemberRefIndex(m) << 1) | 0x1;
                var im = m.ImplementedInterfaceMethods[j++];
                while (im == null) im = m.ImplementedInterfaceMethods[j++];
                mir[i].MethodDeclaration = GetMethodDefOrRefEncoded(im);
                lastMethod = m;
            }
            //this.methodImplEntries = null;
        }

        private void PopulateMethodSpecTable()
        {
            var n = methodSpecEntries.Count;
            if (n == 0) return;
            var msr = writer.methodSpecTable = new MethodSpecRow[n];
            for (var i = 0; i < n; i++)
            {
                var m = methodSpecEntries[i];
                msr[i].Method = GetMethodDefOrRefEncoded(m.Template);
                msr[i].Instantiation = GetBlobIndex(m, true);
                //TODO: sort this and eliminate duplicates.
                //Duplicates can arise when methods are instantiated with method parameters from different methods.
                //TODO: perhaps this duplication should be prevented by Method.GetTemplateInstance?
            }
            //this.methodEntries = null;
        }

        private void PopulateMethodSemanticsTable()
        {
            var n = methodSemanticsEntries.Count;
            if (n == 0) return;
            var msr = writer.methodSemanticsTable = new MethodSemanticsRow[n];
            Member previousOwner = null;
            var index = -1;
            for (var i = 0; i < n; i++)
            {
                var owner = methodSemanticsEntries[i];
                var ownerProperty = owner as Property;
                if (ownerProperty != null)
                {
                    msr[i].Association = (GetPropertyIndex(ownerProperty) << 1) | 1;
                    if (owner != previousOwner)
                    {
                        previousOwner = owner;
                        index = -1;
                        if (ownerProperty.Getter != null)
                        {
                            msr[i].Method = GetMethodIndex(ownerProperty.Getter);
                            msr[i].Semantics = 0x0002;
                            continue;
                        }
                    }

                    if (index == -1)
                    {
                        index = 0;
                        if (ownerProperty.Setter != null)
                        {
                            msr[i].Method = GetMethodIndex(ownerProperty.Setter);
                            msr[i].Semantics = 0x0001;
                            continue;
                        }
                    }

                    msr[i].Method = GetMethodIndex(ownerProperty.OtherMethods[index]);
                    msr[i].Semantics = 0x0004;
                    index++;
                    continue;
                }

                var ownerEvent = owner as Event;
                if (ownerEvent == null)
                {
                    Debug.Fail("");
                    continue;
                }

                msr[i].Association = GetEventIndex(ownerEvent) << 1;
                if (owner != previousOwner)
                {
                    previousOwner = owner;
                    index = -2;
                    if (ownerEvent.HandlerAdder != null)
                    {
                        msr[i].Method = GetMethodIndex(ownerEvent.HandlerAdder);
                        msr[i].Semantics = 0x0008;
                        continue;
                    }
                }

                if (index == -2)
                {
                    index = -1;
                    if (ownerEvent.HandlerRemover != null)
                    {
                        msr[i].Method = GetMethodIndex(ownerEvent.HandlerRemover);
                        msr[i].Semantics = 0x0010;
                        continue;
                    }
                }

                if (index == -1)
                {
                    index = 0;
                    if (ownerEvent.HandlerCaller != null)
                    {
                        msr[i].Method = GetMethodIndex(ownerEvent.HandlerCaller);
                        msr[i].Semantics = 0x0020;
                        continue;
                    }
                }

                msr[i].Method = GetMethodIndex(ownerEvent.OtherMethods[i]);
                msr[i].Semantics = 0x0004;
                index++;
            }

            Array.Sort(msr, new MethodSemanticsRowComparer());
            //this.methodSemanticsEntries = null;
        }

        private class MethodSemanticsRowComparer : IComparer
        {
            int IComparer.Compare(object x, object y)
            {
                var xr = (MethodSemanticsRow)x;
                var yr = (MethodSemanticsRow)y;
                var result = xr.Association - yr.Association;
                if (result == 0) result = xr.Method - yr.Method;
                return result;
            }
        }

        private void PopulateModuleTable()
        {
            var mr = writer.moduleTable = new ModuleRow[1];
            var name = module.Name;
            if (assembly != null)
            {
                if (assembly.ModuleName != null)
                {
                    name = assembly.ModuleName;
                }
                else
                {
                    var extension = ".exe";
                    if (module.Kind == ModuleKindFlags.DynamicallyLinkedLibrary) extension = ".dll";
                    name = name + extension;
                }
            }

            mr[0].Name = GetStringIndex(name);
            mr[0].Mvid = GetGuidIndex(Guid.NewGuid());
        }

        private void PopulateModuleRefTable()
        {
            var n = moduleRefEntries.Count;
            if (n == 0) return;
            var mrr = writer.moduleRefTable = new ModuleRefRow[n];
            for (var i = 0; i < n; i++)
            {
                var moduleRef = moduleRefEntries[i];
                mrr[i].Name = GetStringIndex(moduleRef.Name);
            }
            //this.moduleRefEntries = null;
        }

        private void PopulateNestedClassTable()
        {
            var n = nestedClassEntries.Count;
            if (n == 0) return;
            var ncr = writer.nestedClassTable = new NestedClassRow[n];
            for (var i = 0; i < n; i++)
            {
                var nt = nestedClassEntries[i];
                ncr[i].NestedClass = GetTypeDefIndex(nt);
                ncr[i].EnclosingClass = GetTypeDefIndex(nt.DeclaringType);
            }
            //this.nestedClassEntries = null;
        }

        private void PopulateParamTable()
        {
            var n = paramEntries.Count;
            if (n == 0) return;
            var pr = writer.paramTable = new ParamRow[n];
            for (var i = 0; i < n; i++)
            {
                var p = paramEntries[i];
                if (p == null) continue;
                var paramShouldHaveNoName = (p.Flags & ParameterFlags.ParameterNameMissing) != 0;
                p.Flags &= ~ParameterFlags.ParameterNameMissing;
                pr[i].Flags = (int)p.Flags;
                pr[i].Sequence = p.ParameterListIndex + 1;
                pr[i].Name = paramShouldHaveNoName || p.Name == null ? 0 : GetStringIndex(p.Name.ToString());
            }
            //this.paramEntries = null;
        }

        private void PopulatePropertyTable()
        {
            var n = propertyEntries.Count;
            if (n == 0) return;
            var pr = writer.propertyTable = new PropertyRow[n];
            for (var i = 0; i < n; i++)
            {
                var p = propertyEntries[i];
                if (p == null || p.Name == null) continue;
                pr[i].Flags = (int)p.Flags;
                pr[i].Name = GetStringIndex(p.Name.ToString());
                pr[i].Signature = GetBlobIndex(p);
            }
            //this.propertyEntries = null;
        }

        private void PopulatePropertyMapTable()
        {
            var n = propertyMapEntries.Count;
            if (n == 0) return;
            var pmr = writer.propertyMapTable = new PropertyMapRow[n];
            for (var i = 0; i < n; i++)
            {
                var p = propertyMapEntries[i];
                pmr[i].Parent = GetTypeDefIndex(p.DeclaringType);
                pmr[i].PropertyList = GetPropertyIndex(p);
            }
            //this.propertyMapEntries = null;
        }

        private void PopulateStandAloneSigTable()
        {
            var n = standAloneSignatureEntries.Count;
            if (n == 0) return;
            var sasr = writer.standAloneSigTable = new StandAloneSigRow[n];
            for (var i = 0; i < n; i++)
            {
                var sigWriter = (BinaryWriter)standAloneSignatureEntries[i];
                sasr[i].Signature = GetBlobIndex(sigWriter.BaseStream.ToArray());
            }
        }

        private void PopulateTypeDefTable()
        {
            var n = typeDefEntries.Count;
            if (n == 0) return;
            var tdr = writer.typeDefTable = new TypeDefRow[n];
            for (var i = 0; i < n; i++)
            {
                var t = typeDefEntries[i];
                if (t == null) continue;
                tdr[i].Flags = (int)t.Flags;
                tdr[i].Name = GetStringIndex(t.Name == null ? "" : t.Name.ToString());
#if DEBUG
                if (t.IsGeneric)
                {
                    var tcount = t.TemplateParameters == null ? 0 : t.TemplateParameters.Count;
                    if (tcount > 0)
                    {
                        var quoteIndex = t.Name.Name.LastIndexOf(TargetPlatform.GenericTypeNamesMangleChar);
                        Debug.Assert(quoteIndex > 0);
                        int tcountInName;
                        Debug.Assert(int.TryParse(t.Name.Name.Substring(quoteIndex + 1), out tcountInName));
                        Debug.Assert(tcountInName == tcount);
                    }
                }
#endif
                tdr[i].Namespace = t.Namespace == null
                    ? 0
                    : GetStringIndex(t.Namespace == null ? "" : t.Namespace.ToString());
                tdr[i].Extends = GetTypeDefOrRefOrSpecEncoded(t.BaseType);
                var members = t.Members;
                var m = members.Count;
                for (var j = 0; j < m; j++)
                {
                    var mem = members[j];
                    if (mem == null) continue;
                    if (mem.NodeType == NodeType.Field)
                    {
                        tdr[i].FieldList = GetFieldIndex((Field)mem);
                        break;
                    }
                }

                for (var j = 0; j < m; j++)
                {
                    var mem = members[j];
                    if (mem == null) continue;
                    switch (mem.NodeType)
                    {
                        case NodeType.Method:
                        case NodeType.InstanceInitializer:
                        case NodeType.StaticInitializer:
                            tdr[i].MethodList = GetMethodIndex((Method)mem);
                            goto done;
                    }
                }

                done: ;
            }
            //this.typeDefEntries = null;
        }

        private void PopulateTypeRefTable()
        {
            var n = typeRefEntries.Count;
            if (n == 0) return;
            var trr = writer.typeRefTable = new TypeRefRow[n];
            for (var i = 0; i < n; i++)
            {
                var t = typeRefEntries[i];
                if (t == null || t.Name == null || t.Namespace == null) continue;
                trr[i].Name = GetStringIndex(t.Name.ToString());
                trr[i].Namespace = GetStringIndex(t.Namespace.ToString());
                if (t.DeclaringType == null)
                    if (t.DeclaringModule is AssemblyNode)
                        trr[i].ResolutionScope = (GetAssemblyRefIndex((AssemblyNode)t.DeclaringModule) << 2) | 2;
                    else
                        trr[i].ResolutionScope = (GetModuleRefIndex(t.DeclaringModule) << 2) | 1;
                else
                    trr[i].ResolutionScope = (GetTypeRefIndex(t.DeclaringType) << 2) | 3;
            }
            //this.typeRefEntries = null;
        }

        private void PopulateTypeSpecTable()
        {
            var n = typeSpecEntries.Count;
            if (n == 0) return;
            var tsr = writer.typeSpecTable = new TypeSpecRow[n];
            for (var i = 0; i < n; i++)
            {
                var t = typeSpecEntries[i];
                tsr[i].Signature = GetBlobIndex(t);
                //TODO: eliminate duplicates
            }
            //this.typeSpecEntries = null;
        }

        private void Visit(Node node)
        {
            if (node == null) return;
            switch (node.NodeType)
            {
                case NodeType.AddressDereference:
                    VisitAddressDereference((AddressDereference)node);
                    return;
                case NodeType.Arglist:
                    VisitExpression((Expression)node);
                    return;
                case NodeType.AssignmentStatement:
                    VisitAssignmentStatement((AssignmentStatement)node);
                    return;
#if !MinimalReader && !CodeContracts
        case NodeType.Base :
          this.VisitBase((Base)node); return;
#endif
                case NodeType.Block:
                    VisitBlock((Block)node);
                    return;
#if !MinimalReader
                case NodeType.BlockExpression:
                    VisitBlockExpression((BlockExpression)node);
                    return;
#endif
                case NodeType.Branch:
                    VisitBranch((Branch)node);
                    return;
                case NodeType.DebugBreak:
                    VisitStatement((Statement)node);
                    return;
                case NodeType.Call:
                case NodeType.Calli:
                case NodeType.Callvirt:
                case NodeType.Jmp:
#if !MinimalReader
                case NodeType.MethodCall:
#endif
                    VisitMethodCall((MethodCall)node);
                    return;
                case NodeType.Class:
                case NodeType.ClassParameter:
                    VisitClass((Class)node);
                    return;
                case NodeType.Construct:
                    VisitConstruct((Construct)node);
                    return;
                case NodeType.ConstructArray:
                    VisitConstructArray((ConstructArray)node);
                    return;
                case NodeType.DelegateNode:
                    VisitDelegateNode((DelegateNode)node);
                    return;
                case NodeType.Dup:
                    VisitExpression((Expression)node);
                    return;
                case NodeType.EndFilter:
                    VisitEndFilter((EndFilter)node);
                    return;
                case NodeType.EndFinally:
                    VisitStatement((Statement)node);
                    return;
                case NodeType.EnumNode:
                    VisitEnumNode((EnumNode)node);
                    return;
                case NodeType.Event:
                    VisitEvent((Event)node);
                    return;
                case NodeType.ExpressionStatement:
                    VisitExpressionStatement((ExpressionStatement)node);
                    return;
                case NodeType.Field:
                    VisitField((Field)node);
                    return;
                case NodeType.Indexer:
                    VisitIndexer((Indexer)node);
                    return;
                case NodeType.InstanceInitializer:
                case NodeType.StaticInitializer:
                case NodeType.Method:
                    VisitMethod((Method)node);
                    return;
                case NodeType.TypeParameter:
                case NodeType.Interface:
                    VisitInterface((Interface)node);
                    return;
                case NodeType.Literal:
                    VisitLiteral((Literal)node);
                    return;
                case NodeType.Local:
                    VisitLocal((Local)node);
                    return;
#if !MinimalReader && !CodeContracts
        case NodeType.LocalDeclarationsStatement:
          this.VisitLocalDeclarationsStatement((LocalDeclarationsStatement)node); return;
#endif
                case NodeType.MemberBinding:
                    VisitMemberBinding((MemberBinding)node);
                    return;
                case NodeType.Nop:
                    VisitStatement((Statement)node);
                    return;
                case NodeType.Parameter:
                    VisitParameter((Parameter)node);
                    return;
                case NodeType.Pop:
                    VisitExpression((Expression)node);
                    return;
                case NodeType.Property:
                    VisitProperty((Property)node);
                    return;
                case NodeType.Rethrow:
                case NodeType.Throw:
                    VisitThrow((Throw)node);
                    return;
                case NodeType.Return:
                    VisitReturn((Return)node);
                    return;
                case NodeType.Struct:
#if !MinimalReader
                case NodeType.TypeAlias:
                case NodeType.TypeIntersection:
                case NodeType.TypeUnion:
                case NodeType.TupleType:
#endif
                    VisitStruct((Struct)node);
                    return;
#if !MinimalReader
                case NodeType.SwitchCaseBottom:
                    return;
#endif
                case NodeType.SwitchInstruction:
                    VisitSwitchInstruction((SwitchInstruction)node);
                    return;
                case NodeType.This:
                    VisitThis((This)node);
                    return;

                case NodeType.Cpblk:
                case NodeType.Initblk:
                    VisitTernaryExpression((TernaryExpression)node);
                    return;

                case NodeType.Add:
                case NodeType.Add_Ovf:
                case NodeType.Add_Ovf_Un:
                case NodeType.And:
                case NodeType.Box:
                case NodeType.Castclass:
                case NodeType.Ceq:
                case NodeType.Cgt:
                case NodeType.Cgt_Un:
                case NodeType.Clt:
                case NodeType.Clt_Un:
                case NodeType.Div:
                case NodeType.Div_Un:
                case NodeType.Eq:
                case NodeType.Ge:
                case NodeType.Gt:
#if !MinimalReader
                case NodeType.Is:
#endif
                case NodeType.Isinst:
                case NodeType.Ldvirtftn:
                case NodeType.Le:
                case NodeType.Lt:
                case NodeType.Mkrefany:
                case NodeType.Mul:
                case NodeType.Mul_Ovf:
                case NodeType.Mul_Ovf_Un:
                case NodeType.Ne:
                case NodeType.Or:
                case NodeType.Refanyval:
                case NodeType.Rem:
                case NodeType.Rem_Un:
                case NodeType.Shl:
                case NodeType.Shr:
                case NodeType.Shr_Un:
                case NodeType.Sub:
                case NodeType.Sub_Ovf:
                case NodeType.Sub_Ovf_Un:
                case NodeType.Unbox:
                case NodeType.UnboxAny:
                case NodeType.Xor:
                    VisitBinaryExpression((BinaryExpression)node);
                    return;


                case NodeType.AddressOf:
#if !MinimalReader
                case NodeType.OutAddress:
                case NodeType.RefAddress:
#endif
                case NodeType.ReadOnlyAddressOf:
                    VisitAddressOf((UnaryExpression)node);
                    return;
                case NodeType.Ckfinite:
                case NodeType.Conv_I:
                case NodeType.Conv_I1:
                case NodeType.Conv_I2:
                case NodeType.Conv_I4:
                case NodeType.Conv_I8:
                case NodeType.Conv_Ovf_I:
                case NodeType.Conv_Ovf_I1:
                case NodeType.Conv_Ovf_I1_Un:
                case NodeType.Conv_Ovf_I2:
                case NodeType.Conv_Ovf_I2_Un:
                case NodeType.Conv_Ovf_I4:
                case NodeType.Conv_Ovf_I4_Un:
                case NodeType.Conv_Ovf_I8:
                case NodeType.Conv_Ovf_I8_Un:
                case NodeType.Conv_Ovf_I_Un:
                case NodeType.Conv_Ovf_U:
                case NodeType.Conv_Ovf_U1:
                case NodeType.Conv_Ovf_U1_Un:
                case NodeType.Conv_Ovf_U2:
                case NodeType.Conv_Ovf_U2_Un:
                case NodeType.Conv_Ovf_U4:
                case NodeType.Conv_Ovf_U4_Un:
                case NodeType.Conv_Ovf_U8:
                case NodeType.Conv_Ovf_U8_Un:
                case NodeType.Conv_Ovf_U_Un:
                case NodeType.Conv_R4:
                case NodeType.Conv_R8:
                case NodeType.Conv_R_Un:
                case NodeType.Conv_U:
                case NodeType.Conv_U1:
                case NodeType.Conv_U2:
                case NodeType.Conv_U4:
                case NodeType.Conv_U8:
                case NodeType.Ldftn:
                case NodeType.Ldlen:
                case NodeType.Ldtoken:
                case NodeType.Localloc:
                case NodeType.Neg:
                case NodeType.Not:
                case NodeType.Refanytype:
                case NodeType.Sizeof:
                    VisitUnaryExpression((UnaryExpression)node);
                    return;

                default:
                    // handle type extensions with new NodeType's, that are emitted as ordinary structs and classes
                    var cl = node as Class;
                    if (cl != null)
                    {
                        VisitClass(cl);
                        return;
                    }

                    var st = node as Struct;
                    if (st != null)
                    {
                        VisitStruct(st);
                        return;
                    }

                    Debug.Assert(false, "invalid node: " + node.NodeType);
                    return;
            }
        }

        private void VisitAddressDereference(AddressDereference /*!*/ adr)
        {
            Visit(adr.Address);
            if (adr.Alignment > 0)
            {
                methodBodyHeap.Write((byte)0xfe);
                methodBodyHeap.Write((byte)0x12);
                methodBodyHeap.Write((byte)adr.Alignment);
            }

            if (adr.Volatile)
            {
                methodBodyHeap.Write((byte)0xfe);
                methodBodyHeap.Write((byte)0x13);
            }

            switch (adr.Type.typeCode)
            {
                case ElementType.Int8:
                    methodBodyHeap.Write((byte)0x46);
                    return;
                case ElementType.UInt8:
                    methodBodyHeap.Write((byte)0x47);
                    return;
                case ElementType.Int16:
                    methodBodyHeap.Write((byte)0x48);
                    return;
                case ElementType.Char:
                case ElementType.UInt16:
                    methodBodyHeap.Write((byte)0x49);
                    return;
                case ElementType.Int32:
                    methodBodyHeap.Write((byte)0x4a);
                    return;
                case ElementType.UInt32:
                    methodBodyHeap.Write((byte)0x4b);
                    return;
                case ElementType.Int64:
                case ElementType.UInt64:
                    methodBodyHeap.Write((byte)0x4c);
                    return;
                //case ElementType.UIntPtr:
                //case ElementType.IntPtr: this.methodBodyHeap.Write((byte)0x4d); return;
                case ElementType.Single:
                    methodBodyHeap.Write((byte)0x4e);
                    return;
                case ElementType.Double:
                    methodBodyHeap.Write((byte)0x4f);
                    return;
                default:
                    if (UseGenerics && adr.Type != null && adr.Type != SystemTypes.Object)
                    {
                        methodBodyHeap.Write((byte)0x71);
                        methodBodyHeap.Write(GetTypeToken(adr.Type));
                        return;
                    }

                    if (TypeNode.StripModifiers(adr.Type) is Pointer)
                    {
                        methodBodyHeap.Write((byte)0x4d);
                        return;
                    }

                    methodBodyHeap.Write((byte)0x50);
                    return;
            }
        }

        private void VisitAttributeList(AttributeList attrs, Node /*!*/ node)
        {
            if (attrs == null) return;
            var n = attrs.Count;
            if (n == 0) return;
            var m = n;
            for (var j = 0; j < n; j++)
            {
                var a = attrs[j];
                if (a == null) m--;
            }

            if (m == 0) return;
            n = m;
            var codedIndex = GetCustomAttributeParentCodedIndex(node);
            customAttributeCount += n;
            m = nodesWithCustomAttributes.Count;
            nodesWithCustomAttributes.Add(node);
            var i = 0; //after the for loop i will be position where the new node should be in sorted list
            var nodes = nodesWithCustomAttributes;
            for (i = m; i > 0; i--)
            {
                var other = nodes[i - 1];
                var oci = GetCustomAttributeParentCodedIndex(other);
                if (oci < codedIndex) break;
                if (UseGenerics)
                    if (oci == codedIndex)
                        Debug.Assert(false);
            }

            if (i == m) return; //node is already where it should be
            for (var j = m; j > i; j--) nodes[j] = nodes[j - 1]; //Make space at postion i
            nodes[i] = node;
        }

        private void VisitAddressOf(UnaryExpression /*!*/ expr)
        {
            var operand = expr.Operand;
            if (operand == null) return;
            switch (operand.NodeType)
            {
                case NodeType.Indexer:
                    var indexer = (Indexer)operand;
                    Visit(indexer.Object);
                    if (indexer.Operands == null || indexer.Operands.Count < 1) return;
                    Visit(indexer.Operands[0]);
                    if (expr.NodeType == NodeType.ReadOnlyAddressOf)
                    {
                        methodBodyHeap.Write((byte)0xfe);
                        methodBodyHeap.Write((byte)0x1e);
                    }

                    methodBodyHeap.Write((byte)0x8f);
                    methodBodyHeap.Write(GetTypeToken(indexer.ElementType));
                    stackHeight--;
                    return;
                case NodeType.Local:
                    var li = GetLocalVarIndex((Local)operand);
                    if (li < 256)
                    {
                        methodBodyHeap.Write((byte)0x12);
                        methodBodyHeap.Write((byte)li);
                    }
                    else
                    {
                        methodBodyHeap.Write((byte)0xfe);
                        methodBodyHeap.Write((byte)0x0d);
                        methodBodyHeap.Write((ushort)li);
                    }

                    IncrementStackHeight();
                    return;
                case NodeType.MemberBinding:
                    var mb = (MemberBinding)operand;
                    if (mb.TargetObject != null)
                    {
                        Visit(mb.TargetObject);
                        methodBodyHeap.Write((byte)0x7c);
                    }
                    else
                    {
                        methodBodyHeap.Write((byte)0x7f);
                        IncrementStackHeight();
                    }

                    methodBodyHeap.Write(GetFieldToken((Field)mb.BoundMember));
                    return;
                case NodeType.Parameter:
                case NodeType.This:
#if !MinimalReader
                    var pb = operand as ParameterBinding;
                    if (pb != null) operand = pb.BoundParameter;
#endif
                    var pi = ((Parameter)operand).ArgumentListIndex;
                    if (pi < 256)
                    {
                        methodBodyHeap.Write((byte)0x0f);
                        methodBodyHeap.Write((byte)pi);
                    }
                    else
                    {
                        methodBodyHeap.Write((byte)0xfe);
                        methodBodyHeap.Write((byte)0x0a);
                        methodBodyHeap.Write((ushort)pi);
                    }

                    IncrementStackHeight();
                    return;
            }
        }

        private void VisitAssignmentStatement(AssignmentStatement /*!*/ assignment)
        {
            DefineSequencePoint(assignment);
            var target = assignment.Target;
            switch (assignment.Target.NodeType)
            {
                case NodeType.Local:
                    var loc = (Local)target;
                    Visit(assignment.Source);
                    stackHeight--;
                    var li = GetLocalVarIndex(loc);
                    switch (li)
                    {
                        case 0:
                            methodBodyHeap.Write((byte)0x0a);
                            return;
                        case 1:
                            methodBodyHeap.Write((byte)0x0b);
                            return;
                        case 2:
                            methodBodyHeap.Write((byte)0x0c);
                            return;
                        case 3:
                            methodBodyHeap.Write((byte)0x0d);
                            return;
                        default:
                            if (li < 256)
                            {
                                methodBodyHeap.Write((byte)0x13);
                                methodBodyHeap.Write((byte)li);
                            }
                            else
                            {
                                methodBodyHeap.Write((byte)0xfe);
                                methodBodyHeap.Write((byte)0x0e);
                                methodBodyHeap.Write((ushort)li);
                            }

                            return;
                    }
                case NodeType.MemberBinding:
                    var mb = (MemberBinding)target;
                    if (mb.TargetObject != null) Visit(mb.TargetObject);
                    Visit(assignment.Source);
                    if (mb.TargetObject != null)
                    {
                        if (mb.Alignment != -1)
                        {
                            methodBodyHeap.Write((byte)0xfe);
                            methodBodyHeap.Write((byte)0x12);
                            methodBodyHeap.Write((byte)mb.Alignment);
                        }

                        if (mb.Volatile)
                        {
                            methodBodyHeap.Write((byte)0xfe);
                            methodBodyHeap.Write((byte)0x13);
                        }

                        methodBodyHeap.Write((byte)0x7d);
                    }
                    else
                    {
                        if (mb.Volatile)
                        {
                            methodBodyHeap.Write((byte)0xfe);
                            methodBodyHeap.Write((byte)0x13);
                        }

                        methodBodyHeap.Write((byte)0x80);
                    }

                    methodBodyHeap.Write(GetFieldToken((Field)mb.BoundMember));
                    if (mb.TargetObject != null)
                        stackHeight -= 2;
                    else
                        stackHeight--;
                    return;
                case NodeType.This:
                    Visit(assignment.Source);
                    methodBodyHeap.Write((byte)0x10);
                    methodBodyHeap.Write((byte)0x00);
                    stackHeight--;
                    return;
                case NodeType.Parameter:
#if !MinimalReader
                    var pb = target as ParameterBinding;
                    if (pb != null) target = pb.BoundParameter;
#endif
                    var par = (Parameter)target;
                    Visit(assignment.Source);
                    var pi = par.ArgumentListIndex;
                    if (pi < 256)
                    {
                        methodBodyHeap.Write((byte)0x10);
                        methodBodyHeap.Write((byte)pi);
                    }
                    else
                    {
                        methodBodyHeap.Write((byte)0xfe);
                        methodBodyHeap.Write((byte)0x0b);
                        methodBodyHeap.Write((ushort)pi);
                    }

                    stackHeight--;
                    return;
                case NodeType.Indexer:
                    var indexer = (Indexer)target;
                    Visit(indexer.Object);
                    if (indexer.Operands == null || indexer.Operands.Count < 1) return;
                    Visit(indexer.Operands[0]);
                    Visit(assignment.Source);
                    byte opCode;
                    switch (indexer.ElementType.typeCode)
                    {
                        case ElementType.UIntPtr:
                        case ElementType.IntPtr:
                            opCode = 0x9b;
                            break;
                        case ElementType.Boolean:
                        case ElementType.Int8:
                        case ElementType.UInt8:
                            opCode = 0x9c;
                            break;
                        case ElementType.Char:
                        case ElementType.Int16:
                        case ElementType.UInt16:
                            opCode = 0x9d;
                            break;
                        case ElementType.Int32:
                        case ElementType.UInt32:
                            opCode = 0x9e;
                            break;
                        case ElementType.Int64:
                        case ElementType.UInt64:
                            opCode = 0x9f;
                            break;
                        case ElementType.Single:
                            opCode = 0xa0;
                            break;
                        case ElementType.Double:
                            opCode = 0xa1;
                            break;
                        default:
                            if (UseGenerics && indexer.ElementType != null && indexer.ElementType != SystemTypes.Object)
                                opCode = 0xa4;
                            else if (TypeNode.StripModifiers(indexer.ElementType) is Pointer)
                                opCode = 0x9b;
                            else
                                opCode = 0xa2;
                            break;
                    }

                    methodBodyHeap.Write(opCode);
                    if (opCode == 0xa4) methodBodyHeap.Write(GetTypeToken(indexer.ElementType));
                    stackHeight -= 3;
                    return;
                case NodeType.AddressDereference:
                    var adr = (AddressDereference)target;
                    Visit(adr.Address);
                    if (adr.Type != null)
                    {
                        var lit = assignment.Source as Literal;
                        if (lit != null && lit.Value == null)
                        {
                            if (adr.Type == SystemTypes.Object)
                            {
                                methodBodyHeap.Write((byte)OpCode.Ldnull);
                                IncrementStackHeight();
                                methodBodyHeap.Write((byte)OpCode.Stind_Ref);
                                stackHeight -= 2;
                            }
                            else
                            {
                                methodBodyHeap.Write((byte)0xfe);
                                methodBodyHeap.Write((byte)0x15);
                                methodBodyHeap.Write(GetTypeToken(adr.Type));
                                stackHeight--;
                            }

                            return;
                        }
                    }

                    Visit(assignment.Source);
                    stackHeight -= 2;
                    if (adr.Alignment > 0)
                    {
                        methodBodyHeap.Write((byte)0xfe);
                        methodBodyHeap.Write((byte)0x12);
                        methodBodyHeap.Write((byte)adr.Alignment);
                    }

                    if (adr.Volatile)
                    {
                        methodBodyHeap.Write((byte)0xfe);
                        methodBodyHeap.Write((byte)0x13);
                    }

                    var adrType = TypeNode.StripModifiers(adr.Type);
                    if (adrType == null) return;
                    switch (adrType.typeCode)
                    {
                        case ElementType.Int8:
                        case ElementType.UInt8:
                            methodBodyHeap.Write((byte)0x52);
                            return;
                        case ElementType.Int16:
                        case ElementType.UInt16:
                            methodBodyHeap.Write((byte)0x53);
                            return;
                        case ElementType.Int32:
                        case ElementType.UInt32:
                            methodBodyHeap.Write((byte)0x54);
                            return;
                        case ElementType.Int64:
                        case ElementType.UInt64:
                            methodBodyHeap.Write((byte)0x55);
                            return;
                        case ElementType.Single:
                            methodBodyHeap.Write((byte)0x56);
                            return;
                        case ElementType.Double:
                            methodBodyHeap.Write((byte)0x57);
                            return;
                        case ElementType.UIntPtr:
                        case ElementType.IntPtr:
                            methodBodyHeap.Write((byte)0xdf);
                            return;
                        default:
                            if (UseGenerics && adrType != null && adrType != SystemTypes.Object)
                            {
                                methodBodyHeap.Write((byte)0x81);
                                methodBodyHeap.Write(GetTypeToken(adrType));
                                return;
                            }

                            if (adrType.NodeType == NodeType.Pointer)
                            {
                                methodBodyHeap.Write((byte)0xdf);
                                return;
                            }

                            methodBodyHeap.Write((byte)0x51);
                            return;
                    }
                default:
                    Debug.Assert(false, "unexpected assignment target");
                    return;
            }
        }
#if !MinimalReader && !CodeContracts
    void VisitBase(Base/*!*/ Base) {
      this.IncrementStackHeight();
      this.methodBodyHeap.Write((byte)0x02);
    }
#endif
        private void VisitBinaryExpression(BinaryExpression /*!*/ binaryExpression)
        {
            byte opCode = 0;
            Visit(binaryExpression.Operand1);
            switch (binaryExpression.NodeType)
            {
                case NodeType.Castclass:
                    opCode = 0x74;
                    goto writeOpCodeAndToken;
                case NodeType.Isinst:
                    opCode = 0x75;
                    goto writeOpCodeAndToken;
                case NodeType.Unbox:
                    opCode = 0x79;
                    goto writeOpCodeAndToken;
                case NodeType.UnboxAny:
                    opCode = 0xa5;
                    goto writeOpCodeAndToken;
                case NodeType.Box:
                    opCode = 0x8c;
                    goto writeOpCodeAndToken;
                case NodeType.Refanyval:
                    opCode = 0xc2;
                    goto writeOpCodeAndToken;
                case NodeType.Mkrefany:
                    opCode = 0xc6;
                    writeOpCodeAndToken:
                    methodBodyHeap.Write(opCode);
                    var lit = binaryExpression.Operand2 as Literal;
                    if (lit != null)
                        methodBodyHeap.Write(GetTypeToken((TypeNode)lit.Value));
                    else // TODO: Normalized IR should never use a MemberBinding to represent a type
                        methodBodyHeap.Write(
                            GetTypeToken((TypeNode)((MemberBinding)binaryExpression.Operand2).BoundMember));
                    return;
                case NodeType.Ldvirtftn:
                    opCode = 0x07;
                    methodBodyHeap.Write((byte)0xfe);
                    methodBodyHeap.Write(opCode);
                    methodBodyHeap.Write(
                        GetMethodToken((Method)((MemberBinding)binaryExpression.Operand2).BoundMember));
                    return;
            }

            Visit(binaryExpression.Operand2);
            switch (binaryExpression.NodeType)
            {
                case NodeType.Add:
                    opCode = 0x58;
                    break;
                case NodeType.Sub:
                    opCode = 0x59;
                    break;
                case NodeType.Mul:
                    opCode = 0x5a;
                    break;
                case NodeType.Div:
                    opCode = 0x5b;
                    break;
                case NodeType.Div_Un:
                    opCode = 0x5c;
                    break;
                case NodeType.Rem:
                    opCode = 0x5d;
                    break;
                case NodeType.Rem_Un:
                    opCode = 0x5e;
                    break;
                case NodeType.And:
                    opCode = 0x5f;
                    break;
                case NodeType.Or:
                    opCode = 0x60;
                    break;
                case NodeType.Xor:
                    opCode = 0x61;
                    break;
                case NodeType.Shl:
                    opCode = 0x62;
                    break;
                case NodeType.Shr:
                    opCode = 0x63;
                    break;
                case NodeType.Shr_Un:
                    opCode = 0x64;
                    break;
                case NodeType.Add_Ovf:
                    opCode = 0xd6;
                    break;
                case NodeType.Add_Ovf_Un:
                    opCode = 0xd7;
                    break;
                case NodeType.Mul_Ovf:
                    opCode = 0xd8;
                    break;
                case NodeType.Mul_Ovf_Un:
                    opCode = 0xd9;
                    break;
                case NodeType.Sub_Ovf:
                    opCode = 0xda;
                    break;
                case NodeType.Sub_Ovf_Un:
                    opCode = 0xdb;
                    break;
                case NodeType.Ceq:
                    opCode = 0x01;
                    methodBodyHeap.Write((byte)0xfe);
                    break;
                case NodeType.Cgt:
                    opCode = 0x02;
                    methodBodyHeap.Write((byte)0xfe);
                    break;
                case NodeType.Cgt_Un:
                    opCode = 0x03;
                    methodBodyHeap.Write((byte)0xfe);
                    break;
                case NodeType.Clt:
                    opCode = 0x04;
                    methodBodyHeap.Write((byte)0xfe);
                    break;
                case NodeType.Clt_Un:
                    opCode = 0x05;
                    methodBodyHeap.Write((byte)0xfe);
                    break;
            }

            methodBodyHeap.Write(opCode);
            stackHeight--;
        }

        private void VisitBlock(Block /*!*/ block)
        {
            var mInfo = methodInfo;
            var currentAddress = methodBodyHeap.BaseStream.Position;
            VisitFixupList(methodInfo.fixupIndex[block.UniqueKey] as Fixup, currentAddress);
            mInfo.fixupIndex[block.UniqueKey] = currentAddress;
            methodBodyHeap.BaseStream.Position = currentAddress;
            var savedStackHeight = stackHeight;
            if (exceptionBlock[block.UniqueKey] != null) stackHeight = 1;
            var statements = block.Statements;
            if (statements == null) return;
#if !ROTOR
            if (symWriter != null && block.HasLocals)
            {
                var savedDebugLocals = mInfo.debugLocals;
                var savedSignatureLengths = mInfo.signatureLengths;
                var savedSignatureOffsets = mInfo.signatureOffsets;
                mInfo.debugLocals = new LocalList();
                mInfo.signatureLengths = new Int32List();
                mInfo.signatureOffsets = new Int32List();
                symWriter.OpenScope((uint)currentAddress);
                for (int i = 0, n = statements.Count; i < n; i++)
                    Visit(statements[i]);
                if (stackHeight > 0) stackHeightExitTotal += stackHeight;
                DefineLocalVariables(currentAddress, mInfo.debugLocals);
                mInfo.debugLocals = savedDebugLocals;
                mInfo.signatureLengths = savedSignatureLengths;
                mInfo.signatureOffsets = savedSignatureOffsets;
            }
            else
            {
#endif
                for (int i = 0, n = statements.Count; i < n; i++)
                    Visit(statements[i]);
                if (stackHeight > savedStackHeight) stackHeightExitTotal += stackHeight - savedStackHeight;
#if !ROTOR
            }
#endif
            stackHeight = savedStackHeight;
        }
#if !MinimalReader
        private void VisitBlockExpression(BlockExpression /*!*/ blockExpression)
        {
            if (blockExpression.Block == null) return;
            VisitBlock(blockExpression.Block);
        }
#endif
        private void VisitBranch(Branch /*!*/ branch)
        {
            DefineSequencePoint(branch);
            var bex = branch.Condition as BinaryExpression;
            UnaryExpression uex = null;
            var typeOfCondition = NodeType.Nop;
            if (bex != null)
            {
                switch (bex.NodeType)
                {
                    case NodeType.Eq:
                    case NodeType.Ge:
                    case NodeType.Gt:
                    case NodeType.Le:
                    case NodeType.Lt:
                    case NodeType.Ne:
                        Visit(bex.Operand1);
                        Visit(bex.Operand2);
                        typeOfCondition = bex.NodeType;
                        stackHeight -= 2;
                        break;
                    case NodeType.And:
                    case NodeType.Or:
                    case NodeType.Xor:
                    case NodeType.Isinst:
                    case NodeType.Castclass:
                        typeOfCondition = bex.NodeType;
                        goto default;
                    default:
                        Visit(branch.Condition);
                        stackHeight--;
                        break;
                }
            }
            else
            {
                uex = branch.Condition as UnaryExpression;
                if (uex != null && uex.NodeType == NodeType.LogicalNot)
                {
                    Visit(uex.Operand);
                    typeOfCondition = NodeType.LogicalNot;
                    stackHeight--;
                }
                else if (branch.Condition != null)
                {
                    // Undefined is used here simply as a sentinel value
                    typeOfCondition = NodeType.Undefined;
                    Visit(branch.Condition);
                    stackHeight--;
                }
            }

            var target = GetOffset(branch.Target, ref branch.shortOffset);
            if (branch.ShortOffset)
            {
                switch (typeOfCondition)
                {
                    case NodeType.Nop:
                        if (branch.Condition == null)
                        {
                            if (branch.LeavesExceptionBlock)
                                methodBodyHeap.Write((byte)0xde);
                            else
                                methodBodyHeap.Write((byte)0x2b);
                            break;
                        }

                        methodBodyHeap.Write((byte)0x2d);
                        break;
                    case NodeType.And:
                    case NodeType.Or:
                    case NodeType.Xor:
                    case NodeType.Isinst:
                    case NodeType.Castclass:
                    case NodeType.Undefined:
                        methodBodyHeap.Write((byte)0x2d);
                        break;
                    case NodeType.LogicalNot:
                        methodBodyHeap.Write((byte)0x2c);
                        break;
                    case NodeType.Eq:
                        methodBodyHeap.Write((byte)0x2e);
                        break;
                    case NodeType.Ge:
                        if (branch.BranchIfUnordered)
                            methodBodyHeap.Write((byte)0x34);
                        else
                            methodBodyHeap.Write((byte)0x2f);
                        break;
                    case NodeType.Gt:
                        if (branch.BranchIfUnordered)
                            methodBodyHeap.Write((byte)0x35);
                        else
                            methodBodyHeap.Write((byte)0x30);
                        break;
                    case NodeType.Le:
                        if (branch.BranchIfUnordered)
                            methodBodyHeap.Write((byte)0x36);
                        else
                            methodBodyHeap.Write((byte)0x31);
                        break;
                    case NodeType.Lt:
                        if (branch.BranchIfUnordered)
                            methodBodyHeap.Write((byte)0x37);
                        else
                            methodBodyHeap.Write((byte)0x32);
                        break;
                    case NodeType.Ne:
                        methodBodyHeap.Write((byte)0x33);
                        break;
                }

                methodBodyHeap.Write((sbyte)target);
            }
            else
            {
                switch (typeOfCondition)
                {
                    case NodeType.Nop:
                        if (branch.Condition == null)
                        {
                            if (branch.LeavesExceptionBlock)
                                methodBodyHeap.Write((byte)0xdd);
                            else
                                methodBodyHeap.Write((byte)0x38);
                            break;
                        }

                        methodBodyHeap.Write((byte)0x3a);
                        break;
                    case NodeType.And:
                    case NodeType.Or:
                    case NodeType.Xor:
                    case NodeType.Isinst:
                    case NodeType.Castclass:
                    case NodeType.Undefined:
                        methodBodyHeap.Write((byte)0x3a);
                        break;
                    case NodeType.LogicalNot:
                        methodBodyHeap.Write((byte)0x39);
                        break;
                    case NodeType.Eq:
                        methodBodyHeap.Write((byte)0x3b);
                        break;
                    case NodeType.Ge:
                        if (branch.BranchIfUnordered)
                            methodBodyHeap.Write((byte)0x41);
                        else
                            methodBodyHeap.Write((byte)0x3c);
                        break;
                    case NodeType.Gt:
                        if (branch.BranchIfUnordered)
                            methodBodyHeap.Write((byte)0x42);
                        else
                            methodBodyHeap.Write((byte)0x3d);
                        break;
                    case NodeType.Le:
                        if (branch.BranchIfUnordered)
                            methodBodyHeap.Write((byte)0x43);
                        else
                            methodBodyHeap.Write((byte)0x3e);
                        break;
                    case NodeType.Lt:
                        if (branch.BranchIfUnordered)
                            methodBodyHeap.Write((byte)0x44);
                        else
                            methodBodyHeap.Write((byte)0x3f);
                        break;
                    case NodeType.Ne:
                        methodBodyHeap.Write((byte)0x40);
                        break;
                }

                methodBodyHeap.Write(target);
            }
        }

        private void VisitMethodCall(MethodCall /*!*/ call)
        {
            var mb = (MemberBinding)call.Callee;
            var constraint = call.Constraint;
            Visit(mb.TargetObject);
            var arguments = call.Operands;
            var pops = 0;
            if (arguments != null)
            {
                VisitExpressionList(arguments);
                pops = arguments.Count;
            }

            if (call.Type != CoreSystemTypes.Void)
            {
                VisitReferencedType(call.Type);
                pops--;
            }

            if (pops >= 0)
                stackHeight -= pops;
            else
                IncrementStackHeight(); //make sure the high water mark moves up if necessary
            if (call.IsTailCall)
            {
                methodBodyHeap.Write((byte)0xfe);
                methodBodyHeap.Write((byte)0x14);
            }
            else if (constraint != null)
            {
                methodBodyHeap.Write((byte)0xfe);
                methodBodyHeap.Write((byte)0x16);
                methodBodyHeap.Write(GetTypeToken(constraint));
            }

            switch (call.NodeType)
            {
                case NodeType.Calli:
                    methodBodyHeap.Write((byte)0x29);
                    var sig = new BinaryWriter(new MemoryStream());
                    WriteMethodSignature(sig, (FunctionPointer)mb.BoundMember);
                    methodBodyHeap.Write(0x11000000 | GetStandAloneSignatureIndex(sig));
                    return;
                case NodeType.Callvirt:
                    methodBodyHeap.Write((byte)0x6f);
                    break;
                case NodeType.Jmp:
                    methodBodyHeap.Write((byte)0x27);
                    break;
                default:
                    methodBodyHeap.Write((byte)0x28);
                    break;
            }

            var method = (Method)mb.BoundMember;
            if ((method.CallingConvention & (CallingConventionFlags)7) == CallingConventionFlags.VarArg ||
                (method.CallingConvention & (CallingConventionFlags)7) == CallingConventionFlags.C)
                methodBodyHeap.Write(GetMemberRefToken(method, arguments));
            else
                methodBodyHeap.Write(GetMethodToken(method));
        }

        private void VisitClass(Class /*!*/ Class)
        {
            if (UseGenerics && Class.Template != null && Class.Template.IsGeneric) return;
            VisitAttributeList(Class.Attributes, Class);
            VisitSecurityAttributeList(Class.SecurityAttributes, Class);
            if (Class.BaseClass != null) VisitReferencedType(Class.BaseClass);
            for (int i = 0, n = Class.Interfaces == null ? 0 : Class.Interfaces.Count; i < n; i++)
            {
                GetTypeDefOrRefOrSpecEncoded(Class.Interfaces[i]);
                if (Class.Interfaces[i] != null) interfaceEntries.Add(Class);
            }

            if (Class.NodeType == NodeType.ClassParameter && !(Class is MethodClassParameter))
                interfaceEntries.Add(Class);
            for (int i = 0, n = Class.Members.Count; i < n; i++)
            {
                var mem = Class.Members[i];
                if (mem == null || mem is TypeNode) continue;
                Visit(mem);
            }

            if ((Class.Flags & (TypeFlags.ExplicitLayout | TypeFlags.SequentialLayout)) != 0 &&
                (Class.PackingSize != 0 || Class.ClassSize != 0))
                classLayoutEntries.Add(Class);
        }

        private void VisitConstruct(Construct /*!*/ cons)
        {
            var pops = -1;
            var operands = cons.Operands;
            if (operands != null)
            {
                VisitExpressionList(cons.Operands);
                pops = operands.Count - 1;
            }

            if (pops >= 0)
                stackHeight -= pops;
            else
                IncrementStackHeight();
            methodBodyHeap.Write((byte)0x73);
            var method = ((MemberBinding)cons.Constructor).BoundMember as Method;
            if (method == null) return;
            methodBodyHeap.Write(GetMethodToken(method)); //REVIEW: varargs?
        }

        private void VisitConstructArray(ConstructArray /*!*/ consArr)
        {
            if (consArr == null || consArr.Operands == null || consArr.Operands.Count < 1) return;
            Visit(consArr.Operands[0]);
            methodBodyHeap.Write((byte)0x8d);
            methodBodyHeap.Write(GetTypeToken(consArr.ElementType));
        }

        private void VisitDelegateNode(DelegateNode /*!*/ delegateNode)
        {
            if (UseGenerics && delegateNode.Template != null && delegateNode.Template.IsGeneric) return;
            VisitAttributeList(delegateNode.Attributes, delegateNode);
            VisitSecurityAttributeList(delegateNode.SecurityAttributes, delegateNode);
            VisitReferencedType(CoreSystemTypes.MulticastDelegate);
            for (int i = 0, n = delegateNode.Interfaces == null ? 0 : delegateNode.Interfaces.Count; i < n; i++)
            {
                //REVIEW: is this valid?
                GetTypeDefOrRefOrSpecEncoded(delegateNode.Interfaces[i]);
                if (delegateNode.Interfaces[i] != null) interfaceEntries.Add(delegateNode);
            }

            for (int i = 0, n = delegateNode.Members.Count; i < n; i++)
            {
                var mem = delegateNode.Members[i];
                if (mem == null || mem is TypeNode) continue;
                Visit(mem);
            }
        }

        private void VisitEndFilter(EndFilter /*!*/ endFilter)
        {
            DefineSequencePoint(endFilter);
            Visit(endFilter.Value);
            methodBodyHeap.Write((byte)0xfe);
            methodBodyHeap.Write((byte)0x11);
            stackHeight--;
        }

        private void VisitEnumNode(EnumNode /*!*/ enumNode)
        {
            VisitAttributeList(enumNode.Attributes, enumNode);
            VisitSecurityAttributeList(enumNode.SecurityAttributes, enumNode);
            VisitReferencedType(CoreSystemTypes.Enum);
            for (int i = 0, n = enumNode.Interfaces == null ? 0 : enumNode.Interfaces.Count; i < n; i++)
            {
                GetTypeDefOrRefOrSpecEncoded(enumNode.Interfaces[i]);
                if (enumNode.Interfaces[i] != null) interfaceEntries.Add(enumNode);
            }

            for (int i = 0, n = enumNode.Members.Count; i < n; i++)
                Visit(enumNode.Members[i]);
        }

        private void VisitEvent(Event /*!*/ Event)
        {
            var eindex = eventIndex[Event.UniqueKey];
            if (eindex != null) return;
            var index = eventEntries.Count + 1;
            eventEntries.Add(Event);
            eventIndex[Event.UniqueKey] = index;
            var evindex = eventMapIndex[Event.DeclaringType.UniqueKey];
            if (evindex == null)
            {
                eventMapEntries.Add(Event);
                eventMapIndex[Event.DeclaringType.UniqueKey] = eventMapEntries.Count;
            }

            if (Event.HandlerAdder != null) methodSemanticsEntries.Add(Event);
            if (Event.HandlerRemover != null) methodSemanticsEntries.Add(Event);
            if (Event.HandlerCaller != null) methodSemanticsEntries.Add(Event);
            if (Event.OtherMethods != null)
                for (int i = 0, n = Event.OtherMethods.Count; i < n; i++)
                    methodSemanticsEntries.Add(Event);
            VisitAttributeList(Event.Attributes, Event);
        }

        private void VisitExpression(Expression /*!*/ expression)
        {
            switch (expression.NodeType)
            {
                case NodeType.Dup:
                    methodBodyHeap.Write((byte)0x25);
                    IncrementStackHeight();
                    return;
                case NodeType.Pop:
                    var unex = expression as UnaryExpression;
                    if (unex != null)
                    {
                        Visit(unex.Operand);
                        stackHeight--;
                        methodBodyHeap.Write((byte)0x26);
                    }

                    return;
                case NodeType.Arglist:
                    IncrementStackHeight();
                    methodBodyHeap.Write((byte)0xfe);
                    methodBodyHeap.Write((byte)0x00);
                    return;
            }
        }

        private void VisitExpressionList(ExpressionList expressions)
        {
            if (expressions == null) return;
            for (int i = 0, n = expressions.Count; i < n; i++)
                Visit(expressions[i]);
        }

        private void VisitExpressionStatement(ExpressionStatement /*!*/ statement)
        {
#if !MinimalReader
            if (!(statement.Expression is BlockExpression))
#endif
                DefineSequencePoint(statement);
            Visit(statement.Expression);
        }

        private void VisitField(Field /*!*/ field)
        {
            VisitAttributeList(field.Attributes, field);
            GetFieldIndex(field);
            if (field.IsVolatile)
                field.Type = RequiredModifier.For(CoreSystemTypes.IsVolatile, field.Type);
            VisitReferencedType(field.Type);
        }

        private void VisitFixupList(Fixup fixup, int targetAddress)
        {
            while (fixup != null)
            {
                methodBodyHeap.BaseStream.Position = fixup.fixupLocation;
                if (fixup.shortOffset)
                {
                    var offset = targetAddress - fixup.addressOfNextInstruction;
                    Debug.Assert(-128 <= offset && offset <= 127, "Invalid short branch");
                    methodBodyHeap.Write((byte)offset);
                }
                else
                {
                    methodBodyHeap.Write(targetAddress - fixup.addressOfNextInstruction);
                }

                fixup = fixup.nextFixUp;
            }
        }

        private void VisitGenericParameterList(Member /*!*/ member, TypeNodeList /*!*/ parameters)
        {
            if (member == null || parameters == null || !UseGenerics) return;
            var sign = member is Method ? -1 : 1;
            for (int i = 0, n = parameters.Count; i < n; i++)
            {
                var parameter = parameters[i];
                if (parameter == null) continue;
                typeParameterNumber[parameter.UniqueKey] = sign * (i + 1);
                genericParamEntries.Add(member);
                if (((ITypeParameter)parameter).DeclaringMember != member)
                    parameter = (TypeNode)parameter.Clone();
                genericParameters.Add(parameter);
#if CodeContracts
                for (int j = 0,
                     m = parameter.StructuralElementTypes == null ? 0 : parameter.StructuralElementTypes.Count;
                     j < m;
                     j++)
                    genericParamConstraintEntries.Add(parameter);
#else
        if (parameter.BaseType is Class && parameter.BaseType != CoreSystemTypes.Object)
          this.genericParamConstraintEntries.Add(parameter);
        for (int j = 0, m = parameter.Interfaces == null ? 0 : parameter.Interfaces.Count; j < m; j++)
          this.genericParamConstraintEntries.Add(parameter);
#endif
            }
        }

        private void VisitIndexer(Indexer /*!*/ indexer)
        {
            Visit(indexer.Object);
            if (indexer.Operands == null || indexer.Operands.Count < 1) return;
            Visit(indexer.Operands[0]);
            byte opCode;
            switch (indexer.ElementType.typeCode)
            {
                case ElementType.Boolean:
                case ElementType.Int8:
                    opCode = 0x90;
                    break;
                case ElementType.UInt8:
                    opCode = 0x91;
                    break;
                case ElementType.Int16:
                    opCode = 0x92;
                    break;
                case ElementType.Char:
                case ElementType.UInt16:
                    opCode = 0x93;
                    break;
                case ElementType.Int32:
                    opCode = 0x94;
                    break;
                case ElementType.UInt32:
                    opCode = 0x95;
                    break;
                case ElementType.Int64:
                case ElementType.UInt64:
                    opCode = 0x96;
                    break;
                case ElementType.UIntPtr:
                case ElementType.IntPtr:
                    opCode = 0x97;
                    break;
                case ElementType.Single:
                    opCode = 0x98;
                    break;
                case ElementType.Double:
                    opCode = 0x99;
                    break;
                default:
                    if (UseGenerics && indexer.ElementType != null && indexer.ElementType != SystemTypes.Object)
                        opCode = 0xa3;
                    else if (TypeNode.StripModifiers(indexer.ElementType) is Pointer)
                        opCode = 0x97;
                    else
                        opCode = 0x9a;
                    break;
            }

            methodBodyHeap.Write(opCode);
            if (opCode == 0xa3) methodBodyHeap.Write(GetTypeToken(indexer.ElementType));
            stackHeight--;
        }

        private void VisitInterface(Interface /*!*/ Interface)
        {
            if (UseGenerics && Interface.Template != null && Interface.Template.IsGeneric) return;
            VisitAttributeList(Interface.Attributes, Interface);
            VisitSecurityAttributeList(Interface.SecurityAttributes, Interface);
            var interfaces = Interface.Interfaces;
            for (int i = 0, n = interfaces == null ? 0 : interfaces.Count; i < n; i++)
            {
                GetTypeDefOrRefOrSpecEncoded(interfaces[i]);
                if (interfaces[i] != null) interfaceEntries.Add(Interface);
            }

            if (Interface.NodeType == NodeType.TypeParameter && !(Interface is MethodTypeParameter))
                interfaceEntries.Add(Interface);
            for (int i = 0, n = Interface.Members.Count; i < n; i++)
            {
                var mem = Interface.Members[i];
                if (mem == null || mem is TypeNode) continue;
                Visit(mem);
            }
        }

        private void VisitLocal(Local /*!*/ local)
        {
            IncrementStackHeight();
            var li = GetLocalVarIndex(local);
            switch (li)
            {
                case 0:
                    methodBodyHeap.Write((byte)0x06);
                    return;
                case 1:
                    methodBodyHeap.Write((byte)0x07);
                    return;
                case 2:
                    methodBodyHeap.Write((byte)0x08);
                    return;
                case 3:
                    methodBodyHeap.Write((byte)0x09);
                    return;
                default:
                    if (li < 256)
                    {
                        methodBodyHeap.Write((byte)0x11);
                        methodBodyHeap.Write((byte)li);
                    }
                    else
                    {
                        methodBodyHeap.Write((byte)0xfe);
                        methodBodyHeap.Write((byte)0x0c);
                        methodBodyHeap.Write((ushort)li);
                    }

                    return;
            }
        }
#if !MinimalReader && !CodeContracts
    /// <summary>
    /// This just gets the local variable index for each local declaration.
    /// That associates the debug information with the right block because
    /// it is the block the local is declared in rather than the subblock
    /// it is first referenced in. (When different, the debugger only knows
    /// about the local when control is in the subblock.)
    /// </summary>
    /// <param name="localDeclarations">The list of locals declared at this statement</param>
    void VisitLocalDeclarationsStatement(LocalDeclarationsStatement/*!*/ localDeclarations) {
      if (localDeclarations == null) return;
      LocalDeclarationList decls = localDeclarations.Declarations;
      for (int i = 0, n = decls == null ? 0 : decls.Count; i < n; i++) {
        //^ assert decls != null;
        LocalDeclaration decl = decls[i];
        if (decl == null) continue;
        Field f = decl.Field;
        if (f == null) continue;
        //^ assume this.currentMethod != null;
        Local loc = this.currentMethod.GetLocalForField(f);
        loc.Type = localDeclarations.Type;
        this.GetLocalVarIndex(loc);
      }
    }
#endif
        private void VisitLiteral(Literal /*!*/ literal)
        {
            IncrementStackHeight();
            var ic = literal.Value as IConvertible;
            if (ic == null)
            {
                Debug.Assert(literal.Value == null && !literal.Type.IsValueType);
                methodBodyHeap.Write((byte)0x14);
                return;
            }

            var tc = ic.GetTypeCode();
            switch (tc)
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                    var n = ic.ToInt64(null);
                    switch (n)
                    {
                        case -1:
                            methodBodyHeap.Write((byte)0x15);
                            break;
                        case 0:
                            methodBodyHeap.Write((byte)0x16);
                            break;
                        case 1:
                            methodBodyHeap.Write((byte)0x17);
                            break;
                        case 2:
                            methodBodyHeap.Write((byte)0x18);
                            break;
                        case 3:
                            methodBodyHeap.Write((byte)0x19);
                            break;
                        case 4:
                            methodBodyHeap.Write((byte)0x1a);
                            break;
                        case 5:
                            methodBodyHeap.Write((byte)0x1b);
                            break;
                        case 6:
                            methodBodyHeap.Write((byte)0x1c);
                            break;
                        case 7:
                            methodBodyHeap.Write((byte)0x1d);
                            break;
                        case 8:
                            methodBodyHeap.Write((byte)0x1e);
                            break;
                        default:
                            if (n >= sbyte.MinValue && n <= sbyte.MaxValue)
                            {
                                methodBodyHeap.Write((byte)0x1f);
                                methodBodyHeap.Write((byte)n);
                            }
                            else if ((n >= int.MinValue && n <= int.MaxValue) ||
                                     (n <= uint.MaxValue && (tc == TypeCode.Char || tc == TypeCode.UInt16 ||
                                                             tc == TypeCode.UInt32)))
                            {
                                if (n == uint.MaxValue && tc != TypeCode.Int64)
                                {
                                    methodBodyHeap.Write((byte)0x15);
                                }
                                else
                                {
                                    methodBodyHeap.Write((byte)0x20);
                                    methodBodyHeap.Write((int)n);
                                }
                            }
                            else
                            {
                                methodBodyHeap.Write((byte)0x21);
                                methodBodyHeap.Write(n);
                                tc = TypeCode.Empty; //Suppress conversion to long
                            }

                            break;
                    }

                    if (tc == TypeCode.Int64)
                        methodBodyHeap.Write((byte)0x6a);
                    return;

                case TypeCode.UInt64:
                    methodBodyHeap.Write((byte)0x21);
                    methodBodyHeap.Write(ic.ToUInt64(null));
                    return;

                case TypeCode.Single:
                    methodBodyHeap.Write((byte)0x22);
                    methodBodyHeap.Write(ic.ToSingle(null));
                    return;

                case TypeCode.Double:
                    methodBodyHeap.Write((byte)0x23);
                    methodBodyHeap.Write(ic.ToDouble(null));
                    return;

                case TypeCode.String:
                    methodBodyHeap.Write((byte)0x72);
                    methodBodyHeap.Write(GetUserStringIndex((string)literal.Value) | 0x70000000);
                    return;
            }

            Debug.Assert(false, "Unexpected literal type");
        }

        private void VisitMemberBinding(MemberBinding /*!*/ memberBinding)
        {
            if (memberBinding.TargetObject != null)
            {
                Visit(memberBinding.TargetObject);
                if (memberBinding.Volatile)
                {
                    methodBodyHeap.Write((byte)0xfe);
                    methodBodyHeap.Write((byte)0x13);
                }

                methodBodyHeap.Write((byte)0x7b);
            }
            else
            {
                IncrementStackHeight();
                if (memberBinding.Volatile)
                {
                    methodBodyHeap.Write((byte)0xfe);
                    methodBodyHeap.Write((byte)0x13);
                }

                methodBodyHeap.Write((byte)0x7e);
            }

            methodBodyHeap.Write(GetFieldToken((Field)memberBinding.BoundMember));
        }

        private void VisitMethod(Method /*!*/ method)
        {
            if (UseGenerics && method.Template != null && method.Template.IsGeneric) return;
            GetMethodIndex(method);
            VisitAttributeList(method.Attributes, method);
            VisitSecurityAttributeList(method.SecurityAttributes, method);
            for (int i = 0, n = method.Parameters == null ? 0 : method.Parameters.Count; i < n; i++)
            {
                var par = method.Parameters[i];
                if (par == null) continue;
                VisitAttributeList(par.Attributes, par);
                VisitReferencedType(par.Type);
            }

            if (method.ReturnType != null)
                VisitReferencedType(method.ReturnType);
            if (!method.IsAbstract && method.Body != null)
                if (method.Body.Statements != null && method.Body.Statements.Count > 0)
                    VisitMethodBody(method);
            var implementedInterfaceMethods = method.ImplementedInterfaceMethods;
            for (int i = 0, n = implementedInterfaceMethods == null ? 0 : implementedInterfaceMethods.Count; i < n; i++)
            {
                var im = implementedInterfaceMethods[i];
                if (im == null) continue;
                methodImplEntries.Add(method);
            }

            if ((method.Flags & MethodFlags.PInvokeImpl) != 0 && method.PInvokeImportName != null &&
                method.PInvokeModule != null)
            {
                implMapEntries.Add(method);
                GetStringIndex(method.PInvokeImportName);
                GetModuleRefIndex(method.PInvokeModule);
            }
        }

        private void VisitMethodBody(Method /*!*/ method)
        {
            //Visit body, emitting IL bytes and gathering information
            methodBodyHeap = new BinaryWriter(new MemoryStream());
            methodInfo = new MethodInfo();
#if !MinimalReader && !CodeContracts
      this.currentMethod = method;
#endif
            stackHeightMax = 0;
            stackHeightExitTotal = 0;
            uint methodDefToken = 0;
#if !ROTOR
            if (symWriter != null)
            {
                methodDefToken = (uint)GetMethodDefToken(method);
                methodInfo.debugLocals = new LocalList();
                methodInfo.signatureLengths = new Int32List();
                methodInfo.signatureOffsets = new Int32List();
                methodInfo.statementNodes = new NodeList();
                methodInfo.statementOffsets = new Int32List();
                symWriter.OpenMethod(methodDefToken);
                symWriter.OpenScope(0u);
#if !MinimalReader && !CodeContracts
        MethodScope scope = method.Scope;
        if (scope != null){
          UsedNamespaceList usedNamespaces = scope.UsedNamespaces;
          for (int i = 0, n = usedNamespaces == null ? 0 : usedNamespaces.Count; i < n; i++) {
            //^ assert usedNamespaces != null;
            UsedNamespace uns = usedNamespaces[i];
            if (uns == null || uns.Namespace == null) continue;
            this.symWriter.UsingNamespace(uns.Namespace.ToString());
          }
        }
#endif
            }
#endif
#if !FxCop
            var originalAddress = 0;
            if (method.LocalList != null)
            {
                for (int i = 0, n = method.LocalList.Count; i < n; i++)
                {
                    var loc = method.LocalList[i];
                    if (loc == null) continue;
                    GetLocalVarIndex(loc);
                }
#if !ROTOR
                if (symWriter != null)
                {
                    var currentAddress = methodBodyHeap.BaseStream.Position;
                    originalAddress = currentAddress;
                    symWriter.OpenScope((uint)currentAddress);
                }
#endif
            }
#endif
            var exceptionHandlersCount = method.ExceptionHandlers == null ? 0 : method.ExceptionHandlers.Count;
            if (exceptionHandlersCount > 0)
            {
                exceptionBlock = new TrivialHashtable();
                for (var i = 0; i < exceptionHandlersCount; i++)
                {
                    var eh = method.ExceptionHandlers[i];
                    if (eh == null || eh.HandlerStartBlock == null ||
                        (eh.HandlerType != NodeType.Catch && eh.HandlerType != NodeType.Filter)) continue;
                    if (eh.HandlerType == NodeType.Catch)
                        exceptionBlock[eh.HandlerStartBlock.UniqueKey] = eh;
                    else
                        exceptionBlock[eh.FilterExpression.UniqueKey] = eh;
                }
            }

            VisitBlock(method.Body);

#if !FxCop
            if (method.LocalList != null)
            {
#if !ROTOR
                if (symWriter != null) DefineLocalVariables(originalAddress, method.LocalList);
#endif
            }
#endif

            methodBodiesHeapIndex[method.UniqueKey] = methodBodiesHeap.BaseStream.Position;
            var maxStack =
                stackHeightExitTotal +
                stackHeightMax; //Wildly pessimistic estimate. Works dandy if BBlocks never leave anything on the stack.
            if (exceptionHandlersCount > 0 && maxStack == 0) maxStack = 1;
            var codeSize = methodBodyHeap.BaseStream.Position;
            var localVarSigTok = methodInfo.localVarSigTok;
            var fatHeader = codeSize >= 64 || exceptionHandlersCount > 0 || maxStack > 8 || localVarSigTok != 0;
            if (fatHeader)
            {
                //Emit fat header>	System.Compiler.dll!System.Compiler.Ir2md.VisitMethodBody(System.Compiler.Method method) Line 3699	C#

                byte header = 0x03;
                if (method.InitLocals) header |= 0x10;
                if (exceptionHandlersCount > 0) header |= 0x08;
                methodBodiesHeap.Write(header);
                methodBodiesHeap
                    .Write((byte)0x30); //top 4 bits represent length of fat header in dwords. Heaven only knows why.
                methodBodiesHeap.Write((short)maxStack);
                methodBodiesHeap.Write(codeSize);
                if (localVarSigTok != 0)
                {
                    if (methodInfo.localVarIndex.Count > 127)
                    {
                        //Need to make space for the two byte count
                        methodInfo.localVarSignature.Write((byte)0);
                        var buf = methodInfo.localVarSignature.BaseStream.Buffer;
                        var n = buf.Length;
                        for (var i = n - 2; i > 1; i--) buf[i + 1] = buf[i];
                    }

                    methodInfo.localVarSignature.BaseStream.Position = 0;
                    methodInfo.localVarSignature.Write((byte)7);
                    WriteCompressedInt(methodInfo.localVarSignature, methodInfo.localVarIndex.Count);
                    Debug.Assert(methodInfo.localVarIndex.Count <= 0xFFFE);
                }

                methodBodiesHeap.Write(localVarSigTok);
            }
            else
            {
                //Emit tiny header
                methodBodiesHeap.Write((byte)((codeSize << 2) | 2));
            }

            //Copy body to bodies heap
            methodBodyHeap.BaseStream.WriteTo(methodBodiesHeap.BaseStream);
            var pad = methodBodiesHeap.BaseStream.Position;
            while (pad % 4 != 0)
            {
                pad++;
                methodBodiesHeap.Write((byte)0);
            }

            if (fatHeader)
            {
                //Emit exception handler entries
                var tryOffsets = new int[exceptionHandlersCount];
                var tryLengths = new int[exceptionHandlersCount];
                var handlerOffsets = new int[exceptionHandlersCount];
                var handlerLengths = new int[exceptionHandlersCount];
                var fatFormat = false;
                for (var i = 0; i < exceptionHandlersCount; i++)
                {
                    var eh = method.ExceptionHandlers[i];
                    var tryOffset = tryOffsets[i] = (int)methodInfo.fixupIndex[eh.TryStartBlock.UniqueKey];
                    var tryLength = tryLengths[i] =
                        (int)methodInfo.fixupIndex[eh.BlockAfterTryEnd.UniqueKey] - tryOffset;
                    var handlerOffset = handlerOffsets[i] = (int)methodInfo.fixupIndex[eh.HandlerStartBlock.UniqueKey];
                    var handlerLength = handlerLengths[i] =
                        (int)methodInfo.fixupIndex[eh.BlockAfterHandlerEnd.UniqueKey] - handlerOffset;
                    if (tryOffset > 0xffff || tryLength > 0xff || handlerOffset > 0xffff || handlerLength > 0xff)
                        fatFormat = true;
                }

                if (exceptionHandlersCount * 12 + 4 > 0xff) fatFormat = true;
                if (fatFormat)
                {
                    var dataSize = exceptionHandlersCount * 24 + 4;
                    methodBodiesHeap.Write((byte)0x41);
                    methodBodiesHeap.Write((byte)(dataSize & 0xff));
                    methodBodiesHeap.Write((short)((dataSize >> 8) & 0xffff));
                }
                else
                {
                    var dataSize = exceptionHandlersCount * 12 + 4;
                    methodBodiesHeap.Write((byte)0x01);
                    methodBodiesHeap.Write((byte)dataSize);
                    methodBodiesHeap.Write((short)0);
                }

                for (var i = 0; i < exceptionHandlersCount; i++)
                {
                    var eh = method.ExceptionHandlers[i];
                    byte flags = 0;
                    switch (eh.HandlerType)
                    {
                        case NodeType.Filter:
                            flags = 0x0001;
                            break;
                        case NodeType.Finally:
                            flags = 0x0002;
                            break;
                        case NodeType.FaultHandler:
                            flags = 0x0004;
                            break;
                    }

                    if (fatFormat)
                    {
                        methodBodiesHeap.Write((int)flags);
                        methodBodiesHeap.Write(tryOffsets[i]);
                        methodBodiesHeap.Write(tryLengths[i]);
                        methodBodiesHeap.Write(handlerOffsets[i]);
                        methodBodiesHeap.Write(handlerLengths[i]);
                    }
                    else
                    {
                        methodBodiesHeap.Write((short)flags);
                        methodBodiesHeap.Write((ushort)tryOffsets[i]);
                        methodBodiesHeap.Write((byte)tryLengths[i]);
                        methodBodiesHeap.Write((ushort)handlerOffsets[i]);
                        methodBodiesHeap.Write((byte)handlerLengths[i]);
                    }

                    if (eh.FilterType != null)
                        methodBodiesHeap.Write(GetTypeToken(eh.FilterType));
                    else if (eh.FilterExpression != null)
                        methodBodiesHeap.Write((int)methodInfo.fixupIndex[eh.FilterExpression.UniqueKey]);
                    else
                        methodBodiesHeap.Write(0);
                }
            }
#if !ROTOR
            if (symWriter != null)
            {
                var mInfo = methodInfo;
                var statementNodes = mInfo.statementNodes;
                var statementOffsets = mInfo.statementOffsets;
                if (statementNodes.Count == 0)
                    // hack to make sure there is at least one sequence point
                    statementNodes.Add(new Statement(NodeType.Nop, new SourceContext(HiddenDocument.Document)));
                var n = statementNodes.Count;
                var j = 0;
                var k = 0;
                Document d = null;
                ISymUnmanagedDocumentWriter doc = null;
                for (var i = 0; i < n; i++)
                {
                    var e = statementNodes[i].SourceContext.Document;
                    if (e == null) continue;
                    // don't switch doc for hidden
                    if (e != HiddenDocument.Document && e != d)
                    {
                        d = e;
                        if (doc != null) DefineSequencePoints(statementNodes, statementOffsets, j, k, doc);
                        doc = GetDocumentWriter(d);
                        j = i;
                        k = 0;
                    }

                    k++;
                }

                if (doc == null)
                    // find any doc as we seem to have just a hidden program point
                    doc = GetArbitraryDocWriter();
                if (doc != null) DefineSequencePoints(statementNodes, statementOffsets, j, k, doc);
                symWriter.CloseScope((uint)methodBodyHeap.BaseStream.Position);
#if CodeContracts
                if (method.ExtraDebugInfo != null) method.ExtraDebugInfo.Write(methodDefToken, symWriter, this);
#endif
                symWriter.CloseMethod();
            }
#endif
            //this.methodBodyHeap = null;
            //this.methodInfo = null;
            //this.currentMethod = null;
        }

#if !ROTOR
        private void DefineLocalVariables(int startAddress, LocalList locals)
        {
            var mInfo = methodInfo;
            for (int i = 0, n = locals.Count; i < n; i++)
            {
                var loc = locals[i];
                var name = loc.Name.ToString();
                unsafe
                {
                    fixed (byte* p = mInfo.localVarSignature.BaseStream.Buffer)
                    {
                        var sp = (IntPtr)(p + mInfo.signatureOffsets[i]);
                        var c = (uint)mInfo.signatureLengths[i];
                        var attributes = loc.Attributes;
                        if (!loc.HasNoPDBInfo)
                            symWriter.DefineLocalVariable(name, attributes, c, sp, 1u, (uint)GetLocalVarIndex(loc), 0u,
                                0u, 0u);
                    }
                }
            }

            var posOfFirstInstructionOfNextBlock = methodBodyHeap.BaseStream.Position;
            if (posOfFirstInstructionOfNextBlock > startAddress)
                symWriter.CloseScope((uint)(posOfFirstInstructionOfNextBlock - 1));
            else
                symWriter.CloseScope((uint)startAddress);
        }
#endif
        private void DefineSequencePoint(Node node)
        {
#if !ROTOR
            if (symWriter != null && node != null && node.SourceContext.Document != null &&
                !node.SourceContext.Document.Hidden)
            {
                if (methodInfo.statementNodes.Count > 0)
                {
                    var previous = methodInfo.statementNodes[methodInfo.statementNodes.Count - 1];
                    if (previous != null &&
                        previous.SourceContext.Document == node.SourceContext.Document &&
                        previous.SourceContext.StartPos == node.SourceContext.StartPos &&
                        previous.SourceContext.EndPos == node.SourceContext.EndPos) return;
                }

                methodInfo.statementNodes.Add(node);
                methodInfo.statementOffsets.Add(methodBodyHeap.BaseStream.Position);
            }
#endif
        }
#if !ROTOR
        private void DefineSequencePoints(NodeList /*!*/ statementNodes, Int32List /*!*/ statementOffsets, int start,
                int count, ISymUnmanagedDocumentWriter doc)
            //^ requires this.symWriter != null;
        {
            if (count == 0) return;
            var offsets = new uint[count];
            var lines = new uint[count];
            var columns = new uint[count];
            var endLines = new uint[count];
            var endColumns = new uint[count];
            for (var i = 0; i < count; i++)
            {
                var n = statementNodes[i + start];
                offsets[i] = i + start == 0 ? 0 : (uint)statementOffsets[i + start];
                lines[i] = (uint)n.SourceContext.StartLine;
                columns[i] = (uint)n.SourceContext.StartColumn;
                endLines[i] = (uint)n.SourceContext.EndLine;
                endColumns[i] = (uint)n.SourceContext.EndColumn;
            }

            symWriter.DefineSequencePoints(doc, (uint)count, offsets, lines, columns, endLines, endColumns);
        }
#endif
        private void VisitModule(Module /*!*/ module)
        {
            //REVIEW: check that module has no explicit lists of assembly/module references?
            //this.ForceTemplateTypeMethodBodiesToGetSpecialized(module);
            VisitAttributeList(module.Attributes, module);
            if (assembly != null)
            {
                var m = new Module();
                m.Attributes = assembly.ModuleAttributes;
                VisitAttributeList(m.Attributes, m);
                VisitSecurityAttributeList(assembly.SecurityAttributes, assembly);
            }

            var allTypes = module.Types.Clone();
            for (var k = 0; k < allTypes.Count;)
            {
                var typeCount = module.Types.Count;
                for (int i = k, n = k, m = allTypes.Count; i < (n = allTypes.Count);)
                for (; i < n; i++)
                {
                    var t = allTypes[i];
                    if (t == null) continue;
                    if (UseGenerics && t.Template != null && t.Template.IsGeneric)
                    {
                        allTypes[i] = null;
                        continue;
                    }

                    GetTypeDefIndex(t);
                    if (i >= m) nestedClassEntries.Add(t);
                    var members = t.Members;
                    if (members != null)
                        for (int j = 0, numMembers = members.Count; j < numMembers; j++)
                        {
                            var nt = members[j] as TypeNode;
                            if (nt != null) allTypes.Add(nt);
                        }
                }

                for (int i = k, n = allTypes.Count; i < n; i++)
                {
                    var t = allTypes[i];
                    if (t == null) continue;
                    if (UseGenerics && t.Template != null && t.Template.IsGeneric)
                    {
                        allTypes[i] = null;
                        continue;
                    }

                    var mems = t.Members;
                    if (t is EnumNode) //Work around JIT bug in Beta2
                        for (int jj = 0, mm = mems.Count; jj < mm; jj++)
                        {
                            var f = mems[jj] as Field;
                            if (f == null || f.IsStatic) continue;
                            mems[jj] = mems[0];
                            mems[0] = f;
                            break;
                        }

                    for (int j = 0, m = mems.Count; j < m; j++)
                    {
                        var mem = mems[j];
                        if (mem == null) continue;
                        switch (mem.NodeType)
                        {
                            case NodeType.Field:
                                GetFieldIndex((Field)mem);
                                break;
                            case NodeType.Method:
                            case NodeType.InstanceInitializer:
                            case NodeType.StaticInitializer:
                                var meth = (Method)mem;
                                if (UseGenerics && meth.Template != null && meth.Template.IsGeneric)
                                    GetMethodSpecIndex(meth);
                                else
                                    GetMethodIndex(meth);
                                break;
                        }
                    }
                }

                for (int i = k, n = allTypes.Count; i < n; i++, k++)
                {
                    var t = allTypes[i];
                    if (t == null) continue;
                    Visit(t);
                }

                for (int i = typeCount, n = module.Types.Count; i < n; i++)
                {
                    var t = module.Types[i];
                    if (t == null) continue;
                    Debug.Assert(t.IsNotFullySpecialized);
                    //allTypes.Add(t);
                }
            }
        }
#if !CodeContracts
    sealed class MethodSpecializer : StandardVisitor{
      private Module/*!*/ module;

      internal MethodSpecializer(Module/*!*/ module) {
        this.module = module;
        //^ base();
      }

      public override Method VisitMethod(Method method) {
        if (method == null) return null;
        if (method.Template == null || method.Template.IsGeneric) return method;
        TypeNodeList templateParameters = null;
        TypeNodeList templateArguments = null;
        if (method.TemplateArguments != null && method.TemplateArguments.Count > 0){
          templateParameters = method.Template.TemplateParameters;
          templateArguments = method.TemplateArguments;
        }else{
          TypeNode tdt = method.Template.DeclaringType;
          TypeNode dt = method.DeclaringType;
          templateParameters = tdt.ConsolidatedTemplateParameters;
          templateArguments = dt.ConsolidatedTemplateArguments;
          if (templateArguments == null) templateArguments = templateParameters;
        }
        if (templateParameters == null || templateParameters.Count == 0) return method;
        TypeNode declaringTemplate = method.DeclaringType == null ? null : method.DeclaringType.Template;
        bool savedNewTemplateInstanceIsRecursive = false;
        if (declaringTemplate != null){
          savedNewTemplateInstanceIsRecursive = declaringTemplate.NewTemplateInstanceIsRecursive;
          declaringTemplate.NewTemplateInstanceIsRecursive = method.DeclaringType.IsNotFullySpecialized;
        }
        Duplicator duplicator = new Duplicator(this.module, method.DeclaringType);
#if !MinimalReader && !CodeContracts
        TypeNode closureClone = null;
        if (method.Template.Scope != null && method.Template.Scope.CapturedForClosure){
          duplicator.TypesToBeDuplicated[method.Template.Scope.ClosureClass.UniqueKey] =
 method.Template.Scope.ClosureClass;
          duplicator.RecordOriginalAsTemplate = true;
          closureClone = duplicator.VisitTypeNode(method.Template.Scope.ClosureClass);
        }
#endif
        int n = method.Parameters == null ? 0 : method.Parameters.Count;
        int m = method.Template.Parameters == null ? 0 : method.Template.Parameters.Count;
        if (n != m){Debug.Assert(false); if (n > m) n = m;}
        for (int i = 0; i < n; i++){
          Parameter par = method.Parameters[i];
          Parameter tpar = method.Template.Parameters[i];
          if (par == null || tpar == null) continue;
          duplicator.DuplicateFor[tpar.UniqueKey] = par;
        }
        n = method.TemplateParameters == null ? 0 : method.TemplateParameters.Count;
        m = method.Template.TemplateParameters == null ? 0 : method.Template.TemplateParameters.Count;
        if (n != m && n > 0){Debug.Assert(false); if (n > m) n = m;}
        for (int i = 0; i < n; i++){
          TypeNode tpar = method.TemplateParameters[i];
          TypeNode ttpar = method.Template.TemplateParameters[i];
          if (tpar == null || ttpar == null) continue;
          duplicator.DuplicateFor[ttpar.UniqueKey] = tpar;
        }
        Method dup = duplicator.VisitMethod(method.Template);
        //^ assume dup != null;
        Specializer specializer = new Specializer(this.module, templateParameters, templateArguments);
        specializer.VisitMethod(dup);
#if !MinimalReader && !CodeContracts
        if (closureClone != null){
          specializer.VisitTypeNode(closureClone);
          if (method.TemplateArguments != null && method.TemplateArguments.Count > 0)
            closureClone.Name = Identifier.For(closureClone.Name.ToString()+closureClone.UniqueKey);
          MemberList dtMembers = method.DeclaringType.Members;
          for (int i = 0, nmems = dtMembers == null ? 0 : dtMembers.Count; i < nmems; i++){
            ClosureClass closureRef = dtMembers[i] as ClosureClass;
            if (closureRef != null && closureRef.Name.UniqueIdKey == closureClone.Name.UniqueIdKey){
              //This happens when the declaring type was instantiated after Normalizer has already injected a closure into the template
              dtMembers[i] = closureClone;
              closureClone = null;
              break;
            }
          }
          if (closureClone != null)
            method.DeclaringType.Members.Add(closureClone);
        }
#endif
        if (method.Template.DeclaringType.DeclaringModule != this.module){
          //Dealing with imported IR that misses important type information if it contains explicit stack operations (push, pop, dup) 
          //Call a helper visitor to remove these stack operations and in the process supply the missing type information.
          Unstacker unstacker = new Unstacker();
          unstacker.Visit(dup);
        }
        MethodBodySpecializer mbSpecializer =
 this.module.GetMethodBodySpecializer(templateParameters, templateArguments);
        mbSpecializer.methodBeingSpecialized = method;
        mbSpecializer.dummyMethod = dup;
        mbSpecializer.VisitMethod(dup);
        method.Body = dup.Body;
        // HACK to try to fix parameter declaring method back to the way it was before:
        method.Parameters = method.Parameters;
        method.ExceptionHandlers = dup.ExceptionHandlers;
        if (declaringTemplate != null)
          declaringTemplate.NewTemplateInstanceIsRecursive = savedNewTemplateInstanceIsRecursive;
        return method;
      }
    }
    void ForceTemplateTypeMethodBodiesToGetSpecialized(Module/*!*/ module) {
      MethodSpecializer visitor = new MethodSpecializer(module);
      if (module == null) return;
      TypeNodeList types = module.Types;
      if (types == null) return;
      for (int i = 0; i < types.Count; i++)
        this.ForceTemplateTypeMethodBodiesToGetSpecialized(types[i], visitor);
    }
    void ForceTemplateTypeMethodBodiesToGetSpecialized(TypeNode/*!*/ type, MethodSpecializer/*!*/ visitor) {
      if (type == null) return;
      if (type.IsNotFullySpecialized || type.IsGeneric) return;
      bool savedNewTemplateInstanceIsRecursive = type.NewTemplateInstanceIsRecursive;
      type.NewTemplateInstanceIsRecursive = type.IsNotFullySpecialized;
      MemberList members = type.Members;
      if (members == null) return;
      for (int j = 0; j < members.Count; j++){
        Member mem = members[j];
        if (mem == null) continue;
        TypeNode t = mem as TypeNode;
        if (t != null)
          this.ForceTemplateTypeMethodBodiesToGetSpecialized(t, visitor);
        else
          visitor.VisitMethod(mem as Method);
      }
      type.NewTemplateInstanceIsRecursive = savedNewTemplateInstanceIsRecursive;
    }
#endif

        private void VisitParameter(Parameter /*!*/ parameter)
        {
            IncrementStackHeight();
#if !MinimalReader
            var pb = parameter as ParameterBinding;
            if (pb != null) parameter = pb.BoundParameter;
#endif
            var pi = parameter.ArgumentListIndex;
            switch (pi)
            {
                case 0:
                    methodBodyHeap.Write((byte)0x02);
                    return;
                case 1:
                    methodBodyHeap.Write((byte)0x03);
                    return;
                case 2:
                    methodBodyHeap.Write((byte)0x04);
                    return;
                case 3:
                    methodBodyHeap.Write((byte)0x05);
                    return;
                default:
                    if (pi < 256)
                    {
                        methodBodyHeap.Write((byte)0x0e);
                        methodBodyHeap.Write((byte)pi);
                    }
                    else
                    {
                        methodBodyHeap.Write((byte)0xfe);
                        methodBodyHeap.Write((byte)0x09);
                        methodBodyHeap.Write((ushort)pi);
                    }

                    return;
            }
        }

        private void VisitProperty(Property /*!*/ property)
        {
            var pindex = propertyIndex[property.UniqueKey];
            if (pindex != null) return;
            var index = propertyEntries.Count + 1;
            propertyEntries.Add(property);
            propertyIndex[property.UniqueKey] = index;
            var pmindex = propertyMapIndex[property.DeclaringType.UniqueKey];
            if (pmindex == null)
            {
                propertyMapEntries.Add(property);
                propertyMapIndex[property.DeclaringType.UniqueKey] = propertyMapEntries.Count;
            }

            if (property.Getter != null) methodSemanticsEntries.Add(property);
            if (property.Setter != null) methodSemanticsEntries.Add(property);
            if (property.OtherMethods != null)
                for (int i = 0, n = property.OtherMethods.Count; i < n; i++)
                    methodSemanticsEntries.Add(property);
            VisitAttributeList(property.Attributes, property);
        }

        private void VisitReferencedType(TypeNode type)
        {
            if (type == null) return;
            if (type.IsGeneric && type.Template == null)
            {
                var templParams = type.ConsolidatedTemplateParameters;
                for (int i = 0, n = templParams == null ? 0 : templParams.Count; i < n; i++)
                    typeParameterNumber[templParams[i].UniqueKey] = i + 1;
            }

            switch (type.typeCode)
            {
                case ElementType.Pointer:
                    VisitReferencedType(((Pointer)type).ElementType);
                    return;
                case ElementType.Reference:
                    VisitReferencedType(((Reference)type).ElementType);
                    return;
                case ElementType.Array:
                case ElementType.SzArray:
                    VisitReferencedType(((ArrayType)type).ElementType);
                    return;
                case ElementType.OptionalModifier:
                case ElementType.RequiredModifier:
                    var tm = (TypeModifier)type;
                    VisitReferencedType(tm.Modifier);
                    VisitReferencedType(tm.ModifiedType);
                    return;
                case ElementType.FunctionPointer:
                    var fp = (FunctionPointer)type;
                    VisitReferencedType(fp.ReturnType);
                    for (int i = 0, n = fp.ParameterTypes == null ? 0 : fp.ParameterTypes.Count; i < n; i++)
                        VisitReferencedType(fp.ParameterTypes[i]);
                    return;
                case ElementType.ValueType:
                case ElementType.Class:
                    break;
                default:
                    return;
            }

            if (IsStructural(type) && (!type.IsGeneric || (type.Template != null &&
                                                           type.ConsolidatedTemplateArguments != null &&
                                                           type.ConsolidatedTemplateArguments.Count > 0)))
            {
                GetTypeSpecIndex(type);
            }
            else if (type.DeclaringModule == module)
            {
                GetTypeDefIndex(type);
            }
            else if (type.DeclaringModule != null)
            {
                GetTypeRefIndex(type);
            }
            else if (type.typeCode == ElementType.ValueType || type.typeCode == ElementType.Class)
            {
                //Get here for type parameters
                if (UseGenerics && typeParameterNumber[type.UniqueKey] != null) return;
                type.DeclaringModule = module;
                GetTypeDefIndex(type);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private void VisitReturn(Return /*!*/ Return)
        {
            DefineSequencePoint(Return);
            if (Return.Expression != null)
            {
                Visit(Return.Expression);
                stackHeight--;
            }

            methodBodyHeap.Write((byte)0x2a);
        }

        private void VisitSecurityAttributeList(SecurityAttributeList attrs, Node /*!*/ node)
        {
            if (attrs == null) return;
            var n = attrs.Count;
            if (n == 0) return;
            var m = n;
            for (var j = 0; j < n; j++)
            {
                var a = attrs[j];
                if (a == null) m--;
            }

            if (m == 0) return;
            n = m;
            var codedIndex = GetSecurityAttributeParentCodedIndex(node);
            securityAttributeCount += n;
            m = nodesWithSecurityAttributes.Count;
            nodesWithSecurityAttributes.Add(node);
            var i = 0; //after the for loop i will be position where the new node should be in sorted list
            var nodes = nodesWithSecurityAttributes;
            for (i = m; i > 0; i--)
            {
                var other = nodes[i - 1];
                var oci = GetSecurityAttributeParentCodedIndex(other);
                if (oci < codedIndex) break;
            }

            if (i == m) return; //node is already where it should be
            for (var j = m; j > i; j--) nodes[j] = nodes[j - 1]; //Make space at postion i
            nodes[i] = node;
        }

        private void VisitStatement(Statement /*!*/ statement)
        {
            DefineSequencePoint(statement);
            switch (statement.NodeType)
            {
                case NodeType.Nop:
                    methodBodyHeap.Write((byte)0x00);
                    break;
                case NodeType.DebugBreak:
                    methodBodyHeap.Write((byte)0x01);
                    break;
                case NodeType.EndFinally:
                    methodBodyHeap.Write((byte)0xdc);
                    break;
            }
        }

        private void VisitStruct(Struct /*!*/ Struct)
        {
            if (UseGenerics && Struct.Template != null && Struct.Template.IsGeneric) return;
            VisitAttributeList(Struct.Attributes, Struct);
            VisitSecurityAttributeList(Struct.SecurityAttributes, Struct);
            VisitReferencedType(CoreSystemTypes.ValueType);
            var interfaces = Struct.Interfaces;
            for (int i = 0, n = interfaces == null ? 0 : interfaces.Count; i < n; i++)
            {
                GetTypeDefOrRefOrSpecEncoded(interfaces[i]);
                if (interfaces[i] != null) interfaceEntries.Add(Struct);
            }

            for (int i = 0, n = Struct.Members.Count; i < n; i++)
            {
                var m = Struct.Members[i];
                if (m is TypeNode) continue;
                Visit(m);
            }

            if ((Struct.Flags & (TypeFlags.ExplicitLayout | TypeFlags.SequentialLayout)) != 0 &&
                (Struct.PackingSize != 0 || Struct.ClassSize != 0))
                classLayoutEntries.Add(Struct);
        }

        private void VisitSwitchInstruction(SwitchInstruction /*!*/ switchInstruction)
        {
            Visit(switchInstruction.Expression);
            stackHeight--;
            var targets = switchInstruction.Targets;
            var n = targets != null ? targets.Count : 0;
            var addressOfNextInstruction = methodBodyHeap.BaseStream.Position + 5 + 4 * n;
            methodBodyHeap.Write((byte)0x45);
            methodBodyHeap.Write((uint)n);
            for (var i = 0; i < n; i++)
                methodBodyHeap.Write(GetOffset(targets[i], addressOfNextInstruction));
        }

        private void VisitTernaryExpression(TernaryExpression /*!*/ expression)
        {
            Visit(expression.Operand1);
            Visit(expression.Operand2);
            Visit(expression.Operand3);
            methodBodyHeap.Write((byte)0xfe);
            if (expression.NodeType == NodeType.Cpblk)
                methodBodyHeap.Write((byte)0x17);
            else
                methodBodyHeap.Write((byte)0x18);
            stackHeight -= 3;
        }

        private void VisitThis(This /*!*/ This)
        {
            IncrementStackHeight();
            methodBodyHeap.Write((byte)0x02);
        }

        private void VisitThrow(Throw /*!*/ Throw)
        {
            DefineSequencePoint(Throw);
            if (Throw.NodeType == NodeType.Rethrow)
            {
                methodBodyHeap.Write((byte)0xfe);
                methodBodyHeap.Write((byte)0x1a);
            }
            else
            {
                Visit(Throw.Expression);
                methodBodyHeap.Write((byte)0x7a);
            }

            stackHeight--;
        }

        private void VisitUnaryExpression(UnaryExpression /*!*/ unaryExpression)
        {
            switch (unaryExpression.NodeType)
            {
                case NodeType.Ldtoken:
                    methodBodyHeap.Write((byte)0xd0);
                    var lit = unaryExpression.Operand as Literal;
                    if (lit != null)
                    {
                        if (lit.Value == null) return;
                        methodBodyHeap.Write(GetTypeDefToken((TypeNode)lit.Value));
                    }
                    else
                    {
                        if (unaryExpression.Operand == null) return;
                        var m = ((MemberBinding)unaryExpression.Operand).BoundMember;
                        if (m == null) return;
                        var meth = m as Method;
                        if (meth != null)
                            methodBodyHeap.Write(GetMethodToken(meth));
                        else
                            methodBodyHeap.Write(GetFieldToken((Field)m));
                    }

                    IncrementStackHeight();
                    return;

                case NodeType.Ldftn:
                    methodBodyHeap.Write((byte)0xfe);
                    methodBodyHeap.Write((byte)0x06);
                    methodBodyHeap.Write(GetMethodToken((Method)((MemberBinding)unaryExpression.Operand).BoundMember));
                    IncrementStackHeight();
                    return;

                case NodeType.Sizeof:
                    methodBodyHeap.Write((byte)0xfe);
                    methodBodyHeap.Write((byte)0x1c);
                    methodBodyHeap.Write(GetTypeToken((TypeNode)((Literal)unaryExpression.Operand).Value));
                    IncrementStackHeight();
                    return;

                case NodeType.SkipCheck:
                    methodBodyHeap.Write((byte)0xfe);
                    methodBodyHeap.Write((byte)0x19);
                    switch (unaryExpression.Operand.NodeType)
                    {
                        case NodeType.Castclass:
                        case NodeType.Unbox:
                            methodBodyHeap.Write((byte)0x01);
                            break;
                        default:
                            Debug.Assert(false);
                            methodBodyHeap.Write((byte)0x00);
                            break;
                    }

                    VisitExpression(unaryExpression.Operand);
                    return;
            }

            Visit(unaryExpression.Operand);
            byte opCode = 0;
            switch (unaryExpression.NodeType)
            {
                case NodeType.Neg:
                    opCode = 0x65;
                    break;
                case NodeType.Not:
                    opCode = 0x66;
                    break;
                case NodeType.Conv_I1:
                    opCode = 0x67;
                    break;
                case NodeType.Conv_I2:
                    opCode = 0x68;
                    break;
                case NodeType.Conv_I4:
                    opCode = 0x69;
                    break;
                case NodeType.Conv_I8:
                    opCode = 0x6a;
                    break;
                case NodeType.Conv_R4:
                    opCode = 0x6b;
                    break;
                case NodeType.Conv_R8:
                    opCode = 0x6c;
                    break;
                case NodeType.Conv_U4:
                    opCode = 0x6d;
                    break;
                case NodeType.Conv_U8:
                    opCode = 0x6e;
                    break;
                case NodeType.Conv_R_Un:
                    opCode = 0x76;
                    break;
                case NodeType.Conv_Ovf_I1_Un:
                    opCode = 0x82;
                    break;
                case NodeType.Conv_Ovf_I2_Un:
                    opCode = 0x83;
                    break;
                case NodeType.Conv_Ovf_I4_Un:
                    opCode = 0x84;
                    break;
                case NodeType.Conv_Ovf_I8_Un:
                    opCode = 0x85;
                    break;
                case NodeType.Conv_Ovf_U1_Un:
                    opCode = 0x86;
                    break;
                case NodeType.Conv_Ovf_U2_Un:
                    opCode = 0x87;
                    break;
                case NodeType.Conv_Ovf_U4_Un:
                    opCode = 0x88;
                    break;
                case NodeType.Conv_Ovf_U8_Un:
                    opCode = 0x89;
                    break;
                case NodeType.Conv_Ovf_I_Un:
                    opCode = 0x8a;
                    break;
                case NodeType.Conv_Ovf_U_Un:
                    opCode = 0x8b;
                    break;
                case NodeType.Ldlen:
                    opCode = 0x8e;
                    break;
                case NodeType.Conv_Ovf_I1:
                    opCode = 0xb3;
                    break;
                case NodeType.Conv_Ovf_U1:
                    opCode = 0xb4;
                    break;
                case NodeType.Conv_Ovf_I2:
                    opCode = 0xb5;
                    break;
                case NodeType.Conv_Ovf_U2:
                    opCode = 0xb6;
                    break;
                case NodeType.Conv_Ovf_I4:
                    opCode = 0xb7;
                    break;
                case NodeType.Conv_Ovf_U4:
                    opCode = 0xb8;
                    break;
                case NodeType.Conv_Ovf_I8:
                    opCode = 0xb9;
                    break;
                case NodeType.Conv_Ovf_U8:
                    opCode = 0xba;
                    break;
                case NodeType.Ckfinite:
                    opCode = 0xc3;
                    break;
                case NodeType.Conv_U2:
                    opCode = 0xd1;
                    break;
                case NodeType.Conv_U1:
                    opCode = 0xd2;
                    break;
                case NodeType.Conv_I:
                    opCode = 0xd3;
                    break;
                case NodeType.Conv_Ovf_I:
                    opCode = 0xd4;
                    break;
                case NodeType.Conv_Ovf_U:
                    opCode = 0xd5;
                    break;
                case NodeType.Conv_U:
                    opCode = 0xe0;
                    break;
                case NodeType.Localloc:
                    opCode = 0x0f;
                    methodBodyHeap.Write((byte)0xfe);
                    break;
                case NodeType.Refanytype:
                    opCode = 0x1d;
                    methodBodyHeap.Write((byte)0xfe);
                    break;
            }

            methodBodyHeap.Write(opCode);
        }

        private static void WriteArrayShape(BinaryWriter /*!*/ target, ArrayType /*!*/ arrayType)
        {
            WriteCompressedInt(target, arrayType.Rank);
            var n = arrayType.Sizes == null ? 0 : arrayType.Sizes.Length;
            WriteCompressedInt(target, n);
            for (var i = 0; i < n; i++) //^ assert arrayType.Sizes != null;
                WriteCompressedInt(target, arrayType.Sizes[i]);
            n = arrayType.LowerBounds == null ? 0 : arrayType.LowerBounds.Length;
            WriteCompressedInt(target, n);
            for (var i = 0; i < n; i++) //^ assert arrayType.LowerBounds != null;
                WriteCompressedInt(target, arrayType.LowerBounds[i]);
        }

        internal static void WriteCompressedInt(BinaryWriter /*!*/ target, int val)
        {
            if (val <= 0x7f)
            {
                target.Write((byte)val);
            }
            else if (val < 0x3fff)
            {
                target.Write((byte)((val >> 8) | 0x80));
                target.Write((byte)(val & 0xff));
            }
            else if (val < 0x1fffffff)
            {
                target.Write((byte)((val >> 24) | 0xc0));
                target.Write((byte)((val & 0xff0000) >> 16));
                target.Write((byte)((val & 0xff00) >> 8));
                target.Write((byte)(val & 0xff));
            }
            else
            {
                Debug.Assert(false, "index too large for compression");
            }
        }

        private TypeNode /*!*/ WriteCustomModifiers(BinaryWriter /*!*/ target, TypeNode /*!*/ type)
        {
            switch (type.NodeType)
            {
                case NodeType.RequiredModifier:
                case NodeType.OptionalModifier:
                    var tm = (TypeModifier)type;
                    target.Write((byte)tm.typeCode);
                    WriteTypeDefOrRefEncoded(target, tm.Modifier);
                    return WriteCustomModifiers(target, tm.ModifiedType);
            }

            return type;
        }

        private void WriteCustomAttributeLiteral(BinaryWriter /*!*/ writer, Literal /*!*/ literal, bool needsTag)
        {
            if (literal.Type == null) return;
            var typeCode = literal.Type.typeCode;
            if (needsTag)
            {
                if (typeCode == ElementType.ValueType)
                {
                    //Boxed enum
                    writer.Write((byte)0x55);
                    WriteSerializedTypeName(writer, literal.Type);
                }
                else if (typeCode == ElementType.Class)
                {
                    //a Type value
                    writer.Write((byte)0x50);
                }
                else if (typeCode != ElementType.Object) //a primitive
                {
                    writer.Write((byte)typeCode);
                }
            }

            var value = literal.Value;
            //if (value == null) return; //TODO: nope, find some other way
            switch (typeCode)
            {
                case ElementType.Boolean:
                    writer.Write((bool)value);
                    return;
                case ElementType.Char:
                    writer.Write((ushort)(char)value);
                    return;
                case ElementType.Double:
                    writer.Write((double)value);
                    return;
                case ElementType.Single:
                    writer.Write((float)value);
                    return;
                case ElementType.Int16:
                    writer.Write((short)value);
                    return;
                case ElementType.Int32:
                    writer.Write((int)value);
                    return;
                case ElementType.Int64:
                    writer.Write((long)value);
                    return;
                case ElementType.Int8:
                    writer.Write((sbyte)value);
                    return;
                case ElementType.UInt16:
                    writer.Write((ushort)value);
                    return;
                case ElementType.UInt32:
                    writer.Write((uint)value);
                    return;
                case ElementType.UInt64:
                    writer.Write((ulong)value);
                    return;
                case ElementType.UInt8:
                    writer.Write((byte)value);
                    return;
                case ElementType.String:
                    writer.Write((string)value, false);
                    return;
                case ElementType.ValueType:
                    WriteCustomAttributeLiteral(writer, new Literal(value, ((EnumNode)literal.Type).UnderlyingType),
                        false);
                    return;
                case ElementType.Class:
                    if (value == null && literal.Type == CoreSystemTypes.Type)
                        writer.Write((byte)0xff);
                    else
                        WriteSerializedTypeName(writer, (TypeNode)value);
                    return;
                case ElementType.SzArray:
                    var elemType = ((ArrayType)literal.Type).ElementType;
                    if (needsTag)
                        writer.Write((byte)elemType.typeCode);
                    var array = (Array)value;
                    var numElems = array == null ? -1 : array.Length;
                    writer.Write(numElems);
                    var elemNeedsTag = elemType == CoreSystemTypes.Object;
                    for (var i = 0; i < numElems; i++)
                    {
                        var elemValue = array.GetValue(i);
                        var elemLit = elemValue as Literal;
                        if (elemLit == null) elemLit = new Literal(elemValue, elemType);
                        WriteCustomAttributeLiteral(writer, elemLit, elemNeedsTag);
                    }

                    return;
                case ElementType.Object:
                    var lit = (Literal)literal.Clone();
                    TypeNode t = null;
                    switch (Convert.GetTypeCode(lit.Value))
                    {
                        case TypeCode.Boolean:
                            t = CoreSystemTypes.Boolean;
                            break;
                        case TypeCode.Byte:
                            t = CoreSystemTypes.UInt8;
                            break;
                        case TypeCode.Char:
                            t = CoreSystemTypes.Char;
                            break;
                        case TypeCode.Double:
                            t = CoreSystemTypes.Double;
                            break;
                        case TypeCode.Int16:
                            t = CoreSystemTypes.Int16;
                            break;
                        case TypeCode.Int32:
                            t = CoreSystemTypes.Int32;
                            break;
                        case TypeCode.Int64:
                            t = CoreSystemTypes.Int64;
                            break;
                        case TypeCode.SByte:
                            t = CoreSystemTypes.Int8;
                            break;
                        case TypeCode.Single:
                            t = CoreSystemTypes.Single;
                            break;
                        case TypeCode.String:
                            t = CoreSystemTypes.String;
                            break;
                        case TypeCode.UInt16:
                            t = CoreSystemTypes.UInt16;
                            break;
                        case TypeCode.UInt32:
                            t = CoreSystemTypes.UInt32;
                            break;
                        case TypeCode.UInt64:
                            t = CoreSystemTypes.UInt64;
                            break;
                        case TypeCode.Empty:
                        case TypeCode.Object:
                            var arr = lit.Value as Array;
                            if (arr != null)
                            {
#if !NoReflection
                                t = TypeNode.GetTypeNode(arr.GetType());
#else
                System.Type reflType = arr.GetType();
                System.Type reflElemType = reflType.GetElementType();
                AssemblyNode assem = AssemblyNode.GetAssembly(reflType.Assembly.Location);
                TypeNode cciElemType =
 assem.GetType(Identifier.For(reflElemType.Namespace), Identifier.For(reflElemType.Name));
                t = cciElemType.GetArrayType(reflType.GetArrayRank());
#endif
                            }
                            else
                            {
                                t = CoreSystemTypes.Type;
                            }

                            break;
                    }

                    if (t == null) break;
                    lit.Type = t;
                    WriteCustomAttributeLiteral(writer, lit, true);
                    return;
            }

            Debug.Assert(false, "Unexpected type in custom attribute");
        }

        private static bool AttributesContains(AttributeList al, TypeNode /*!*/ a)
        {
            if (al == null) return false;
            for (int i = 0, n = al.Count; i < n; i++)
                if (al[i] != null && al[i].Type == a)
                    return true;
            return false;
        }

        private void WriteMethodSignature(BinaryWriter /*!*/ target, Method /*!*/ method)
        {
            if (UseGenerics)
            {
                if (method.Template != null && method.Template.IsGeneric)
                {
                    //Signature is being used in MethodDef table
                    var types = method.TemplateArguments;
                    var m = types == null ? 0 : types.Count;
                    target.Write((byte)(method.CallingConvention | CallingConventionFlags.Generic));
                    WriteCompressedInt(target, m);
                }
                else if (method.DeclaringType.Template != null && method.DeclaringType.Template.IsGeneric)
                {
                    var unspecializedMethod = GetUnspecializedMethod(method);
                    WriteMethodSignature(target, unspecializedMethod);
                    return;
                }
                else if (method.IsGeneric)
                {
                    var types = method.TemplateParameters;
                    var m = types == null ? 0 : types.Count;
                    target.Write((byte)(method.CallingConvention | CallingConventionFlags.Generic));
                    WriteCompressedInt(target, m);
                }
                else
                {
                    target.Write((byte)method.CallingConvention);
                }
            }
            else
            {
                target.Write((byte)method.CallingConvention);
            }

            var pars = method.Parameters;
            var n = pars == null ? 0 : pars.Count;
            WriteCompressedInt(target, n);

            var returnType = method.ReturnType;
#if ExtendedRuntime
      if (method.HasOutOfBandContract || AttributesContains(method.ReturnAttributes, SystemTypes.NotNullAttribute)) {
        returnType =
 TypeNode.DeepStripModifiers(returnType, (method.Template != null) ? method.Template.ReturnType : null, SystemTypes.NonNullType, SystemTypes.NullableType);
    //    returnType = TypeNode.DeepStripModifier(returnType, SystemTypes.NullableType, (method.Template != null) ? returnType.GetTemplateInstance(returnType, returnType.TemplateArguments) : null);
      }
#endif
            if (returnType == null) returnType = SystemTypes.Object;
            WriteTypeSignature(target, returnType, true);
            for (var i = 0; i < n; i++)
            {
                var p = pars[i];
                if (p == null) continue;
                var parameterType = p.Type;
#if ExtendedRuntime
        if (method.HasOutOfBandContract || AttributesContains(p.Attributes, SystemTypes.NotNullAttribute)) {
          parameterType =
 TypeNode.DeepStripModifiers(parameterType, (method.Template != null) ? method.Template.Parameters[i].Type : null, SystemTypes.NonNullType, SystemTypes.NullableType);
          //parameterType = TypeNode.DeepStripModifier(parameterType, SystemTypes.NullableType, (method.Template != null) ? parameterType.GetTemplateInstance(parameterType, parameterType.TemplateArguments) : null);
        }
#endif
                if (parameterType == null) parameterType = SystemTypes.Object;
                WriteTypeSignature(target, parameterType);
            }
        }

        private void WriteMethodSpecSignature(BinaryWriter /*!*/ target, Method /*!*/ method)
            //^ requires this.UseGenerics && method.Template != null && method.Template.IsGeneric;
        {
            Debug.Assert(UseGenerics && method.Template != null && method.Template.IsGeneric);
            target.Write((byte)0x0a);
            var types = method.TemplateArguments;
            var m = types == null ? 0 : types.Count;
            WriteCompressedInt(target, m);
            for (var i = 0; i < m; i++) //^ assert types != null;
                WriteTypeSignature(target, types[i]);
        }

        private void WriteMethodSignature(BinaryWriter /*!*/ target, FunctionPointer /*!*/ fp)
        {
            target.Write((byte)fp.CallingConvention);
            var parTypes = fp.ParameterTypes;
            var n = parTypes == null ? 0 : parTypes.Count;
            WriteCompressedInt(target, n);
            if (fp.ReturnType != null)
                WriteTypeSignature(target, fp.ReturnType);
            var m = fp.VarArgStart;
            for (var i = 0; i < n; i++)
            {
                //^ assert parTypes != null;
                if (i == m) target.Write((byte)0x41); //Sentinel
                WriteTypeSignature(target, parTypes[i]);
            }
        }

        private void WritePropertySignature(BinaryWriter /*!*/ target, Property /*!*/ prop)
        {
            byte propHeader = 0x8;
            if (!prop.IsStatic)
                propHeader |= 0x20; //bizarre redundant way to indicate that property accessors are instance methods
            target.Write(propHeader);
            var pars = prop.Parameters;
            var n = pars == null ? 0 : pars.Count;
            WriteCompressedInt(target, n);
            if (prop.Type != null) WriteTypeSignature(target, prop.Type);
            for (var i = 0; i < n; i++)
            {
                //^ assert pars != null;
                var par = pars[i];
                if (par == null || par.Type == null) continue;
                WriteTypeSignature(target, par.Type);
            }
        }

        private void WriteSerializedTypeName(BinaryWriter target, TypeNode type)
        {
            if (target == null || type == null) return;
            target.Write(GetSerializedTypeName(type), false);
        }

        private string GetSerializedTypeName(TypeNode /*!*/ type)
        {
            var isAssemblyQualified = true;
            return GetSerializedTypeName(type, ref isAssemblyQualified);
        }

        private string GetSerializedTypeName(TypeNode /*!*/ type, ref bool isAssemblyQualified)
        {
            if (type == null) return null;
            VisitReferencedType(type);
            var sb = new StringBuilder();
            var tMod = type as TypeModifier;
            if (tMod != null)
            {
                sb.Append(GetTypeDefOrRefOrSpecEncoded(type));
                sb.Append('!');
                return sb.ToString();
            }

            var arrType = type as ArrayType;
            if (arrType != null)
            {
                type = arrType.ElementType;
                var isAssemQual = false;
                AppendSerializedTypeName(sb, arrType.ElementType, ref isAssemQual);
                if (arrType.IsSzArray())
                {
                    sb.Append("[]");
                }
                else
                {
                    sb.Append('[');
                    if (arrType.Rank == 1) sb.Append('*');
                    for (var i = 1; i < arrType.Rank; i++) sb.Append(',');
                    sb.Append(']');
                }

                goto done;
            }

            var pointer = type as Pointer;
            if (pointer != null)
            {
                type = pointer.ElementType;
                var isAssemQual = false;
                AppendSerializedTypeName(sb, pointer.ElementType, ref isAssemQual);
                sb.Append('*');
                goto done;
            }

            var reference = type as Reference;
            if (reference != null)
            {
                type = reference.ElementType;
                var isAssemQual = false;
                AppendSerializedTypeName(sb, reference.ElementType, ref isAssemQual);
                sb.Append('&');
                goto done;
            }

            if (type.Template == null)
            {
                var escapedName = type.FullName.Replace(",", "\\,");
                sb.Append(escapedName);
            }
            else
            {
                sb.Append(type.Template.FullName);
                sb.Append('[');
                for (int i = 0,
                     n = type.ConsolidatedTemplateArguments == null ? 0 : type.ConsolidatedTemplateArguments.Count;
                     i < n;
                     i++)
                {
                    //^ assert type.ConsolidatedTemplateArguments != null;
                    var isAssemQual = true;
                    AppendSerializedTypeName(sb, type.ConsolidatedTemplateArguments[i], ref isAssemQual);
                    if (i < n - 1) sb.Append(',');
                }

                sb.Append(']');
            }

            done:
            if (isAssemblyQualified)
                AppendAssemblyQualifierIfNecessary(sb, type, out isAssemblyQualified);
            return sb.ToString();
        }

        private void AppendAssemblyQualifierIfNecessary(StringBuilder /*!*/ sb, TypeNode type,
            out bool isAssemQualified)
        {
            isAssemQualified = false;
            if (type == null) return;
            var referencedAssembly = type.DeclaringModule as AssemblyNode;
            if (referencedAssembly != null &&
                referencedAssembly != module /*&& referencedAssembly != CoreSystemTypes.SystemAssembly*/)
            {
                sb.Append(", ");
                sb.Append(referencedAssembly.StrongName);
                isAssemQualified = true;
            }
        }

        private void AppendSerializedTypeName(StringBuilder /*!*/ sb, TypeNode type, ref bool isAssemQualified)
        {
            if (type == null) return;
            var argTypeName = GetSerializedTypeName(type, ref isAssemQualified);
            if (isAssemQualified) sb.Append('[');
            sb.Append(argTypeName);
            if (isAssemQualified) sb.Append(']');
        }

        private void WriteTypeDefOrRefEncoded(BinaryWriter /*!*/ target, TypeNode /*!*/ type)
        {
            if (!type.IsGeneric && IsStructural(type) && !(type is ITypeParameter))
                WriteTypeSpecEncoded(target, type);
            else if (type.DeclaringModule == module)
                WriteTypeDefEncoded(target, type);
            else if (type.DeclaringModule != null)
                WriteTypeRefEncoded(target, type);
            else
                Debug.Assert(false);
        }

        private void WriteTypeDefEncoded(BinaryWriter /*!*/ target, TypeNode /*!*/ type)
        {
            var tok = GetTypeDefIndex(type);
            WriteCompressedInt(target, tok << 2);
        }

        private void WriteTypeRefEncoded(BinaryWriter /*!*/ target, TypeNode /*!*/ type)
        {
            var tok = GetTypeRefIndex(type);
            WriteCompressedInt(target, (tok << 2) | 1);
        }

        private void WriteTypeSpecEncoded(BinaryWriter /*!*/ target, TypeNode /*!*/ type)
        {
            var tok = GetTypeSpecIndex(type);
            WriteCompressedInt(target, (tok << 2) | 2);
        }

        private void WriteTypeSignature(BinaryWriter /*!*/ target, TypeNode /*!*/ type)
        {
            WriteTypeSignature(target, type, false);
        }

        private void WriteTypeSignature(BinaryWriter /*!*/ target, TypeNode /*!*/ type, bool instantiateGenericTypes)
        {
            if (type == null) return;
            var t = WriteCustomModifiers(target, type);
            if (UseGenerics)
            {
                if (t.Template != null && t.Template.IsGeneric && t.TemplateParameters == null)
                {
                    target.Write((byte)0x15);
                    var template = t.Template;
                    while (template.Template != null) template = template.Template;
                    WriteTypeSignature(target, template);
                    var templArgs = t.ConsolidatedTemplateArguments;
                    var n = templArgs == null ? 0 : templArgs.Count;
                    WriteCompressedInt(target, n);
                    for (var i = 0; i < n; i++)
                    {
                        //^ assume templArgs != null;
                        var targ = templArgs[i];
                        if (targ == null) continue;
                        WriteTypeSignature(target, targ);
                    }

                    return;
                }

                if (t.IsGeneric && instantiateGenericTypes)
                {
                    while (t.Template != null) t = t.Template;
                    target.Write((byte)0x15);
                    WriteTypeSignature(target, t);
                    var templPars = t.ConsolidatedTemplateParameters;
                    var n = templPars == null ? 0 : templPars.Count;
                    WriteCompressedInt(target, n);
                    for (var i = 0; i < n; i++)
                    {
                        //^ assume templPars != null;
                        var tp = templPars[i];
                        if (tp == null) continue;
                        WriteTypeSignature(target, tp);
                    }

                    return;
                }

                if (t is ITypeParameter)
                {
                    var num = typeParameterNumber[t.UniqueKey];
                    if (num == null)
                    {
                        if (t is MethodTypeParameter || t is MethodClassParameter)
                            num = -(((ITypeParameter)t).ParameterListIndex + 1);
                        else
                            num = ((ITypeParameter)t).ParameterListIndex + 1;
                    }

                    if (num is int)
                    {
                        var number = (int)num;
                        if (number < 0)
                        {
                            target.Write((byte)0x1e);
                            number = -number;
                        }
                        else
                        {
                            target.Write((byte)0x13);
                        }

                        WriteCompressedInt(target, number - 1);
                        return;
                    }
                }
            }

            target.Write((byte)t.typeCode);
            switch (t.typeCode)
            {
                case ElementType.Pointer:
                    WriteTypeSignature(target, ((Pointer)t).ElementType);
                    break;
                case ElementType.Reference:
                    WriteTypeSignature(target, ((Reference)t).ElementType);
                    break;
                case ElementType.ValueType:
                case ElementType.Class:
                    WriteTypeDefOrRefEncoded(target, t);
                    break;
                case ElementType.Array:
                    WriteTypeSignature(target, ((ArrayType)t).ElementType);
                    WriteArrayShape(target, (ArrayType)t);
                    break;
                case ElementType.FunctionPointer:
                    WriteMethodSignature(target, (FunctionPointer)t);
                    break;
                case ElementType.SzArray:
                    WriteTypeSignature(target, ((ArrayType)t).ElementType);
                    break;
            }
        }

#if !ROTOR
        void IMetaDataEmit.SetModuleProps(string szName)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.Save(string szFile, uint dwSaveFlags)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SaveToStream(void* pIStream, uint dwSaveFlags)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataEmit.GetSaveSize(uint fSave)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineTypeDef(char* szTypeDef, uint dwTypeDefFlags, uint tkExtends,
            uint* rtkImplements)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineNestedType(char* szTypeDef, uint dwTypeDefFlags, uint tkExtends,
            uint* rtkImplements, uint tdEncloser)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetHandler([MarshalAs(UnmanagedType.IUnknown)] [In] object pUnk)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineMethod(uint td, char* zName, uint dwMethodFlags, byte* pvSigBlob,
            uint cbSigBlob, uint ulCodeRVA, uint dwImplFlags)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.DefineMethodImpl(uint td, uint tkBody, uint tkDecl)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineTypeRefByName(uint tkResolutionScope, char* szName)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineImportType(IntPtr pAssemImport, void* pbHashValue, uint cbHashValue,
            IMetaDataImport pImport,
            uint tdImport, IntPtr pAssemEmit)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineMemberRef(uint tkImport, string szName, byte* pvSigBlob, uint cbSigBlob)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineImportMember(IntPtr pAssemImport, void* pbHashValue, uint cbHashValue,
            IMetaDataImport pImport, uint mbMember, IntPtr pAssemEmit, uint tkParent)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineEvent(uint td, string szEvent, uint dwEventFlags, uint tkEventType,
            uint mdAddOn, uint mdRemoveOn, uint mdFire, uint* rmdOtherMethods)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetClassLayout(uint td, uint dwPackSize, COR_FIELD_OFFSET* rFieldOffsets,
            uint ulClassSize)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.DeleteClassLayout(uint td)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetFieldMarshal(uint tk, byte* pvNativeType, uint cbNativeType)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.DeleteFieldMarshal(uint tk)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefinePermissionSet(uint tk, uint dwAction, void* pvPermission, uint cbPermission)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetRVA(uint md, uint ulRVA)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.GetTokenFromSig(byte* pvSig, uint cbSig)
        {
            var sig = new BinaryWriter(new MemoryStream());
            for (var i = 0; i < cbSig; i++) sig.Write(*(pvSig + i));
            return (uint)(0x11000000 | GetStandAloneSignatureIndex(sig));
        }

        uint IMetaDataEmit.DefineModuleRef(string szName)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetParent(uint mr, uint tk)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.GetTokenFromTypeSpec(byte* pvSig, uint cbSig)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SaveToMemory(void* pbData, uint cbData)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataEmit.DefineUserString(string szString, uint cchString)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.DeleteToken(uint tkObj)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetMethodProps(uint md, uint dwMethodFlags, uint ulCodeRVA, uint dwImplFlags)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetTypeDefProps(uint td, uint dwTypeDefFlags, uint tkExtends, uint* rtkImplements)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetEventProps(uint ev, uint dwEventFlags, uint tkEventType, uint mdAddOn,
            uint mdRemoveOn, uint mdFire, uint* rmdOtherMethods)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.SetPermissionSetProps(uint tk, uint dwAction, void* pvPermission, uint cbPermission)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.DefinePinvokeMap(uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetPinvokeMap(uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.DeletePinvokeMap(uint tk)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineCustomAttribute(uint tkObj, uint tkType, void* pCustomAttribute,
            uint cbCustomAttribute)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetCustomAttributeValue(uint pcv, void* pCustomAttribute, uint cbCustomAttribute)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineField(uint td, string szName, uint dwFieldFlags, byte* pvSigBlob,
            uint cbSigBlob, uint dwCPlusTypeFlag,
            void* pValue, uint cchValue)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineProperty(uint td, string szProperty, uint dwPropFlags, byte* pvSig, uint cbSig,
            uint dwCPlusTypeFlag,
            void* pValue, uint cchValue, uint mdSetter, uint mdGetter, uint* rmdOtherMethods)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.DefineParam(uint md, uint ulParamSeq, string szName, uint dwParamFlags,
            uint dwCPlusTypeFlag, void* pValue, uint cchValue)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetFieldProps(uint fd, uint dwFieldFlags, uint dwCPlusTypeFlag, void* pValue,
            uint cchValue)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetPropertyProps(uint pr, uint dwPropFlags, uint dwCPlusTypeFlag, void* pValue,
            uint cchValue, uint mdSetter, uint mdGetter, uint* rmdOtherMethods)
        {
            throw new NotImplementedException();
        }

        unsafe void IMetaDataEmit.SetParamProps(uint pd, string szName, uint dwParamFlags, uint dwCPlusTypeFlag,
            void* pValue, uint cchValue)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataEmit.DefineSecurityAttributeSet(uint tkObj, IntPtr rSecAttrs, uint cSecAttrs)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.ApplyEditAndContinue([MarshalAs(UnmanagedType.IUnknown)] object pImport)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataEmit.TranslateSigWithScope(IntPtr pAssemImport, void* pbHashValue, uint cbHashValue,
            IMetaDataImport import, byte* pbSigBlob, uint cbSigBlob, IntPtr pAssemEmit, IMetaDataEmit emit,
            byte* pvTranslatedSig, uint cbTranslatedSigMax)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetMethodImplFlags(uint md, uint dwImplFlags)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.SetFieldRVA(uint fd, uint ulRVA)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.Merge(IMetaDataImport pImport, IntPtr pHostMapToken,
            [MarshalAs(UnmanagedType.IUnknown)] object pHandler)
        {
            throw new NotImplementedException();
        }

        void IMetaDataEmit.MergeEnd()
        {
            throw new NotImplementedException();
        }

        [PreserveSig]
        void IMetaDataImport.CloseEnum(uint hEnum)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.CountEnum(uint hEnum)
        {
            throw new NotImplementedException();
        }

        void IMetaDataImport.ResetEnum(uint hEnum, uint ulPos)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumTypeDefs(ref uint phEnum,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rTypeDefs, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumInterfaceImpls(ref uint phEnum, uint td,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rImpls, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumTypeRefs(ref uint phEnum,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rTypeRefs, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.FindTypeDefByName(string szTypeDef, uint tkEnclosingClass)
        {
            throw new NotImplementedException();
        }

        Guid IMetaDataImport.GetScopeProps(StringBuilder szName, uint cchName, out uint pchName)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetModuleFromScope()
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetTypeDefProps(uint td, IntPtr szTypeDef, uint cchTypeDef, out uint pchTypeDef,
            IntPtr pdwTypeDefFlags)
        {
            pchTypeDef = 0;
            if (td == 0) return 0;
            TypeNode t = null;
            if ((td & 0xFF000000) == 0x1B000000)
            {
                t = typeSpecEntries[(int)(td & 0xFFFFFF) - 1];
                if (t.Template != null) t = t.Template;
            }
            else
            {
                t = typeDefEntries[(int)(td & 0xFFFFFF) - 1];
            }

            if (t == null || t.Name == null) return 0;
            var tName = GetProperFullTypeName(t);
            if (tName == null) return 0;
            pchTypeDef = (uint)tName.Length;
            if (pchTypeDef >= cchTypeDef) pchTypeDef = cchTypeDef - 1;
            var pTypeDef = (char*)szTypeDef.ToPointer();
            for (var i = 0; i < pchTypeDef; i++) *(pTypeDef + i) = tName[i];
            *(pTypeDef + pchTypeDef) = (char)0;
            var pFlags = (uint*)pdwTypeDefFlags.ToPointer();
            *pFlags = (uint)t.Flags;
            var bt = t.BaseType;
            if (bt == null) return 0;
            return (uint)GetTypeToken(bt);
        }

        private static string GetProperFullTypeName(TypeNode type)
        {
            if (type.DeclaringType == null) return type.FullName;
            return type.Name.Name;
        }

        uint IMetaDataImport.GetInterfaceImplProps(uint iiImpl, out uint pClass)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetTypeRefProps(uint tr, out uint ptkResolutionScope, StringBuilder szName, uint cchName)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.ResolveTypeRef(uint tr, [In] ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out object ppIScope)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumMembers(ref uint phEnum, uint cl,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rMembers, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumMembersWithName(ref uint phEnum, uint cl, string szName,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rMembers, uint cMax)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.EnumMethods(ref uint phEnum, uint cl, uint* rMethods, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumMethodsWithName(ref uint phEnum, uint cl, string szName,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rMethods, uint cMax)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.EnumFields(ref uint phEnum, uint cl, uint* rFields, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumFieldsWithName(ref uint phEnum, uint cl, string szName,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rFields, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumParams(ref uint phEnum, uint mb,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rParams, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumMemberRefs(ref uint phEnum, uint tkParent,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rMemberRefs, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumMethodImpls(ref uint phEnum, uint td,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rMethodBody,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
            uint[] rMethodDecl, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumPermissionSets(ref uint phEnum, uint tk, uint dwActions,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rPermission,
            uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.FindMember(uint td, string szName,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pvSigBlob, uint cbSigBlob)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.FindMethod(uint td, string szName,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pvSigBlob, uint cbSigBlob)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.FindField(uint td, string szName,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pvSigBlob, uint cbSigBlob)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.FindMemberRef(uint td, string szName,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pvSigBlob, uint cbSigBlob)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetMethodProps(uint mb, out uint pClass, IntPtr szMethod, uint cchMethod,
            out uint pchMethod, IntPtr pdwAttr,
            IntPtr ppvSigBlob, IntPtr pcbSigBlob, IntPtr pulCodeRVA)
        {
            Method m = null;
            if ((mb & 0xFF000000) == 0x0A000000)
                m = memberRefEntries[(int)(mb & 0xFFFFFF) - 1] as Method;
            else
                m = methodEntries[(int)(mb & 0xFFFFFF) - 1];
            pchMethod = 0;
            pClass = 0;
            if (m == null || m.DeclaringType == null) return 0;
            pClass = (uint)GetTypeDefToken(m.DeclaringType);
            var methName = m.Name == null ? null : m.Name.ToString();
            if (methName == null) return 0;
            pchMethod = (uint)methName.Length;
            var pMethName = (char*)szMethod.ToPointer();
            for (var i = 0; i < pchMethod; i++) *(pMethName + i) = methName[i];
            *(pMethName + pchMethod) = (char)0;
            return 0;
        }

        unsafe uint IMetaDataImport.GetMemberRefProps(uint mr, ref uint ptk, StringBuilder szMember, uint cchMember,
            out uint pchMember, out byte* ppvSigBlob)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.EnumProperties(ref uint phEnum, uint td, uint* rProperties, uint cMax)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.EnumEvents(ref uint phEnum, uint td, uint* rEvents, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetEventProps(uint ev, out uint pClass, StringBuilder szEvent, uint cchEvent,
            out uint pchEvent, out uint pdwEventFlags,
            out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 11)]
            uint[] rmdOtherMethod, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumMethodSemantics(ref uint phEnum, uint mb,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] rEventProp, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetMethodSemantics(uint mb, uint tkEventProp)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetClassLayout(uint td, out uint pdwPackSize,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] COR_FIELD_OFFSET[] rFieldOffset, uint cMax,
            out uint pcFieldOffset)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetFieldMarshal(uint tk, out byte* ppvNativeType)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetRVA(uint tk, out uint pulCodeRVA)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetPermissionSetProps(uint pm, out uint pdwAction, out void* ppvPermission)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetSigFromToken(uint mdSig, out byte* ppvSig)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetModuleRefProps(uint mur, StringBuilder szName, uint cchName)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumModuleRefs(ref uint phEnum,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rModuleRefs, uint cmax)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetTypeSpecFromToken(uint typespec, out byte* ppvSig)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetNameFromToken(uint tk)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumUnresolvedMethods(ref uint phEnum,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rMethods, uint cMax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetUserString(uint stk, StringBuilder szString, uint cchString)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetPinvokeMap(uint tk, out uint pdwMappingFlags, StringBuilder szImportName,
            uint cchImportName, out uint pchImportName)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumSignatures(ref uint phEnum,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rSignatures, uint cmax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumTypeSpecs(ref uint phEnum,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rTypeSpecs, uint cmax)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumUserStrings(ref uint phEnum,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] rStrings, uint cmax)
        {
            throw new NotImplementedException();
        }

        [PreserveSig]
        int IMetaDataImport.GetParamForMethodIndex(uint md, uint ulParamSeq, out uint pParam)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.EnumCustomAttributes(ref uint phEnum, uint tk, uint tkType,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] rCustomAttributes, uint cMax)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType,
            out void* ppBlob)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.FindTypeRef(uint tkResolutionScope, string szName)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetMemberProps(uint mb, out uint pClass, StringBuilder szMember, uint cchMember,
            out uint pchMember, out uint pdwAttr,
            out byte* ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags,
            out uint pdwCPlusTypeFlag, out void* ppValue)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetFieldProps(uint mb, out uint pClass, StringBuilder szField, uint cchField,
            out uint pchField, out uint pdwAttr,
            out byte* ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out void* ppValue)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetPropertyProps(uint prop, out uint pClass, StringBuilder szProperty,
            uint cchProperty, out uint pchProperty, out uint pdwPropFlags,
            out byte* ppvSig, out uint pbSig, out uint pdwCPlusTypeFlag, out void* ppDefaultValue,
            out uint pcchDefaultValue, out uint pmdSetter,
            out uint pmdGetter, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 14)] uint[] rmdOtherMethod,
            uint cMax)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetParamProps(uint tk, out uint pmd, out uint pulSequence, StringBuilder szName,
            uint cchName, out uint pchName,
            out uint pdwAttr, out uint pdwCPlusTypeFlag, out void* ppValue)
        {
            throw new NotImplementedException();
        }

        unsafe uint IMetaDataImport.GetCustomAttributeByName(uint tkObj, string szName, out void* ppData)
        {
            throw new NotImplementedException();
        }

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IMetaDataImport.IsValidToken(uint tk)
        {
            throw new NotImplementedException();
        }

        uint IMetaDataImport.GetNestedClassProps(uint tdNestedClass)
        {
            TypeNode t = null;
            if ((tdNestedClass & 0xFF000000) == 0x1B000000)
                t = typeSpecEntries[(int)(tdNestedClass & 0xFFFFFF) - 1];
            else
                t = typeDefEntries[(int)(tdNestedClass & 0xFFFFFF) - 1];
            if (t == null || t.DeclaringType == null) return 0;
            return (uint)GetTypeToken(t.DeclaringType);
        }

        unsafe uint IMetaDataImport.GetNativeCallConvFromSig(void* pvSig, uint cbSig)
        {
            throw new NotImplementedException();
        }

        int IMetaDataImport.IsGlobal(uint pd)
        {
            throw new NotImplementedException();
        }
#endif
    }
#if WHIDBEYwithGenericsAndIEqualityComparer
    public class ByteArrayKeyComparer : IEqualityComparer, IComparer
    {
        int IComparer.Compare(object x, object y)
        {
            if (x == null || y == null) throw new ArgumentNullException();
            var xa = (byte[])x;
            var ya = (byte[])y;
            var n = xa.Length;
            var result = n - ya.Length;
            if (result != 0) return result;
            for (var i = 0; i < n; i++)
            {
                result = xa[i] - ya[i];
                if (result != 0) return result;
            }

            return 0;
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            if (x == null || y == null) return x == y;
            return ((IComparer)this).Compare(x, y) == 0;
        }

        int IEqualityComparer.GetHashCode(object /*!*/ x)
        {
            Debug.Assert(x != null);
            var xa = (byte[])x;
            var hcode = 1;
            for (int i = 0, n = xa.Length; i < n; i++)
                hcode = hcode * 17 + xa[i];
            return hcode;
        }
    }
#elif WHIDBEYwithGenerics
  public class ByteArrayKeyComparer : IKeyComparer{
    int IComparer.Compare(object x, object y) {
      if (x == null || y == null) throw new ArgumentNullException();
      byte[] xa = (byte[])x;
      byte[] ya = (byte[])y;
      int n = xa.Length;
      int result = n - ya.Length;
      if (result != 0) return result;
      for (int i = 0; i < n; i++){
        result = xa[i] - ya[i];
        if (result != 0) return result;
      }
      return 0;
    }
    bool IKeyComparer.Equals(object x, object y){
      return ((IKeyComparer)this).Compare(x, y) == 0;
    }
    int IHashCodeProvider.GetHashCode(object x) {
      Debug.Assert(x != null);
      byte[] xa = (byte[])x;
      int hcode = 1;
      for (int i = 0, n = xa.Length; i < n; i++)
        hcode = hcode * 17 + xa[i];
      return hcode;
    }
  }
#else
  public class ByteArrayComparer : IComparer{
    int IComparer.Compare(object x, object y){
      if (x == null || y == null) throw new ArgumentNullException();
      byte[] xa = (byte[])x;
      byte[] ya = (byte[])y;
      int n = xa.Length;
      int result = n - ya.Length;
      if (result != 0) return result;
      for (int i = 0; i < n; i++){
        result = xa[i] - ya[i];
        if (result != 0) return result;
      }
      return 0;
    }
  }
  public class ByteArrayHasher : IHashCodeProvider{
    int IHashCodeProvider.GetHashCode(object x){
      Debug.Assert(x != null);
      byte[] xa = (byte[])x;
      int hcode = 1;
      for (int i = 0, n = xa.Length; i < n; i++)
        hcode = hcode*17 + xa[i];
      return hcode;
    }
  }
#endif
    internal class Fixup
    {
        internal int addressOfNextInstruction;
        internal int fixupLocation;
        internal Fixup nextFixUp;
        internal bool shortOffset;
    }

    internal class MethodInfo
    {
        internal readonly TrivialHashtable /*!*/
            fixupIndex = new TrivialHashtable(16);

        internal TrivialHashtable<int> /*!*/
            localVarIndex;

        internal BinaryWriter /*!*/
            localVarSignature;

        internal int localVarSigTok;

#if !ROTOR
        internal NodeList /*!*/
            statementNodes;

        internal LocalList /*!*/
            debugLocals;

        internal Int32List /*!*/
            signatureLengths;

        internal Int32List /*!*/
            signatureOffsets;

        internal Int32List /*!*/
            statementOffsets;
#endif
    }

    public class KeyFileNotFoundException : ArgumentException
    {
    }

    public class AssemblyCouldNotBeSignedException : ApplicationException
    {
        public const string DefaultMessage = "Assembly could not be signed.";

        public AssemblyCouldNotBeSignedException() : base(DefaultMessage)
        {
        }

        public AssemblyCouldNotBeSignedException(Exception innerException) : base(DefaultMessage, innerException)
        {
        }

        public AssemblyCouldNotBeSignedException(string message, Exception innerException) : base(message,
            innerException)
        {
        }
    }

    public class DebugSymbolsCouldNotBeWrittenException : ApplicationException
    {
    }

    internal class Writer
    {
        private Writer()
        {
        }

        internal static void WritePE(CompilerParameters /*!*/ compilerParameters, Module /*!*/ module)
            //^ requires module.Location != null;
        {
            if (compilerParameters == null)
            {
                Debug.Assert(false);
                return;
            }

            var options = compilerParameters as CompilerOptions;
            if (options == null)
            {
                WritePE(module.Location, compilerParameters.IncludeDebugInformation, module, false, null, null);
            }
            else
            {
                if (options.FileAlignment > 512) module.FileAlignment = options.FileAlignment;
                WritePE(module.Location, options.IncludeDebugInformation, module, options.DelaySign,
                    options.AssemblyKeyFile, options.AssemblyKeyName);
            }
        }

        internal static void WritePE(string /*!*/ location, bool writeDebugSymbols, Module /*!*/ module)
        {
            WritePE(location, writeDebugSymbols, module, false, null, null);
        }

        private static void WritePE(string /*!*/ location, bool writeDebugSymbols, Module /*!*/ module, bool delaySign,
            string keyFileName, string keyName)
        {
            var assem = module as AssemblyNode;
            location = Path.GetFullPath(location);
            module.Directory = Path.GetDirectoryName(location);
            var keyFileNameDoesNotExist = false;
            if (assem != null)
            {
                if (keyName != null)
                    assem.KeyContainerName = keyName;
                if (keyFileName != null && keyFileName.Length > 0)
                {
                    if (!File.Exists(keyFileName)) keyFileName = Path.Combine(module.Directory, keyFileName);
                    if (File.Exists(keyFileName))
                        using (var keyFile = File.OpenRead(keyFileName))
                        {
                            var size = keyFile.Length;
                            if (size > int.MaxValue) throw new FileLoadException();
                            var n = (int)size;
                            var key = new byte[n];
                            keyFile.Read(key, 0, n);
                            assem.KeyBlob = key;
                        }
                    else
                        keyFileNameDoesNotExist = true;
                }

                try
                {
                    assem.PublicKeyOrToken = GetPublicKey(assem, delaySign);
                }
                catch (ArgumentException ex)
                {
                    throw assem.KeyBlob != null
                        ? new AssemblyCouldNotBeSignedException(
                            ex.Message +
                            " (If you are trying to use a PFX, use the VS_KEY_* key container instead of the key file.)",
                            ex)
                        : new AssemblyCouldNotBeSignedException(ex);
                }
            }

            using (var exeFstream = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var debugSymbolsLocation = writeDebugSymbols ? Path.ChangeExtension(location, "pdb") : null;
                if (debugSymbolsLocation != null && File.Exists(debugSymbolsLocation))
                    File.Delete(debugSymbolsLocation);
                var exeMstream = new MemoryStream(300000);
                Ir2md.WritePE(module, debugSymbolsLocation, new BinaryWriter(exeMstream));
                exeMstream.WriteTo(exeFstream);
            }

            if (keyFileNameDoesNotExist) throw new KeyFileNotFoundException();
            if (delaySign || assem == null) return;
            if (assem.KeyBlob != null || (assem.KeyContainerName != null && assem.KeyContainerName.Length > 0))
                try
                {
                    ClrStrongName.SignatureGeneration(location, keyName, assem.KeyBlob);
                }
                catch (Exception ex)
                {
                    throw new AssemblyCouldNotBeSignedException(ex);
                }
        }

        private static byte[] GetPublicKey(AssemblyNode /*!*/ assem, bool delaySign)
        {
            Debug.Assert(assem != null);
            if (assem.KeyContainerName != null) return new StrongNameKeyPair(assem.KeyContainerName).PublicKey;
            if (assem.KeyBlob != null)
                try
                {
                    return new StrongNameKeyPair(assem.KeyBlob).PublicKey;
                }
                catch
                {
                    if (delaySign)
                        return assem.KeyBlob;
                    throw;
                }

            return assem.PublicKeyOrToken;
        }

        internal static void WritePE(Stream /*!*/ executable, Stream debugSymbols, Module /*!*/ module)
        {
            var mstream = new MemoryStream();
            Ir2md.WritePE(module, null, new BinaryWriter(mstream)); //TODO: need to write the PDB symbols to the stream
            mstream.WriteTo(executable);
        }

        internal static void WritePE(out byte[] executable, Module /*!*/ module)
        {
            var mstream = new MemoryStream();
            Ir2md.WritePE(module, null, new BinaryWriter(mstream));
            executable = mstream.ToArray();
        }

        internal static void WritePE(out byte[] executable, out byte[] debugSymbols, Module /*!*/ module)
        {
            var mstream = new MemoryStream();
            Ir2md.WritePE(module, null, new BinaryWriter(mstream));
            executable = mstream.ToArray();
            debugSymbols = null;
        }

        internal static void AddWin32Icon(Module /*!*/ module, string win32IconFilePath)
        {
            if (module == null || win32IconFilePath == null)
            {
                Debug.Assert(false);
                return;
            }

            using (var resStream = File.OpenRead(win32IconFilePath))
            {
                AddWin32Icon(module, resStream);
            }
        }

        internal static unsafe void AddWin32Icon(Module /*!*/ module, Stream win32IconStream)
        {
            if (module == null || win32IconStream == null)
            {
                Debug.Assert(false);
                return;
            }

            var size = win32IconStream.Length;
            if (size > int.MaxValue) throw new FileLoadException();
            var n = (int)size;
            var buffer = new byte[n];
            win32IconStream.Read(buffer, 0, n);
            var pb = (byte*)Marshal.AllocHGlobal(n);
            for (var i = 0; i < n; i++) pb[i] = buffer[i];
            var cursor = new MemoryCursor(pb, n /*, module*/);
            if (module.Win32Resources == null) module.Win32Resources = new Win32ResourceList();
            int reserved = cursor.ReadUInt16();
            if (reserved != 0) throw new NullReferenceException();
            int resourceType = cursor.ReadUInt16();
            if (resourceType != 1) throw new NullReferenceException();
            int imageCount = cursor.ReadUInt16();
            var indexHeap = new BinaryWriter(new MemoryStream());
            indexHeap.Write((ushort)0); //Reserved
            indexHeap.Write((ushort)1); //idType
            indexHeap.Write((ushort)imageCount);
            var resource = new Win32Resource();
            for (var i = 0; i < imageCount; i++)
            {
                resource = new Win32Resource();
                resource.CodePage = 0;
                resource.Id = module.Win32Resources.Count + 2;
                resource.LanguageId = 0;
                resource.Name = null;
                resource.TypeId = 3;
                resource.TypeName = null;
                indexHeap.Write(cursor.ReadByte()); //width
                indexHeap.Write(cursor.ReadByte()); //height
                indexHeap.Write(cursor.ReadByte()); //color count
                indexHeap.Write(cursor.ReadByte()); //reserved
                indexHeap.Write(cursor.ReadUInt16()); //planes
                indexHeap.Write(cursor.ReadUInt16()); //bit count
                var len = cursor.ReadInt32();
                var offset = cursor.ReadInt32();
                indexHeap.Write((uint)len);
                indexHeap.Write((ushort)(module.Win32Resources.Count + 2));
                var c = new MemoryCursor(cursor);
                c.Position = offset;
                resource.Data = c.ReadBytes(len);
                module.Win32Resources.Add(resource);
            }

            resource.CodePage = 0;
            resource.Data = indexHeap.BaseStream.ToArray();
            resource.Id = 0x7f00;
            resource.LanguageId = 0;
            resource.Name = null;
            resource.TypeId = 0xe;
            resource.TypeName = null;
            module.Win32Resources.Add(resource);
        }

        internal static void AddWin32ResourceFileToModule(Module /*!*/ module, string /*!*/ win32ResourceFilePath)
        {
            if (module == null || win32ResourceFilePath == null)
            {
                Debug.Assert(false);
                return;
            }

            using (var resStream = File.OpenRead(win32ResourceFilePath))
            {
                AddWin32ResourceFileToModule(module, resStream);
            }
        }

        internal static unsafe void AddWin32ResourceFileToModule(Module /*!*/ module, Stream /*!*/ win32ResourceStream)
        {
            if (module == null || win32ResourceStream == null)
            {
                Debug.Assert(false);
                return;
            }

            var size = win32ResourceStream.Length;
            if (size > int.MaxValue) throw new FileLoadException();
            var n = (int)size;
            var buffer = new byte[n];
            win32ResourceStream.Read(buffer, 0, n);
            var pb = (byte*)Marshal.AllocHGlobal(n);
            for (var i = 0; i < n; i++) pb[i] = buffer[i];
            var cursor = new MemoryCursor(pb, n /*, module*/);
            if (module.Win32Resources == null) module.Win32Resources = new Win32ResourceList();
            while (cursor.Position < n)
            {
                var resource = new Win32Resource();
                resource.CodePage = 0; //Review: Should this be settable?
                var dataSize = cursor.ReadInt32();
                cursor.ReadUInt32(); //headerSize
                if (cursor.Int16(0) == -1)
                {
                    cursor.ReadInt16();
                    resource.TypeId = cursor.ReadUInt16();
                    resource.TypeName = null;
                }
                else
                {
                    resource.TypeId = 0;
                    resource.TypeName = cursor.ReadUTF16();
                }

                if (cursor.Int16(0) == -1)
                {
                    cursor.ReadInt16();
                    resource.Id = cursor.ReadUInt16();
                    resource.Name = null;
                }
                else
                {
                    resource.Id = 0;
                    resource.Name = cursor.ReadUTF16();
                }

                cursor.ReadUInt32(); //dataVersion
                cursor.ReadUInt16(); //memoryFlags
                resource.LanguageId = cursor.ReadUInt16();
                cursor.ReadUInt32(); //version
                cursor.ReadUInt32(); //characteristics
                resource.Data = cursor.ReadBytes(dataSize);
                if (resource.Data != null)
                    module.Win32Resources.Add(resource);
            }
        }

        internal static void AddWin32VersionInfo(Module /*!*/ module, CompilerOptions /*!*/ options)
        {
            if (module == null || options == null)
            {
                Debug.Assert(false);
                return;
            }

            var resource = new Win32Resource();
            resource.CodePage = 0;
            resource.Id = 1;
            resource.LanguageId = 0;
            resource.Name = null;
            resource.TypeId = 0x10;
            resource.TypeName = null;
            resource.Data = FillInVsVersionStructure(module, options);
            if (module.Win32Resources == null) module.Win32Resources = new Win32ResourceList();
            module.Win32Resources.Add(resource);
        }

        private static byte[] FillInVsVersionStructure(Module /*!*/ module, CompilerOptions /*!*/ options)
        {
            var assembly = module as AssemblyNode;
            var data = new BinaryWriter(new MemoryStream(), Encoding.Unicode);
            data.Write((ushort)0); //Space for length
            data.Write((ushort)0x34); //VS_FIXEDFILEINFO length
            data.Write((ushort)0); //Type of data in version resource
            data.Write("VS_VERSION_INFO", true);
            data.Write((ushort)0); //Padding to 4 byte boundary
            // VS_FIXEDFILEINFO starts here
            data.Write(0xFEEF04BD); //Signature
            data.Write((uint)0x00010000); //Version of VS_FIXEDFILEINFO
            var fileVersion = ParseVersion(options.TargetInformation.Version, true);
            if (fileVersion == null && assembly != null) fileVersion = assembly.Version;
            if (fileVersion == null) fileVersion = new Version();
            data.Write((ushort)fileVersion.Minor);
            data.Write((ushort)fileVersion.Major);
            data.Write((ushort)fileVersion.Revision);
            data.Write((ushort)fileVersion.Build);
            var productVersion = ParseVersion(options.TargetInformation.ProductVersion, true);
            if (productVersion == null) productVersion = fileVersion;
            data.Write((ushort)productVersion.Minor);
            data.Write((ushort)productVersion.Major);
            data.Write((ushort)productVersion.Revision);
            data.Write((ushort)productVersion.Build);
            data.Write((uint)0x3f); //FileFlagsMask
            data.Write((uint)0x0); //FileFlags
            data.Write((uint)0x4); //OS: Win32 (After all, this is a Win32 resource.)
            if (options.GenerateExecutable)
                data.Write((uint)1); //App
            else
                data.Write((uint)2); //Dll
            data.Write((uint)0); //File subtype
            data.Write((ulong)0); //File Date
            // VarFileInfo
            data.Write((ushort)0x44); //Length of VarFileInfo
            data.Write((ushort)0x0); //Length of value
            data.Write((ushort)0x1); //type (text)
            data.Write("VarFileInfo", true);
            data.Write((ushort)0); //padding to 4 byte boundary
            // Var
            data.Write((ushort)0x24); //Length of Var
            data.Write((ushort)0x04); //length of Value
            data.Write((ushort)0); //Type (binary)
            data.Write("Translation", true);
            data.Write((uint)0); //Padding
            data.Write((ushort)0x4b0); //Code Page for Unicode
            // StringFileInfo
            var positionOfInfoLength = data.BaseStream.Position;
            data.Write((ushort)0); //length of rest of resource
            data.Write((ushort)0); //Value length, always 0
            data.Write((ushort)1); //Type (text)
            data.Write("StringFileInfo", true);
            // StringInfo
            var stringInfoLengthPos = data.BaseStream.Position;
            data.Write((ushort)0); //Space for length
            data.Write((ushort)0); //Value length, always 0
            data.Write((ushort)1); //Type (text)
            data.Write("000004b0", true); //Code page for Unicode
            WriteVersionString(data, options.TargetInformation.Description, "Comments");
            WriteVersionString(data, options.TargetInformation.Company, "CompanyName");
            WriteVersionString(data, options.TargetInformation.Title, "FileDescription");
            WriteVersionString(data, ConvertToString(fileVersion), "FileVersion");
            var fileName = module.Name + (options.GenerateExecutable ? ".exe" : ".dll");
            WriteVersionString(data, fileName, "InternalName");
            WriteVersionString(data, options.TargetInformation.Copyright, "LegalCopyright");
            WriteVersionString(data, options.TargetInformation.Trademark, "LegalTrademarks");
            WriteVersionString(data, fileName, "OriginalFilename");
            WriteVersionString(data, options.TargetInformation.Product, "ProductName");
            WriteVersionString(data, ConvertToString(productVersion), "ProductVersion");
            if (assembly != null)
                WriteVersionString(data, assembly.Version == null ? "" : assembly.Version.ToString(),
                    "Assembly Version");
            var len = data.BaseStream.Position;
            data.BaseStream.Position = stringInfoLengthPos;
            data.Write((ushort)(len - stringInfoLengthPos));
            data.BaseStream.Position = 0;
            data.Write((ushort)len);
            data.BaseStream.Position = positionOfInfoLength;
            data.Write((ushort)len - positionOfInfoLength);
            return data.BaseStream.ToArray();
        }

        private static void WriteVersionString(BinaryWriter /*!*/ data, string value, string /*!*/ key)
        {
            if (value == null) return;
            var totalLength = 6;
            totalLength += key.Length * 2;
            totalLength += 4 - totalLength % 4;
            totalLength += value.Length * 2;
            totalLength += 4 - totalLength % 4;
            data.Write((ushort)totalLength);
            data.Write((ushort)(value.Length + 1));
            data.Write((ushort)1); //Type (text)
            data.Write(key, true);
            if (data.BaseStream.Position % 4 != 0) data.Write((char)0);
            data.Write(value, true);
            if (data.BaseStream.Position % 4 != 0) data.Write((char)0);
        }

        private static string /*!*/ ConvertToString(Version /*!*/ version)
        {
            var sb = new StringBuilder();
            sb.Append(version.Major.ToString());
            if (version.Minor != 0 || version.Build != 0 || version.Revision != 0)
            {
                sb.Append('.');
                sb.Append(version.Minor.ToString());
            }

            if (version.Build != 0 || version.Revision != 0)
            {
                sb.Append('.');
                sb.Append(version.Build.ToString());
            }

            if (version.Revision != 0)
            {
                sb.Append('.');
                sb.Append(version.Revision.ToString());
            }

            return sb.ToString();
        }

        private static Version ParseVersion(string vString, bool allowWildcards)
        {
            if (vString == null) return null;
            ushort major = 1;
            ushort minor = 0;
            ushort build = 0;
            ushort revision = 0;
            try
            {
                var n = vString.Length;
                var i = vString.IndexOf('.', 0);
                if (i < 0) throw new FormatException();
                major = ushort.Parse(vString.Substring(0, i), CultureInfo.InvariantCulture);
                var j = vString.IndexOf('.', i + 1);
                if (j < i + 1)
                {
                    minor = ushort.Parse(vString.Substring(i + 1, n - i - 1), CultureInfo.InvariantCulture);
                }
                else
                {
                    minor = ushort.Parse(vString.Substring(i + 1, j - i - 1), CultureInfo.InvariantCulture);
                    if (vString[j + 1] == '*' && allowWildcards)
                    {
                        if (j + 1 < n - 1) return null;
                        build = DaysSince2000();
                        revision = SecondsSinceMidnight();
                    }
                    else
                    {
                        var k = vString.IndexOf('.', j + 1);
                        if (k < j + 1)
                        {
                            build = ushort.Parse(vString.Substring(j + 1, n - j - 1), CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            build = ushort.Parse(vString.Substring(j + 1, k - j - 1), CultureInfo.InvariantCulture);
                            if (vString[k + 1] == '*' && allowWildcards)
                            {
                                if (j + 1 < n - 1) return null;
                                revision = SecondsSinceMidnight();
                            }
                            else
                            {
                                revision = ushort.Parse(vString.Substring(k + 1, n - k - 1),
                                    CultureInfo.InvariantCulture);
                            }
                        }
                    }
                }
            }
            catch (FormatException)
            {
                major = minor = build = revision = ushort.MaxValue;
            }
            catch (OverflowException)
            {
                major = minor = build = revision = ushort.MaxValue;
            }

            if (major == ushort.MaxValue && minor == ushort.MaxValue && build == ushort.MaxValue &&
                revision == ushort.MaxValue) return null;
            return new Version(major, minor, build, revision);
        }

        private static ushort DaysSince2000()
        {
            return (ushort)(DateTime.Now - new DateTime(2000, 1, 1)).Days;
        }

        private static ushort SecondsSinceMidnight()
        {
            var sinceMidnight = DateTime.Now - DateTime.Today;
            return (ushort)((sinceMidnight.Hours * 60 * 60 + sinceMidnight.Minutes * 60 + sinceMidnight.Seconds) / 2);
        }
    }
}
#endif