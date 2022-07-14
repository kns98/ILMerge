// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
#if CCINamespace
using Microsoft.Cci.Metadata;
#else
#endif

#if CCINamespace
namespace Microsoft.Cci{
#else
namespace System.Compiler
{
#endif
#if !(FxCop || NoWriter)
    /// <summary>
    ///     High performance replacement for System.IO.BinaryWriter.
    /// </summary>
    public sealed class BinaryWriter
    {
        public MemoryStream /*!*/
            BaseStream;

        private readonly bool UTF8 = true;

        public BinaryWriter(MemoryStream /*!*/ output)
        {
            BaseStream = output;
            //^ base();
        }

        public BinaryWriter(MemoryStream output, bool unicode)
        {
            BaseStream = output;
            UTF8 = !unicode;
        }

        public BinaryWriter(MemoryStream /*!*/ output, Encoding /*!*/ encoding)
        {
            Debug.Assert(encoding == Encoding.Unicode);
            BaseStream = output;
            UTF8 = false;
            //^ base();
        }

        public void Align(uint alignment)
        {
            var m = BaseStream;
            var i = m.Position;
            while (i % alignment > 0)
            {
                m.Buffer[i++] = 0;
                m.Position = i;
            }
        }


        public void Write(bool value)
        {
            var m = BaseStream;
            var i = m.Position;
            m.Position = i + 1;
            m.Buffer[i] = (byte)(value ? 1 : 0);
        }

        public void Write(byte value)
        {
            var m = BaseStream;
            var i = m.Position;
            m.Position = i + 1;
            m.Buffer[i] = value;
        }

        public void Write(sbyte value)
        {
            var m = BaseStream;
            var i = m.Position;
            m.Position = i + 1;
            m.Buffer[i] = (byte)value;
        }

        public void Write(byte[] buffer)
        {
            if (buffer == null) return;
            BaseStream.Write(buffer, 0, buffer.Length);
        }

        public void Write(char ch)
        {
            var m = BaseStream;
            var i = m.Position;
            if (UTF8)
            {
                if (ch < 0x80)
                {
                    m.Position = i + 1;
                    m.Buffer[i] = (byte)ch;
                }
                else
                {
                    Write(new[] { ch });
                }
            }
            else
            {
                m.Position = i + 2;
                var buffer = m.Buffer;
                buffer[i++] = (byte)ch;
                buffer[i] = (byte)(ch >> 8);
            }
        }

        public void Write(char[] chars)
        {
            if (chars == null) return;
            var m = BaseStream;
            var n = chars.Length;
            var i = m.Position;
            if (UTF8)
            {
                m.Position = i + n;
                var buffer = m.Buffer;
                for (var j = 0; j < n; j++)
                {
                    var ch = chars[j];
                    if ((ch & 0x80) != 0) goto writeUTF8;
                    buffer[i++] = (byte)ch;
                }

                return;
                writeUTF8:
                var ch32 = 0;
                for (var j = n - (m.Position - i); j < n; j++)
                {
                    var ch = chars[j];
                    if (ch < 0x80)
                    {
                        m.Position = i + 1;
                        buffer = m.Buffer;
                        buffer[i++] = (byte)ch;
                    }
                    else if (ch < 0x800)
                    {
                        m.Position = i + 2;
                        buffer = m.Buffer;
                        buffer[i++] = (byte)(((ch >> 6) & 0x1F) | 0xC0);
                        buffer[i] = (byte)((ch & 0x3F) | 0x80);
                    }
                    else if (0xD800 <= ch && ch <= 0xDBFF)
                    {
                        ch32 = (ch & 0x3FF) << 10;
                    }
                    else if (0xDC00 <= ch && ch <= 0xDFFF)
                    {
                        ch32 |= ch & 0x3FF;
                        m.Position = i + 4;
                        buffer = m.Buffer;
                        buffer[i++] = (byte)(((ch32 >> 18) & 0x7) | 0xF0);
                        buffer[i++] = (byte)(((ch32 >> 12) & 0x3F) | 0x80);
                        buffer[i++] = (byte)(((ch32 >> 6) & 0x3F) | 0x80);
                        buffer[i] = (byte)((ch32 & 0x3F) | 0x80);
                    }
                    else
                    {
                        m.Position = i + 3;
                        buffer = m.Buffer;
                        buffer[i++] = (byte)(((ch >> 12) & 0xF) | 0xE0);
                        buffer[i++] = (byte)(((ch >> 6) & 0x3F) | 0x80);
                        buffer[i] = (byte)((ch & 0x3F) | 0x80);
                    }
                }
            }
            else
            {
                m.Position = i + n * 2;
                var buffer = m.Buffer;
                for (var j = 0; j < n; j++)
                {
                    var ch = chars[j];
                    buffer[i++] = (byte)ch;
                    buffer[i++] = (byte)(ch >> 8);
                }
            }
        }

