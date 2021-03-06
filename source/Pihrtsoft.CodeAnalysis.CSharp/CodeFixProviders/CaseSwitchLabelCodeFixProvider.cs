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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CaseSwitchLabelCodeFixProvider))]
    [Shared]
    public class CaseSwitchLabelCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(DiagnosticIdentifiers.RemoveUnnecessaryCaseLabel);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            CaseSwitchLabelSyntax label = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<CaseSwitchLabelSyntax>();

            if (label == null)
                return;

            CodeAction codeAction = CodeAction.Create(
                "Remove unnecessary case label",
                cancellationToken =>
                {
                    return RemoveCaseSwitchLabelAsync(
                        context.Document,
                        label,
                        cancellationToken);
                },
                DiagnosticIdentifiers.RemoveUnnecessaryCaseLabel + EquivalenceKeySuffix);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> RemoveCaseSwitchLabelAsync(
            Document document,
            CaseSwitchLabelSyntax label,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            var switchSection = (SwitchSectionSyntax)label.Parent;

            SwitchSectionSyntax newNode = switchSection.RemoveNode(label, GetRemoveOptions(label))
                .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode newRoot = oldRoot.ReplaceNode(switchSection, newNode);

            return document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxRemoveOptions GetRemoveOptions(CaseSwitchLabelSyntax label)
        {
            if (label.GetLeadingTrivia().IsWhitespaceOrEndOfLine()
                && label.GetTrailingTrivia().IsWhitespaceOrEndOfLine())
            {
                return SyntaxRemoveOptions.KeepNoTrivia;
            }

            return SyntaxRemoveOptions.KeepExteriorTrivia;
        }
    }
}
