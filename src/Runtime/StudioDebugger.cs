namespace Cthangover.Mods.ScenarioStudio
{
    using System;
    using System.Collections.Generic;
    using Cthangover.Core.Mods.Resolvers;
    using Cthangover.Mods.Dialog;
    using Cthangover.Mods.Dialog.Action;
    using Cthangover.Mods.Dialog.Executable;

    /// <summary>
    /// The studio's <see cref="IDialogDebugger"/>: pause, step and breakpoints
    /// for scenario dialogs. Only dialogs shown in the <see cref="DialogBox"/>
    /// with a <see cref="ScenarioEvent"/> as locker are held — standalone
    /// runtimes (hover commands) and click dialogs cannot be resumed and always
    /// run through. Breakpoints are keyed by scenario name, the identity that
    /// survives an edit moving the file into the studio layer.
    /// <para/>
    /// "Pause" holds the dialog before the next action; "continue" and "step"
    /// let exactly one action pass the hold (<c>allowOnce</c>), step then holds
    /// again. The first action of every new scenario event raises
    /// <see cref="ScenarioStarted"/> before anything runs, which is when the
    /// rollback snapshot is taken.
    /// </summary>
    public sealed class StudioDebugger : IDialogDebugger
    {
        private readonly Dictionary<string, HashSet<int>> _breakpoints = new();
        private readonly HashSet<int> _executed = new();
        private int _allowOnce;

        /// <summary>The scenario event the debugger last saw run.</summary>
        public ScenarioEvent Event { get; private set; }

        /// <summary>Exact active queue's reader provenance; source drift keeps this snapshot stale until an actual rebind.</summary>
        public StudioSourceProjection Projection { get; private set; }

        public bool IsPaused { get; private set; }

        /// <summary>Scenario line the dialog is held at, 0 when not held.</summary>
        public int HeldLine { get; private set; }

        /// <summary>Raised before the first action of a new scenario event runs.</summary>
        public event Action<ScenarioEvent> ScenarioStarted;

        /// <summary>Raised when the dialog is held at a line (pause or breakpoint).</summary>
        public event Action<int, bool> Held;

        /// <summary>Raised after any change worth redrawing (an action ran, a hold, a toggle).</summary>
        public event Action Changed;

        public bool ShouldHold(DialogRuntime runtime, IActionCommand action)
        {
            if (runtime.DialogBox == null || runtime.DialogBox.Locker is not ScenarioEvent scenario)
                return false;

            if (scenario != Event || Projection == null || !Projection.OwnsQueue(runtime.Dialog?.Queue))
            {
                Event = scenario;
                Projection = new StudioSourceProjection(ReadSource(scenario), scenario.ScenarioText, runtime.Dialog?.Queue,
                    !string.IsNullOrEmpty(scenario.SourceModId));
                _executed.Clear();
                HeldLine = 0;
                ScenarioStarted?.Invoke(scenario);
            }

            var line = IsProjectionCurrent(scenario, runtime.Dialog?.Queue) ? Projection.RootLine(action.SourceLine) : 0;
            if (_allowOnce > 0)
            {
                _allowOnce--;
                return Pass(line);
            }

            var breakpoint = line > 0 && HasBreakpoint(scenario.ID, line) && Projection.IsBreakpointAction(action, line);
            if (!IsPaused && !breakpoint)
                return Pass(line);

            IsPaused = true;
            HeldLine = line;
            Held?.Invoke(line, breakpoint);
            Changed?.Invoke();
            return true;
        }

        public bool IsExecuted(int line) => _executed.Contains(line);

        public bool HasBreakpoint(string scenario, int line) =>
            scenario != null && _breakpoints.TryGetValue(scenario, out var lines) && lines.Contains(line);

        public void ToggleBreakpoint(string scenario, int line)
        {
            if (string.IsNullOrEmpty(scenario))
                return;
            if (!_breakpoints.TryGetValue(scenario, out var lines))
                _breakpoints[scenario] = lines = new HashSet<int>();
            if (!lines.Remove(line))
                lines.Add(line);
            Changed?.Invoke();
        }

        /// <summary>Turns pause on: the next action of a scenario dialog will be held.</summary>
        public void Pause()
        {
            IsPaused = true;
            Changed?.Invoke();
        }

        /// <summary>
        /// Turns pause off and lets the held action run (<paramref name="box"/>
        /// resumes it); without a hold it only clears the pause.
        /// </summary>
        public void Continue(DialogBox box)
        {
            IsPaused = false;
            ReleaseHold(box);
            Changed?.Invoke();
        }

        /// <summary>
        /// Runs exactly one action and holds before the next. When the dialog is
        /// not held but waits for a click, the click is made for the user.
        /// </summary>
        public void Step(DialogBox box)
        {
            IsPaused = true;
            if (box == null || !box.IsRunning)
                return;

            if (box.Runtime.IsHeld)
            {
                ReleaseHold(box);
            }
            else if (!box.IsWaitingAnswer)
            {
                _allowOnce = 1;
                box.NextAction();
            }
            Changed?.Invoke();
        }

        /// <summary>
        /// "Set next statement" of an IDE: the action of scenario line
        /// <paramref name="line"/> becomes the next one to run and the dialog is
        /// held before it in pause, whether it was held, waited for a click or
        /// for an answer. Skipped and re-run lines change nothing in the game
        /// state on their own. False when the line has no action in the queue
        /// of the running dialog (a comment, an answer, a line added in the studio).
        /// </summary>
        public bool SetNext(DialogBox box, int line)
        {
            var queue = box?.Runtime.Dialog?.Queue;
            if (line <= 0 || queue == null || !box.IsRunning || box.Locker != Event)
                return false;

            var target = FirstActionIndex(box, line);
            if (target < 0 || !box.MoveTo(target))
                return false;

            IsPaused = true;
            HeldLine = line;
            _allowOnce = 0;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Forgets the current event, e.g. before restarting it.</summary>
        public void Reset()
        {
            Event = null;
            Projection = null;
            HeldLine = 0;
            _allowOnce = 0;
            _executed.Clear();
        }

        /// <summary>Queue index of the first action built from scenario line <paramref name="line"/>, -1 when none.</summary>
        public static int IndexOf(List<IActionCommand> queue, int line)
        {
            for (var i = 0; i < queue.Count; i++)
            {
                if (queue[i]?.SourceLine == line)
                    return i;
            }
            return -1;
        }

        private void ReleaseHold(DialogBox box)
        {
            if (box == null || !box.Runtime.IsHeld)
                return;
            HeldLine = 0;
            _allowOnce = 1;
            box.Resume();
        }

        /// <summary>Checks source and actual queue identity; a stale snapshot never silently rebinds to a moving provider.</summary>
        public bool IsProjectionCurrent(ScenarioEvent scenario, IReadOnlyList<IActionCommand> queue)
        {
            if (scenario == null || scenario != Event || Projection == null || !Projection.IsUsable)
            {
                return false;
            }

            var wasUsable = Projection.IsUsable;
            var current = Projection.Matches(scenario.ScenarioText, ReadSource(scenario), queue);
            if (wasUsable && !current)
            {
                Changed?.Invoke();
            }

            return current;
        }

        /// <summary>Projected root row of an actual running expanded action; zero explicitly denotes unavailable correspondence.</summary>
        public int RootLine(DialogBox box, int expandedLine)
        {
            return box != null && box.Locker == Event && IsProjectionCurrent(Event, box.Runtime.Dialog?.Queue)
                ? Projection.RootLine(expandedLine) : 0;
        }

        /// <summary>First actual queue target for a root row; requires the currently running event's exact source binding.</summary>
        public int FirstActionIndex(DialogBox box, int rootLine)
        {
            return box != null && box.IsRunning && box.Locker == Event
                && IsProjectionCurrent(Event, box.Runtime.Dialog?.Queue) ? Projection.FirstActionIndex(rootLine) : -1;
        }

        private static ResolvedModText ReadSource(ScenarioEvent scenario)
        {
            return string.IsNullOrEmpty(scenario?.SourceModId)
                ? null : ScenarioDefs.ReadSource(scenario.SourceModId, scenario.SourcePath);
        }

        private bool Pass(int line)
        {
            if (line > 0)
                _executed.Add(line);
            Changed?.Invoke();
            return false;
        }
    }
}
