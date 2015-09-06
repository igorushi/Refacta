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
    internal class TypeAndFileNameMismatchRefactoringProvider : CodeRefactoringProvider
    {
        private EnvDTE80.DTE2 dte;

        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }
        internal EnvDTE80.DTE2 DTE
        {
            get
            {
                if (dte == null)
                    dte = (EnvDTE80.DTE2)ServiceProvider.GetService(typeof(EnvDTE.DTE));
                return dte;
            }
        }
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            if (!await TypeAndFileNameMismatch(context))
                return;

            context.RegisterRefactoring(await new RenameTypeRefactoring(context).GetCodeAction());
            context.RegisterRefactoring(await new RenameFileRefactoring(DTE,context).GetCodeAction());
            context.RegisterRefactoring(await new MoveTypeToFileRefactoring(context).GetCodeAction());
        }

        private static async Task<bool> TypeAndFileNameMismatch(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            var typeDecl = node as BaseTypeDeclarationSyntax;
            if (typeDecl == null)
                return false;

            string fileName = GetDocumentName(context.Document);
            var typeName = typeDecl.Identifier.Text;

            if (string.Equals(typeName, fileName, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static string GetDocumentName(Document document)
        {
            return Path.GetFileNameWithoutExtension(document.Name);
        }
    }
}