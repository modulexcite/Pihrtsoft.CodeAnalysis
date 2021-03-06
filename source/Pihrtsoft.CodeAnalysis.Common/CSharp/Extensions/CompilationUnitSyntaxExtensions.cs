﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pihrtsoft.CodeAnalysis.CSharp
{
    public static class CompilationUnitSyntaxExtensions
    {
        internal static SyntaxNode AddUsingDirective(this CompilationUnitSyntax compilationUnit, ITypeSymbol typeSymbol)
        {
            if (compilationUnit == null)
                throw new ArgumentNullException(nameof(compilationUnit));

            if (typeSymbol == null)
                throw new ArgumentNullException(nameof(typeSymbol));

            if (typeSymbol.IsKind(SymbolKind.NamedType))
            {
                foreach (ITypeSymbol typeSymbol2 in ((INamedTypeSymbol)typeSymbol).GetAllTypeArgumentsAndSelf())
                    compilationUnit = AddUsingDirectivePrivate(compilationUnit, typeSymbol2);

                return compilationUnit;
            }
            else
            {
                return AddUsingDirectivePrivate(compilationUnit, typeSymbol);
            }
        }

        private static CompilationUnitSyntax AddUsingDirectivePrivate(this CompilationUnitSyntax compilationUnit, ITypeSymbol type)
        {
            if (compilationUnit == null)
                throw new ArgumentNullException(nameof(compilationUnit));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type.ContainingNamespace == null)
                return compilationUnit;

            return AddUsingDirective(compilationUnit, type.ContainingNamespace);
        }

        internal static CompilationUnitSyntax AddUsingDirective(this CompilationUnitSyntax compilationUnit, INamespaceSymbol namespaceSymbol)
        {
            if (compilationUnit == null)
                throw new ArgumentNullException(nameof(compilationUnit));

            if (namespaceSymbol == null)
                throw new ArgumentNullException(nameof(namespaceSymbol));

            if (namespaceSymbol.IsGlobalNamespace)
                return compilationUnit;

            return AddUsingDirective(compilationUnit, namespaceSymbol.ToString());
        }

        private static CompilationUnitSyntax AddUsingDirective(this CompilationUnitSyntax compilationUnit, string @namespace)
        {
            if (compilationUnit == null)
                throw new ArgumentNullException(nameof(compilationUnit));

            UsingDirectiveSyntax usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(@namespace));

            int index = 0;

            SyntaxList<UsingDirectiveSyntax>.Enumerator en = compilationUnit.Usings.GetEnumerator();

            bool isSkippingSystem = true;

            while (en.MoveNext())
            {
                int result = string.Compare(
                    en.Current.Name.ToString(),
                    usingDirective.Name.ToString(),
                    StringComparison.Ordinal);

                if (result == 0)
                    return compilationUnit;

                if (isSkippingSystem)
                {
                    if (en.Current.IsSystem())
                    {
                        index++;
                        continue;
                    }
                    else
                    {
                        isSkippingSystem = false;
                    }
                }

                if (result > 0)
                    index++;
            }

            SyntaxList<UsingDirectiveSyntax> usings = compilationUnit.Usings.Insert(index, usingDirective);

            return compilationUnit.WithUsings(usings);
        }

        private static bool IsAlreadyContainedInNamespace(
            TypeSyntax typeSyntax,
            ITypeSymbol typeSymbol,
            SemanticModel semanticModel)
        {
            NamespaceDeclarationSyntax namespaceDeclaration = typeSyntax.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();

            if (namespaceDeclaration != null)
            {
                INamespaceSymbol namespaceSymbol = semanticModel.GetDeclaredSymbol(namespaceDeclaration);
                if (namespaceSymbol != null)
                {
                    return namespaceSymbol
                        .ContainingNamespacesAndSelf()
                        .Any(f => string.Equals(f.ToString(), typeSymbol.ContainingNamespace.ToString(), StringComparison.Ordinal));
                }
            }

            return false;
        }
    }
}
