// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System.Collections.Generic;
using System.Compiler.Metadata;
using System.IO;
using Microsoft.Cci.Pdb;

namespace System.Compiler
{
    internal class PdbInfo
    {
        private Reader reader;
        private readonly string sourceServerData;

        private readonly Dictionary<uint, PdbTokenLine> tokenToSourceMapping;

        //private uint[] remapTable;
        private readonly Dictionary<uint, PdbFunction> pdbFunctionMap;


        public PdbInfo(FileStream inputStream, Reader reader)
        {
            this.reader = reader;
            pdbFunctionMap =
                PdbFile.LoadFunctionMap(inputStream, out tokenToSourceMapping, out sourceServerData, reader);
            //inputStream.Seek(0L, IO.SeekOrigin.Begin);
            //this.remapTable = PdbFile.LoadRemapTable(inputStream);
        }

#if false
    public Method GetMethodFromPdbToken(uint token)
    {
      // remap if necessary
      if (this.remapTable != null)
      {
        token = 0x06000000 | this.remapTable[token & 0xffffff];
      }
      return reader.GetMemberFromToken((int)token) as Method;
    }
#endif

        public PdbFunction GetMethodInfo(uint token)
        {
            PdbFunction result;
            pdbFunctionMap.TryGetValue(token, out result);
            return result;
        }
    }
}