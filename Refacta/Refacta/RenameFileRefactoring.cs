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
using EnvDTE80;

namespace Refacta
{
    internal class RenameFileRefactoring
    {
        private CodeRefactoringContext context;
        private DTE2 dte;

        public RenameFileRefactoring(DTE2 dTE, CodeRefactoringContext context)
        {
            this.dte = dTE;
            this.context = context;
        }

        public async Task<CodeAction> GetCodeAction()
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            var typeDecl = node as BaseTypeDeclarationSyntax;
            var typeName = typeDecl.Identifier.Text;

            return NoPreviewDocumentCodeAction.Create("Rename file to match type", c => RenameFile(context.Document, typeName, c));
        }

        private async Task<Solution> RenameFileByRecreate(Document document, string typeName, CancellationToken cancellationToken)
        {

            try
            {
                var root = await document.GetSyntaxRootAsync();
                var newDocument = document.Project.AddDocument(typeName + ".cs", root.GetText());
                var newProject = newDocument.Project.RemoveDocument(document.Id);
                return newProject.Solution;
                //var newName = typeName + ".cs";

                //DTE.ActiveDocument.ProjectItem.Name = newName;
                //return document;
            }
            catch (Exception e)
            {
                LogException(e);
                return document.Project.Solution;
            }

        }

        private async Task<Document> RenameFile(Document document, string typeName, CancellationToken cancellationToken)
        {

            try
            {
                var newName = typeName + ".cs";
                dte.ActiveDocument.ProjectItem.Name = newName;
                return document;
            }
            catch (Exception e)
            {
                LogException(e);
                return document;
            }

        }

        private void LogException(Exception e)
        {

        }
    }
}