        public unsafe void Write(double value)
        {
            var m = BaseStream;
            var i = m.Position;
            m.Position = i + 8;
            fixed (byte* b = m.Buffer)
            {
                *(double*)(b + i) = value;
            }
        }

        public void Write(short value)
        {
            var m = BaseStream;
            var i = m.Position;
            m.Position = i + 2;
            var buffer = m.Buffer;
            buffer[i++] = (byte)value;
            buffer[i] = (byte)(value >> 8);
        }

        public void Write(ushort value)
        {
            var m = BaseStream;
            var i = m.Position;
            m.Position = i + 2;
            var buffer = m.Buffer;
            buffer[i++] = (byte)value;
            buffer[i] = (byte)(value >> 8);
        }

        public void Write(int value)
        {
            var m = BaseStream;
            var i = m.Position;
            m.Position = i + 4;
            var buffer = m.Buffer;
            buffer[i++] = (byte)value;
            buffer[i++] = (byte)(value >> 8);
            buffer[i++] = (byte)(value >> 16);
            buffer[i] = (byte)(value >> 24);
        }

        public void Write(uint value)
        {
            var m = BaseStream;
            var i = m.Position;
            m.Position = i + 4;
            var buffer = m.Buffer;
            buffer[i++] = (byte)value;
            buffer[i++] = (byte)(value >> 8);
            buffer[i++] = (byte)(value >> 16);
            buffer[i] = (byte)(value >> 24);
        }

        public void Write(long value)
        {
            var m = BaseStream;
            var i = m.Position;
            m.Position = i + 8;
            var buffer = m.Buffer;
            var lo = (uint)value;
            var hi = (uint)(value >> 32);
            buffer[i++] = (byte)lo;
            buffer[i++] = (byte)(lo >> 8);
            buffer[i++] = (byte)(lo >> 16);
            buffer[i++] = (byte)(lo >> 24);
            buffer[i++] = (byte)hi;
            buffer[i++] = (byte)(hi >> 8);
            buffer[i++] = (byte)(hi >> 16);
            buffer[i] = (byte)(hi >> 24);
        }

        public void Write(ulong value)
        {
            var m = BaseStream;
            var i = m.Position;
            m.Position = i + 8;
            var buffer = m.Buffer;
            var lo = (uint)value;
            var hi = (uint)(value >> 32);
            buffer[i++] = (byte)lo;
            buffer[i++] = (byte)(lo >> 8);
            buffer[i++] = (byte)(lo >> 16);
            buffer[i++] = (byte)(lo >> 24);
            buffer[i++] = (byte)hi;
            buffer[i++] = (byte)(hi >> 8);
            buffer[i++] = (byte)(hi >> 16);
            buffer[i] = (byte)(hi >> 24);
        }

        public unsafe void Write(float value)
        {
            var m = BaseStream;
            var i = m.Position;
            m.Position = i + 4;
            fixed (byte* b = m.Buffer)
            {
                *(float*)(b + i) = value;
            }
        }

        public void Write(string str)
        {
            Write(str, false);
        }

