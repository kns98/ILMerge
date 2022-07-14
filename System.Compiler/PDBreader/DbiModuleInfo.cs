﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

namespace Microsoft.Cci.Pdb
{
    internal class DbiModuleInfo
    {
        internal readonly int cbLines; // 44..57
        internal readonly int cbOldLines; // 40..43
        internal readonly int cbSyms; // 36..39

        internal readonly short files; // 48..49

        //internal DbiSecCon section;                //  4..31
        internal readonly ushort flags; // 32..33
        internal readonly string moduleName;
        internal readonly int niCompiler;
        internal readonly int niSource;
        internal readonly string objectName;
        internal readonly uint offsets;

        internal readonly int opened; //  0..3
        internal readonly short pad1; // 50..51
        internal readonly short stream; // 34..35

        internal DbiModuleInfo(BitAccess bits, bool readStrings)
        {
            bits.ReadInt32(out opened);
            new DbiSecCon(bits);
            bits.ReadUInt16(out flags);
            bits.ReadInt16(out stream);
            bits.ReadInt32(out cbSyms);
            bits.ReadInt32(out cbOldLines);
            bits.ReadInt32(out cbLines);
            bits.ReadInt16(out files);
            bits.ReadInt16(out pad1);
            bits.ReadUInt32(out offsets);
            bits.ReadInt32(out niSource);
            bits.ReadInt32(out niCompiler);
            if (readStrings)
            {
                bits.ReadCString(out moduleName);
                bits.ReadCString(out objectName);
            }
            else
            {
                bits.SkipCString(out moduleName);
                bits.SkipCString(out objectName);
            }

            bits.Align(4);
            //if (opened != 0 || pad1 != 0) {
            //  throw new PdbException("Invalid DBI module. "+
            //                                 "(opened={0}, pad={1})", opened, pad1);
            //}
        }
    }
}