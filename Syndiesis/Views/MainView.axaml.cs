using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.CodeAnalysis.Text;
using Syndiesis.Controls;
using Syndiesis.Utilities;
using Syndiesis.Utilities.Specific;
using Syndiesis.ViewModels;
using System;
using System.Threading.Tasks;

namespace Syndiesis.Views;

public partial class MainView : UserControl
{
    public readonly AnalysisPipelineHandler AnalysisPipelineHandler = new();

    public readonly MainWindowViewModel ViewModel = new();

    public event Action? SettingsRequested;

    public MainView()
    {
        InitializeComponent();
        InitializeView();
        InitializeEvents();

        Focusable = true;
    }

    private void InitializeView()
    {
        const string initializingSource = """
            using System;

            namespace Example;

            Console.WriteLine("Initializing application...");

            """;

        codeEditor.Editor = ViewModel.Editor;
        codeEditor.SetSource(initializingSource);
        codeEditor.CursorPosition = new(4, 48);
        codeEditor.AssociatedTreeView = syntaxTreeView.listView;
    }

    private void InitializeEvents()
    {
        codeEditor.CodeChanged += TriggerPipeline;
        codeEditor.CursorMoved += HandleCursorPositionChanged;
        syntaxTreeView.listView.HoveredNode += HandleHoveredNode;
        syntaxTreeView.NewRootNodeLoaded += HandleNewRootNodeLoaded;

        syntaxTreeView.RegisterAnalysisPipelineHandler(AnalysisPipelineHandler);

        InitializeButtonEvents();
    }

    private void InitializeButtonEvents()
    {
        resetCodeButton.Click += HandleResetClick;
        pasteOverButton.Click += HandlePasteOverClick;
        settingsButton.Click += HandleSettingsClick;
        collapseAllButton.Click += CollapseAllClick;
        expandAllButton.Click += ExpandAllClick;
    }

    private void ExpandAllClick(object? sender, RoutedEventArgs e)
    {
        syntaxTreeView.listView.RootNode.SetExpansionWithoutAnimationRecursively(true);
    }

    private void CollapseAllClick(object? sender, RoutedEventArgs e)
    {
        syntaxTreeView.listView.CollapseAll();
    }

    private void HandleSettingsClick(object? sender, RoutedEventArgs e)
    {
        SettingsRequested?.Invoke();
    }

    private void HandlePasteOverClick(object? sender, RoutedEventArgs e)
    {
        _ = HandlePasteClick();
    }

    private async Task HandlePasteClick()
    {
        var pasteText = await this.GetClipboardTextAsync();
        if (pasteText is null)
            return;

        SetSource(pasteText);
    }

    private void HandleResetClick(object? sender, RoutedEventArgs e)
    {
        Reset();
    }

    private void HandleNewRootNodeLoaded()
    {
        // trigger showing the current position of the cursor
        var position = codeEditor.CursorPosition;
        ShowCurrentCursorPosition(position);
    }

    private void HandleCursorPositionChanged(LinePosition position)
    {
        ShowCurrentCursorPosition(position);
    }

    private void ShowCurrentCursorPosition(LinePosition position)
    {
        var index = ViewModel.Editor.MultilineEditor.GetIndex(position);
        syntaxTreeView.listView.HighlightPosition(index);
    }

    private void HandleHoveredNode(SyntaxTreeListNode? obj)
    {
        codeEditor.ShowHoveredSyntaxNode(obj);
    }

    private void TriggerPipeline()
    {
        var currentSource = ViewModel.Editor.MultilineEditor.FullString();
        AnalysisPipelineHandler.InitiateAnalysis(currentSource);
    }

    public void ApplyCurrentSettings()
    {
        var settings = AppSettings.Instance;
        AnalysisPipelineHandler.AnalysisExecution.NodeLineOptions = settings.NodeLineOptions;
        AnalysisPipelineHandler.UserInputDelay = settings.UserInputDelay;
        expandAllButton.IsVisible = settings.EnableExpandingAllNodes;
        ForceRedoAnalysis();
    }

    public void Reset()
    {
        const string defaultCode = """
            #define SYNDIESIS

            using System;

            namespace Example;

            public class Program
            {
                public static void Main(string[] args)
                {
                    // using conditional compilation symbols is fun
                    const string greetings =
            #if SYNDIESIS
                        "Hello Syndiesis!"
            #else
                        "Hello World!"
            #endif
                        ;
                    Console.WriteLine(greetings);
                }
            }

            """;

        SetSource(defaultCode);
    }

    private void SetSource(string source)
    {
        var analysisPipelineHandler = AnalysisPipelineHandler;
        var viewModel = ViewModel;

        analysisPipelineHandler.IgnoreInputDelayOnce();
        codeEditor.SetSource(source);
    }

    public void ForceRedoAnalysis()
    {
        var analysisPipelineHandler = AnalysisPipelineHandler;

        analysisPipelineHandler.IgnoreInputDelayOnce();
        TriggerPipeline();
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        codeEditor.Focus();
    }
}
