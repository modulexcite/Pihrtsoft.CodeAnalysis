﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pihrtsoft.CodeAnalysis.CSharp.Refactoring
{
    internal static class ForToForEachRefactoring
    {
        private const string ElementName = "item";

        public static async Task<Document> RefactorAsync(
            Document document,
            ForStatementSyntax forStatement,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            IEnumerable<IdentifierNameSyntax> identifierNames = await GetIdentifierNamesAsync(document, forStatement, oldRoot, semanticModel, cancellationToken);

            List<ElementAccessExpressionSyntax> expressions = identifierNames
                .Select(f => f.Parent.Parent.Parent)
                .Cast<ElementAccessExpressionSyntax>()
                .ToList();

            var rewriter = new ForToForEachSyntaxRewriter(expressions, ElementName);

            ForEachStatementSyntax forEachStatement = ForEachStatement(
                IdentifierName("var"),
                ElementName,
                GetCollectionExpression(forStatement),
                (StatementSyntax)rewriter.Visit(forStatement.Statement));

            forEachStatement = forEachStatement
                .WithTriviaFrom(forStatement)
                .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode newRoot = oldRoot.ReplaceNode(forStatement, forEachStatement);

            return document.WithSyntaxRoot(newRoot);
        }

        public static async Task<bool> CanRefactorAsync(
            CodeRefactoringContext context,
            ForStatementSyntax forStatement,
            SemanticModel semanticModel)
        {
            ExpressionSyntax value = forStatement
                .Declaration?
                .Variables.SingleOrDefault()?
                .Initializer?
                .Value;

            if (value?.IsKind(SyntaxKind.NumericLiteralExpression) != true)
                return false;

            if (((LiteralExpressionSyntax)value).Token.ValueText != "0")
                return false;

            if (forStatement.Condition?.IsKind(SyntaxKind.LessThanExpression) != true)
                return false;

            ExpressionSyntax expression = ((BinaryExpressionSyntax)forStatement.Condition).Right;

            if (expression?.IsKind(SyntaxKind.SimpleMemberAccessExpression) != true)
                return false;

            var memberAccessExpression = (MemberAccessExpressionSyntax)expression;

            string memberName = memberAccessExpression.Name?.Identifier.ValueText;
            if (memberName != "Count" && memberName != "Length")
                return false;

            if (forStatement.Incrementors.SingleOrDefault()?.IsKind(SyntaxKind.PostIncrementExpression) != true)
                return false;

            return await CheckIndexReferencesAsync(context, forStatement, semanticModel);
        }

        private static async Task<bool> CheckIndexReferencesAsync(
            CodeRefactoringContext context,
            ForStatementSyntax forStatement,
            SemanticModel semanticModel)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            ISymbol collectionSymbol = semanticModel.GetSymbolInfo(GetCollectionExpression(forStatement)).Symbol;

            if (collectionSymbol == null)
                return false;

            IEnumerable<IdentifierNameSyntax> identifierNames = await GetIdentifierNamesAsync(context.Document, forStatement, root, semanticModel, context.CancellationToken);

            foreach (IdentifierNameSyntax identifierName in identifierNames)
            {
                if (identifierName.Parent?.IsKind(SyntaxKind.Argument) != true)
                    return false;

                if (identifierName.Parent.Parent?.IsKind(SyntaxKind.BracketedArgumentList) != true)
                    return false;

                if (identifierName.Parent.Parent.Parent?.IsKind(SyntaxKind.ElementAccessExpression) != true)
                    return false;

                var elementAccess = (ElementAccessExpressionSyntax)identifierName.Parent.Parent.Parent;

                ISymbol symbol = semanticModel.GetSymbolInfo(elementAccess.Expression, context.CancellationToken).Symbol;

                if (!collectionSymbol.Equals(symbol))
                    return false;
            }

            return true;
        }

        private static async Task<IEnumerable<IdentifierNameSyntax>> GetIdentifierNamesAsync(
            Document document,
            ForStatementSyntax forStatement,
            SyntaxNode root,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            IEnumerable<ReferencedSymbol> referencedSymbols = await SymbolFinder.FindReferencesAsync(
                semanticModel.GetDeclaredSymbol(forStatement.Declaration.Variables[0]),
                document.Project.Solution,
                cancellationToken);

            return GetIdentifierNames(referencedSymbols, forStatement, root);
        }

        private static IEnumerable<IdentifierNameSyntax> GetIdentifierNames(
            IEnumerable<ReferencedSymbol> referencedSymbols,
            ForStatementSyntax forStatement,
            SyntaxNode root)
        {
            foreach (ReferencedSymbol referencedSymbol in referencedSymbols)
            {
                foreach (ReferenceLocation item in referencedSymbol.Locations)
                {
                    IdentifierNameSyntax node = root
                        .FindNode(item.Location.SourceSpan, getInnermostNodeForTie: true)
                        .FirstAncestorOrSelf<IdentifierNameSyntax>();

                    if (forStatement.Statement.Span.Contains(node.Span))
                        yield return node;
                }
            }
        }

        private static ExpressionSyntax GetCollectionExpression(ForStatementSyntax forStatement)
        {
            var condition = (BinaryExpressionSyntax)forStatement.Condition;

            var memberAccessExpression = (MemberAccessExpressionSyntax)condition.Right;

            return memberAccessExpression.Expression;
        }

        private class ForToForEachSyntaxRewriter : CSharpSyntaxRewriter
        {
            private readonly IList<ElementAccessExpressionSyntax> _expressions;
            private readonly string _elementName;

            public ForToForEachSyntaxRewriter(IList<ElementAccessExpressionSyntax> expressions, string elementName = ElementName)
            {
                _expressions = expressions;
                _elementName = elementName;
            }

            public override SyntaxNode VisitElementAccessExpression(ElementAccessExpressionSyntax node)
            {
                if (_expressions.Contains(node))
                    return IdentifierName(_elementName).WithTriviaFrom(node);

                return base.VisitElementAccessExpression(node);
            }
        }
    }
}
