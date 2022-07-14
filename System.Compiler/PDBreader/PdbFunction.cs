// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Compiler;
using System.Compiler.Metadata;

namespace Microsoft.Cci.Pdb
{
    internal class PdbFunction
    {
        internal static readonly Guid msilMetaData = new Guid(0xc6ea3fc9, 0x59b3, 0x49d6, 0xbc, 0x25,
            0x09, 0x02, 0xbb, 0xab, 0xb4, 0x60);

        internal static readonly IComparer byAddress = new PdbFunctionsByAddress();
        internal static readonly IComparer byAddressAndToken = new PdbFunctionsByAddressAndToken();
        internal uint address;

        internal PdbConstant[] constants;

        //internal IEnumerable<NamespaceList>/*?*/ namespaceScopes;
        internal string /*?*/
            iteratorClass;

        internal List<ILocalScope> /*?*/
            iteratorScopes;

        internal PdbLines[] lines;
        internal Method method = null;

        internal string name;
        //internal uint length;

        //internal byte[] metadata;
        internal PdbScope[] scopes;
        //internal string module;
        //internal ushort flags;

        internal uint segment;
        internal PdbSlot[] slots;
        internal uint slotToken;

        internal PdbSynchronizationInformation /*?*/
            synchronizationInformation;
        //static internal readonly IComparer byToken = new PdbFunctionsByToken();

        internal uint token;
        internal uint tokenOfMethodWhoseUsingInfoAppliesToThisMethod;
        internal uint unmappedToken;
        internal string[] usedNamespaces;

        internal ushort[] /*?*/
            usingCounts;

        internal PdbFunction()
        {
        }

        internal PdbFunction( /*string module, */ ManProcSym proc, BitAccess bits, Reader reader)
        {
            token = proc.token;
            unmappedToken = proc.token;
            //this.module = module;
            this.name = proc.name;
            //this.flags = proc.flags;
            segment = proc.seg;
            address = proc.off;
            //this.length = proc.len;

            if (proc.seg != 1) throw new PdbDebugException("Segment is {0}, not 1.", proc.seg);
            if (proc.parent != 0 || proc.next != 0)
                throw new PdbDebugException("Warning parent={0}, next={1}",
                    proc.parent, proc.next);
            //if (proc.dbgStart != 0 || proc.dbgEnd != 0) {
            //  throw new PdbDebugException("Warning DBG start={0}, end={1}",
            //                              proc.dbgStart, proc.dbgEnd);
            //}

            int constantCount;
            int scopeCount;
            int slotCount;
            int usedNamespacesCount;
            CountScopesAndSlots(bits, proc.end, out constantCount, out scopeCount, out slotCount,
                out usedNamespacesCount);
            var scope = constantCount > 0 || slotCount > 0 || usedNamespacesCount > 0 ? 1 : 0;
            var slot = 0;
            var constant = 0;
            var usedNs = 0;
            scopes = new PdbScope[scopeCount + scope];
            slots = new PdbSlot[slotCount];
            constants = new PdbConstant[constantCount];
            usedNamespaces = new string[usedNamespacesCount];

            if (scope > 0)
                scopes[0] = new PdbScope(address, proc.len, slots, constants, usedNamespaces);

            while (bits.Position < proc.end)
            {
                ushort siz;
                ushort rec;

                bits.ReadUInt16(out siz);
                var star = bits.Position;
                var stop = bits.Position + siz;
                bits.Position = star;
                bits.ReadUInt16(out rec);

                switch ((SYM)rec)
                {
                    case SYM.S_OEM:
                    {
                        // 0x0404
                        OemSymbol oem;

                        bits.ReadGuid(out oem.idOem);
                        bits.ReadUInt32(out oem.typind);
                        // internal byte[]   rgl;        // user data, force 4-byte alignment

                        if (oem.idOem == msilMetaData)
                        {
                            var name = bits.ReadString();
                            if (name == "MD2")
                            {
                                byte version;
                                bits.ReadUInt8(out version);
                                if (version == 4)
                                {
                                    byte count;
                                    bits.ReadUInt8(out count);
                                    bits.Align(4);
                                    while (count-- > 0)
                                        ReadCustomMetadata(bits);
                                }
                            }
                            else if (name == "asyncMethodInfo")
                            {
                                synchronizationInformation = new PdbSynchronizationInformation(method, bits, reader);
                            }

                            bits.Position = stop;
                            break;
                        }

                        throw new PdbDebugException("OEM section: guid={0} ti={1}",
                            oem.idOem, oem.typind);
                        // bits.Position = stop;
                    }

                    case SYM.S_BLOCK32:
                    {
                        var block = new BlockSym32();

                        bits.ReadUInt32(out block.parent);
                        bits.ReadUInt32(out block.end);
                        bits.ReadUInt32(out block.len);
                        bits.ReadUInt32(out block.off);
                        bits.ReadUInt16(out block.seg);
                        bits.SkipCString(out block.name);
                        bits.Position = stop;

                        scopes[scope++] = new PdbScope(address, block, bits, out slotToken);
                        bits.Position = (int)block.end;
                        break;
                    }

                    case SYM.S_MANSLOT:
                        slots[slot++] = new PdbSlot(bits);
                        bits.Position = stop;
                        break;

                    case SYM.S_MANCONSTANT:
                        constants[constant++] = new PdbConstant(bits);
                        bits.Position = stop;
                        break;

                    case SYM.S_UNAMESPACE:
                        bits.ReadCString(out usedNamespaces[usedNs++]);
                        bits.Position = stop;
                        break;

                    case SYM.S_END:
                        bits.Position = stop;
                        break;

                    default:
                    {
                        //throw new PdbDebugException("Unknown SYM: {0}", (SYM)rec);
                        bits.Position = stop;
                        break;
                    }
                }
            }

            if (bits.Position != proc.end) throw new PdbDebugException("Not at S_END");

            ushort esiz;
            ushort erec;
            bits.ReadUInt16(out esiz);
            bits.ReadUInt16(out erec);

            if (erec != (ushort)SYM.S_END) throw new PdbDebugException("Missing S_END");
        }

