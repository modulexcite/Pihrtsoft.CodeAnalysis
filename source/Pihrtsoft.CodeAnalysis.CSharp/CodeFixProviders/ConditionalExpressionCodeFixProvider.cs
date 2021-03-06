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
using Microsoft.CodeAnalysis.Formatting;

namespace Pihrtsoft.CodeAnalysis.CSharp.CodeFixProviders
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConditionalExpressionCodeFixProvider))]
    [Shared]
    public class ConditionalExpressionCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticIdentifiers.AddParenthesesToConditionalExpressionCondition,
                    DiagnosticIdentifiers.UseCoalesceExpressionInsteadOfConditionalExpression);
            }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            ConditionalExpressionSyntax node = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<ConditionalExpressionSyntax>();

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case DiagnosticIdentifiers.AddParenthesesToConditionalExpressionCondition:
                        {
                            CodeAction codeAction = CodeAction.Create(
                                "Add parentheses to condition",
                                cancellationToken => AddParenthesesToConditionAsync(context.Document, node, cancellationToken),
                                diagnostic.Id + EquivalenceKeySuffix);

                            context.RegisterCodeFix(codeAction, diagnostic);

                            break;
                        }
                    case DiagnosticIdentifiers.UseCoalesceExpressionInsteadOfConditionalExpression:
                        {
                            CodeAction codeAction = CodeAction.Create(
                                "Simplify conditional expression",
                                cancellationToken =>
                                {
                                    return UseCoalesceExpressionInsteadOfConditionalExpressionAsync(
                                        context.Document,
                                        node,
                                        cancellationToken);
                                },
                                diagnostic.Id + EquivalenceKeySuffix);

                            context.RegisterCodeFix(codeAction, diagnostic);

                            break;
                        }
                }
            }
        }

        private static async Task<Document> AddParenthesesToConditionAsync(
            Document document,
            ConditionalExpressionSyntax conditionalExpression,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            ConditionalExpressionSyntax newNode = conditionalExpression
                .WithCondition(
                    SyntaxFactory.ParenthesizedExpression(
                        conditionalExpression.Condition.WithoutTrivia()
                    ).WithTriviaFrom(conditionalExpression.Condition)
                ).WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode newRoot = oldRoot.ReplaceNode(conditionalExpression, newNode);

            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> UseCoalesceExpressionInsteadOfConditionalExpressionAsync(
            Document document,
            ConditionalExpressionSyntax conditionalExpression,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            var binaryExpression = (BinaryExpressionSyntax)conditionalExpression.Condition.UnwrapParentheses();

            ExpressionSyntax left = (binaryExpression.IsKind(SyntaxKind.EqualsExpression))
                ? conditionalExpression.WhenFalse
                : conditionalExpression.WhenTrue;

            ExpressionSyntax right = (binaryExpression.IsKind(SyntaxKind.EqualsExpression))
                ? conditionalExpression.WhenTrue
                : conditionalExpression.WhenFalse;

            BinaryExpressionSyntax newNode = SyntaxFactory.BinaryExpression(
                SyntaxKind.CoalesceExpression,
                left.WithoutTrivia(),
                right.WithoutTrivia());

            SyntaxNode newRoot = oldRoot.ReplaceNode(
                conditionalExpression,
                newNode.WithTriviaFrom(conditionalExpression));

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
