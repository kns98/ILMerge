// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System;
using System.IO;
using System.Text;

namespace Microsoft.Cci.Pdb
{
    internal class BitAccess
    {
        internal BitAccess(byte[] buffer)
        {
            this.Buffer = buffer;
            Position = 0;
        }

        internal BitAccess(int capacity)
        {
            Buffer = new byte[capacity];
        }

        internal byte[] Buffer { get; private set; }

        internal int Position { get; set; }

        internal void FillBuffer(Stream stream, int capacity)
        {
            MinCapacity(capacity);
            stream.Read(Buffer, 0, capacity);
            Position = 0;
        }

        internal void Append(Stream stream, int count)
        {
            var newCapacity = Position + count;
            if (Buffer.Length < newCapacity)
            {
                var newBuffer = new byte[newCapacity];
                Array.Copy(Buffer, newBuffer, Buffer.Length);
                Buffer = newBuffer;
            }

            stream.Read(Buffer, Position, count);
            Position += count;
        }

        //internal void WriteBuffer(Stream stream, int count) {
        //  stream.Write(buffer, 0, count);
        //}

        internal void MinCapacity(int capacity)
        {
            if (Buffer.Length < capacity) Buffer = new byte[capacity];
            Position = 0;
        }

        internal void Align(int alignment)
        {
            while (Position % alignment != 0) Position++;
        }

        //internal void WriteInt32(int value) {
        //  buffer[offset + 0] = (byte)value;
        //  buffer[offset + 1] = (byte)(value >> 8);
        //  buffer[offset + 2] = (byte)(value >> 16);
        //  buffer[offset + 3] = (byte)(value >> 24);
        //  offset += 4;
        //}

        //internal void WriteInt32(int[] values) {
        //  for (int i = 0; i < values.Length; i++) {
        //    WriteInt32(values[i]);
        //  }
        //}

        //internal void WriteBytes(byte[] bytes) {
        //  for (int i = 0; i < bytes.Length; i++) {
        //    buffer[offset++] = bytes[i];
        //  }
        //}

        internal void ReadInt16(out short value)
        {
            value = (short)((Buffer[Position + 0] & 0xFF) |
                            (Buffer[Position + 1] << 8));
            Position += 2;
        }

        internal void ReadInt8(out sbyte value)
        {
            value = (sbyte)Buffer[Position];
            Position += 1;
        }

        internal void ReadInt32(out int value)
        {
            value = (Buffer[Position + 0] & 0xFF) |
                    (Buffer[Position + 1] << 8) |
                    (Buffer[Position + 2] << 16) |
                    (Buffer[Position + 3] << 24);
            Position += 4;
        }

        internal void ReadInt64(out long value)
        {
            value = (long)(((ulong)Buffer[Position + 0] & 0xFF) |
                           ((ulong)Buffer[Position + 1] << 8) |
                           ((ulong)Buffer[Position + 2] << 16) |
                           ((ulong)Buffer[Position + 3] << 24) |
                           ((ulong)Buffer[Position + 4] << 32) |
                           ((ulong)Buffer[Position + 5] << 40) |
                           ((ulong)Buffer[Position + 6] << 48) |
                           ((ulong)Buffer[Position + 7] << 56));
            Position += 8;
        }

        internal void ReadUInt16(out ushort value)
        {
            value = (ushort)((Buffer[Position + 0] & 0xFF) |
                             (Buffer[Position + 1] << 8));
            Position += 2;
        }

        internal void ReadUInt8(out byte value)
        {
            value = (byte)(Buffer[Position + 0] & 0xFF);
            Position += 1;
        }

        internal void ReadUInt32(out uint value)
        {
            value = (uint)((Buffer[Position + 0] & 0xFF) |
                           (Buffer[Position + 1] << 8) |
                           (Buffer[Position + 2] << 16) |
                           (Buffer[Position + 3] << 24));
            Position += 4;
        }

        internal void ReadUInt64(out ulong value)
        {
            value = ((ulong)Buffer[Position + 0] & 0xFF) |
                    ((ulong)Buffer[Position + 1] << 8) |
                    ((ulong)Buffer[Position + 2] << 16) |
                    ((ulong)Buffer[Position + 3] << 24) |
                    ((ulong)Buffer[Position + 4] << 32) |
                    ((ulong)Buffer[Position + 5] << 40) |
                    ((ulong)Buffer[Position + 6] << 48) |
                    ((ulong)Buffer[Position + 7] << 56);
            Position += 8;
        }

        internal void ReadInt32(int[] values)
        {
            for (var i = 0; i < values.Length; i++) ReadInt32(out values[i]);
        }

        internal void ReadUInt32(uint[] values)
        {
            for (var i = 0; i < values.Length; i++) ReadUInt32(out values[i]);
        }

        internal void ReadBytes(byte[] bytes)
        {
            for (var i = 0; i < bytes.Length; i++) bytes[i] = Buffer[Position++];
        }

        internal float ReadFloat()
        {
            var result = BitConverter.ToSingle(Buffer, Position);
            Position += 4;
            return result;
        }

        internal double ReadDouble()
        {
            var result = BitConverter.ToDouble(Buffer, Position);
            Position += 8;
            return result;
        }

        internal decimal ReadDecimal()
        {
            var bits = new int[4];
            ReadInt32(bits);
            return new decimal(bits[2], bits[3], bits[1], bits[0] < 0, (byte)((bits[0] & 0x00FF0000) >> 16));
        }

        internal void ReadBString(out string value)
        {
            ushort len;
            ReadUInt16(out len);
            value = Encoding.UTF8.GetString(Buffer, Position, len);
            Position += len;
        }

        internal string ReadBString(int len)
        {
            var result = Encoding.UTF8.GetString(Buffer, Position, len);
            Position += len;
            return result;
        }

        internal void ReadCString(out string value)
        {
            var len = 0;
            while (Position + len < Buffer.Length && Buffer[Position + len] != 0) len++;
            value = Encoding.UTF8.GetString(Buffer, Position, len);
            Position += len + 1;
        }

        internal void SkipCString(out string value)
        {
            var len = 0;
            while (Position + len < Buffer.Length && Buffer[Position + len] != 0) len++;
            Position += len + 1;
            value = null;
        }

        internal void ReadGuid(out Guid guid)
        {
            uint a;
            ushort b;
            ushort c;
            byte d;
            byte e;
            byte f;
            byte g;
            byte h;
            byte i;
            byte j;
            byte k;

            ReadUInt32(out a);
            ReadUInt16(out b);
            ReadUInt16(out c);
            ReadUInt8(out d);
            ReadUInt8(out e);
            ReadUInt8(out f);
            ReadUInt8(out g);
            ReadUInt8(out h);
            ReadUInt8(out i);
            ReadUInt8(out j);
            ReadUInt8(out k);

            guid = new Guid(a, b, c, d, e, f, g, h, i, j, k);
        }

        internal string ReadString()
        {
            var len = 0;
            while (Position + len < Buffer.Length && Buffer[Position + len] != 0) len += 2;
            var result = Encoding.Unicode.GetString(Buffer, Position, len);
            Position += len + 2;
            return result;
        }
    }
}