        public void Write(string str, bool emitNullTerminator)
        {
            if (str == null)
            {
                Debug.Assert(!emitNullTerminator);
                Write((byte)0xff);
                return;
            }

            var n = str.Length;
            if (!emitNullTerminator)
            {
                if (UTF8)
                    Ir2md.WriteCompressedInt(this, GetUTF8ByteCount(str));
                else
                    Ir2md.WriteCompressedInt(this, n * 2);
            }

            var m = BaseStream;
            var i = m.Position;
            if (UTF8)
            {
                m.Position = i + n;
                var buffer = m.Buffer;
                for (var j = 0; j < n; j++)
                {
                    var ch = str[j];
                    if (ch >= 0x80) goto writeUTF8;
                    buffer[i++] = (byte)ch;
                }

                if (emitNullTerminator)
                {
                    m.Position = i + 1;
                    buffer = m.Buffer;
                    buffer[i] = 0;
                }

                return;
                writeUTF8:
                var ch32 = 0;
                for (var j = n - (m.Position - i); j < n; j++)
                {
                    var ch = str[j];
                    if (ch < 0x80)
                    {
                        m.Position = i + 1;
                        buffer = m.Buffer;
                        buffer[i++] = (byte)ch;
                    }
                    else if (ch < 0x800)
                    {
                        m.Position = i + 2;
                        buffer = m.Buffer;
                        buffer[i++] = (byte)(((ch >> 6) & 0x1F) | 0xC0);
                        buffer[i++] = (byte)((ch & 0x3F) | 0x80);
                    }
                    else if (0xD800 <= ch && ch <= 0xDBFF)
                    {
                        ch32 = (ch & 0x3FF) << 10;
                    }
                    else if (0xDC00 <= ch && ch <= 0xDFFF)
                    {
                        ch32 |= ch & 0x3FF;
                        m.Position = i + 4;
                        buffer = m.Buffer;
                        buffer[i++] = (byte)(((ch32 >> 18) & 0x7) | 0xF0);
                        buffer[i++] = (byte)(((ch32 >> 12) & 0x3F) | 0x80);
                        buffer[i++] = (byte)(((ch32 >> 6) & 0x3F) | 0x80);
                        buffer[i++] = (byte)((ch32 & 0x3F) | 0x80);
                    }
                    else
                    {
                        m.Position = i + 3;
                        buffer = m.Buffer;
                        buffer[i++] = (byte)(((ch >> 12) & 0xF) | 0xE0);
                        buffer[i++] = (byte)(((ch >> 6) & 0x3F) | 0x80);
                        buffer[i++] = (byte)((ch & 0x3F) | 0x80);
                    }
                }

                if (emitNullTerminator)
                {
                    m.Position = i + 1;
                    buffer = m.Buffer;
                    buffer[i] = 0;
                }
            }
            else
            {
                m.Position = i + n * 2;
                var buffer = m.Buffer;
                for (var j = 0; j < n; j++)
                {
                    var ch = str[j];
                    buffer[i++] = (byte)ch;
                    buffer[i++] = (byte)(ch >> 8);
                }

                if (emitNullTerminator)
                {
                    m.Position = i + 2;
                    buffer = m.Buffer;
                    buffer[i++] = 0;
                    buffer[i] = 0;
                }
            }
        }

        public static int GetUTF8ByteCount(string str)
        {
            var count = 0;
            for (int i = 0, n = str == null ? 0 : str.Length; i < n; i++)
            {
                //^ assume str != null;
                var ch = str[i];
                if (ch < 0x80)
                    count += 1;
                else if (ch < 0x800)
                    count += 2;
                else if (0xD800 <= ch && ch <= 0xDBFF)
                    count += 2;
                else if (0xDC00 <= ch && ch <= 0xDFFF)
                    count += 2;
                else
                    count += 3;
            }

            return count;
        }
    }

    public sealed class MemoryStream
    {
        public byte[] /* ! */
            Buffer;

        public int Length;
        public int position;

        public MemoryStream()
            : this(64)
        {
        }

        public MemoryStream(int capacity)
        {
            Buffer = new byte[capacity];
            Length = 0;
            position = 0;
            //^ base();
        }

        public MemoryStream(byte[] /*!*/ bytes)
        {
            if (bytes == null) Debug.Fail("");
            Buffer = bytes;
            Length = bytes.Length;
            position = 0;
            //^ base();
        }

        public int Position
        {
            get { return position; }
            set
            {
                var myBuffer = Buffer;
                var n = myBuffer.Length;
                if (value >= n) Grow(myBuffer, n, value);
                if (value > Length) Length = value;
                position = value;
            }
        }

        private void Grow(byte[] /*!*/ myBuffer, int n, int m)
        {
            if (myBuffer == null)
            {
                Debug.Fail("");
                return;
            }

            var n2 = n * 2;
            while (m >= n2) n2 = n2 * 2;
            var newBuffer = Buffer = new byte[n2];
            for (var i = 0; i < n; i++)
                newBuffer[i] = myBuffer[i]; //TODO: optimize this
        }

        public void Seek(long offset, SeekOrigin loc)
        {
            Contract.Assume(loc == SeekOrigin.Begin);
            Contract.Assume(offset <= int.MaxValue);
            Position = (int)offset;
        }

