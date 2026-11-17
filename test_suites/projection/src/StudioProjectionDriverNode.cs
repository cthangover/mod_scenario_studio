namespace Cthangover.Mods.ScenarioStudio.Tests.Projection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using Cthangover.Core.Bootstrap;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Mods.Providers;
    using Cthangover.Core.Mods.Resolvers;
    using Cthangover.Core.Mods.Testing;
    using Cthangover.Core.Scenes;
    using Cthangover.Core.Settings;
    using Cthangover.Core.UI;
    using Cthangover.Core.Utils;
    using Cthangover.Mods.Dialog;
    using Cthangover.Mods.Dialog.Action;
    using Cthangover.Mods.Dialog.Action.Impls;
    using Cthangover.Mods.Dialog.Executable;
    using Cthangover.Mods.Interface.View;
    using Godot;

    /// <summary>
    /// One new real Studio case: raw authored files drive an actual held dialog, dock gutter, projected navigation and layer restart.
    /// Every created fixture is owned by this case in a verified write lane/private layer; production resources remain untouched.
    /// </summary>
    public partial class StudioProjectionDriverNode : Node
    {
        private const string Selector = "studio_projection_review";
        private const string Terminal = "studio_projection";
        private const string Case = "studio/raw_includes_roundtrip_debug_projection";
        private const string Suite = "scenario_studio__projection";
        private const string FixtureName = "studio_projection_probe";
        private const string Prefix = "scenarios/__studio_projection";
        private const string RootPath = Prefix + "/" + FixtureName + ".scenario";
        private const string Raw = "scene: studio_projection_probe\r\ninclude_syntax: explicit-v1\r\n---\r\n"
            + "set parent ${reference}\r\n$include{parts/one.txt}\r\n$include{parts/empty.txt}\r\nptext \"Root tail\"\r\nend\r\n";
        private const string First = "\r\n:first\r\nptext \"Included first\"\r\n"
            + "$include{two.txt}\r\n$include{empty.txt}\r\n";
        private const string Second = ":deep\nptext \"Included second\"\n";
        private const string Empty = " \r\n\t";
        private const string UiRoot = "Frame/Dock/Column/";
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private readonly Dictionary<string, bool> facts = new(StringComparer.Ordinal);
        private readonly List<string> captures = new();
        private readonly Dictionary<string, string> fixture = new(StringComparer.Ordinal)
        {
            [RootPath] = Raw,
            [Prefix + "/parts/one.txt"] = First,
            [Prefix + "/parts/two.txt"] = Second,
            [Prefix + "/parts/empty.txt"] = Empty
        };
        private int phase;
        private double phaseStarted;
        private bool finished;
        private string error;
        private TestExecutionContext context;
        private StudioPanel panel;
        private string sourceRoot;
        private string originalScene;
        private SavePackage originalState;
        private bool ownsSourcePrefix;
        private bool ownsLayerPrefix;
        private bool ownsSceneFile;
        private IModFileProvider originalLayerProvider;
        private FolderModFileProvider replacement;

        /// <summary>Owns its timer; no temporary Godot node or event object survives case shutdown detached from a parent.</summary>
        public StudioProjectionDriverNode()
        {
            if (!GameEnvironment.IsRuntime)
            {
                throw new InvalidOperationException("Studio projection driver requires the real runtime");
            }

            var timer = new Timer
            {
                WaitTime = .05,
                Autostart = true,
                ProcessMode = ProcessModeEnum.Always
            };
            AddChild(timer);
            timer.Timeout += Tick;
        }

        private static StudioLive Live => StudioLive.Instance;
        private static DialogBox Box => Live?.Box;
        private static ScenarioEvent Event => Box?.Locker as ScenarioEvent;

        private void Tick()
        {
            if (finished)
            {
                return;
            }

            try
            {
                if (elapsed.Elapsed.TotalSeconds > 120)
                {
                    throw new TimeoutException("Studio projection overall timeout at phase " + phase);
                }

                if (!Advance())
                {
                    if (elapsed.Elapsed.TotalSeconds - phaseStarted > (phase == 0 ? 40 : 12))
                    {
                        throw new TimeoutException("Studio projection state did not become ready at phase " + phase);
                    }

                    return;
                }

                phase++;
                phaseStarted = elapsed.Elapsed.TotalSeconds;
            }
            catch (Exception failure)
            {
                error = failure.ToString();
                GameLogger.Log("TEST", "Studio projection failed at phase " + phase + ": " + error, LogLevel.Error);
                Finish();
            }
        }

        private bool Advance()
        {
            switch (phase)
            {
                case 0:
                    if (Live == null || Box == null || string.IsNullOrEmpty(StudioLive.SceneName))
                    {
                        return false;
                    }

                    StartFixture();
                    return true;
                case 1:
                    if (Event?.ID != FixtureName || !Box.Runtime.IsHeld)
                    {
                        return false;
                    }

                    AppServices.Current.Ui.ToggleEntry("studio");
                    return true;
                case 2:
                    if (!UiHost.Instance.TryGetInstance("studio", out var control) || control is not StudioPanel opened)
                    {
                        return false;
                    }

                    panel = opened;
                    Press(UiRoot + "Header/Row/ModeSwitch/Items/AuthorButton");
                    return true;
                case 3:
                    if (panel.Session.ScenarioName != FixtureName || !FindRow(5, out _))
                    {
                        return false;
                    }

                    CheckRootAndNavigate();
                    return true;
                case 4:
                    if (!Box.Runtime.IsHeld || Live.CurrentLine() != 5)
                    {
                        return false;
                    }

                    Fact(Live.Debugger.HasBreakpoint(FixtureName, 5), "gutterBreakpointBound");
                    Live.Debugger.Continue(Box);
                    return true;
                case 5:
                    if (Box.Runtime.CurrentAction is not ActionText text || text.Text != "Included first")
                    {
                        return false;
                    }

                    Fact(!Box.Runtime.IsHeld, "includeBreakpointFirstActionOnly");
                    Box.NextAction();
                    return true;
                case 6:
                    if (Box.Runtime.CurrentAction is not ActionText second || second.Text != "Included second"
                        || panel.Session.LabelName != ":deep")
                    {
                        return false;
                    }

                    CheckNestedAndRestart();
                    return true;
                case 7:
                    if (Event?.StartLabel != "deep" || Box.Runtime.CurrentAction is not ActionText resumed
                        || resumed.Text != "Included second")
                    {
                        return false;
                    }

                    Fact(!Box.Runtime.IsHeld, "fromIncludedLabel");
                    ChangeSourceAndCheckStale();
                    return true;
                case 8:
                    if (Live.HasSourceProjection || Live.CurrentLine() != 0 || !FindRow(5, out var stale)
                        || stale.SourceLine != 0 || stale.Error?.Key != "studio/error_source_mapping_unavailable")
                    {
                        return false;
                    }

                    Fact(!stale.CanRunNext && !stale.IsCurrent, "staleDockHonestlyUnavailable");
                    CheckStaleAndRebind();
                    return true;
                case 9:
                    if (!Live.HasSourceProjection || !Box.Runtime.IsHeld || Live.CurrentLine() != 5)
                    {
                        return false;
                    }

                    Fact(true, "restartRebindsExactSource");
                    Fact(Live.SetNextLine(4), "directRootSetNext");
                    Live.Insert(3, "# inserted without changing the active program");
                    return true;
                case 10:
                    if (!FindRow(5, out var shifted) || shifted.SourceLine != 4)
                    {
                        return false;
                    }

                    Fact(shifted.IsCurrent && Live.CanRunFrom(4), "shiftedRetainedRowsMapped");
                    Fact(FindRow(4, out var added) && added.SourceLine == 0 && !added.CanRunNext, "newRowsHaveNoTarget");
                    Live.ReplaceLine(4, "set parent pending_edit");
                    return true;
                case 11:
                    if (!FindRow(5, out var changed) || changed.SourceLine != 0)
                    {
                        return false;
                    }

                    Fact(!Live.CanRunFrom(4) && !Live.SetNextLine(4), "changedRowsRefused");
                    Live.Discard();
                    Live.ReplaceLine(6, "ptext \"Root tail edited\"");
                    Fact(Live.Apply()?.Kind == StudioScopeKind.Layer, "rawApplyToLayer");
                    return true;
                case 12:
                    if (Event?.SourceModId != StudioLayer.Id || !Box.Runtime.IsHeld || !Live.HasSourceProjection
                        || panel.Session.FileScope?.ModId != StudioLayer.Id || !FindRow(7, out var savedTail)
                        || savedTail.Text != "ptext \"Root tail edited\"")
                    {
                        return false;
                    }

                    CheckSavedLayer();
                    Capture("studio_projection_saved.png");
                    ReplaceProvider();
                    return true;
                case 13:
                    if (Live.HasSourceProjection)
                    {
                        return false;
                    }

                    Fact(!Live.CanRunFrom(5) && !Live.SetNextLine(5), "providerReplacementRefused");
                    Live.Restart(StudioRestartMode.Scenario);
                    return true;
                case 14:
                    if (!Live.HasSourceProjection || !Box.Runtime.IsHeld)
                    {
                        return false;
                    }

                    Fact(true, "providerRestartRebinds");
                    Finish();
                    return true;
                default:
                    throw new InvalidOperationException("Unexpected Studio projection phase");
            }
        }

        private void StartFixture()
        {
            context = TestExecutionContextRuntime.Current;
            Fact(context != null && context.TestId == Selector && context.Access == "write", "verifiedWriteContext");
            var layerRoot = Path.GetFullPath(Live.Layer.RootPath);
            var userRoot = Path.GetFullPath(context.UserDataRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Fact(Live.Layer.ForceLayer && layerRoot.StartsWith(userRoot, StringComparison.OrdinalIgnoreCase), "isolatedLayer");
            var provider = ModManager.Instance.GetMod(Suite)?.FileProvider as FolderModFileProvider;
            Fact(provider != null, "ownSuiteProvider");
            sourceRoot = provider.RootPath;
            var projectRoot = Path.GetFullPath(context.ProjectRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Fact(Path.GetFullPath(sourceRoot).StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase), "sourceInsideWriteLane");
            originalScene = StudioLive.SceneName;
            originalState = SaveService.BuildPackage("__studio_projection_before");
            var sourcePrefix = Path.Combine(sourceRoot, Prefix.Replace('/', Path.DirectorySeparatorChar));
            Fact(!Directory.Exists(sourcePrefix) && !File.Exists(sourcePrefix), "sourceFixtureInitiallyAbsent");
            Directory.CreateDirectory(sourcePrefix);
            ownsSourcePrefix = true;
            var layerPrefix = Path.Combine(Live.Layer.RootPath, Prefix.Replace('/', Path.DirectorySeparatorChar));
            Fact(!Directory.Exists(layerPrefix) && !File.Exists(layerPrefix), "layerFixtureInitiallyAbsent");
            ownsLayerPrefix = true;
            var sceneFile = Path.Combine(Live.Layer.RootPath, "scenes", FixtureName + ".json");
            Fact(!File.Exists(sceneFile) && !Directory.Exists(sceneFile), "sceneFixtureInitiallyAbsent");
            Live.Layer.Write("scenes/" + FixtureName + ".json", "{\"name\":\"" + FixtureName + "\"}");
            ownsSceneFile = true;
            foreach (var input in fixture)
            {
                WriteSource(input.Key, input.Value);
            }

            SceneManager.ReloadContent();
            Box.Abort();
            Live.Debugger.Reset();
            Live.Debugger.Pause();
            GetTree().Root.GetNode<SceneManager>("SceneManager").SwitchScene(FixtureName);
        }

        private void CheckRootAndNavigate()
        {
            Fact(StudioLive.SourceText(Event) == Raw, "rawRootDisplayed");
            Fact(Live.HasSourceProjection && Live.CurrentLine() == 4, "exactActiveBinding");
            var result = ScenarioDefs.ReadSource(Suite, RootPath);
            var wrong = new StudioSourceProjection(result, Event.ScenarioText + "\n# different program", Box.Runtime.Dialog.Queue, true);
            Fact(!wrong.HasActualLabel("first") && wrong.RootLine(5) == 0, "unboundLabelRefused");
            var sameActions = new List<IActionCommand>(Box.Runtime.Dialog.Queue);
            var changedEvent = new StudioSourceProjection(result, Event.ScenarioText, sameActions, true);
            Fact(changedEvent.Matches(Event.ScenarioText, result, sameActions)
                && !changedEvent.Matches(Event.ScenarioText + "\n# changed event", result, sameActions)
                && !changedEvent.Matches(Event.ScenarioText, result, sameActions), "eventTextDriftStickyRefused");
            var changedQueue = new StudioSourceProjection(result, Event.ScenarioText, sameActions, true);
            sameActions.Add(null);
            Fact(!changedQueue.Matches(Event.ScenarioText, result, sameActions), "queueMutationRefused");
            sameActions.RemoveAt(sameActions.Count - 1);
            Fact(!changedQueue.Matches(Event.ScenarioText, result, sameActions) && changedQueue.RootLine(5) == 0,
                "restoredQueueStillStale");
            var inline = new StudioSourceProjection(null, Event.ScenarioText, Box.Runtime.Dialog.Queue, false);
            Fact(inline.Matches(Event.ScenarioText, null, Box.Runtime.Dialog.Queue) && inline.RootLine(8) == 8,
                "inlineCoordinatesRetained");
            Fact(FindRow(5, out var rootInclude) && rootInclude.Text == "$include{parts/one.txt}"
                && rootInclude.Error == null && rootInclude.CanRunNext, "rawIncludeRowConsumed");
            Fact(Live.Debugger.Projection.FirstActionIndex(6) == -1 && !Live.CanRunFrom(6)
                && !Live.SetNextLine(6) && !Live.ToggleBreakpoint(FixtureName, 6), "emptyIncludeHasNoAction");
            Capture("studio_projection_root.png");
            foreach (var row in Descendants<StudioCodeLineView>(panel))
            {
                if (row.Line.SourceLine != 5)
                {
                    continue;
                }

                using var click = new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
                    Pressed = true
                };
                row.Gutter.EmitSignal(Control.SignalName.GuiInput, click);
                Fact(Live.SetNextLine(5), "includeSetNextAccepted");
                var first = Live.Debugger.Projection.FirstActionIndex(5);
                Fact(first >= 0 && ReferenceEquals(Box.Runtime.CurrentAction, Box.Runtime.Dialog.Queue[first]), "setNextFirstActualAction");
                return;
            }

            throw new InvalidOperationException("Actual Studio gutter for root include is missing");
        }

        private void CheckNestedAndRestart()
        {
            Fact(!Box.Runtime.IsHeld && Box.Runtime.CurrentAction.SourceLine == 8 && Live.CurrentLine() == 5,
                "nestedHighlightRootDirective");
            Fact(FindRow(5, out var rootInclude) && rootInclude.IsCurrent, "dockNestedRootHighlight");
            Fact(Live.TryActiveLabel(out var label) && label == "deep", "nearestIncludedLabel");
            Capture("studio_projection_include.png");
            Live.Restart(StudioRestartMode.FromLabel);
        }

        private void ChangeSourceAndCheckStale()
        {
            WriteSource(Prefix + "/parts/one.txt", First + "\r\n# source changed during hold");
            Fact(!Live.HasSourceProjection && !Live.CanRunFrom(5), "sourceDriftRefused");
        }

        private void CheckStaleAndRebind()
        {
            var before = Event;
            Fact(!Live.SetNextLine(6) && !Live.ToggleBreakpoint(FixtureName, 6), "staleLineOperationsRefused");
            Live.Restart(StudioRestartMode.FromLabel);
            Fact(ReferenceEquals(before, Event), "staleFromLabelRefused");
            WriteSource(Prefix + "/parts/one.txt", First);
            Fact(!Live.HasSourceProjection, "staleUntilExplicitRestart");
            Live.Restart(StudioRestartMode.Scenario);
        }

        private void CheckSavedLayer()
        {
            var expected = Raw.Replace("ptext \"Root tail\"", "ptext \"Root tail edited\"");
            Fact(StudioLive.SourceText(Event) == expected, "rawRootSavedExact");
            Fact(StudioLive.SourceText(Event).Contains("${reference}")
                && StudioLive.SourceText(Event).Contains("$include{parts/one.txt}"), "noExpandedOverwrite");
            var result = ScenarioDefs.ReadSource(StudioLayer.Id, RootPath);
            Fact(result != null && result.InputSnapshots.Count == 5 && result.ExpandedText == Event.ScenarioText,
                "rawInputsReachableAfterLayerRestart");
            foreach (var input in fixture)
            {
                Fact(File.ReadAllText(Path.Combine(sourceRoot, input.Key.Replace('/', Path.DirectorySeparatorChar))) == input.Value,
                    "sourceUntouched:" + input.Key);
                if (input.Key != RootPath)
                {
                    Fact(ModManager.Instance.ReadFileText(StudioLayer.Id, input.Key) == input.Value, "rawDependencyCopied:" + input.Key);
                }
            }

            Fact(Live.Copy(FixtureName) == null && Live.LastEditError == null, "copyClearedAfterRebind");
            Fact(Box.Runtime.CurrentAction.SourceLine == 5 && Live.CurrentLine() == 5, "runtimeExpandedLinesRetained");
        }

        private void ReplaceProvider()
        {
            Fact(ModOverlays.TryGet(StudioLayer.Id, out originalLayerProvider), "layerProviderCaptured");
            replacement = new FolderModFileProvider(Live.Layer.RootPath, StudioLayer.Id);
            ModOverlays.Mount(StudioLayer.Id, replacement);
            Fact(!Live.HasSourceProjection, "sameBytesNewProviderInvalidates");
        }

        private void Capture(string name)
        {
            Directory.CreateDirectory(context.ScreenshotsDirectory);
            if (!ViewportCapture.SavePng(GetViewport(), Path.Combine(context.ScreenshotsDirectory, name)))
            {
                throw new InvalidOperationException("Unable to capture " + name);
            }

            captures.Add(name);
        }

        private bool FindRow(int number, out StudioCodeLine found)
        {
            foreach (var line in panel.Session.Lines)
            {
                if (line.Number == number)
                {
                    found = line;
                    return true;
                }
            }

            found = null;
            return false;
        }

        private void Press(string path)
        {
            var button = panel.GetNode<Button>(path);
            button.EmitSignal(Button.SignalName.Pressed);
        }

        private static IEnumerable<T> Descendants<T>(Node parent) where T : Node
        {
            foreach (var child in parent.GetChildren())
            {
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var nested in Descendants<T>(child))
                {
                    yield return nested;
                }
            }
        }

        private void WriteSource(string path, string text)
        {
            var absolute = Path.Combine(sourceRoot, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, text);
        }

        private void Fact(bool condition, string key)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Studio projection assertion: " + key);
            }

            if (!facts.TryAdd(key, true))
            {
                throw new InvalidOperationException("Duplicate Studio assertion: " + key);
            }
        }

        private void Finish()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            try
            {
                Cleanup();
            }
            catch (Exception cleanup)
            {
                error = (error ?? string.Empty) + "\nCleanup: " + cleanup;
                GameLogger.Log("TEST", "Studio projection cleanup failed: " + cleanup, LogLevel.Error);
            }

            if (context != null)
            {
                var row = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["status"] = error == null ? "OK" : "FAIL",
                    ["case"] = Case,
                    ["terminal"] = Terminal,
                    ["assertionCount"] = facts.Count,
                    ["captureCount"] = captures.Count,
                    ["captures"] = string.Join(",", captures),
                    ["sourceRootSha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Raw))).ToLowerInvariant(),
                    ["error"] = error,
                    ["phase"] = phase
                };
                foreach (var fact in facts)
                {
                    row.Add(fact.Key, fact.Value);
                }

                var directory = Path.Combine(context.ArtifactRoot, "outputs", "studio_projection");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "row.json"), JsonSerializer.Serialize(row));
            }

            var passed = error == null ? 1 : 0;
            var failed = error == null ? 0 : 1;
            GameLogger.Log("TEST", $"[TEST_RESULT] name={Terminal} passed={passed} failed={failed} skipped=0");
            ApplicationExit.Quit(GetTree(), failed);
        }

        private void Cleanup()
        {
            if (Live == null)
            {
                return;
            }

            Live.Discard();
            Box?.Abort();
            Live.Debugger.Reset();
            if (originalLayerProvider != null)
            {
                ModOverlays.Mount(StudioLayer.Id, originalLayerProvider);
            }

            replacement?.Dispose();
            if (ownsSourcePrefix)
            {
                Directory.Delete(Path.Combine(sourceRoot, Prefix.Replace('/', Path.DirectorySeparatorChar)), true);
            }

            if (ownsLayerPrefix)
            {
                var path = Path.Combine(Live.Layer.RootPath, Prefix.Replace('/', Path.DirectorySeparatorChar));
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }

            if (ownsSceneFile)
            {
                File.Delete(Path.Combine(Live.Layer.RootPath, "scenes", FixtureName + ".json"));
            }

            if (originalState != null)
            {
                SaveService.ApplyPackage(originalState, "__studio_projection_before");
                SceneManager.ReloadContent();
                Live.GoToScene(originalScene);
            }

            if (panel != null && panel.IsVisibleInTree())
            {
                AppServices.Current.Ui.ToggleEntry("studio");
            }

            Fact(!ownsSourcePrefix || !Directory.Exists(Path.Combine(sourceRoot, Prefix.Replace('/', Path.DirectorySeparatorChar))),
                "sourceFixtureRestored");
            Fact(!ownsLayerPrefix || !Directory.Exists(Path.Combine(Live.Layer.RootPath, Prefix.Replace('/', Path.DirectorySeparatorChar))),
                "layerFixtureCleaned");
        }
    }
}
