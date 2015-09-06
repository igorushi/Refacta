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
    class MoveTypeToFileRefactoring 
    {
        private CodeRefactoringContext context;

        public MoveTypeToFileRefactoring(CodeRefactoringContext context)
        {
            this.context = context;
        }

        public async Task<CodeAction> GetCodeAction()
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);
            var typeDecl = node as BaseTypeDeclarationSyntax;

            return CodeAction.Create("Move type to separate file", c => MoveTypeToFile(context.Document, typeDecl, c));
        }

        private async Task<Solution> MoveTypeToFile(Document document, BaseTypeDeclarationSyntax typeDeclaration, CancellationToken c)
        {
            try
            {
                var curentDocument = await DeleteDeclarationFromDocument(typeDeclaration, document);
                var newDocument = MoveTypeToFile(typeDeclaration, curentDocument);
                return newDocument.Project.Solution;
                //dte.ActiveDocument.ProjectItem. -> new_item.Open();
            }
            catch (Exception e)
            {
                LogException(e);
                return document.Project.Solution;
            }
        }

        private Document MoveTypeToFile(BaseTypeDeclarationSyntax typeDeclaration, Document curentDocument)
        {
            var typeName = typeDeclaration.Identifier.Text;
            var fileName = typeName + ".cs";
            var newDocument = MoveTypeToFile(curentDocument, typeDeclaration, fileName);
            return newDocument;
        }

        private static async Task<Document> DeleteDeclarationFromDocument(SyntaxNode node, Document curentDocument)
        {
            var root = await curentDocument.GetSyntaxRootAsync();
            var newTypeDeclarationParent = node.Parent.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
            var newRoot = root.ReplaceNode(node.Parent, newTypeDeclarationParent);
            return curentDocument.WithSyntaxRoot(newRoot);
        }

        private Document MoveTypeToFile(Document document, BaseTypeDeclarationSyntax typeDeclaration, string newFileName)
        {
            var nameSpaces = typeDeclaration.GetAncestorsOrThis<NamespaceDeclarationSyntax>().Reverse();
            var root = typeDeclaration.GetAncestorOrThis<CompilationUnitSyntax>();


            //agregate -> create namespaces from bottom to top
            var rootNameSpace = nameSpaces.Aggregate(typeDeclaration, (MemberDeclarationSyntax curentBody, NamespaceDeclarationSyntax curentNamespace) =>
            {
                var newBody = new SyntaxList<MemberDeclarationSyntax>().Add(curentBody);
                return SyntaxFactory.NamespaceDeclaration(curentNamespace.Name, curentNamespace.Externs, curentNamespace.Usings, newBody)
                                    .WithLeadingTrivia(curentNamespace.GetLeadingTrivia())
                                    .WithTrailingTrivia(curentNamespace.GetTrailingTrivia());
            });

            var newRoot = SyntaxFactory.CompilationUnit(root.Externs, root.Usings, root.AttributeLists, new SyntaxList<MemberDeclarationSyntax>().Add(rootNameSpace));

            var newDoc = document.Project.AddDocument(newFileName, newRoot, document.Folders);
            return newDoc;
        }



        private void LogException(Exception e)
        {

        }
    }
}