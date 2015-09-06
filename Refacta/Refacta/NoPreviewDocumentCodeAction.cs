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