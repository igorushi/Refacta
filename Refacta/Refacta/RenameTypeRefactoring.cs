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
    class RenameTypeRefactoring
    {
        private CodeRefactoringContext context;

        public RenameTypeRefactoring(CodeRefactoringContext context)
        {
            this.context = context;
        }

        public async Task<CodeAction> GetCodeAction()
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            var typeDecl = node as BaseTypeDeclarationSyntax;
            return CodeAction.Create("Rename type to match file", c => RenameType(context.Document, typeDecl, c));
        }

        private async Task<Solution> RenameType(Document document, BaseTypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            try
            {
                var newName = GetDocumentName(document);

                // Get the symbol representing the type to be renamed.
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
                var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

                // Produce a new solution that has all references to that type renamed, including the declaration.
                var originalSolution = document.Project.Solution;
                var optionSet = originalSolution.Workspace.Options;
                var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

                // Return the new solution with the now-uppercase type name.
                return newSolution;
            }
            catch (Exception e)
            {
                LogException(e);
                return document.Project.Solution;
            }

        }


        private static string GetDocumentName(Document document)
        {
            return Path.GetFileNameWithoutExtension(document.Name);
        }

        private void LogException(Exception e)
        {
            
        }
    }
}