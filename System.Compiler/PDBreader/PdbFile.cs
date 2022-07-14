// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Compiler.Metadata;
using System.Diagnostics.SymbolStore;
using System.IO;

namespace Microsoft.Cci.Pdb
{
    internal class PdbFile
    {
        private static readonly PdbFunction match = new PdbFunction();

        private PdbFile() // This class can't be instantiated.
        {
        }

        private static void LoadGuidStream(BitAccess bits, out Guid doctype, out Guid language, out Guid vendor)
        {
            bits.ReadGuid(out language);
            bits.ReadGuid(out vendor);
            bits.ReadGuid(out doctype);
        }

        private static Dictionary<string, int> LoadNameIndex(BitAccess bits)
        {
            var result = new Dictionary<string, int>();
            int ver;
            int sig;
            int age;
            Guid guid;
            bits.ReadInt32(out ver); //  0..3  Version
            bits.ReadInt32(out sig); //  4..7  Signature
            bits.ReadInt32(out age); //  8..11 Age
            bits.ReadGuid(out guid); // 12..27 GUID

            //if (ver != 20000404) {
            //  throw new PdbDebugException("Unsupported PDB Stream version {0}", ver);
            //}

            // Read string buffer.
            int buf;
            bits.ReadInt32(out buf); // 28..31 Bytes of Strings

            var beg = bits.Position;
            var nxt = bits.Position + buf;

            bits.Position = nxt;

            // Read map index.
            int cnt; // n+0..3 hash size.
            int max; // n+4..7 maximum ni.

            bits.ReadInt32(out cnt);
            bits.ReadInt32(out max);

            var present = new BitSet(bits);
            var deleted = new BitSet(bits);
            if (!deleted.IsEmpty) throw new PdbDebugException("Unsupported PDB deleted bitset is not empty.");

            var j = 0;
            for (var i = 0; i < max; i++)
                if (present.IsSet(i))
                {
                    int ns;
                    int ni;
                    bits.ReadInt32(out ns);
                    bits.ReadInt32(out ni);

                    string name;
                    var saved = bits.Position;
                    bits.Position = beg + ns;
                    bits.ReadCString(out name);
                    bits.Position = saved;

                    result.Add(name.ToUpperInvariant(), ni);
                    j++;
                }

            if (j != cnt) throw new PdbDebugException("Count mismatch. ({0} != {1})", j, cnt);
            return result;
        }

        private static IntHashTable LoadNameStream(BitAccess bits)
        {
            var ht = new IntHashTable();

            uint sig;
            int ver;
            bits.ReadUInt32(out sig); //  0..3  Signature
            bits.ReadInt32(out ver); //  4..7  Version

            // Read (or skip) string buffer.
            int buf;
            bits.ReadInt32(out buf); //  8..11 Bytes of Strings

            if (sig != 0xeffeeffe || ver != 1)
                throw new PdbDebugException("Unsupported Name Stream version. " +
                                            "(sig={0:x8}, ver={1})",
                    sig, ver);
            var beg = bits.Position;
            var nxt = bits.Position + buf;
            bits.Position = nxt;

            // Read hash table.
            int siz;
            bits.ReadInt32(out siz); // n+0..3 Number of hash buckets.
            nxt = bits.Position;

            for (var i = 0; i < siz; i++)
            {
                int ni;
                string name;

                bits.ReadInt32(out ni);

                if (ni != 0)
                {
                    var saved = bits.Position;
                    bits.Position = beg + ni;
                    bits.ReadCString(out name);
                    bits.Position = saved;

                    ht.Add(ni, name);
                }
            }

            bits.Position = nxt;

            return ht;
        }

        private static int FindFunction(PdbFunction[] funcs, ushort sec, uint off)
        {
            match.segment = sec;
            match.address = off;

            return Array.BinarySearch(funcs, match, PdbFunction.byAddress);
        }

