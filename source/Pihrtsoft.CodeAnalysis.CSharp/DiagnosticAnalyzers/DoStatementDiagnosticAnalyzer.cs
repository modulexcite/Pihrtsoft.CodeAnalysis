﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Pihrtsoft.CodeAnalysis.CSharp.Refactoring;

namespace Pihrtsoft.CodeAnalysis.CSharp.DiagnosticAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoStatementDiagnosticAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(DiagnosticDescriptors.AvoidUsageOfDoStatementToCreateInfiniteLoop);

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.RegisterSyntaxNodeAction(f => AnalyzeDoStatement(f), SyntaxKind.DoStatement);
        }

        private void AnalyzeDoStatement(SyntaxNodeAnalysisContext context)
        {
            if (GeneratedCodeAnalyzer?.IsGeneratedCode(context) == true)
                return;

            var doStatement = (DoStatementSyntax)context.Node;

            if (DoStatementRefactoring.CanConvertToWhileStatement(doStatement))
            {
                context.ReportDiagnostic(
                    DiagnosticDescriptors.AvoidUsageOfDoStatementToCreateInfiniteLoop,
                    doStatement.DoKeyword.GetLocation());
            }
        }
    }
}
