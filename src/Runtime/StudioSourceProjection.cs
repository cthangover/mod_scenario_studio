namespace Cthangover.Mods.ScenarioStudio
{
    using System;
    using System.Collections.Generic;
    using Cthangover.Core.Mods.Resolvers;
    using Cthangover.Mods.Dialog.Action;

    /// <summary>
    /// Binds one actual queue to the shared reader's immutable source snapshot. Runtime SourceLine stays expanded;
    /// only Studio coordinates are projected. Source/queue drift and composite root rows never get guessed targets.
    /// </summary>
    public sealed class StudioSourceProjection
    {
        private readonly IReadOnlyList<IActionCommand> queue;
        private readonly IActionCommand[] actions;
        private readonly int[] sourceLines;
        private readonly string[] ids;
        private readonly string[] expandedLines;
        private readonly bool bound;
        private readonly bool fileBacked;
        private bool stale;

        /// <summary>Captures the exact program passed to the parser and references to its resulting queue actions.</summary>
        public StudioSourceProjection(ResolvedModText source, string expandedText, IReadOnlyList<IActionCommand> queue, bool fileBacked)
        {
            Source = source;
            ExpandedText = expandedText;
            this.queue = queue;
            this.fileBacked = fileBacked;
            bound = expandedText != null && queue != null && (!fileBacked || source?.ExpandedText == expandedText);
            expandedLines = (expandedText ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            actions = new IActionCommand[queue?.Count ?? 0];
            sourceLines = new int[actions.Length];
            ids = new string[actions.Length];
            for (var i = 0; i < actions.Length; i++)
            {
                actions[i] = queue[i];
                sourceLines[i] = queue[i]?.SourceLine ?? 0;
                ids[i] = queue[i]?.ID;
            }
        }

        /// <summary>The reader result actually associated with this queue; null for inline programs.</summary>
        public ResolvedModText Source { get; }

        /// <summary>Exact active parser input, including expanded fragments and their labels.</summary>
        public string ExpandedText { get; }

        /// <summary>Once observed source/queue drift invalidates this binding, a fresh execution is required to rebind.</summary>
        public bool IsUsable => bound && !stale;

        /// <summary>Queue replacement is a fresh execution and permits rebind; source drift alone does not.</summary>
        public bool OwnsQueue(IReadOnlyList<IActionCommand> currentQueue)
        {
            return ReferenceEquals(queue, currentQueue);
        }

        /// <summary>
        /// Validates exact event text, provider/input identity and the captured queue coordinates before any line
        /// operation.
        /// </summary>
        public bool Matches(string eventText, ResolvedModText currentSource, IReadOnlyList<IActionCommand> currentQueue)
        {
            if (!IsUsable)
            {
                return false;
            }

            if (eventText != ExpandedText || !OwnsQueue(currentQueue) || currentQueue.Count != actions.Length
                || fileBacked && !Source.HasSameSource(currentSource))
            {
                stale = true;
                return false;
            }

            for (var i = 0; i < actions.Length; i++)
            {
                if (!ReferenceEquals(actions[i], currentQueue[i]) || sourceLines[i] != (currentQueue[i]?.SourceLine ?? 0)
                    || ids[i] != currentQueue[i]?.ID)
                {
                    stale = true;
                    return false;
                }
            }

            return true;
        }

        /// <summary>Maps an expanded action row to an unambiguous authored root row; zero means unavailable.</summary>
        public int RootLine(int expandedLine)
        {
            if (!IsUsable || expandedLine <= 0 || expandedLine > expandedLines.Length)
            {
                return 0;
            }

            if (!fileBacked)
            {
                return expandedLine;
            }

            var origin = Source.OriginOfLine(expandedLine);
            return origin != null && Source.CanProjectRootLine(origin.RootLine) ? origin.RootLine : 0;
        }

        /// <summary>First actual queue action of a root row, including the first action emitted by a root include directive.</summary>
        public int FirstActionIndex(int rootLine)
        {
            if (rootLine > 0)
            {
                for (var i = 0; i < sourceLines.Length; i++)
                {
                    if (RootLine(sourceLines[i]) == rootLine)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>A root include breakpoint stops only its first actual action; direct root rows keep their existing behavior.</summary>
        public bool IsBreakpointAction(IActionCommand action, int rootLine)
        {
            if (action == null || RootLine(action.SourceLine) != rootLine || rootLine <= 0)
            {
                return false;
            }

            var origin = Source?.OriginOfLine(action.SourceLine);
            var first = FirstActionIndex(rootLine);
            return first >= 0 && (origin == null || origin.RootIncludeOffset == 0 || ReferenceEquals(actions[first], action));
        }

        /// <summary>Only a unique label backed by an actual action in this exact program can satisfy a raw root jump target.</summary>
        public bool HasActualLabel(string target)
        {
            if (!IsUsable)
            {
                return false;
            }

            var count = 0;
            for (var i = 0; i < ids.Length; i++)
            {
                var line = sourceLines[i];
                if (ids[i] == target && line > 0 && line <= expandedLines.Length
                    && expandedLines[line - 1].Trim() == ":" + target)
                {
                    count++;
                }
            }

            return count == 1;
        }

        /// <summary>
        /// Finds the nearest preceding label that is an actual unique queue target, including included labels.
        /// No preceding label succeeds with null; duplicate targets refuse rather than restarting at another occurrence.
        /// </summary>
        public bool TryPreviousLabel(int expandedLine, out string label)
        {
            label = null;
            if (!IsUsable || expandedLine <= 0 || expandedLine > expandedLines.Length)
            {
                return false;
            }

            for (var line = expandedLine; line > 0; line--)
            {
                var text = expandedLines[line - 1].Trim();
                if (!text.StartsWith(':') || text.Length == 1)
                {
                    continue;
                }

                var name = text.Substring(1).Trim();
                var count = 0;
                var exact = false;
                for (var i = 0; i < ids.Length; i++)
                {
                    if (ids[i] == name)
                    {
                        count++;
                        exact |= sourceLines[i] == line;
                    }
                }

                if (count > 1)
                {
                    return false;
                }

                if (count == 1 && exact)
                {
                    label = name;
                    return true;
                }
            }

            return true;
        }
    }
}