        private static void LoadManagedLines(PdbFunction[] funcs,
            IntHashTable names,
            BitAccess bits,
            MsfDirectory dir,
            Dictionary<string, int> nameIndex,
            PdbReader reader,
            uint limit,
            Dictionary<string, PdbSource> sourceCache)
        {
            Array.Sort(funcs, PdbFunction.byAddressAndToken);

            var begin = bits.Position;
            var checks = ReadSourceFileInfo(bits, limit, names, dir, nameIndex, reader, sourceCache);

            // Read the lines next.
            bits.Position = begin;
            while (bits.Position < limit)
            {
                int sig;
                int siz;
                bits.ReadInt32(out sig);
                bits.ReadInt32(out siz);
                var endSym = bits.Position + siz;

                switch ((DEBUG_S_SUBSECTION)sig)
                {
                    case DEBUG_S_SUBSECTION.LINES:
                    {
                        CV_LineSection sec;

                        bits.ReadUInt32(out sec.off);
                        bits.ReadUInt16(out sec.sec);
                        bits.ReadUInt16(out sec.flags);
                        bits.ReadUInt32(out sec.cod);
                        var funcIndex = FindFunction(funcs, sec.sec, sec.off);
                        if (funcIndex < 0) break;
                        var func = funcs[funcIndex];
                        if (func.lines == null)
                            while (funcIndex > 0)
                            {
                                var f = funcs[funcIndex - 1];
                                if (f.lines != null || f.segment != sec.sec || f.address != sec.off) break;
                                func = f;
                                funcIndex--;
                            }
                        else
                            while (funcIndex < funcs.Length - 1 && func.lines != null)
                            {
                                var f = funcs[funcIndex + 1];
                                if (f.segment != sec.sec || f.address != sec.off) break;
                                func = f;
                                funcIndex++;
                            }

                        if (func.lines != null) break;

                        // Count the line blocks.
                        var begSym = bits.Position;
                        var blocks = 0;
                        while (bits.Position < endSym)
                        {
                            CV_SourceFile file;
                            bits.ReadUInt32(out file.index);
                            bits.ReadUInt32(out file.count);
                            bits.ReadUInt32(out file.linsiz); // Size of payload.
                            var linsiz = (int)file.count * (8 + ((sec.flags & 1) != 0 ? 4 : 0));
                            bits.Position += linsiz;
                            blocks++;
                        }

                        func.lines = new PdbLines[blocks];
                        var block = 0;

                        bits.Position = begSym;
                        while (bits.Position < endSym)
                        {
                            CV_SourceFile file;
                            bits.ReadUInt32(out file.index);
                            bits.ReadUInt32(out file.count);
                            bits.ReadUInt32(out file.linsiz); // Size of payload.

                            var src = (PdbSource)checks[(int)file.index];
                            var tmp = new PdbLines(src, file.count);
                            func.lines[block++] = tmp;
                            var lines = tmp.lines;

                            var plin = bits.Position;
                            var pcol = bits.Position + 8 * (int)file.count;

                            for (var i = 0; i < file.count; i++)
                            {
                                CV_Line line;
                                var column = new CV_Column();

                                bits.Position = plin + 8 * i;
                                bits.ReadUInt32(out line.offset);
                                bits.ReadUInt32(out line.flags);

                                var lineBegin = line.flags & (uint)CV_Line_Flags.linenumStart;
                                var delta = (line.flags & (uint)CV_Line_Flags.deltaLineEnd) >> 24;
                                //bool statement = ((line.flags & (uint)CV_Line_Flags.fStatement) == 0);
                                if ((sec.flags & 1) != 0)
                                {
                                    bits.Position = pcol + 4 * i;
                                    bits.ReadUInt16(out column.offColumnStart);
                                    bits.ReadUInt16(out column.offColumnEnd);
                                }

                                lines[i] = new PdbLine(line.offset,
                                    lineBegin,
                                    column.offColumnStart,
                                    lineBegin + delta,
                                    column.offColumnEnd);
                            }
                        }

                        break;
                    }
                }

                bits.Position = endSym;
            }
        }

