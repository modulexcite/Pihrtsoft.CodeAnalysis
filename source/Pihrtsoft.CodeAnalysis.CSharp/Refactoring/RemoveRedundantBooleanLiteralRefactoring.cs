﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Pihrtsoft.CodeAnalysis.CSharp.Refactoring
{
    internal static class RemoveRedundantBooleanLiteralRefactoring
    {
        public static async Task<Document> RefactorAsync(
            Document document,
            BinaryExpressionSyntax binaryExpression,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            SyntaxNode newNode = Refactor(binaryExpression)
                .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode newRoot = oldRoot.ReplaceNode(binaryExpression, newNode);

            return document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxNode Refactor(BinaryExpressionSyntax binaryExpression)
        {
            if (binaryExpression.Left.IsAnyKind(SyntaxKind.TrueLiteralExpression, SyntaxKind.FalseLiteralExpression))
            {
                SyntaxTriviaList triviaList = SyntaxFactory.TriviaList()
                    .AddRange(binaryExpression.Left.GetLeadingTrivia())
                    .AddRange(binaryExpression.Left.GetTrailingTrivia())
                    .AddRange(binaryExpression.OperatorToken.LeadingTrivia)
                    .AddRange(binaryExpression.OperatorToken.TrailingTrivia)
                    .AddRange(binaryExpression.Right.GetLeadingTrivia());

                return binaryExpression.Right
                    .WithLeadingTrivia(triviaList)
                    .WithTrailingTrivia(binaryExpression.Right.GetTrailingTrivia());
            }
            else
            {
                SyntaxTriviaList triviaList = SyntaxFactory.TriviaList()
                    .AddRange(binaryExpression.Left.GetTrailingTrivia())
                    .AddRange(binaryExpression.OperatorToken.LeadingTrivia)
                    .AddRange(binaryExpression.OperatorToken.TrailingTrivia)
                    .AddRange(binaryExpression.Right.GetLeadingTrivia())
                    .AddRange(binaryExpression.Right.GetTrailingTrivia());

                return binaryExpression.Left
                    .WithLeadingTrivia(binaryExpression.Left.GetLeadingTrivia())
                    .WithTrailingTrivia(triviaList);
            }
        }
    }
}
