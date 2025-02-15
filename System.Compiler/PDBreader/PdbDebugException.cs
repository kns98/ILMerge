﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System.IO;

namespace Microsoft.Cci.Pdb
{
    internal class PdbDebugException : IOException
    {
        internal PdbDebugException(string format, params object[] args)
            : base(string.Format(format, args))
        {
        }
    }
}