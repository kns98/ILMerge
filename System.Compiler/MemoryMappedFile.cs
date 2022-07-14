// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
#if CCINamespace
using Microsoft.Cci;
#else
#endif

#if CCINamespace
namespace Microsoft.Cci.Metadata{
#else
namespace System.Compiler.Metadata
{
#endif
    internal sealed unsafe class MemoryCursor
    {
        internal readonly int Length;
        private readonly byte* buffer;
        private byte* pb;

#if !ROTOR
        internal MemoryCursor(MemoryMappedFile /*!*/ memoryMap)
            : this(memoryMap.Buffer, memoryMap.Length)
        {
        }
#endif
        internal MemoryCursor(byte* buffer, int length, int position)
        {
            this.buffer = buffer;
            pb = buffer + position;
            Length = length;
        }

        internal MemoryCursor(byte* buffer, int length)
            : this(buffer, length, 0)
        {
        }

        internal MemoryCursor(MemoryCursor /*!*/ c)
        {
            buffer = c.buffer;
            pb = c.pb;
            Length = c.Length;
        }

        internal int Position
        {
            get { return (int)(pb - buffer); }
            set { pb = buffer + value; }
        }

        internal byte* GetBuffer()
        {
            return buffer;
        }

        internal void Align(int size)
        {
            Contract.Requires(size == 2 || size == 4 || size == 8 || size == 16 || size == 32 || size == 64);
            var remainder = Position & (size - 1);
            if (remainder != 0)
                pb += size - remainder;
        }

        //internal System.Char Char(int i){ return *(System.Char*)(this.pb+i*sizeof(System.Char)); }
        //internal System.SByte SByte(int i){ return *(System.SByte*)(this.pb+i*sizeof(System.SByte)); }
        internal short Int16(int i)
        {
            return *(short*)(pb + i * sizeof(short));
        }

        internal int Int32(int i)
        {
            return *(int*)(pb + i * sizeof(int));
        }

        //internal System.Int64 Int64(int i){ return *(System.Int64*)(this.pb+i*sizeof(System.Int64)); }
        internal byte Byte(int i)
        {
            return *(pb + i * sizeof(byte));
        }

        internal ushort UInt16(int i)
        {
            return *(ushort*)(pb + i * sizeof(ushort));
        }
        //internal System.UInt32 UInt32(int i){ return *(System.UInt32*)(this.pb+i*sizeof(System.UInt32)); }
        //internal System.UInt64 UInt64(int i){ return *(System.UInt64*)(this.pb+i*sizeof(System.UInt64)); }
        //internal System.Boolean Boolean(int i){ return *(System.Boolean*)(this.pb+i*sizeof(System.Boolean)); }
        //internal System.Single Single(int i){ return *(System.Single*)(this.pb+i*sizeof(System.Single)); }
        //internal System.Double Double(int i){ return *(System.Double*)(this.pb+i*sizeof(System.Double)); }

        //internal void SkipChar(int c){ this.pb += c*sizeof(System.Char); }
        //internal void SkipSByte(int c){ this.pb += c*sizeof(System.SByte); }
        internal void SkipInt16(int c)
        {
            pb += c * sizeof(short);
        }

        internal void SkipInt32(int c)
        {
            pb += c * sizeof(int);
        }

        //internal void SkipInt64(int c){ this.pb += c*sizeof(System.Int64); }
        internal void SkipByte(int c)
        {
            pb += c * sizeof(byte);
        }

        internal void SkipUInt16(int c)
        {
            pb += c * sizeof(ushort);
        }
        //internal void SkipUInt32(int c){ this.pb += c*sizeof(System.UInt32); }
        //internal void SkipUInt64(int c){ this.pb += c*sizeof(System.UInt64); }
        //internal void SkipBoolean(int c){ this.pb += c*sizeof(System.Boolean); }
        //internal void SkipSingle(int c){ this.pb += c*sizeof(System.Single); }
        //internal void SkipDouble(int c){ this.pb += c*sizeof(System.Double); }

        internal char ReadChar()
        {
            var pb = this.pb;
            var v = *(char*)pb;
            this.pb = pb + sizeof(char);
            return v;
        }

