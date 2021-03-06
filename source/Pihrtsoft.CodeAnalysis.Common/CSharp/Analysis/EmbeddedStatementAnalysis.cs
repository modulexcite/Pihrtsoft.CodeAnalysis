﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pihrtsoft.CodeAnalysis.CSharp.Analysis
{
    public static class EmbeddedStatementAnalysis
    {
        public static StatementSyntax GetEmbeddedStatement(SyntaxNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            StatementSyntax statement = GetStatement(node);

            if (statement == null)
                return null;

            if (statement.IsKind(SyntaxKind.Block))
                return null;

            return statement;
        }

        public static StatementSyntax GetEmbeddedStatementThatShouldBeInsideBlock(SyntaxNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            StatementSyntax statement = GetStatement(node);

            if (statement == null)
                return null;

            if (statement.IsKind(SyntaxKind.Block))
                return null;

            if (statement.IsSingleline() && AllowsEmbeddedStatement(node))
                return null;

            return statement;
        }

        public static BlockSyntax GetBlockThatCanBeEmbeddedStatement(SyntaxNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var block = GetStatement(node) as BlockSyntax;
            if (block != null && block.Statements.Count == 1)
            {
                StatementSyntax statement = block.Statements[0];

                if (!statement.IsKind(SyntaxKind.LocalDeclarationStatement)
                    && !statement.IsKind(SyntaxKind.LabeledStatement)
                    && statement.IsSingleline()
                    && AllowsEmbeddedStatement(node))
                {
                    return block;
                }
            }

            return null;
        }

        public static bool IsEmbeddableStatement(StatementSyntax statement)
        {
            if (statement == null)
                throw new ArgumentNullException(nameof(statement));

            if (!statement.IsKind(SyntaxKind.LocalDeclarationStatement)
                && !statement.IsKind(SyntaxKind.LabeledStatement)
                && statement.Parent?.IsKind(SyntaxKind.Block) == true)
            {
                var block = (BlockSyntax)statement.Parent;

                return block.Statements.Count == 1
                    && block.Parent != null
                    && SupportsEmbeddedStatement(block.Parent);
            }

            return false;
        }

        public static bool IsEmbeddedStatement(StatementSyntax statement)
        {
            if (statement == null)
                throw new ArgumentNullException(nameof(statement));

            return !statement.IsKind(SyntaxKind.Block)
                && statement.Parent != null
                && SupportsEmbeddedStatement(statement.Parent)
                && (!statement.Parent.IsKind(SyntaxKind.ElseClause) || !statement.IsKind(SyntaxKind.IfStatement));
        }

        public static bool ContainsEmbeddedStatement(IfStatementSyntax ifStatement)
        {
            if (ifStatement == null)
                throw new ArgumentNullException(nameof(ifStatement));

            return ifStatement.Statement?.IsKind(SyntaxKind.Block) == false;
        }

        public static bool ContainsEmbeddedStatement(ElseClauseSyntax elseClause)
        {
            if (elseClause == null)
                throw new ArgumentNullException(nameof(elseClause));

            return elseClause.Statement?.IsKind(SyntaxKind.Block) == false;
        }

        public static bool ContainsEmbeddedStatement(ForEachStatementSyntax forEachStatement)
        {
            if (forEachStatement == null)
                throw new ArgumentNullException(nameof(forEachStatement));

            return forEachStatement.Statement?.IsKind(SyntaxKind.Block) == false;
        }

        public static bool ContainsEmbeddedStatement(ForStatementSyntax forStatement)
        {
            if (forStatement == null)
                throw new ArgumentNullException(nameof(forStatement));

            return forStatement.Statement?.IsKind(SyntaxKind.Block) == false;
        }

        public static bool ContainsEmbeddedStatement(UsingStatementSyntax usingStatement)
        {
            if (usingStatement == null)
                throw new ArgumentNullException(nameof(usingStatement));

            return usingStatement.Statement?.IsKind(SyntaxKind.Block) == false;
        }

        public static bool ContainsEmbeddedStatement(WhileStatementSyntax whileStatement)
        {
            if (whileStatement == null)
                throw new ArgumentNullException(nameof(whileStatement));

            return whileStatement.Statement?.IsKind(SyntaxKind.Block) == false;
        }

        public static bool ContainsEmbeddedStatement(DoStatementSyntax doStatement)
        {
            if (doStatement == null)
                throw new ArgumentNullException(nameof(doStatement));

            return doStatement.Statement?.IsKind(SyntaxKind.Block) == false;
        }

        public static bool ContainsEmbeddedStatement(LockStatementSyntax lockStatement)
        {
            if (lockStatement == null)
                throw new ArgumentNullException(nameof(lockStatement));

            return lockStatement.Statement?.IsKind(SyntaxKind.Block) == false;
        }

        public static bool ContainsEmbeddedStatement(FixedStatementSyntax fixedStatement)
        {
            if (fixedStatement == null)
                throw new ArgumentNullException(nameof(fixedStatement));

            return fixedStatement.Statement?.IsKind(SyntaxKind.Block) == false;
        }

        public static bool SupportsEmbeddedStatement(SyntaxNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            switch (node.Kind())
            {
                case SyntaxKind.IfStatement:
                    return true;
                case SyntaxKind.ElseClause:
                    return true;
                case SyntaxKind.ForEachStatement:
                    return true;
                case SyntaxKind.ForStatement:
                    return true;
                case SyntaxKind.UsingStatement:
                    return true;
                case SyntaxKind.WhileStatement:
                    return true;
                case SyntaxKind.DoStatement:
                    return true;
                case SyntaxKind.LockStatement:
                    return true;
                case SyntaxKind.FixedStatement:
                    return true;
                default:
                    return false;
            }
        }

        private static StatementSyntax GetStatement(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.IfStatement:
                    return ((IfStatementSyntax)node).Statement;
                case SyntaxKind.ForEachStatement:
                    return ((ForEachStatementSyntax)node).Statement;
                case SyntaxKind.ForStatement:
                    return ((ForStatementSyntax)node).Statement;
                case SyntaxKind.WhileStatement:
                    return ((WhileStatementSyntax)node).Statement;
                case SyntaxKind.DoStatement:
                    return ((DoStatementSyntax)node).Statement;
                case SyntaxKind.LockStatement:
                    return ((LockStatementSyntax)node).Statement;
                case SyntaxKind.FixedStatement:
                    return ((FixedStatementSyntax)node).Statement;
                case SyntaxKind.UsingStatement:
                    {
                        StatementSyntax statement = ((UsingStatementSyntax)node).Statement;
                        if (statement?.IsKind(SyntaxKind.UsingStatement) != true)
                            return statement;

                        break;
                    }
                case SyntaxKind.ElseClause:
                    {
                        var elseClause = (ElseClauseSyntax)node;

                        if (IfElseChainAnalysis.IsEndOfChain(elseClause))
                            return elseClause.Statement;

                        break;
                    }
            }

            return null;
        }

        private static bool AllowsEmbeddedStatement(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.IfStatement:
                    {
                        return ((IfStatementSyntax)node).Condition?.IsMultiline() != true;
                    }
                case SyntaxKind.ElseClause:
                    {
                        return true;
                    }
                case SyntaxKind.DoStatement:
                    {
                        return ((DoStatementSyntax)node).Condition?.IsMultiline() != true;
                    }
                case SyntaxKind.ForEachStatement:
                    {
                        return ((ForEachStatementSyntax)node).HasParenthesesOnSameLine();
                    }
                case SyntaxKind.ForStatement:
                    {
                        var forStatement = (ForStatementSyntax)node;

                        return forStatement.HasParenthesesOnSameLine()
                            || forStatement.Statement?.IsKind(SyntaxKind.EmptyStatement) == true;
                    }
                case SyntaxKind.UsingStatement:
                    {
                        return ((UsingStatementSyntax)node).DeclarationOrExpression()?.IsMultiline() != true;
                    }
                case SyntaxKind.WhileStatement:
                    {
                        var whileStatement = (WhileStatementSyntax)node;

                        return whileStatement.Condition?.IsMultiline() != true
                            || whileStatement.Statement?.IsKind(SyntaxKind.EmptyStatement) == true;
                    }
                case SyntaxKind.LockStatement:
                    {
                        return ((LockStatementSyntax)node).Expression?.IsMultiline() != true;
                    }
                case SyntaxKind.FixedStatement:
                    {
                        return ((FixedStatementSyntax)node).Declaration?.IsMultiline() != true;
                    }
                default:
                    {
                        Debug.Assert(false, node.Kind().ToString());

                        return false;
                    }
            }
        }
    }
}
