﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Pihrtsoft.CodeAnalysis.CSharp.DiagnosticAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamespaceDeclarationDiagnosticAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(DiagnosticDescriptors.RemoveEmptyNamespaceDeclaration);

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.RegisterSyntaxNodeAction(f => AnalyzerNamespaceDeclaration(f), SyntaxKind.NamespaceDeclaration);
        }

        private void AnalyzerNamespaceDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (GeneratedCodeAnalyzer?.IsGeneratedCode(context) == true)
                return;

            var declaration = (NamespaceDeclarationSyntax)context.Node;

            if (declaration.Members.Count == 0
                && !declaration.OpenBraceToken.IsMissing
                && !declaration.CloseBraceToken.IsMissing
                && declaration.OpenBraceToken.TrailingTrivia.IsWhitespaceOrEndOfLine()
                && declaration.CloseBraceToken.LeadingTrivia.IsWhitespaceOrEndOfLine())
            {
                context.ReportDiagnostic(
                    DiagnosticDescriptors.RemoveEmptyNamespaceDeclaration,
                    declaration.GetLocation());
            }
        }
    }
}
