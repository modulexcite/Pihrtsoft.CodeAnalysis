﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Pihrtsoft.CodeAnalysis.CSharp.Refactoring;

namespace Pihrtsoft.CodeAnalysis.CSharp.DiagnosticAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConvertForEachToForDiagnosticAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticDescriptors.ConvertForEachToFor,
                    DiagnosticDescriptors.ConvertForEachToForFadeOut);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.RegisterSyntaxNodeAction(f => AnalyzeForEachStatement(f), SyntaxKind.ForEachStatement);
        }

        private void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
        {
            if (GeneratedCodeAnalyzer?.IsGeneratedCode(context) == true)
                return;

            var forEachStatement = (ForEachStatementSyntax)context.Node;

            if (!ForEachToForRefactoring.CanRefactor(forEachStatement, context.SemanticModel, context.CancellationToken))
                return;

            context.ReportDiagnostic(
                    DiagnosticDescriptors.ConvertForEachToFor,
                    forEachStatement.Type.GetLocation());

            TextSpan span = TextSpan.FromBounds(forEachStatement.ForEachKeyword.SpanStart + 3, forEachStatement.ForEachKeyword.Span.End);

            Location location = Location.Create(forEachStatement.SyntaxTree, span);

            context.ReportDiagnostic(DiagnosticDescriptors.ConvertForEachToForFadeOut, location);
        }
    }
}
