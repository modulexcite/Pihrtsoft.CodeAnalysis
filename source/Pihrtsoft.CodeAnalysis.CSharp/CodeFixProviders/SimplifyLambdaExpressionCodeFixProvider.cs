﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Pihrtsoft.CodeAnalysis.CSharp.Refactoring;

namespace Pihrtsoft.CodeAnalysis.CSharp.CodeFixProviders
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SimplifyLambdaExpressionCodeFixProvider))]
    [Shared]
    public class SimplifyLambdaExpressionCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(DiagnosticIdentifiers.SimplifyLambdaExpression);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            BlockSyntax block = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<BlockSyntax>();

            if (block == null)
                return;

            CodeAction codeAction = CodeAction.Create(
                "Simplify lambda expression",
                cancellationToken => SimplifyLambdaExpressionAsync(context.Document, (LambdaExpressionSyntax)block.Parent, cancellationToken),
                DiagnosticIdentifiers.SimplifyLambdaExpression + EquivalenceKeySuffix);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        public static async Task<Document> SimplifyLambdaExpressionAsync(
            Document document,
            LambdaExpressionSyntax lambda,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            LambdaExpressionSyntax newLambda = SimplifyLambdaExpressionRefactoring.SimplifyLambdaExpression(lambda)
                .WithTriviaFrom(lambda)
                .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode newRoot = oldRoot.ReplaceNode(lambda, newLambda);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
