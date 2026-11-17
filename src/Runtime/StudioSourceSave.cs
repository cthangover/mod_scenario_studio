namespace Cthangover.Mods.ScenarioStudio
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Mods.Providers;
    using Cthangover.Core.Mods.Resolvers;

    /// <summary>
    /// Preflights a raw root edit and its consumed raw dependencies through the shared reader, then writes a self-contained layer.
    /// Existing dependency files are never replaced. Source edits, destination lookup shadows and incoherent inputs refuse before writes.
    /// </summary>
    public static class StudioSourceSave
    {
        /// <summary>
        /// Resolves the edited root against its original provider, validates the exact planned layer resolution and writes the root last.
        /// The caller owns both providers/root directory. Failure keys are localized by Studio;
        /// I/O rollback removes only unchanged new files.
        /// </summary>
        public static bool TrySave(IModFileProvider source, string modId, string rootPath, string originalRaw,
            string editedRaw, string layerRoot, out string error)
        {
            error = "studio/error_layer_source_conflict";
            rootPath = ModPath.Normalize(rootPath);
            if (source == null || source.ReadFileText(rootPath) != originalRaw)
            {
                error = "studio/error_source_changed";
                return false;
            }

            var rootEdit = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [rootPath] = editedRaw
            };
            using var sourcePreview = new StudioTextOverlayProvider(source, rootEdit);
            var resolved = ScenarioDefs.ReadSource(modId, rootPath, sourcePreview);
            if (resolved == null)
            {
                error = "studio/error_source_mapping_unavailable";
                return false;
            }

            var plan = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var input in resolved.InputSnapshots)
            {
                if (plan.TryGetValue(input.Key, out var previous) && previous != input.Value)
                {
                    error = "studio/error_incoherent_source";
                    return false;
                }

                plan[input.Key] = input.Value;
            }

            using var layer = new FolderModFileProvider(layerRoot, "studio_source_save");
            foreach (var input in plan)
            {
                var existing = layer.ReadFileText(input.Key);
                var expected = input.Key == rootPath ? originalRaw : input.Value;
                var diskPath = LayerPath(layerRoot, input.Key);
                if (existing == null && (File.Exists(diskPath) || Directory.Exists(diskPath)))
                {
                    return false;
                }

                if (existing != null && existing != expected)
                {
                    return false;
                }
            }

            var oldRoot = layer.ReadFileText(rootPath);
            using var layerPreview = new StudioTextOverlayProvider(layer, plan);
            var destination = ScenarioDefs.ReadSource(modId, rootPath, layerPreview);
            if (!SameInputs(resolved, destination))
            {
                return false;
            }

            var created = new Dictionary<string, string>(StringComparer.Ordinal);
            var rootFile = LayerPath(layerRoot, rootPath);
            var temporary = rootFile + ".studio-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                foreach (var input in plan)
                {
                    if (input.Key == rootPath)
                    {
                        continue;
                    }

                    var path = LayerPath(layerRoot, input.Key);
                    if (File.Exists(path))
                    {
                        if (File.ReadAllText(path) != input.Value)
                        {
                            throw new IOException("Studio layer source changed after preflight");
                        }

                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    {
                        writer.Write(input.Value);
                    }

                    created.Add(path, input.Value);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(rootFile));
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(editedRaw);
                }

                var currentRoot = File.Exists(rootFile) ? File.ReadAllText(rootFile) : null;
                if (currentRoot != oldRoot)
                {
                    throw new IOException("Studio layer root changed after preflight");
                }

                File.Move(temporary, rootFile, oldRoot != null);
                error = null;
                return true;
            }
            catch (IOException)
            {
                error = "studio/error_layer_write_failed";
            }
            catch (UnauthorizedAccessException)
            {
                error = "studio/error_layer_write_failed";
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }

                if (error != null)
                {
                    foreach (var input in created)
                    {
                        if (File.Exists(input.Key) && File.ReadAllText(input.Key) == input.Value)
                        {
                            File.Delete(input.Key);
                        }
                    }
                }
            }

            return false;
        }

        private static string LayerPath(string layerRoot, string path)
        {
            return Path.Combine(layerRoot, path.Replace('/', Path.DirectorySeparatorChar));
        }

        private static bool SameInputs(ResolvedModText source, ResolvedModText destination)
        {
            if (destination == null || source.ExpandedText != destination.ExpandedText
                || source.InputSnapshots.Count != destination.InputSnapshots.Count)
            {
                return false;
            }

            for (var i = 0; i < source.InputSnapshots.Count; i++)
            {
                if (!source.InputSnapshots[i].Equals(destination.InputSnapshots[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
