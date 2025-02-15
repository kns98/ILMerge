﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

namespace Microsoft.Cci.Pdb
{
    internal class DataStream
    {
        //internal int GetPage(int index) {
        //  return pages[index];
        //}

        internal int contentSize;
        internal int[] pages;

        internal DataStream()
        {
        }

        internal DataStream(int contentSize, BitAccess bits, int count)
        {
            this.contentSize = contentSize;
            if (count > 0)
            {
                pages = new int[count];
                bits.ReadInt32(pages);
            }
        }

        //private void AddPages(int page0, int count) {
        //  if (pages == null) {
        //    pages = new int[count];
        //    for (int i = 0; i < count; i++) {
        //      pages[i] = page0 + i;
        //    }
        //  } else {
        //    int[] old = pages;
        //    int used = old.Length;

        //    pages = new int[used + count];
        //    Array.Copy(old, pages, used);
        //    for (int i = 0; i < count; i++) {
        //      pages[used + i] = page0 + i;
        //    }
        //  }
        //}

        //internal int Pages {
        //  get { return pages == null ? 0 : pages.Length; }
        //}

        internal int Length => contentSize;

        internal void Read(PdbReader reader, BitAccess bits)
        {
            bits.MinCapacity(contentSize);
            Read(reader, 0, bits.Buffer, 0, contentSize);
        }

        internal void Read(PdbReader reader, int position,
            byte[] bytes, int offset, int data)
        {
            if (position + data > contentSize)
                throw new PdbDebugException("DataStream can't read off end of stream. " +
                                            "(pos={0},siz={1})",
                    position, data);
            if (position == contentSize) return;

            var left = data;
            var page = position / reader.pageSize;
            var rema = position % reader.pageSize;

            // First get remained of first page.
            if (rema != 0)
            {
                var todo = reader.pageSize - rema;
                if (todo > left) todo = left;

                reader.Seek(pages[page], rema);
                reader.Read(bytes, offset, todo);

                offset += todo;
                left -= todo;
                page++;
            }

            // Now get the remaining pages.
            while (left > 0)
            {
                var todo = reader.pageSize;
                if (todo > left) todo = left;

                reader.Seek(pages[page], 0);
                reader.Read(bytes, offset, todo);

                offset += todo;
                left -= todo;
                page++;
            }
        }
    }
}