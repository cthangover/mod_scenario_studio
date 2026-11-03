namespace Cthangover.Mods.ScenarioStudio
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Cthangover.Core.UI.Tool;
    using Cthangover.Mods.Dialog;
    using Cthangover.Mods.Interface.Base.Lists;
    using Cthangover.Mods.Interface.Tool;
    using Cthangover.Mods.Interface.Theming;
    using Cthangover.Tools.Contracts;
    using Cthangover.Tools.Services;
    using Godot;

    /// <summary>The existing Studio dock's background tab. It inspects compiled execution and opens the existing source editor.</summary>
    public partial class StudioBackgroundPage : ModWidget
    {
        private OptionButton scenarios;
        private OptionButton executions;
        private Label state;
        private Label facts;
        private Label status;
        private CodeEdit code;
        private TextEdit journal;
        private CheckButton world;
        private readonly List<BackgroundScenarioInfo> catalog = new();
        private readonly List<BackgroundRunInfo> runs = new();
        private bool refreshing;
        private double elapsed;
        public string SelectedScenarioId { get; private set; }
        public string SelectedRunId { get; private set; }
        public BackgroundRunInfo SelectedRun { get; private set; }
        public event Action Changed;

        protected override void Construct()
        {
            Name = "BackgroundExecutions";
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var column = new VBoxContainer { Name = "Column", AnchorRight = 1, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            AddChild(column);
            column.AddChild(Text("studio/background_hint"));
            scenarios = new OptionButton { Name = "Scenarios", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            column.AddChild(scenarios); scenarios.ItemSelected += OnScenarioPicked;
            state = Text(""); column.AddChild(state);
            var row = new HFlowContainer(); column.AddChild(row);
            Button(row, "Play", "studio/background_play", OnPlay);
            Button(row, "Company", "studio/background_company", OnCompany);
            Button(row, "OpenEditor", "studio/background_edit", OpenEditor);
            executions = new OptionButton { Name = "Executions", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            column.AddChild(executions); executions.ItemSelected += OnRunPicked;
            row = new HFlowContainer(); column.AddChild(row);
            Button(row, "Pause", "studio/pause", OnPause);
            Button(row, "Step", "studio/step", OnStep);
            Button(row, "Continue", "studio/continue", OnContinue);
            Button(row, "Clock", "studio/background_clock", OnClock);
            Button(row, "Restart", "studio/restart", OnRestart);
            Button(row, "Stop", "studio/background_stop", OnStop);
            world = new CheckButton { Name = "PauseWorld", Text = Tr("studio/background_world") };
            column.AddChild(world); world.Toggled += OnWorldToggled;
            facts = Text(""); facts.Name = "Facts"; facts.MaxLinesVisible = 7; column.AddChild(facts);
            column.AddChild(Text("studio/background_code"));
            code = new CodeEdit
            {
                Name = "CompiledCode", Editable = false, CustomMinimumSize = new Vector2(0, 230),
                SizeFlagsHorizontal = SizeFlags.ExpandFill, GuttersDrawLineNumbers = true,
                GuttersDrawBreakpointsGutter = true, GuttersDrawExecutingLines = true,
            };
            code.SyntaxHighlighter = ScenarioSyntaxService.CreateHighlighter();
            code.AddThemeFontOverride("font", StudioStyles.Mono);
            code.AddThemeFontSizeOverride("font_size", 13);
            column.AddChild(code); code.BreakpointToggled += OnBreakpoint;
            column.AddChild(Text("studio/background_events"));
            journal = new TextEdit { Name = "Events", Editable = false, CustomMinimumSize = new Vector2(0, 130) };
            column.AddChild(journal);
            status = Text(""); status.Name = "Status"; column.AddChild(status);
            Refresh();
        }
        public override void _Process(double delta)
        {
            if (!IsVisibleInTree()) return;
            elapsed += delta;
            if (elapsed < 0.35) return;
            elapsed = 0;
            if (!scenarios.GetPopup().Visible && !executions.GetPopup().Visible) { Refresh(); Changed?.Invoke(); }
        }
        private static Label Text(string key) => new Label
        {
            Text = key.Length == 0 ? "" : TranslationServer.Translate(key),
            AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        private static void Button(Node row, string name, string key, Action handler)
        {
            var button = new Button { Name = name, Text = TranslationServer.Translate(key), FocusMode = FocusModeEnum.None };
            row.AddChild(button); button.Pressed += handler;
        }
        public void Refresh()
        {
            if (scenarios == null) return;
            refreshing = true;
            try
            {
                var tools = BackgroundScenarios.Provider;
                if (tools == null) { state.Text = Tr("studio/background_unavailable"); return; }
                catalog.Clear(); catalog.AddRange(tools.Catalog());
                runs.Clear(); runs.AddRange(tools.Executions());
                scenarios.Clear(); executions.Clear();
                BackgroundScenarioInfo selected = null;
                for (var i = 0; i < catalog.Count; i++)
                {
                    var item = catalog[i]; scenarios.AddItem(item.Id);
                    if (item.Id == SelectedScenarioId) { scenarios.Select(i); selected = item; }
                }
                if (selected == null && catalog.Count > 0) { selected = catalog[0]; SelectedScenarioId = selected.Id; scenarios.Select(0); }
                SelectedRun = null;
                for (var i = 0; i < runs.Count; i++)
                {
                    var item = runs[i]; executions.AddItem(item.ScenarioId + " [" + item.RunId.Substring(0, 6) + "]");
                    if (item.RunId == SelectedRunId) { executions.Select(i); SelectedRun = item; }
                }
                if (SelectedRun == null)
                    for (var i = 0; i < runs.Count; i++)
                        if (runs[i].ScenarioId == SelectedScenarioId) { SelectedRun = runs[i]; executions.Select(i); break; }
                SelectedRunId = SelectedRun?.RunId;
                state.Text = selected == null ? Tr("studio/background_empty")
                    : selected.Map + " · " + Tr(StatusKey(selected.Status)) + "\n" + selected.ModId + "/" + selected.Path;
                var builder = new StringBuilder(SelectedRun == null ? selected?.Details ?? "" : "");
                if (SelectedRun != null)
                {
                    builder.AppendLine(SelectedRun.Label + " · " + SelectedRun.Wait + " · " + SelectedRun.Remaining.ToString("F2") + "s");
                    foreach (var variable in SelectedRun.Variables) builder.AppendLine(variable.Key + " = " + variable.Value);
                    builder.AppendLine(SelectedRun.Details);
                }
                facts.Text = builder.ToString().Replace("\r\n", "\n");
                facts.TooltipText = facts.Text;
                var body = SelectedRun?.Body ?? selected?.Body ?? "";
                if (code.Text != body) code.Text = body;
                code.ClearExecutingLines();
                var scenarioId = SelectedRun?.ScenarioId ?? SelectedScenarioId;
                for (var i = 0; i < code.GetLineCount(); i++) code.SetLineAsBreakpoint(i, tools.HasBreakpoint(scenarioId, i + 1));
                if (SelectedRun?.Line > 0 && SelectedRun.Line <= code.GetLineCount()) code.SetLineAsExecuting(SelectedRun.Line - 1, true);
                world.SetPressedNoSignal(tools.WorldPaused);
                CustomMinimumSize = new Vector2(0, GetNode<VBoxContainer>("Column").GetCombinedMinimumSize().Y);
                var trace = tools.Events; builder.Clear();
                for (var i = Math.Max(0, trace.Count - 24); i < trace.Count; i++) builder.AppendLine(trace[i]);
                if (journal.Text != builder.ToString()) { journal.Text = builder.ToString(); journal.ScrollVertical = journal.GetLineCount(); }
            }
            finally { refreshing = false; }
        }
        private static string StatusKey(string value) => value switch
        {
            "running" => "studio/background_status_running",
            "no_world" => "studio/background_status_no_world",
            "wrong_map" => "studio/background_status_wrong_map",
            "stopped" => "studio/background_status_stopped",
            "once" => "studio/background_status_once",
            "cooldown" => "studio/background_status_cooldown",
            "actor_missing" => "studio/background_status_actor_missing",
            "moving" => "studio/background_status_moving",
            "busy" => "studio/background_status_busy",
            "trigger" => "studio/background_status_trigger",
            "ready" => "studio/background_status_ready",
            "condition_false" => "studio/background_status_condition_false",
            _ => "studio/background_empty",
        };
        private void OnScenarioPicked(long index)
        {
            if (refreshing || index < 0 || index >= catalog.Count) return;
            SelectedScenarioId = catalog[(int)index].Id; SelectedRunId = null; Refresh(); Changed?.Invoke();
        }
        private void OnRunPicked(long index)
        {
            if (refreshing || index < 0 || index >= runs.Count) return;
            SelectedRunId = runs[(int)index].RunId; SelectedScenarioId = runs[(int)index].ScenarioId; Refresh(); Changed?.Invoke();
        }
        /// <summary>Public selection is also used by the editor's live-debug entry point.</summary>
        public void SelectScenario(string id) { SelectedScenarioId = id; SelectedRunId = null; Refresh(); }
        public void SelectExecution(string id)
        {
            SelectedRunId = id;
            if (BackgroundScenarios.Provider != null)
                foreach (var run in BackgroundScenarios.Provider.Executions()) if (run.RunId == id) SelectedScenarioId = run.ScenarioId;
            Refresh();
        }
        public void Send(string command)
        {
            if (BackgroundScenarios.Provider == null) return;
            status.Text = BackgroundScenarios.Provider.Command(SelectedRunId, command, out var error) ? Tr("studio/background_applied") : error;
            Refresh(); Changed?.Invoke();
        }
        private void Play(bool company)
        {
            foreach (var item in catalog)
                if (item.Id == SelectedScenarioId)
                {
                    status.Text = BackgroundScenarios.Provider.Play(item.ModId, item.Path, item.Source, company, out var error)
                        ? Tr("studio/background_queued") : error;
                    Refresh(); return;
                }
        }
        private void OnPlay() => Play(false);
        private void OnCompany() => Play(true);
        private void OnPause() => Send("pause");
        private void OnStep() => Send("step");
        private void OnContinue() => Send("continue");
        private void OnClock() => Send("clock");
        private void OnRestart() => Send("restart");
        private void OnStop() => Send("stop");
        private void OnWorldToggled(bool value) { if (!refreshing && BackgroundScenarios.Provider != null) BackgroundScenarios.Provider.WorldPaused = value; }
        private void OnBreakpoint(long line)
        {
            if (refreshing) return;
            BackgroundScenarios.Provider?.ToggleBreakpoint(SelectedRun?.ScenarioId ?? SelectedScenarioId, (int)line + 1);
        }
        public void OpenEditor()
        {
            foreach (var item in catalog)
            {
                if (item.Id != SelectedScenarioId) continue;
                var window = ToolFactory.Instance.Get("scenario_editor")?.CreateWindow();
                if (window is IScenarioResourceEditor editor) { editor.OpenScenario(item.ModId, item.Path); ToolWindow.ShowWindow(window); }
                else { window?.Free(); status.Text = Tr("studio/background_editor_missing"); }
                return;
            }
        }
    }
}