        private static void LoadFuncsFromDbiModule(BitAccess bits,
            DbiModuleInfo info,
            IntHashTable names,
            ArrayList funcList,
            bool readStrings,
            MsfDirectory dir,
            Dictionary<string, int> nameIndex,
            PdbReader reader,
            Reader ilreader,
            Dictionary<string, PdbSource> sourceCache
        )
        {
            PdbFunction[] funcs = null;

            bits.Position = 0;
            int sig;
            bits.ReadInt32(out sig);
            if (sig != 4) throw new PdbDebugException("Invalid signature. (sig={0})", sig);

            bits.Position = 4;
            // Console.WriteLine("{0}:", info.moduleName);
            funcs = PdbFunction.LoadManagedFunctions( /*info.moduleName,*/
                bits, (uint)info.cbSyms,
                readStrings, ilreader);
            if (funcs != null)
            {
                bits.Position = info.cbSyms + info.cbOldLines;
                LoadManagedLines(funcs, names, bits, dir, nameIndex, reader,
                    (uint)(info.cbSyms + info.cbOldLines + info.cbLines),
                    sourceCache);

                for (var i = 0; i < funcs.Length; i++) funcList.Add(funcs[i]);
            }
        }

        private static void LoadDbiStream(BitAccess bits,
            out DbiModuleInfo[] modules,
            out DbiDbgHdr header,
            bool readStrings)
        {
            var dh = new DbiHeader(bits);
            header = new DbiDbgHdr();

            //if (dh.sig != -1 || dh.ver != 19990903) {
            //  throw new PdbException("Unsupported DBI Stream version, sig={0}, ver={1}",
            //                         dh.sig, dh.ver);
            //}

            // Read gpmod section.
            var modList = new ArrayList();
            var end = bits.Position + dh.gpmodiSize;
            while (bits.Position < end)
            {
                var mod = new DbiModuleInfo(bits, readStrings);
                modList.Add(mod);
            }

            if (bits.Position != end)
                throw new PdbDebugException("Error reading DBI stream, pos={0} != {1}",
                    bits.Position, end);

            if (modList.Count > 0)
                modules = (DbiModuleInfo[])modList.ToArray(typeof(DbiModuleInfo));
            else
                modules = null;

            // Skip the Section Contribution substream.
            bits.Position += dh.secconSize;

            // Skip the Section Map substream.
            bits.Position += dh.secmapSize;

            // Skip the File Info substream.
            bits.Position += dh.filinfSize;

            // Skip the TSM substream.
            bits.Position += dh.tsmapSize;

            // Skip the EC substream.
            bits.Position += dh.ecinfoSize;

            // Read the optional header.
            end = bits.Position + dh.dbghdrSize;
            if (dh.dbghdrSize > 0) header = new DbiDbgHdr(bits);
            bits.Position = end;
        }

        internal static PdbFunction[] LoadFunctions(Stream read,
            out Dictionary<uint, PdbTokenLine> tokenToSourceMapping, out string sourceServerData,
            Reader ilreader)
        {
            tokenToSourceMapping = new Dictionary<uint, PdbTokenLine>();
            var bits = new BitAccess(512 * 1024);
            var head = new PdbFileHeader(read, bits);
            var reader = new PdbReader(read, head.pageSize);
            var dir = new MsfDirectory(reader, head, bits);
            DbiModuleInfo[] modules = null;
            DbiDbgHdr header;
            var sourceCache = new Dictionary<string, PdbSource>();

            dir.streams[1].Read(reader, bits);
            var nameIndex = LoadNameIndex(bits);
            int nameStream;
            if (!nameIndex.TryGetValue("/NAMES", out nameStream)) throw new PdbDebugException("No `name' stream");
            dir.streams[nameStream].Read(reader, bits);
            var names = LoadNameStream(bits);

            int srcsrvStream;
            if (!nameIndex.TryGetValue("SRCSRV", out srcsrvStream))
            {
                sourceServerData = string.Empty;
            }
            else
            {
                var dataStream = dir.streams[srcsrvStream];
                var bytes = new byte[dataStream.contentSize];
                dataStream.Read(reader, bits);
                sourceServerData = bits.ReadBString(bytes.Length);
            }

            dir.streams[3].Read(reader, bits);
            LoadDbiStream(bits, out modules, out header, true);

            var funcList = new ArrayList();

            if (modules != null)
                for (var m = 0; m < modules.Length; m++)
                {
                    var module = modules[m];
                    if (module.stream > 0)
                    {
                        dir.streams[module.stream].Read(reader, bits);
                        if (module.moduleName == "TokenSourceLineInfo")
                        {
                            LoadTokenToSourceInfo(bits, module, names, dir, nameIndex, reader, tokenToSourceMapping,
                                sourceCache);
                            continue;
                        }

                        LoadFuncsFromDbiModule(bits, module, names, funcList, true, dir, nameIndex, reader, ilreader,
                            sourceCache);
                    }
                }

            var funcs = (PdbFunction[])funcList.ToArray(typeof(PdbFunction));

            // After reading the functions, apply the token remapping table if it exists.
            if (header.snTokenRidMap != 0 && header.snTokenRidMap != 0xffff)
            {
                var ridMap = new uint[dir.streams[header.snTokenRidMap].Length / 4];
                dir.streams[header.snTokenRidMap].Read(reader, bits);
                bits.ReadUInt32(ridMap);

                foreach (var func in funcs) func.MapTokens(ridMap, ilreader);
            }
            else
            {
                foreach (var func in funcs) func.MapTokens(null, ilreader);
            }

            //
            Array.Sort(funcs, PdbFunction.byAddressAndToken);
            //Array.Sort(funcs, PdbFunction.byToken);
            return funcs;
        }

