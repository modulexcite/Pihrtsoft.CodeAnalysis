﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Pihrtsoft.CodeAnalysis.CSharp.Refactoring
{
    public static class TypeSyntaxRefactoring
    {
        private static readonly SymbolDisplayFormat _symbolDisplayFormatForTypeSyntax = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public static SymbolDisplayFormat SymbolDisplayFormat { get; } = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public static TypeSyntax CreateTypeSyntax(ITypeSymbol typeSymbol)
        {
            return CreateTypeSyntax(typeSymbol, _symbolDisplayFormatForTypeSyntax);
        }

        public static TypeSyntax CreateTypeSyntax(ITypeSymbol typeSymbol, SymbolDisplayFormat symbolDisplayFormat)
        {
            if (typeSymbol == null)
                throw new ArgumentNullException(nameof(typeSymbol));

            string s = typeSymbol.ToDisplayString(symbolDisplayFormat);

            return SyntaxFactory.ParseTypeName(s);
        }

        public static async Task<Document> ChangeTypeToExplicitAsync(
            Document document,
            TypeSyntax type,
            ITypeSymbol typeSymbol,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            TypeSyntax newType = CreateTypeSyntax(typeSymbol)
                .WithTriviaFrom(type)
                .WithAdditionalAnnotations(Simplifier.Annotation);

            SyntaxNode newRoot = oldRoot.ReplaceNode(type, newType);

            return document.WithSyntaxRoot(newRoot);
        }

        public static async Task<Document> ChangeTypeToImplicitAsync(
            Document document,
            TypeSyntax type,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            IdentifierNameSyntax newType = SyntaxFactory
                .IdentifierName("var")
                .WithTriviaFrom(type);

            SyntaxNode newRoot = oldRoot.ReplaceNode(type, newType);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
