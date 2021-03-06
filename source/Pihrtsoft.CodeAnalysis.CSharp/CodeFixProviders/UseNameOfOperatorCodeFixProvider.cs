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

namespace Pihrtsoft.CodeAnalysis.CSharp.CodeFixProviders
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseNameOfOperatorCodeFixProvider))]
    [Shared]
    public class UseNameOfOperatorCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(DiagnosticIdentifiers.UseNameOfOperator);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            Diagnostic diagnostic = context.Diagnostics[0];

            LiteralExpressionSyntax node = root
                .FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<LiteralExpressionSyntax>();

            if (node == null)
                return;

            CodeAction codeAction = CodeAction.Create(
                "Use nameof operator",
                cancellationToken => UseNameOfOperatorAsync(context.Document, node, cancellationToken),
                DiagnosticIdentifiers.UseNameOfOperator + EquivalenceKeySuffix);

            context.RegisterCodeFix(codeAction, diagnostic);
        }

        private static async Task<Document> UseNameOfOperatorAsync(
            Document document,
            LiteralExpressionSyntax literalExpression,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            InvocationExpressionSyntax newNode = SyntaxHelper.NameOf(literalExpression.Token.ValueText)
                .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode newRoot = oldRoot.ReplaceNode(literalExpression, newNode);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
