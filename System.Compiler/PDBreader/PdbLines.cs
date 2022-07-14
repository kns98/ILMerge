// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

namespace Microsoft.Cci.Pdb
{
    internal struct PdbLine
    {
        internal readonly uint offset;
        internal readonly uint lineBegin;
        internal readonly uint lineEnd;
        internal readonly ushort colBegin;
        internal readonly ushort colEnd;

        internal PdbLine(uint offset, uint lineBegin, ushort colBegin, uint lineEnd, ushort colEnd)
        {
            this.offset = offset;
            this.lineBegin = lineBegin;
            this.colBegin = colBegin;
            this.lineEnd = lineEnd;
            this.colEnd = colEnd;
        }
    }

    internal class PdbLines
    {
        internal readonly PdbSource file;
        internal readonly PdbLine[] lines;

        internal PdbLines(PdbSource file, uint count)
        {
            this.file = file;
            lines = new PdbLine[count];
        }
    }
}