        internal sbyte ReadSByte()
        {
            var pb = this.pb;
            var v = *(sbyte*)pb;
            this.pb = pb + sizeof(sbyte);
            return v;
        }

        internal short ReadInt16()
        {
            var pb = this.pb;
            var v = *(short*)pb;
            this.pb = pb + sizeof(short);
            return v;
        }

        internal int ReadInt32()
        {
            var pb = this.pb;
            var v = *(int*)pb;
            this.pb = pb + sizeof(int);
            return v;
        }

        internal long ReadInt64()
        {
            var pb = this.pb;
            var v = *(long*)pb;
            this.pb = pb + sizeof(long);
            return v;
        }

        internal byte ReadByte()
        {
            var pb = this.pb;
            var v = *pb;
            this.pb = pb + sizeof(byte);
            return v;
        }

        internal ushort ReadUInt16()
        {
            var pb = this.pb;
            var v = *(ushort*)pb;
            this.pb = pb + sizeof(ushort);
            return v;
        }

        internal uint ReadUInt32()
        {
            var pb = this.pb;
            var v = *(uint*)pb;
            this.pb = pb + sizeof(uint);
            return v;
        }

        internal ulong ReadUInt64()
        {
            var pb = this.pb;
            var v = *(ulong*)pb;
            this.pb = pb + sizeof(ulong);
            return v;
        }

        internal bool ReadBoolean()
        {
            var pb = this.pb;
            var v = *(bool*)pb;
            this.pb = pb + sizeof(bool);
            return v;
        }

        internal float ReadSingle()
        {
            var pb = this.pb;
            var v = *(float*)pb;
            this.pb = pb + sizeof(float);
            return v;
        }

        internal double ReadDouble()
        {
            var pb = this.pb;
            var v = *(double*)pb;
            this.pb = pb + sizeof(double);
            return v;
        }

        internal int ReadReference(int refSize)
        {
            if (refSize == 2) return ReadUInt16();
            return ReadInt32();
        }

