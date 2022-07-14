// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

namespace Microsoft.Cci.Pdb
{
    internal class PdbSlot
    {
        internal ushort flags;
        internal string name;
        internal uint slot;

        internal uint typeToken;
        //internal uint segment;
        //internal uint address;

        internal PdbSlot(BitAccess bits)
        {
            AttrSlotSym slot;

            bits.ReadUInt32(out slot.index);
            bits.ReadUInt32(out slot.typind);
            bits.ReadUInt32(out slot.offCod);
            bits.ReadUInt16(out slot.segCod);
            bits.ReadUInt16(out slot.flags);
            bits.ReadCString(out slot.name);

            this.slot = slot.index;
            typeToken = slot.typind;
            name = slot.name;
            flags = slot.flags;
            //this.segment = slot.segCod;
            //this.address = slot.offCod;
        }
    }
}