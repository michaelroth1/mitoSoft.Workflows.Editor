using ReactiveUI;
using mitoSoft.Workflows.Editor.Helpers.CodeGenerator;
using mitoSoft.Workflows.Editor.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Text;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public partial class MainWindowViewModel
    {
        public ReactiveCommand<string, Unit> CommandCopyError { get; set; }
        public ReactiveCommand<Unit, Unit> CommandCopySchemeName { get; set; }

        public ReactiveCommand<string, Unit> CommandOpenChildWindow { get; set; }

        private void SetupCommands()
        {
            CommandCopyError = ReactiveCommand.Create<string>(CopyError);
            CommandCopySchemeName = ReactiveCommand.Create(CopySchemeName);
            CommandOpenChildWindow = ReactiveCommand.Create<string>(OpenChildWindow);

        }

        private void CopyError(string errrorText)
        {
            Clipboard.SetText(errrorText);
        }
        private void CopySchemeName()
        {
            Clipboard.SetText(this.NodesCanvas.SchemePath);
        }

        private void OpenChildWindow(string SubSchemePath)
        {
            var detail = new MainWindow( NodesCanvas.MainWindowViewModel, SubSchemePath );

            detail.ViewModel.NodesCanvas.ProjectSaved = true;

            detail.ShowDialog();
        }
    }
}
