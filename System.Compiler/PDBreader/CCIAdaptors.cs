// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System.Compiler;

namespace Microsoft.Cci.Pdb
{
    public interface ILocalScope
    {
        /// <summary>
        ///     The offset of the first operation in the scope.
        /// </summary>
        uint Offset { get; }

        /// <summary>
        ///     The length of the scope. Offset+Length equals the offset of the first operation outside the scope, or equals the
        ///     method body length.
        /// </summary>
        uint Length { get; }

        /// <summary>
        ///     The definition of the method in which this local scope is defined.
        /// </summary>
        Method MethodDefinition { get; }
    }


    internal sealed class PdbIteratorScope : ILocalScope
    {
        internal PdbIteratorScope(uint offset, uint length)
        {
            this.Offset = offset;
            this.Length = length;
        }

        public uint Offset { get; }

        public uint Length { get; }

        public Method MethodDefinition { get; set; }
    }
}