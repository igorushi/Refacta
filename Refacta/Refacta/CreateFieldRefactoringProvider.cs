using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.CodeAnalysis.Editing;

namespace Refacta
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(TypeAndFileNameMismatchRefactoringProvider)), Shared]
    internal class CreateFieldRefactoringProvider : CodeRefactoringProvider
    {
        private string yaya;
        public CreateFieldRefactoringProvider(string kuku)
        {
            yaya = kuku;
        }
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            var parameterDeclaration = node as ParameterSyntax;
            if (parameterDeclaration == null)
                return;


            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetDeclaredSymbol(parameterDeclaration);

            var ctorDeclaration = GetParentOfType<ConstructorDeclarationSyntax>(node);
            if (ctorDeclaration == null)
                return;

            var classDeclaration = GetParentOfType<ClassDeclarationSyntax>(node);
            var fieldDeclarations = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>();

            //ctorDeclaration.DescendantNodes().OfType()

            //foreach (var field in fieldDeclarations)
            //{
            //    if(field.N)

            //}

        }

        private static T GetParentOfType<T>(SyntaxNode node)
        {
            return node.Ancestors().OfType<T>().FirstOrDefault();
        }
    }
}