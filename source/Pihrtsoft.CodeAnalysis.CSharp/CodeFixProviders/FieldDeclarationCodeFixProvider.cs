﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pihrtsoft.CodeAnalysis.CSharp.CodeFixProviders
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FieldDeclarationCodeFixProvider))]
    [Shared]
    public class FieldDeclarationCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(DiagnosticIdentifiers.SplitDeclarationIntoMultipleDeclarations);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            FieldDeclarationSyntax fieldDeclaration = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<FieldDeclarationSyntax>();

            if (fieldDeclaration == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Split declaration into multiple declarations",
                    cancellationToken =>
                    {
                        return SplitVariablesIntoMultipleDeclarationsAsync(
                            context.Document,
                            fieldDeclaration,
                            cancellationToken);
                    },
                    DiagnosticIdentifiers.SplitDeclarationIntoMultipleDeclarations + EquivalenceKeySuffix),
                context.Diagnostics);
        }

        private static async Task<Document> SplitVariablesIntoMultipleDeclarationsAsync(
            Document document,
            FieldDeclarationSyntax declaration,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            var containingMember = (MemberDeclarationSyntax)declaration.Parent;

            SyntaxList<MemberDeclarationSyntax> members = containingMember.GetMembers();

            SyntaxList<MemberDeclarationSyntax> newMembers = members.ReplaceRange(
                declaration,
                CreateFieldDeclarations(declaration));

            MemberDeclarationSyntax newNode = containingMember.SetMembers(newMembers);

            SyntaxNode newRoot = oldRoot.ReplaceNode(containingMember, newNode);

            return document.WithSyntaxRoot(newRoot);
        }

        private static IEnumerable<FieldDeclarationSyntax> CreateFieldDeclarations(FieldDeclarationSyntax declaration)
        {
            SeparatedSyntaxList<VariableDeclaratorSyntax> variables = declaration.Declaration.Variables;

            FieldDeclarationSyntax declaration2 = declaration.WithoutTrivia();

            for (int i = 0; i < variables.Count; i++)
            {
                FieldDeclarationSyntax newDeclaration = FieldDeclaration(
                    declaration2.AttributeLists,
                    declaration2.Modifiers,
                    VariableDeclaration(
                        declaration2.Declaration.Type,
                        SingletonSeparatedList(variables[i])));

                if (i == 0)
                    newDeclaration = newDeclaration.WithLeadingTrivia(declaration.GetLeadingTrivia());

                if (i == variables.Count - 1)
                    newDeclaration = newDeclaration.WithTrailingTrivia(declaration.GetTrailingTrivia());

                yield return newDeclaration.WithAdditionalAnnotations(Formatter.Annotation);
            }
        }
    }
}
