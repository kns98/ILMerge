using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ILMerging.Tests.Helpers
{
    [DebuggerDisplay("{ToString(),nq}")]
    public sealed class TempFile : IDisposable
    {
        private string path;

        public TempFile() : this(System.IO.Path.GetTempFileName())
        {
        }

        public TempFile(string path)
        {
            this.path = path;
        }

        public string Path => path;

        public void Dispose()
        {
            var path = Interlocked.Exchange(ref this.path, null);
            if (path != null) File.Delete(path);
        }

        public static TempFile WithExtension(string extension)
        {
            return new TempFile(
                System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    System.IO.Path.ChangeExtension(System.IO.Path.GetRandomFileName(), extension)));
        }

        public static implicit operator string(TempFile tempFile)
        {
            return tempFile.path;
        }

        public override string ToString()
        {
            return path;
        }
    }
}