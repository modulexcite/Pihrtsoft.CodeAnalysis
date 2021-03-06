﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Pihrtsoft.CodeAnalysis.CSharp.Refactoring;
using Pihrtsoft.CodeAnalysis.CSharp.Removers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pihrtsoft.CodeAnalysis.CSharp.CodeRefactoringProviders
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ParameterListCodeRefactoringProvider))]
    public class ParameterListCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            ParameterListSyntax parameterList = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<ParameterListSyntax>();

            if (parameterList == null)
                return;

            if (parameterList.Parameters.Count == 0)
                return;

            if (parameterList.IsSingleline())
            {
                if (parameterList.Parameters.Count > 1)
                {
                    context.RegisterRefactoring(
                        "Format each parameter on separate line",
                        cancellationToken => FormatEachParameterOnNewLineAsync(context.Document, parameterList, cancellationToken));
                }
            }
            else
            {
                context.RegisterRefactoring(
                    "Format all parameters on a single line",
                    cancellationToken => FormatAllParametersOnSingleLineAsync(context.Document, parameterList, cancellationToken));
            }

            DuplicateParameterRefactoring.Refactor(context, parameterList);

            SwapParametersRefactoring.Refactor(context, parameterList);
        }

        private static async Task<Document> FormatEachParameterOnNewLineAsync(
            Document document,
            ParameterListSyntax parameterList,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            SyntaxNode newRoot = oldRoot.ReplaceNode(
                parameterList,
                CreateMultilineList(parameterList));

            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> FormatAllParametersOnSingleLineAsync(
            Document document,
            ParameterListSyntax parameterList,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            ParameterListSyntax newParameterList = WhitespaceOrEndOfLineRemover.RemoveFrom(parameterList)
                .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode newRoot = oldRoot.ReplaceNode(parameterList, newParameterList);

            return document.WithSyntaxRoot(newRoot);
        }

        private static ParameterListSyntax CreateMultilineList(ParameterListSyntax list)
        {
            SeparatedSyntaxList<ParameterSyntax> parameters = SeparatedList<ParameterSyntax>(CreateNodesAndTokens(list));

            SyntaxToken openParen = Token(SyntaxKind.OpenParenToken).WithTrailingNewLine();

            return ParameterList(parameters).WithOpenParenToken(openParen);
        }

        private static IEnumerable<SyntaxNodeOrToken> CreateNodesAndTokens(ParameterListSyntax list)
        {
            SyntaxTriviaList trivia = list.Parent.GetIndentTrivia().Add(SyntaxHelper.DefaultIndent);

            SeparatedSyntaxList<ParameterSyntax>.Enumerator en = list.Parameters.GetEnumerator();

            if (en.MoveNext())
            {
                yield return en.Current.WithLeadingTrivia(trivia);

                while (en.MoveNext())
                {
                    yield return Token(SyntaxKind.CommaToken).WithTrailingNewLine();

                    yield return en.Current.WithLeadingTrivia(trivia);
                }
            }
        }
    }
}