        internal static uint[] LoadRemapTable(Stream read)
        {
            //tokenToSourceMapping = new Dictionary<uint, PdbTokenLine>();
            var bits = new BitAccess(512 * 1024);
            var head = new PdbFileHeader(read, bits);
            var reader = new PdbReader(read, head.pageSize);
            var dir = new MsfDirectory(reader, head, bits);
            DbiModuleInfo[] modules = null;
            DbiDbgHdr header;

            dir.streams[1].Read(reader, bits);
            var nameIndex = LoadNameIndex(bits);
            int nameStream;
            if (!nameIndex.TryGetValue("/NAMES", out nameStream)) throw new PdbDebugException("No `name' stream");
            dir.streams[nameStream].Read(reader, bits);
            var names = LoadNameStream(bits);

            int srcsrvStream;
            string sourceServerData;
            if (!nameIndex.TryGetValue("SRCSRV", out srcsrvStream))
            {
                sourceServerData = string.Empty;
            }
            else
            {
                var dataStream = dir.streams[srcsrvStream];
                var bytes = new byte[dataStream.contentSize];
                dataStream.Read(reader, bits);
                sourceServerData = bits.ReadBString(bytes.Length);
            }

            dir.streams[3].Read(reader, bits);
            LoadDbiStream(bits, out modules, out header, true);

#if SKIP_THIS
      ArrayList funcList = new ArrayList();

      if (modules != null)
      {
        for (int m = 0; m < modules.Length; m++)
        {
          var module = modules[m];
          if (module.stream > 0)
          {
            dir.streams[module.stream].Read(reader, bits);
            if (module.moduleName == "TokenSourceLineInfo")
            {
              LoadTokenToSourceInfo(bits, module, names, dir, nameIndex, reader, tokenToSourceMapping);
              continue;
            }
            LoadFuncsFromDbiModule(bits, module, names, funcList, true, dir, nameIndex, reader, ilreader);
          }
        }
      }

      PdbFunction[] funcs = (PdbFunction[])funcList.ToArray(typeof(PdbFunction));
#endif

            // After reading the functions, apply the token remapping table if it exists.
            if (header.snTokenRidMap != 0 && header.snTokenRidMap != 0xffff)
            {
                var ridMap = new uint[dir.streams[header.snTokenRidMap].Length / 4];
                dir.streams[header.snTokenRidMap].Read(reader, bits);
                bits.ReadUInt32(ridMap);
                return ridMap;
            }

            return null;
        }

