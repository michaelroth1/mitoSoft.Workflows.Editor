using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using mitoSoft.Workflows.Editor.Helpers.Enums;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public class MessageViewModel : ReactiveObject
    {
        public TypeMessage TypeMessage { get; set; }
        [Reactive] public string Text { get; set; }
        public MessageViewModel(TypeMessage typeMessage, string text)
        {
            TypeMessage = typeMessage;
            Text = text;
        }
    }
}
