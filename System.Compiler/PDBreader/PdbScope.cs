﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System;

namespace Microsoft.Cci.Pdb
{
    internal class PdbScope
    {
        //internal uint segment;
        internal uint address;
        internal PdbConstant[] constants;
        internal uint length;
        internal uint offset;
        internal PdbScope[] scopes;
        internal PdbSlot[] slots;
        internal string[] usedNamespaces;

        internal PdbScope(uint address, uint length, PdbSlot[] slots, PdbConstant[] constants, string[] usedNamespaces)
        {
            this.constants = constants;
            this.slots = slots;
            scopes = new PdbScope[0];
            this.usedNamespaces = usedNamespaces;
            this.address = address;
            offset = 0;
            this.length = length;
        }

        internal PdbScope(uint funcOffset, BlockSym32 block, BitAccess bits, out uint typind)
        {
            //this.segment = block.seg;
            address = block.off;
            offset = block.off - funcOffset;
            length = block.len;
            typind = 0;

            int constantCount;
            int scopeCount;
            int slotCount;
            int namespaceCount;
            PdbFunction.CountScopesAndSlots(bits, block.end, out constantCount, out scopeCount, out slotCount,
                out namespaceCount);
            constants = new PdbConstant[constantCount];
            scopes = new PdbScope[scopeCount];
            slots = new PdbSlot[slotCount];
            usedNamespaces = new string[namespaceCount];
            var constant = 0;
            var scope = 0;
            var slot = 0;
            var usedNs = 0;

            while (bits.Position < block.end)
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
                        var sub = new BlockSym32();

                        bits.ReadUInt32(out sub.parent);
                        bits.ReadUInt32(out sub.end);
                        bits.ReadUInt32(out sub.len);
                        bits.ReadUInt32(out sub.off);
                        bits.ReadUInt16(out sub.seg);
                        bits.SkipCString(out sub.name);

                        bits.Position = stop;
                        scopes[scope++] = new PdbScope(funcOffset, sub, bits, out typind);
                        break;
                    }

                    case SYM.S_MANSLOT:
                        slots[slot++] = new PdbSlot(bits);
                        bits.Position = stop;
                        break;

                    case SYM.S_UNAMESPACE:
                        bits.ReadCString(out usedNamespaces[usedNs++]);
                        bits.Position = stop;
                        break;

                    case SYM.S_END:
                        bits.Position = stop;
                        break;

                    case SYM.S_MANCONSTANT:
                        constants[constant++] = new PdbConstant(bits);
                        bits.Position = stop;
                        break;

                    default:
                        //throw new PdbException("Unknown SYM in scope {0}", (SYM)rec);
                        bits.Position = stop;
                        break;
                }
            }

            if (bits.Position != block.end) throw new Exception("Not at S_END");

            ushort esiz;
            ushort erec;
            bits.ReadUInt16(out esiz);
            bits.ReadUInt16(out erec);

            if (erec != (ushort)SYM.S_END) throw new Exception("Missing S_END");
        }
    }
}