        private static void LoadTokenToSourceInfo(BitAccess bits, DbiModuleInfo module, IntHashTable names,
            MsfDirectory dir,
            Dictionary<string, int> nameIndex, PdbReader reader, Dictionary<uint, PdbTokenLine> tokenToSourceMapping,
            Dictionary<string, PdbSource> sourceCache)
        {
            bits.Position = 0;
            int sig;
            bits.ReadInt32(out sig);
            if (sig != 4) throw new PdbDebugException("Invalid signature. (sig={0})", sig);

            bits.Position = 4;

            while (bits.Position < module.cbSyms)
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
                        OemSymbol oem;

                        bits.ReadGuid(out oem.idOem);
                        bits.ReadUInt32(out oem.typind);
                        // internal byte[]   rgl;        // user data, force 4-byte alignment

                        if (oem.idOem == PdbFunction.msilMetaData)
                        {
                            var name = bits.ReadString();
                            if (name == "TSLI")
                            {
                                uint token;
                                uint file_id;
                                uint line;
                                uint column;
                                uint endLine;
                                uint endColumn;
                                bits.ReadUInt32(out token);
                                bits.ReadUInt32(out file_id);
                                bits.ReadUInt32(out line);
                                bits.ReadUInt32(out column);
                                bits.ReadUInt32(out endLine);
                                bits.ReadUInt32(out endColumn);
                                PdbTokenLine tokenLine;
                                if (!tokenToSourceMapping.TryGetValue(token, out tokenLine))
                                {
                                    tokenToSourceMapping.Add(token,
                                        new PdbTokenLine(token, file_id, line, column, endLine, endColumn));
                                }
                                else
                                {
                                    while (tokenLine.nextLine != null) tokenLine = tokenLine.nextLine;
                                    tokenLine.nextLine =
                                        new PdbTokenLine(token, file_id, line, column, endLine, endColumn);
                                }
                            }

                            bits.Position = stop;
                            break;
                        }

                        throw new PdbDebugException("OEM section: guid={0} ti={1}",
                            oem.idOem, oem.typind);
                        // bits.Position = stop;

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

            bits.Position = module.cbSyms + module.cbOldLines;
            var limit = module.cbSyms + module.cbOldLines + module.cbLines;
            var sourceFiles = ReadSourceFileInfo(bits, (uint)limit, names, dir, nameIndex, reader, sourceCache);
            foreach (var tokenLine in tokenToSourceMapping.Values)
                tokenLine.sourceFile = (PdbSource)sourceFiles[(int)tokenLine.file_id];
        }

        private static IntHashTable ReadSourceFileInfo(BitAccess bits, uint limit, IntHashTable names, MsfDirectory dir,
            Dictionary<string, int> nameIndex, PdbReader reader, Dictionary<string, PdbSource> sourceCache)
        {
            var checks = new IntHashTable();

            var begin = bits.Position;
            while (bits.Position < limit)
            {
                int sig;
                int siz;
                bits.ReadInt32(out sig);
                bits.ReadInt32(out siz);
                var place = bits.Position;
                var endSym = bits.Position + siz;

                switch ((DEBUG_S_SUBSECTION)sig)
                {
                    case DEBUG_S_SUBSECTION.FILECHKSMS:
                        while (bits.Position < endSym)
                        {
                            CV_FileCheckSum chk;

                            var ni = bits.Position - place;
                            bits.ReadUInt32(out chk.name);
                            bits.ReadUInt8(out chk.len);
                            bits.ReadUInt8(out chk.type);

                            var name = (string)names[(int)chk.name];

                            PdbSource src;
                            if (!sourceCache.TryGetValue(name, out src))
                            {
                                int guidStream;
                                var doctypeGuid = SymDocumentType.Text;
                                var languageGuid = Guid.Empty;
                                var vendorGuid = Guid.Empty;
                                if (nameIndex.TryGetValue("/SRC/FILES/" + name.ToUpperInvariant(), out guidStream))
                                {
                                    var guidBits = new BitAccess(0x100);
                                    dir.streams[guidStream].Read(reader, guidBits);
                                    LoadGuidStream(guidBits, out doctypeGuid, out languageGuid, out vendorGuid);
                                }

                                src = new PdbSource( /*(uint)ni,*/ name, doctypeGuid, languageGuid, vendorGuid);
                                sourceCache.Add(name, src);
                            }

                            checks.Add(ni, src);
                            bits.Position += chk.len;
                            bits.Align(4);
                        }

                        bits.Position = endSym;
                        break;

                    default:
                        bits.Position = endSym;
                        break;
                }
            }

            return checks;
        }

        internal static Dictionary<uint, PdbFunction> LoadFunctionMap(FileStream inputStream,
            out Dictionary<uint, PdbTokenLine> tokenToSourceMapping, out string sourceServerData, Reader reader)
        {
            var funcs = LoadFunctions(inputStream, out tokenToSourceMapping, out sourceServerData, reader);
            var result = new Dictionary<uint, PdbFunction>(funcs.Length + 10);
            foreach (var pdbFunction in funcs)
                result[pdbFunction.token] = pdbFunction;

            return result;
        }
    }
}