        internal int ReadCompressedInt()
        {
            var headerByte = ReadByte();
            int result;
            if ((headerByte & 0x80) == 0x00)
                result = headerByte;
            else if ((headerByte & 0x40) == 0x00)
                result = ((headerByte & 0x3f) << 8) | ReadByte();
            else if (headerByte == 0xFF)
                result = -1;
            else
                result = ((headerByte & 0x3f) << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte();
            return result;
        }

        internal byte[] /*!*/ ReadBytes(int c)
        {
            var result = new byte[c];
            var pb = this.pb;
            for (var i = 0; i < c; i++)
                result[i] = *pb++;
            this.pb = pb;
            return result;
        }

        internal Identifier /*!*/ ReadIdentifierFromSerString()
        {
            var pb = this.pb;
            var headerByte = *pb++;
            uint length = 0;
            if ((headerByte & 0x80) == 0x00)
                length = headerByte;
            else if ((headerByte & 0x40) == 0x00)
                length = (uint)((headerByte & 0x3f) << 8) | *pb++;
            else
                length = (uint)((headerByte & 0x3f) << 24) | (uint)(*pb++ << 16) | (uint)(*pb++ << 8) | *pb++;
            this.pb = pb + length;
            return Identifier.For(pb, length /*, this.KeepAlive*/);
        }

        internal string /*!*/ ReadUTF8(int bytesToRead)
        {
            var buffer = new char[bytesToRead];
            var pb = this.pb;
            this.pb += bytesToRead;
            var j = 0;
            while (bytesToRead > 0)
            {
                var b = *pb++;
                bytesToRead--;
                if ((b & 0x80) == 0 || bytesToRead == 0)
                {
                    buffer[j++] = (char)b;
                    continue;
                }

                char ch;
                var b1 = *pb++;
                bytesToRead--;
                if ((b & 0x20) == 0)
                {
                    ch = (char)(((b & 0x1F) << 6) | (b1 & 0x3F));
                }
                else
                {
                    if (bytesToRead == 0)
                    {
                        //Dangling lead bytes, do not decompose
                        buffer[j++] = (char)((b << 8) | b1);
                        break;
                    }

                    var b2 = *pb++;
                    bytesToRead--;
                    uint ch32;
                    if ((b & 0x10) == 0)
                    {
                        ch32 = (uint)(((b & 0x0F) << 12) | ((b1 & 0x3F) << 6) | (b2 & 0x3F));
                    }
                    else
                    {
                        if (bytesToRead == 0)
                        {
                            //Dangling lead bytes, do not decompose
                            buffer[j++] = (char)((b << 8) | b1);
                            buffer[j++] = (char)b2;
                            break;
                        }

                        var b3 = *pb++;
                        bytesToRead--;
                        ch32 = (uint)(((b & 0x07) << 18) | ((b1 & 0x3F) << 12) | ((b2 & 0x3F) << 6) | (b3 & 0x3F));
                    }

                    if ((ch32 & 0xFFFF0000) == 0)
                    {
                        ch = (char)ch32;
                    }
                    else
                    {
                        //break up into UTF16 surrogate pair
                        buffer[j++] = (char)((ch32 >> 10) | 0xD800);
                        ch = (char)((ch32 & 0x3FF) | 0xDC00);
                    }
                }

                buffer[j++] = ch;
            }

            if (j > 0 && buffer[j - 1] == 0) j--;
            return new string(buffer, 0, j);
        }

        internal string /*!*/ ReadUTF8()
        {
            var pb = this.pb;
            var sb = new StringBuilder();
            byte b = 0;
            for (;;)
            {
                b = *pb++;
                if (b == 0) break;
                if ((b & 0x80) == 0)
                {
                    sb.Append((char)b);
                    continue;
                }

                char ch;
                var b1 = *pb++;
                if (b1 == 0)
                {
                    //Dangling lead byte, do not decompose
                    sb.Append((char)b);
                    break;
                }

                if ((b & 0x20) == 0)
                {
                    ch = (char)(((b & 0x1F) << 6) | (b1 & 0x3F));
                }
                else
                {
                    var b2 = *pb++;
                    if (b2 == 0)
                    {
                        //Dangling lead bytes, do not decompose
                        sb.Append((char)((b << 8) | b1));
                        break;
                    }

                    uint ch32;
                    if ((b & 0x10) == 0)
                    {
                        ch32 = (uint)(((b & 0x0F) << 12) | ((b1 & 0x3F) << 6) | (b2 & 0x3F));
                    }
                    else
                    {
                        var b3 = *pb++;
                        if (b3 == 0)
                        {
                            //Dangling lead bytes, do not decompose
                            sb.Append((char)((b << 8) | b1));
                            sb.Append((char)b2);
                            break;
                        }

                        ch32 = (uint)(((b & 0x07) << 18) | ((b1 & 0x3F) << 12) | ((b2 & 0x3F) << 6) | (b3 & 0x3F));
                    }

                    if ((ch32 & 0xFFFF0000) == 0)
                    {
                        ch = (char)ch32;
                    }
                    else
                    {
                        //break up into UTF16 surrogate pair
                        sb.Append((char)((ch32 >> 10) | 0xD800));
                        ch = (char)((ch32 & 0x3FF) | 0xDC00);
                    }
                }

                sb.Append(ch);
            }

            this.pb = pb;
            return sb.ToString();
        }

        internal string /*!*/ ReadUTF16(int charsToRead)
        {
            var pc = (char*)pb;
            var buffer = new char[charsToRead];
            for (var i = 0; i < charsToRead; i++)
                buffer[i] = *pc++;
            pb = (byte*)pc;
            return new string(buffer, 0, charsToRead);
        }

        internal string /*!*/ ReadUTF16()
        {
            var result = new string((char*)pb);
            pb += (result.Length + 1) * 2;
            return result;
        }

        internal string /*!*/ ReadASCII(int bytesToRead)
        {
            var c = bytesToRead;
            if (bytesToRead == -1) c = 128; //buffer size
            var pb = this.pb;
            var buffer = new char[c];
            var j = 0;
            byte b = 0;
            Restart:
            while (j < c)
            {
                b = *pb++;
                if (b == 0) break;
                buffer[j++] = (char)b;
            }

            if (bytesToRead == -1)
            {
                if (b != 0)
                {
                    var newBuffer = new char[c *= 2];
                    for (var copy = 0; copy < j; copy++)
                        newBuffer[copy] = buffer[copy];
                    buffer = newBuffer;
                    goto Restart;
                }

                this.pb = pb;
            }
            else
            {
                this.pb += bytesToRead;
            }

            return new string(buffer, 0, j);
        }

        internal string /*!*/ ReadASCII()
        {
            return ReadASCII(-1);
        }
    }

#if !ROTOR
#if !FxCop
    /// <summary>
    ///     Public only for use by the Framework. Do not use this class.
    ///     Well, if you really really must, use it only if you can tolerate keeping the file locked for at least as long as
    ///     any Identifier
    ///     derived from the file stays alive.
    /// </summary>
    public sealed unsafe class MemoryMappedFile : IDisposable, ISourceTextBuffer
    {
#else
  unsafe sealed class MemoryMappedFile : IDisposable{
#endif
        private byte* buffer;
        private int length;

