﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using Pihrtsoft.CodeAnalysis.CSharp.Refactoring;

namespace Pihrtsoft.CodeAnalysis.CSharp.CodeFixProviders
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ChangeForEachDeclaredTypeToExplicitCodeFixProvider))]
    [Shared]
    public class ChangeForEachDeclaredTypeToExplicitCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(DiagnosticIdentifiers.DeclareExplicitTypeInForEach);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            ForEachStatementSyntax forEachStatement = root.FindNode(context.Span, getInnermostNodeForTie: true)?.FirstAncestorOrSelf<ForEachStatementSyntax>();

            if (forEachStatement == null)
                return;

            SemanticModel semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);

            if (semanticModel == null)
                return;

            ITypeSymbol typeSymbol = semanticModel.GetTypeInfo(forEachStatement.Type, context.CancellationToken).Type;

            if (typeSymbol == null)
                return;

            CodeAction codeAction = CodeAction.Create(
                $"Change type to '{typeSymbol.ToDisplayString(TypeSyntaxRefactoring.SymbolDisplayFormat)}'",
                cancellationToken => ChangeDeclaredTypeToExplicitAsync(context.Document, forEachStatement.Type, typeSymbol, cancellationToken),
                DiagnosticIdentifiers.DeclareExplicitTypeInForEach + EquivalenceKeySuffix);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> ChangeDeclaredTypeToExplicitAsync(
            Document document,
            TypeSyntax type,
            ITypeSymbol typeSymbol,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            TypeSyntax newType = TypeSyntaxRefactoring.CreateTypeSyntax(typeSymbol)
                .WithTriviaFrom(type)
                .WithAdditionalAnnotations(Simplifier.Annotation);

            SyntaxNode newRoot = oldRoot.ReplaceNode(type, newType);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}