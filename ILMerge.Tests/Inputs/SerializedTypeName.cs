using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SerializedTypeName
{
    public class M : ITwoTypeArgs<int, int>
    {
        IEnumerator<int> ITwoTypeArgs<int, int>.Mk()
        {
            yield return 42;
        }

        internal static void Main()
        {
            var asm = Assembly.GetCallingAssembly();
            var types = asm.DefinedTypes;
            Console.WriteLine(types.First());
        }
    }

    public interface ITwoTypeArgs<T1, T2>
    {
        IEnumerator<int> Mk();
    }
}