        private static string StripNamespace(string module)
        {
            var li = module.LastIndexOf('.');
            if (li > 0) return module.Substring(li + 1);
            return module;
        }


        internal static PdbFunction[] LoadManagedFunctions( /*string module,*/
            BitAccess bits, uint limit,
            bool readStrings,
            Reader reader
        )
        {
            //string mod = StripNamespace(module);
            var begin = bits.Position;
            var count = 0;

            while (bits.Position < limit)
            {
                ushort siz;
                ushort rec;

                bits.ReadUInt16(out siz);
                var star = bits.Position;
                var stop = bits.Position + siz;
                bits.Position = star;
                bits.ReadUInt16(out rec);

                switch ((SYM)rec)
                {
                    case SYM.S_GMANPROC:
                    case SYM.S_LMANPROC:
                        ManProcSym proc;
                        bits.ReadUInt32(out proc.parent);
                        bits.ReadUInt32(out proc.end);
                        bits.Position = (int)proc.end;
                        count++;
                        break;

                    case SYM.S_END:
                        bits.Position = stop;
                        break;

                    default:
                        //Console.WriteLine("{0,6}: {1:x2} {2}",
                        //                  bits.Position, rec, (SYM)rec);
                        bits.Position = stop;
                        break;
                }
            }

            if (count == 0) return null;

            bits.Position = begin;
            var funcs = new PdbFunction[count];
            var func = 0;

            while (bits.Position < limit)
            {
                ushort siz;
                ushort rec;

                bits.ReadUInt16(out siz);
                var star = bits.Position;
                var stop = bits.Position + siz;
                bits.ReadUInt16(out rec);

                switch ((SYM)rec)
                {
                    case SYM.S_GMANPROC:
                    case SYM.S_LMANPROC:
                        ManProcSym proc;
                        //int offset = bits.Position;

                        bits.ReadUInt32(out proc.parent);
                        bits.ReadUInt32(out proc.end);
                        bits.ReadUInt32(out proc.next);
                        bits.ReadUInt32(out proc.len);
                        bits.ReadUInt32(out proc.dbgStart);
                        bits.ReadUInt32(out proc.dbgEnd);
                        bits.ReadUInt32(out proc.token);
                        bits.ReadUInt32(out proc.off);
                        bits.ReadUInt16(out proc.seg);
                        bits.ReadUInt8(out proc.flags);
                        bits.ReadUInt16(out proc.retReg);
                        if (readStrings)
                            bits.ReadCString(out proc.name);
                        else
                            bits.SkipCString(out proc.name);
                        //Console.WriteLine("token={0:X8} [{1}::{2}]", proc.token, module, proc.name);

                        bits.Position = stop;

                        funcs[func++] = new PdbFunction( /*module,*/ proc, bits, reader);
                        break;

                    default:
                    {
                        //throw new PdbDebugException("Unknown SYMREC {0}", (SYM)rec);
                        bits.Position = stop;
                        break;
                    }
                }
            }

            return funcs;
        }

