# Scenario Studio — debugging and editing scenarios while the game runs

Scenario Studio is a dock beside the running game that shows which scenario is playing and which line is on the screen. It can pause the dialog, step through it line by line, stop at breakpoints, move the next line, restart a scenario with the game's state rolled back, and change the scenario on the spot — and then shows the change in the game at once.

It has two modes: **Novice** shows the story moment in plain blocks; **Author** shows the scenario text with line numbers.

## Opening it

A game that includes the Developer Tools toolset (mechanic *Scenario Studio*) has a **Studio** button — the quill — on its toolbar. The launch flag `--studio` opens the dock at start. The pictures below are from the Cthangover kitchen; edits went into a test layer, never into the game's own files.

## Novice mode

![The kitchen with the Studio dock on the right in Novice mode: scene home_kitchen, scenario home_kitchen_cooking, a layer badge; under "On screen now" the line "New line" and the question "A room equipped as a kitchen"; the answers Leave (goes to scene home_vestibule), Cook (action toggle_cooking_workbench) and New answer, each with "Choose where" and Edit; buttons "+ Line before the question" and "+ Answer"; the box "This change affects" with the target layer, and Apply at the bottom.](assets/studio-novice.jpg)

The **Now** tab shows the moment on the screen: the line, the question and every answer with where it leads. **Edit** changes a text, **Choose where** changes where an answer leads (a label, another scene, the end), **+ Line** and **+ Answer** add to the moment. Every change shows in **This change affects** which mod or layer it will be written to.

## Author mode, pausing and breakpoints

![The dock in Author mode: Pause, Step, Continue, Resources and Restart on the transport bar; the scenario text of home_kitchen with line 6 — the select "A room fitted out as a kitchen" — marked by a red breakpoint dot and the ▶ marker of the current line; under it "after line 9" with + Line, + Choice, + Go to scene, and the target layer my_studio.](assets/dock-breakpoint.jpg)

- **Pause** holds the dialog before its next line; **Step** runs one line; **Continue** goes on.
- A click in the margin sets a **breakpoint**: the dialog stops before that line.
- Dragging the **▶** marker to another line makes that line run next.
- **+ Line**, **+ Choice**, **+ Go to scene** add after the current line; **Edit text** edits the whole scenario.

## Apply and restart

**Apply** writes the changes and restarts the scenario. Here a line "New line" was added before the question; after Apply it is on the screen:

![The dock after Apply: on screen now "New line", the status "Applied to "my_studio": scenario restarted with rollback".](assets/dock-applied.jpg)

**Restart** offers: the scenario again without rollback, the scenario again **with state rollback** (flags, items, the clock and the rest are put back as they were when the scenario started), the scene again, or continuing from the current label.

## Inspecting state

![The State tab: "Values can be changed to test another branch", the clock with day 24 and hour 23:00.](assets/dock-state.jpg)

The **State** tab shows the game state grouped by owner: the clock, the variables of the running dialog, the quests the scenario refers to, the inventory and which scenarios already ran. Variables, item counts and quest status can be changed (Enter writes them into the game) to try another branch. **Scene** lists the scenarios of the current scene, **Map** shows the scenes and the ways between them (and where to drop new backgrounds), and **Log** records what happened:

![The Log tab: entered home_kitchen, home_kitchen_cooking started, the edit "+6 → my_studio", restarts, "breakpoint, line 6", "next line moved to 5", "checkpoint in home_kitchen".](assets/dock-log.jpg)

## Where changes are written

- A mod installed as a folder can be changed in place.
- Otherwise changes go to the game's **studio layer** (`user://studio/<game>`), which wins over every mod and leaves the mods untouched. The layer can be exported as a new mod, which is then added to the game's collection.
- **Resources** re-reads backgrounds dropped into a mod or into the layer's `backgrounds/` folder while the game runs; a new scene can then be made on such a background from the Map tab.

## If something goes wrong

1. **There is no Studio button.** The game's collection does not include the studio; add the Developer Tools toolset or launch with `--studio`.
2. **Step does nothing.** The dialog waits for an answer: pick one first.
3. **Other players do not see a change.** Changes in the studio layer stay in this game on this computer; export the layer as a mod to share them.
