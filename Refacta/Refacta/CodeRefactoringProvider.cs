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
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(RefactaCodeRefactoringProvider)), Shared]
    internal class RefactaCodeRefactoringProvider : CodeRefactoringProvider
    {
        private EnvDTE80.DTE2 dte;

        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }
        internal EnvDTE80.DTE2 DTE
        {
            get
            {
                if(dte==null)
                    dte = (EnvDTE80.DTE2)ServiceProvider.GetService(typeof(EnvDTE.DTE));
                return dte;
            }
        }
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);
            var typeDecl = node as TypeDeclarationSyntax;
            if (typeDecl == null)
                return;

            string fileName = GetDocumentName(context.Document);
            var typeName = typeDecl.Identifier.Text;

            if (!string.Equals(typeName, fileName, StringComparison.OrdinalIgnoreCase))
            {
                var renameTypeAction = CodeAction.Create("Rename type to match file", c => RenameType(context.Document, typeDecl, c));
                context.RegisterRefactoring(renameTypeAction);

                var renameFileAction = NoPreviewDocumentCodeAction.Create("Rename file to match type", c => RenameFile(context.Document, typeName, c));
                context.RegisterRefactoring(renameFileAction);

                var moveTypeAction = CodeAction.Create("Move type to separate file", c => MoveTypeToFile(context.Document, typeDecl, c));
                context.RegisterRefactoring(moveTypeAction);
            }
        }


        private static string GetDocumentName(Document document)
        {
            return Path.GetFileNameWithoutExtension(document.Name);
        }

        private async Task<Solution> MoveTypeToFile(Document document, TypeDeclarationSyntax typeDeclaration, CancellationToken c)
        {
            try
            {
                var typeName = typeDeclaration.Identifier.Text;
                var newRenamedDocument = MoveTypeToFile(document, typeDeclaration, typeName + ".cs");
                var newSolution = newRenamedDocument.Project.Solution;
                var curentDocument = newSolution.GetDocument(document.Id);

                var root = await curentDocument.GetSyntaxRootAsync();
                var newTypeDeclarationParent = typeDeclaration.Parent.RemoveNode(typeDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
                var newRoot = root.ReplaceNode(typeDeclaration.Parent, newTypeDeclarationParent);
                return curentDocument.WithSyntaxRoot(newRoot).Project.Solution;
            }
            catch (Exception e)
            {
                return document.Project.Solution;
            }
        }

        private Document MoveTypeToFile(Document document, TypeDeclarationSyntax typeDeclaration, string newFileName)
        {
            var nameSpaces = typeDeclaration.GetAncestorsOrThis<NamespaceDeclarationSyntax>().Reverse();
            var root = typeDeclaration.GetAncestorOrThis<CompilationUnitSyntax>();


            //agregate -> create namespaces from bottom to top
            var rootNameSpace = nameSpaces.Aggregate(typeDeclaration, (MemberDeclarationSyntax curentBody, NamespaceDeclarationSyntax curentNamespace) =>
            {
                var newBody = new SyntaxList<MemberDeclarationSyntax>().Add(curentBody);
                return SyntaxFactory.NamespaceDeclaration(curentNamespace.Name, curentNamespace.Externs, curentNamespace.Usings, newBody);
            });

            var newRoot = SyntaxFactory.CompilationUnit(root.Externs, root.Usings, root.AttributeLists, new SyntaxList<MemberDeclarationSyntax>().Add(rootNameSpace));

            var newDoc = document.Project.AddDocument(newFileName, newRoot, document.Folders);
            return newDoc;
        }

        private async Task<Solution> RenameType(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
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

        private async Task<Document> RenameFile(Document document, string typeName, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var newName = typeName + ".cs";
                DTE.ActiveDocument.ProjectItem.Name = newName;
                return document;
            });
        }

        
    }

    class NoPreviewDocumentCodeAction : CodeAction
    {
        private Func<CancellationToken, Task<Document>> createChangedDocument;
        private string title;

        public NoPreviewDocumentCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
        {
            this.title = title;
            this.createChangedDocument = createChangedDocument;
        }

        public static CodeAction Create(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
        {
            return new NoPreviewDocumentCodeAction(title, createChangedDocument);
        }
        public override string Title
        {
            get { return title; }
        }
        protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            return createChangedDocument(cancellationToken);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            return new CodeActionOperation[0];
        }
    }
}