        internal static void CountScopesAndSlots(BitAccess bits, uint limit,
            out int constants, out int scopes, out int slots, out int usedNamespaces)
        {
            var pos = bits.Position;
            BlockSym32 block;
            constants = 0;
            slots = 0;
            scopes = 0;
            usedNamespaces = 0;

            while (bits.Position < limit)
            {
                ushort siz;
                ushort rec;

                bits.ReadUInt16(out siz);
                var star = bits.Position;
                var stop = bits.Position + siz;
                bits.Position = star;
                bits.ReadUInt16(out rec);

                switch ((SYM)rec)
                {
                    case SYM.S_BLOCK32:
                    {
                        bits.ReadUInt32(out block.parent);
                        bits.ReadUInt32(out block.end);

                        scopes++;
                        bits.Position = (int)block.end;
                        break;
                    }

                    case SYM.S_MANSLOT:
                        slots++;
                        bits.Position = stop;
                        break;

                    case SYM.S_UNAMESPACE:
                        usedNamespaces++;
                        bits.Position = stop;
                        break;

                    case SYM.S_MANCONSTANT:
                        constants++;
                        bits.Position = stop;
                        break;

                    default:
                        bits.Position = stop;
                        break;
                }
            }

            bits.Position = pos;
        }

        // This method will crash with Dev15+ in debug builds. We replaced with the one below
        private void ReadCustomMetadata_OLD(BitAccess bits)
        {
            var savedPosition = bits.Position;
            byte version;
            bits.ReadUInt8(out version);
            if (version != 4) throw new PdbDebugException("Unknown custom metadata item version: {0}", version);
            byte kind;
            bits.ReadUInt8(out kind);
            bits.Align(4);
            uint numberOfBytesInItem;
            bits.ReadUInt32(out numberOfBytesInItem);
            switch (kind)
            {
                case 0:
                    ReadUsingInfo(bits);
                    break;
                case 1:
                    ReadForwardInfo(bits);
                    break;
                case 2: break; // this.ReadForwardedToModuleInfo(bits); break;
                case 3:
                    ReadIteratorLocals(bits);
                    break;
                case 4:
                    ReadForwardIterator(bits);
                    break;
                default: throw new PdbDebugException("Unknown custom metadata item kind: {0}", kind);
            }

            bits.Position = savedPosition + (int)numberOfBytesInItem;
        }

        private void ReadCustomMetadata(BitAccess bits)
        {
            var savedPosition = bits.Position;
            byte version;
            bits.ReadUInt8(out version);
            byte kind;
            bits.ReadUInt8(out kind);
            bits.Align(4);
            uint numberOfBytesInItem;
            bits.ReadUInt32(out numberOfBytesInItem);
            if (version == 4)
                switch (kind)
                {
                    case 0:
                        ReadUsingInfo(bits);
                        break;
                    case 1:
                        ReadForwardInfo(bits);
                        break;
                    case 2: break; // this.ReadForwardedToModuleInfo(bits); break;
                    case 3:
                        ReadIteratorLocals(bits);
                        break;
                    case 4:
                        ReadForwardIterator(bits);
                        break;
                }

            bits.Position = savedPosition + (int)numberOfBytesInItem;
        }


        private void ReadForwardIterator(BitAccess bits)
        {
            iteratorClass = bits.ReadString();
        }

        private void ReadIteratorLocals(BitAccess bits)
        {
            uint numberOfLocals;
            bits.ReadUInt32(out numberOfLocals);
            iteratorScopes = new List<ILocalScope>((int)numberOfLocals);
            while (numberOfLocals-- > 0)
            {
                uint ilStartOffset;
                uint ilEndOffset;
                bits.ReadUInt32(out ilStartOffset);
                bits.ReadUInt32(out ilEndOffset);
                iteratorScopes.Add(new PdbIteratorScope(ilStartOffset, ilEndOffset - ilStartOffset));
            }
        }

        //private void ReadForwardedToModuleInfo(BitAccess bits) {
        //}

        private void ReadForwardInfo(BitAccess bits)
        {
            bits.ReadUInt32(out tokenOfMethodWhoseUsingInfoAppliesToThisMethod);
        }

        private void ReadUsingInfo(BitAccess bits)
        {
            ushort numberOfNamespaces;
            bits.ReadUInt16(out numberOfNamespaces);
            usingCounts = new ushort[numberOfNamespaces];
            for (ushort i = 0; i < numberOfNamespaces; i++) bits.ReadUInt16(out usingCounts[i]);
        }

        //internal class PdbFunctionsByToken : IComparer {
        //  public int Compare(Object x, Object y) {
        //    PdbFunction fx = (PdbFunction)x;
        //    PdbFunction fy = (PdbFunction)y;

