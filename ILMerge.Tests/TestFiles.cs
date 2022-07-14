using System.IO;
using NUnit.Framework;

namespace ILMerging.Tests
{
    internal static class TestFiles
    {
        public static string TestSnk => FromCurrentDir("test.snk");

        public static string TestPfx => FromCurrentDir("test.pfx");

        private static string FromCurrentDir(string fileName)
        {
            return Path.Combine(TestContext.CurrentContext.TestDirectory, fileName);
        }
    }
}