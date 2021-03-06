﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Pihrtsoft.CodeAnalysis.CSharp
{
    public static class SyntaxListExtensions
    {
        public static bool Contains<T>(this SyntaxList<T> list, SyntaxKind kind) where T : SyntaxNode
        {
            return list.IndexOf(kind) != -1;
        }
    }
}
