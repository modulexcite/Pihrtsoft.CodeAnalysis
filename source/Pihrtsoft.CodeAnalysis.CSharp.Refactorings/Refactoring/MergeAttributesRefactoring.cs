﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Pihrtsoft.CodeAnalysis.CSharp.Refactoring
{
    public static class MergeAttributesRefactoring
    {
        public static void Refactor(CodeRefactoringContext context, MemberDeclarationSyntax member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            SyntaxList<AttributeListSyntax> attributeLists = member.GetAttributeLists();

            if (attributeLists.Count > 0)
            {
                AttributeListSyntax[] lists = AttributeRefactoring.GetSelectedAttributeLists(attributeLists, context.Span).ToArray();

                if (lists.Length > 1)
                {
                    context.RegisterRefactoring(
                        "Merge attributes",
                        cancellationToken =>
                        {
                            return RefactorAsync(
                                context.Document,
                                member,
                                lists,
                                cancellationToken);
                        });
                }
            }
        }

        private static async Task<Document> RefactorAsync(
            Document document,
            MemberDeclarationSyntax member,
            AttributeListSyntax[] attributeLists,
            CancellationToken cancellationToken)
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

            SyntaxList<AttributeListSyntax> lists = member.GetAttributeLists();

            int index = lists.IndexOf(attributeLists[0]);

            for (int i = attributeLists.Length - 1; i >= 1; i--)
                lists = lists.RemoveAt(index);

            AttributeListSyntax list = AttributeRefactoring.MergeAttributes(attributeLists)
                .WithAdditionalAnnotations(Formatter.Annotation);

            lists = lists.Replace(lists[index], list);

            root = root.ReplaceNode(member, member.SetAttributeLists(lists));

            return document.WithSyntaxRoot(root);
        }
    }
}