        public MemoryMappedFile(string fileName)
        {
            OpenMap(fileName);
        }

        ~MemoryMappedFile()
        {
            CloseMap();
        }

        public void Dispose()
        {
            CloseMap();
            GC.SuppressFinalize(this);
        }

        public byte* Buffer
        {
            get
            {
                Debug.Assert(buffer != null);
                return buffer;
            }
        }

        public int Length
        {
            get
            {
                Debug.Assert(buffer != null);
                return length;
            }
        }
#if !FxCop
        string ISourceText.Substring(int start, int length)
        {
            Debug.Assert(false, "Can't use Substring on memory mapped files");
            return null;
        }

        char ISourceText.this[int index]
        {
            get
            {
                Debug.Assert(false, "Can't access memory mapped files via an indexer, use Buffer");
                return ' ';
            }
        }
#endif
        private void OpenMap(string filename)
        {
            IntPtr hmap;
            int length;
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > int.MaxValue)
                    throw new FileLoadException(ExceptionStrings.FileTooBig, filename);
                length = unchecked((int)stream.Length);
#if WHIDBEY && !OldWhidbey
                hmap = CreateFileMapping(stream.SafeFileHandle.DangerousGetHandle(), IntPtr.Zero,
                    PageAccess.PAGE_READONLY, 0, length, null);
#else
        hmap = CreateFileMapping(stream.Handle, IntPtr.Zero, PageAccess.PAGE_READONLY, 0, length, null);
#endif
                if (hmap == IntPtr.Zero)
                {
                    var rc = Marshal.GetLastWin32Error();
                    throw new FileLoadException(string.Format(CultureInfo.CurrentCulture,
                        ExceptionStrings.CreateFileMappingReturnedErrorCode, rc.ToString()), filename);
                }
            }

            buffer = (byte*)MapViewOfFile(hmap, FileMapAccess.FILE_MAP_READ, 0, 0, (IntPtr)length);
            CloseHandle(hmap);
            if (buffer == null)
            {
                var rc = Marshal.GetLastWin32Error();
                throw new FileLoadException(string.Format(CultureInfo.CurrentCulture,
                    ExceptionStrings.MapViewOfFileReturnedErrorCode, rc.ToString()), filename);
            }

            this.length = length;
        }

        private void CloseMap()
        {
            if (buffer != null)
            {
                UnmapViewOfFile(buffer);
                buffer = null;
            }
        }
#if !FxCop
        void ISourceText.MakeCollectible()
        {
        }
#endif

        private enum PageAccess
        {
            PAGE_READONLY = 0x02
        }

        private enum FileMapAccess
        {
            FILE_MAP_READ = 0x0004
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false,
            ThrowOnUnmappableChar = true)]
        private static extern IntPtr CreateFileMapping(
            IntPtr hFile, // handle to file
            IntPtr lpAttributes, // security
            PageAccess flProtect, // protection
            int dwMaximumSizeHigh, // high-order DWORD of size
            int dwMaximumSizeLow, // low-order DWORD of size
            string lpName // object name
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void* MapViewOfFile(
            IntPtr hFileMappingObject, // handle to file-mapping object
            FileMapAccess dwDesiredAccess, // access mode
            int dwFileOffsetHigh, // high-order DWORD of offset
            int dwFileOffsetLow, // low-order DWORD of offset
            IntPtr dwNumberOfBytesToMap // number of bytes to map
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnmapViewOfFile(
            void* lpBaseAddress // starting address
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(
            IntPtr hObject // handle to object
        );
    }
#endif
}