        //    if (fx.token < fy.token) {
        //      return -1;
        //    } else if (fx.token > fy.token) {
        //      return 1;
        //    } else {
        //      return 0;
        //    }
        //  }

        //}

        private static void MapToken(ref uint token, uint[] ridMap)
        {
            if (ridMap == null) return;
            token = 0x06000000 | ridMap[token & 0xffffff];
        }

        internal void MapTokens(uint[] ridMap, Reader reader)
        {
            MapToken(ref token, ridMap);

            MapTokens(synchronizationInformation, ridMap, reader);
        }

        private void MapTokens(PdbSynchronizationInformation pdbSynchronizationInformation, uint[] ridMap,
            Reader reader)
        {
            if (pdbSynchronizationInformation == null) return;

            MapToken(ref pdbSynchronizationInformation.kickoffMethodToken, ridMap);

            MapTokens(pdbSynchronizationInformation.synchronizationPoints, ridMap, reader);
        }

        private void MapTokens(PdbSynchronizationPoint[] syncPoints, uint[] ridMap, Reader reader)
        {
            if (syncPoints == null) return;

            for (var i = 0; i < syncPoints.Length; i++) MapTokens(syncPoints[i], ridMap, reader);
        }

        private void MapTokens(PdbSynchronizationPoint syncpoint, uint[] ridMap, Reader reader)
        {
            MapToken(ref syncpoint.continuationMethodToken, ridMap);
        }

        internal class PdbFunctionsByAddress : IComparer
        {
            public int Compare(object x, object y)
            {
                var fx = (PdbFunction)x;
                var fy = (PdbFunction)y;

                if (fx.segment < fy.segment)
                    return -1;
                if (fx.segment > fy.segment)
                    return 1;
                if (fx.address < fy.address)
                    return -1;
                if (fx.address > fy.address)
                    return 1;
                return 0;
            }
        }

        internal class PdbFunctionsByAddressAndToken : IComparer
        {
            public int Compare(object x, object y)
            {
                var fx = (PdbFunction)x;
                var fy = (PdbFunction)y;

                if (fx.segment < fy.segment) return -1;

                if (fx.segment > fy.segment) return 1;

                if (fx.address < fy.address) return -1;

                if (fx.address > fy.address) return 1;

                if (fx.token < fy.token)
                    return -1;
                if (fx.token > fy.token)
                    return 1;
                return 0;
            }
        }
    }

    internal class PdbSynchronizationInformation
    {
        internal uint generatedCatchHandlerIlOffset;
        internal uint kickoffMethodToken;
        internal Method moveNextMethod;
        private readonly Reader reader;
        internal PdbSynchronizationPoint[] synchronizationPoints;

        internal PdbSynchronizationInformation(Method moveNext, BitAccess bits, Reader reader)
        {
            this.reader = reader;
            uint asyncStepInfoCount;
            bits.ReadUInt32(out kickoffMethodToken);
            bits.ReadUInt32(out generatedCatchHandlerIlOffset);
            bits.ReadUInt32(out asyncStepInfoCount);
            synchronizationPoints = new PdbSynchronizationPoint[asyncStepInfoCount];
            for (uint i = 0; i < asyncStepInfoCount; i += 1)
                synchronizationPoints[i] = new PdbSynchronizationPoint(bits, reader);
            // this.asyncMethod = reader.pdbInfo.GetMethodFromPdbToken(kickoffMethodToken);
            moveNextMethod = moveNext;
        }

        public Method AsyncMethod => reader.GetMemberFromToken((int)kickoffMethodToken) as Method;

        public Method MoveNextMethod => moveNextMethod;

        public uint GeneratedCatchHandlerOffset => generatedCatchHandlerIlOffset;

        public PdbSynchronizationPoint[] SynchronizationPoints => synchronizationPoints;
    }

    internal class PdbSynchronizationPoint
    {
        internal uint continuationMethodToken;
        internal uint continuationOffset;
        private readonly Reader reader;
        internal uint synchronizeOffset;

        internal PdbSynchronizationPoint(BitAccess bits, Reader reader)
        {
            this.reader = reader;
            bits.ReadUInt32(out synchronizeOffset);
            bits.ReadUInt32(out continuationMethodToken);
            bits.ReadUInt32(out continuationOffset);
            //this.continuationMethod = reader.pdbInfo.GetMethodFromPdbToken(continuationMethodToken);
        }

        public uint SynchronizeOffset => synchronizeOffset;

        public Method /*?*/ ContinuationMethod => reader.GetMemberFromToken((int)continuationMethodToken) as Method;

        public uint ContinuationOffset => continuationOffset;
    }
}