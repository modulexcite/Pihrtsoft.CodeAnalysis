﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Pihrtsoft.CodeAnalysis;
using Pihrtsoft.CodeAnalysis.CSharp.Analysis;

namespace Pihrtsoft.CodeAnalysis.CSharp.CodeFixProviders
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ElseClauseCodeFixProvider))]
    [Shared]
    public class ElseClauseCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticIdentifiers.RemoveEmptyElseClause,
                    DiagnosticIdentifiers.SimplifyElseClauseContainingIfStatement);
            }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            ElseClauseSyntax elseClause = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<ElseClauseSyntax>();

            if (elseClause == null)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case DiagnosticIdentifiers.RemoveEmptyElseClause:
                        {
                            CodeAction codeAction = CodeAction.Create(
                                "Remove empty else clause",
                                cancellationToken => RemoveEmptyElseClauseAsync(context.Document, elseClause, cancellationToken),
                                diagnostic.Id + EquivalenceKeySuffix);

                            context.RegisterCodeFix(codeAction, diagnostic);
                            break;
                        }
                    case DiagnosticIdentifiers.SimplifyElseClauseContainingIfStatement:
                        {
                            if (!CheckTrivia(elseClause))
                                return;

                            CodeAction codeAction = CodeAction.Create(
                                "Remove braces from else clause",
                                cancellationToken => RemoveBracesFromElseClauseAsync(context.Document, elseClause, cancellationToken),
                                diagnostic.Id + EquivalenceKeySuffix);

                            context.RegisterCodeFix(codeAction, diagnostic);
                            break;
                        }
                }
            }
        }

        private static bool CheckTrivia(ElseClauseSyntax elseClause)
        {
            if (elseClause.ElseKeyword.TrailingTrivia.Any(f => !f.IsWhitespaceOrEndOfLine()))
                return false;

            var block = (BlockSyntax)elseClause.Statement;

            if (block.OpenBraceToken.LeadingTrivia.Any(f => !f.IsWhitespaceOrEndOfLine()))
                return false;

            if (block.OpenBraceToken.TrailingTrivia.Any(f => !f.IsWhitespaceOrEndOfLine()))
                return false;

            var ifStatement = (IfStatementSyntax)block.Statements[0];

            if (ifStatement.IfKeyword.LeadingTrivia.Any(f => !f.IsWhitespaceOrEndOfLine()))
                return false;

            if (ifStatement.GetTrailingTrivia().Any(f => !f.IsWhitespaceOrEndOfLine()))
                return false;

            if (block.CloseBraceToken.LeadingTrivia.Any(f => !f.IsWhitespaceOrEndOfLine()))
                return false;

            return true;
        }

        private static async Task<Document> RemoveEmptyElseClauseAsync(
            Document document,
            ElseClauseSyntax elseClause,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            SyntaxNode newRoot = GetNewRoot(oldRoot, elseClause);

            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> RemoveBracesFromElseClauseAsync(
            Document document,
            ElseClauseSyntax elseClause,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            var block = (BlockSyntax)elseClause.Statement;

            var ifStatement = (IfStatementSyntax)block.Statements[0];

            ElseClauseSyntax newElseClause = elseClause
                .WithStatement(ifStatement)
                .WithElseKeyword(elseClause.ElseKeyword.WithoutTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode newRoot = oldRoot.ReplaceNode(elseClause, newElseClause);

            return document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxNode GetNewRoot(SyntaxNode oldRoot, ElseClauseSyntax elseClause)
        {
            if (elseClause.Parent?.IsKind(SyntaxKind.IfStatement) == true)
            {
                var ifStatement = (IfStatementSyntax)elseClause.Parent;

                if (ifStatement.Statement?.GetTrailingTrivia().IsWhitespaceOrEndOfLine() == true)
                {
                    IfStatementSyntax newIfStatement = ifStatement
                        .WithStatement(ifStatement.Statement.WithTrailingTrivia(elseClause.GetTrailingTrivia()))
                        .WithElse(null);

                    return oldRoot.ReplaceNode(ifStatement, newIfStatement);
                }
            }

            return oldRoot.RemoveNode(elseClause, SyntaxRemoveOptions.KeepExteriorTrivia);
        }
    }
}
