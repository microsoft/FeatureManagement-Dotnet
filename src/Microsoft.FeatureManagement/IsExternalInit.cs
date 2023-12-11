// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

// The init accessor for properties is supported in C# 9.0 and later.
// This class is used to compile .NET frameworks that don't support C# 9.0 or later while still using the init accessor for a property.
// The code referenced for this file can be found here: https://github.com/dotnet/roslyn/issues/45510#issuecomment-725091019

#if NETSTANDARD2_0 || NETSTANDARD2_1

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}

#endif
