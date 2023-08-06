using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using mitoSoft.Workflows.Editor.Helpers.Enums;
using System.Windows;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public class DialogViewModel : ReactiveValidationObject<DialogViewModel>
    {
        [Reactive] public bool? Visibility { get; set; }
        [Reactive] public DialogType Type { get; set; }
        [Reactive] public DialogResult Result { get; set; }
        [Reactive] public string Title { get; set; }


        [Reactive] public string MessageBoxText { get; set; }
        [Reactive] public MessageBoxButton MessageBoxButtons{ get; set; }


        [Reactive] public string FileDialogFilter { get; set; }
        [Reactive] public string FileName { get; set; }

        [Reactive] public string DefaultPath { get; set; }

        private void Show()
        {
            Visibility = true;
        }
        private void Clear()
        {
            Type = DialogType.noCorrect;
            Result = DialogResult.noCorrect;
            Title = "";
            MessageBoxText = "";
            FileDialogFilter = "";
            FileName = "";
        }
        public void ShowMessageBox(string messageBoxText, string caption, MessageBoxButton button)
        {
            Clear();
            MessageBoxText = messageBoxText;
            MessageBoxButtons = button;
            Type = DialogType.MessageBox;
            Title = caption;
            Show();
        }

        public void ShowOpenFileDialog(string filter, string fileName, string title, string defaultPath = null)
        {
            Clear();
            FileDialogFilter = filter;
            FileName = fileName;
            Title = title;
            Type = DialogType.OpenFileDialog;
            DefaultPath = defaultPath;
            Show();
        }

        public void ShowSaveFileDialog(string filter, string fileName, string title,string defaultPath=null)
        {
            Clear();
            FileDialogFilter = filter;
            FileName = fileName;
            Title = title;
            Type = DialogType.SaveFileDialog;
            DefaultPath = defaultPath;
            Show();
        }

    }
}
