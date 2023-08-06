using System;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DynamicData.Binding;
using mitoSoft.Workflows.Editor.Helpers.Enums;
using System.IO;
using Splat;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Media;
using DynamicData;
using Microsoft.Extensions.Configuration;
using System.Windows.Input;
using mitoSoft.Workflows.Editor.ViewModel;
using static System.ComponentModel.Design.ObjectSelectorEditor;

using mitoSoft.Workflows.Editor.Helpers.HashSetComparer;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public partial class NodesCanvasViewModel : ReactiveObject
    {
        public SourceList<BaseNodeViewModel> Nodes { get; set; } = new SourceList<BaseNodeViewModel>();

        public ObservableCollectionExtended<ConnectViewModel> Connects { get; set; } = new ObservableCollectionExtended<ConnectViewModel>();

        public ObservableCollectionExtended<BaseNodeViewModel> NodesForView { get; set; } = new ObservableCollectionExtended<BaseNodeViewModel>();

        public ObservableCollectionExtended<MessageViewModel> Messages { get; set; } = new ObservableCollectionExtended<MessageViewModel>();

        [Reactive] public BaseNodeViewModel SelectedNode { get; set; }

        [Reactive] public MainWindowViewModel MainWindowViewModel { get; set; }

        [Reactive] public ConnectorViewModel SelectedTransition { get; set; }

        public Dictionary<HashSet<string>, SequenceNodeViewModel> SeqPattern { get; set; } = new Dictionary<HashSet<string>, SequenceNodeViewModel>(new HashSetComparer<string>());

        [Reactive] public (Point Point, NodeType NodeType) AddNodeType { get; set; }

        [Reactive] public (Point Point, NodeType NodeType) AddParallelNodeType { get; set; }

        [Reactive] public (Point Point, NodeType NodeType) AddSubworkflowNodeType { get; set; }

        [Reactive] public Point PositionLeft { get; set; }

        [Reactive] public SelectorViewModel Selector { get; set; } = new SelectorViewModel();

        [Reactive] public DialogViewModel Dialog { get; set; } = new DialogViewModel();

        [Reactive] public CutterViewModel Cutter { get; set; }

        [Reactive] public ConnectViewModel DraggedConnect { get; set; }

        [Reactive] public ConnectorViewModel ConnectorPreviewForDrop { get; set; }

        [Reactive] public BaseNodeViewModel StartState { get; set; }

        [Reactive] public Matrix RenderTransformMatrix { get; set; }

        [Reactive] public bool ProjectSaved { get; set; } = true;

        [Reactive] public bool CodeSaved { get; set; } = true;

        [Reactive] public TypeMessage DisplayMessageType { get; set; }

        [Reactive] public string SchemePath { get; set; }

        [Reactive] public string CodePath { get; set; }


        [Reactive] public bool ShowAddSequenceControlItem { get; set; } = false;

        [Reactive] public bool ShowResolveSequenceControlItem { get; set; } = false;

        [Reactive] public bool ShowGoToDefinition { get; set; } = false;

        [Reactive] public bool NeedExit { get; set; }

        [Reactive] public string ImagePath { get; set; }

        [Reactive] public ImageFormats ImageFormat { get; set; }

        [Reactive] public bool WithoutMessages { get; set; }

        [Reactive] public Themes Theme { get; set; }

        [Reactive] public NodeCanvasClickMode ClickMode { get; set; } = NodeCanvasClickMode.Default;

        static Dictionary<Themes, string> themesPaths { get; set; } = new Dictionary<Themes, string>()
        {
            {Themes.Dark, @"Styles\Themes\Dark.xaml" },
            {Themes.Light, @"Styles\Themes\Light.xaml"},
        };

        public int NodesCount { get; set; } = 0;

        public int TransitionsCount { get; set; } = 0;

        public int EdgeCount { get; set; } = 0;

        public double ScaleMax { get; set; } = 5;

        public double ScaleMin { get; set; } = 0.2;

        public double ScaleStep { get; set; } = 1.2;

        public Point ScaleCenter { get; set; }

        public NodesCanvasViewModel()
        {
            SetTheme(Themes.Dark);

            AddNodeType = (PositionLeft, NodeType.Node);

            Cutter = new CutterViewModel(this);

            Nodes.Connect().ObserveOnDispatcher().Bind(NodesForView).Subscribe();

            SetupCommands();

            SetupSubscriptions();

            SetupStartState();
        }

        private void SetTheme(Themes theme)
        {
            var configuration = Locator.Current.GetService<IConfiguration>();

            configuration.GetSection("Appearance:Theme").Set(theme);

            Application.Current.Resources.Clear();

            var uri = new Uri(themesPaths[theme], UriKind.RelativeOrAbsolute);

            ResourceDictionary resourceDict = Application.LoadComponent(uri) as ResourceDictionary;

            Application.Current.Resources.MergedDictionaries.Add(resourceDict);

            LoadIcons();

            Theme = theme;
        }

        private void LoadIcons()
        {
            string path = @"Icons\Icons.xaml";

            var uri = new Uri(path, UriKind.RelativeOrAbsolute);

            ResourceDictionary resourceDict = Application.LoadComponent(uri) as ResourceDictionary;

            Application.Current.Resources.MergedDictionaries.Add(resourceDict);
        }

        private void ChangeTheme()
        {
            if (Theme == Themes.Dark)
            {
                SetTheme(Themes.Light);
            }
            else if (Theme == Themes.Light)
            {
                SetTheme(Themes.Dark);
            }
        }

        public string SchemeName()
        {
            if (!string.IsNullOrEmpty(this.SchemePath))
            {
                return Path.GetFileNameWithoutExtension(this.SchemePath);
            }
            else
            {
                return "SimpleStateMachine";
            }
        }

        private string CodePathName()
        {
            if (!string.IsNullOrEmpty(this.CodePath))
            {
                return Path.GetFileNameWithoutExtension(this.CodePath);
            }
            else
            {
                return "SimpleStateMachine";
            }
        }

        private void ChangeMouseCursor(NodeCanvasClickMode clickMode)
        {
            Mouse.OverrideCursor = clickMode switch
            {
                NodeCanvasClickMode.Delete => Cursors.No,
                NodeCanvasClickMode.Select => Cursors.Cross,
                NodeCanvasClickMode.Cut => Cursors.Hand,
                NodeCanvasClickMode.noCorrect => throw new NotImplementedException(),
                _ => null
            };
        }

        #region Setup Subscriptions

        private void SetupSubscriptions()
        {
            this.WhenAnyValue(x => x.NodesForView.Count)
                .Buffer(2, 1)
                .Select(x => (Previous: x[0], Current: x[1]))
                .Subscribe(x => UpdateCount(x.Previous, x.Current));

            this.WhenAnyValue(x => x.ClickMode)
                .Subscribe(value => ChangeMouseCursor(value));
        }


        #endregion Setup Subscriptions

        #region Setup Nodes

        private void SetupStartState()
        {
            string name = Nodes.Items.Any(x => x.Name == "Start") ? GetNameForNewNode(NodeType.Node) : "Start";

            StartState = new NodeViewModel(this, name, new Point());

            SetAsStart(StartState);

            Nodes.Add(StartState);

            this.ProjectSaved = true;
        }

        private void SetAsStart(BaseNodeViewModel node)
        {
            node.Input.Visible = false;

            node.CanBeDelete = false;

            StartState = node;
        }

        private string GetNameForNewNode(NodeType NodeType)
        {
            switch (NodeType)
            {
                case NodeType.Node:
                    return GetNextNodeName("State");
                case NodeType.ParallelNode:
                    return GetNextNodeName("ParallelNode");
                case NodeType.SequenceNode:
                    return GetNextNodeName("SequenceNode");
                case NodeType.SubWorkflowNode:
                    return GetNextNodeName("SubWorkflowNode");
                default:
                    return "";
            }
        }

        public string GetNameForTransition()
        {
            var name = this.GetNextEdgeName();
            EdgeCount++;
            return name;
        }

        private void UpdateCount(int oldValue, int newValue)
        {
            if (newValue > oldValue)
            {
                NodesCount++;
            }
        }

        private string GetNextEdgeName()
        {
            for (int i = 1; i < 1000; i++)
            {
                var t = $"T{i}";

                var found = this.Connects.Any(c => c.FromConnector.Name == t);

                if (!found)
                {
                    return t;
                }
            }

            throw new Exception("Error determining next transition-name!");
        }

        private string GetNextNodeName(string prefix)
        {
            for (int i = 1; i < 1000; i++)
            {
                var s = $"{prefix}{i}";

                var found = this.NodesForView.Any(n => n.Name == s);

                if (!found)
                {
                    return s;
                }
            }

            throw new Exception("Error determining next nodename!");
        }

        #endregion Setup Nodes

        #region Logging

        public void LogDebug(string message, params object[] args)
        {
            if (!WithoutMessages)
                Messages.Add(new MessageViewModel(TypeMessage.Debug, string.Format(message, args)));
        }

        public void LogError(string message, params object[] args)
        {
            DisplayMessageType = TypeMessage.Error;
            if (!WithoutMessages)
                Messages.Add(new MessageViewModel(TypeMessage.Error, string.Format(message, args)));
        }

        public void LogInformation(string message, params object[] args)
        {
            if (!WithoutMessages)
                Messages.Add(new MessageViewModel(TypeMessage.Information, string.Format(message, args)));
        }

        public void LogWarning(string message, params object[] args)
        {
            if (!WithoutMessages)
                Messages.Add(new MessageViewModel(TypeMessage.Warning, string.Format(message, args)));
        }

        #endregion Logging
    }
}
