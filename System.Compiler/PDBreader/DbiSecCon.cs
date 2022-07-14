// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

namespace Microsoft.Cci.Pdb
{
    internal struct DbiSecCon
    {
        internal DbiSecCon(BitAccess bits)
        {
            bits.ReadInt16(out section);
            bits.ReadInt16(out pad1);
            bits.ReadInt32(out offset);
            bits.ReadInt32(out size);
            bits.ReadUInt32(out flags);
            bits.ReadInt16(out module);
            bits.ReadInt16(out pad2);
            bits.ReadUInt32(out dataCrc);
            bits.ReadUInt32(out relocCrc);
            //if (pad1 != 0 || pad2 != 0) {
            //  throw new PdbException("Invalid DBI section. "+
            //                                 "(pad1={0}, pad2={1})",
            //                         pad1, pad2);
            //}
        }

        internal readonly short section; // 0..1
        internal readonly short pad1; // 2..3
        internal readonly int offset; // 4..7
        internal readonly int size; // 8..11
        internal readonly uint flags; // 12..15
        internal readonly short module; // 16..17
        internal readonly short pad2; // 18..19
        internal readonly uint dataCrc; // 20..23
        internal readonly uint relocCrc; // 24..27
    }
}