        public byte[] /*!*/ ToArray()
        {
            var n = Length;
            var source = Buffer;
            if (source.Length == n) return Buffer; //unlikely, but the check is cheap
            var result = new byte[n];
            for (var i = 0; i < n; i++)
                result[i] = source[i]; //TODO: optimize this
            return result;
        }

        public void Write(byte[] /*!*/ buffer, int index, int count)
        {
            var p = position;
            Position = p + count;
            var myBuffer = Buffer;
            for (int i = 0, j = p, k = index; i < count; i++)
                myBuffer[j++] = buffer[k++]; //TODO: optimize this
        }

        public void WriteTo(MemoryStream /*!*/ stream)
        {
            stream.Write(Buffer, 0, Length);
        }

        public void WriteTo(Stream /*!*/ stream)
        {
            stream.Write(Buffer, 0, Length);
        }
    }

    public enum SeekOrigin
    {
        Begin,
        Current,
        End
    }
#endif
    /// <summary>
    ///     A version of System.IO.Path that does not throw exceptions.
    /// </summary>
#if FxCop || NoWriter
  internal sealed class BetterPath {
#else
    public sealed class BetterPath
    {
#endif
        public static readonly char AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar;
        public static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;
        public static readonly char VolumeSeparatorChar = Path.VolumeSeparatorChar;

        public static string ChangeExtension(string path, string extension)
        {
            if (path == null) return null;
            var text1 = path;
            var num1 = path.Length;
            while (--num1 >= 0)
            {
                var ch1 = path[num1];
                if (ch1 == '.')
                {
                    text1 = path.Substring(0, num1);
                    break;
                }

                if (ch1 == DirectorySeparatorChar || ch1 == AltDirectorySeparatorChar || ch1 == VolumeSeparatorChar)
                    break;
            }

            if (extension == null || path.Length == 0) return text1;
            if (extension.Length == 0 || extension[0] != '.')
                text1 = text1 + ".";
            return text1 + extension;
        }

        public static string Combine(string path1, string path2)
        {
            if (path1 == null || path1.Length == 0) return path2;
            if (path2 == null || path2.Length == 0) return path1;
            var ch = path2[0];
            if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar ||
                (path2.Length >= 2 && path2[1] == VolumeSeparatorChar))
                return path2;
            ch = path1[path1.Length - 1];
            if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar)
                return path1 + DirectorySeparatorChar + path2;
            return path1 + path2;
        }

        public static string GetExtension(string path)
        {
            if (path == null) return null;
            var length = path.Length;
            for (var i = length; --i >= 0;)
            {
                var ch = path[i];
                if (ch == '.')
                {
                    if (i != length - 1)
                        return path.Substring(i, length - i);
                    return string.Empty;
                }

                if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                    break;
            }

            return string.Empty;
        }

        public static string GetFileName(string path)
        {
            if (path == null) return null;
            var length = path.Length;
            for (var i = length; --i >= 0;)
            {
                var ch = path[i];
                if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                    return path.Substring(i + 1);
            }

            return path;
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            int num1;
            path = GetFileName(path);
            if (path == null) return null;
            if ((num1 = path.LastIndexOf('.')) == -1) return path;
            return path.Substring(0, num1);
        }

        public static string GetDirectoryName(string path)
        {
            if (path == null) return null;
            var length = path.Length;
            for (var i = length; --i >= 0;)
            {
                var ch = path[i];
                if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                    return path.Substring(0, i);
            }

            return path;
        }

        public static char[] GetInvalidFileNameChars()
        {
#if WHIDBEY
            return Path.GetInvalidFileNameChars();
#else
      return System.IO.Path.InvalidPathChars;
#endif
        }

        public static char[] GetInvalidPathChars()
        {
#if WHIDBEY
            return Path.GetInvalidPathChars();
#else
      return System.IO.Path.InvalidPathChars;
#endif
        }

        public static string GetTempFileName()
        {
            return Path.GetTempFileName();
        }

        public static bool HasExtension(string path)
        {
            if (path != null)
            {
                var num1 = path.Length;
                while (--num1 >= 0)
                {
                    var ch1 = path[num1];
                    if (ch1 == '.')
                    {
                        if (num1 != path.Length - 1) return true;
                        return false;
                    }

                    if (ch1 == DirectorySeparatorChar || ch1 == AltDirectorySeparatorChar ||
                        ch1 == VolumeSeparatorChar) break;
                }
            }

            return false;
        }
    }
}