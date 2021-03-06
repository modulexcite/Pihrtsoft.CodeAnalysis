﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SimplifyAssignmentExpressionCodeFixProvider))]
    [Shared]
    public class SimplifyAssignmentExpressionCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(DiagnosticIdentifiers.SimplifyAssignmentExpression);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            AssignmentExpressionSyntax assignmentExpression = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<AssignmentExpressionSyntax>();

            if (assignmentExpression == null)
                return;

            if (assignmentExpression
                .DescendantTrivia(assignmentExpression.Span)
                .All(f => f.IsWhitespaceOrEndOfLine()))
            {
                CodeAction codeAction = CodeAction.Create(
                    "Simplify assignment expression",
                    cancellationToken =>
                    {
                        return SimplifyAssignmentExpressionAsync(
                            context.Document,
                            assignmentExpression,
                            cancellationToken);
                    },
                    DiagnosticIdentifiers.SimplifyAssignmentExpression + EquivalenceKeySuffix);

                context.RegisterCodeFix(codeAction, context.Diagnostics);
            }
        }

        private static async Task<Document> SimplifyAssignmentExpressionAsync(
            Document document,
            AssignmentExpressionSyntax assignmentExpression,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            AssignmentExpressionSyntax newNode = AssignmentExpressionRefactoring.Simplify(assignmentExpression)
                .WithTriviaFrom(assignmentExpression)
                .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode newRoot = oldRoot.ReplaceNode(assignmentExpression, newNode);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
