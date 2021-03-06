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
    public class AttributeArgumentListDiagnosticAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(DiagnosticDescriptors.RemoveEmptyAttributeArgumentList);

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.RegisterSyntaxNodeAction(f => AnalyzeAttributeArgumentList(f), SyntaxKind.AttributeArgumentList);
        }

        private void AnalyzeAttributeArgumentList(SyntaxNodeAnalysisContext context)
        {
            if (GeneratedCodeAnalyzer?.IsGeneratedCode(context) == true)
                return;

            var attributeArgumentList = (AttributeArgumentListSyntax)context.Node;

            if (attributeArgumentList.Arguments.Count == 0)
            {
                context.ReportDiagnostic(
                    DiagnosticDescriptors.RemoveEmptyAttributeArgumentList,
                    attributeArgumentList.GetLocation());
            }
        }
    }
}
