namespace Cthangover.Mods.ScenarioStudio
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Cthangover.Core.Mods;

    /// <summary>
    /// In-memory authoring preview over one borrowed provider. Only explicitly supplied logical text paths are replaced;
    /// every other read stays in that same provider. Disposal never disposes the borrowed source.
    /// </summary>
    internal sealed class StudioTextOverlayProvider : IModFileProvider
    {
        private readonly IModFileProvider source;
        private readonly Dictionary<string, string> texts = new(StringComparer.Ordinal);

        /// <summary>Copies preview entries so later caller edits cannot alter a shared-reader resolution in progress.</summary>
        internal StudioTextOverlayProvider(IModFileProvider source, IReadOnlyDictionary<string, string> texts)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            foreach (var pair in texts)
            {
                this.texts.Add(ModPath.Normalize(pair.Key), pair.Value);
            }
        }

        /// <inheritdoc />
        public string Mod => source.Mod;

        /// <inheritdoc />
        public string RootPath => source.RootPath;

        /// <inheritdoc />
        public bool FileExists(string path)
        {
            return texts.ContainsKey(ModPath.Normalize(path)) || source.FileExists(path);
        }

        /// <inheritdoc />
        public string ReadFileText(string path)
        {
            return texts.TryGetValue(ModPath.Normalize(path), out var text) ? text : source.ReadFileText(path);
        }

        /// <inheritdoc />
        public byte[] ReadFileBinary(string path)
        {
            return texts.TryGetValue(ModPath.Normalize(path), out var text) ? Encoding.UTF8.GetBytes(text) : source.ReadFileBinary(path);
        }

        /// <inheritdoc />
        public Stream OpenStream(string path)
        {
            return texts.TryGetValue(ModPath.Normalize(path), out var text)
                ? new MemoryStream(Encoding.UTF8.GetBytes(text), false) : source.OpenStream(path);
        }

        /// <inheritdoc />
        public string GetFileSystemPath(string path)
        {
            return texts.ContainsKey(ModPath.Normalize(path)) ? null : source.GetFileSystemPath(path);
        }

        /// <inheritdoc />
        public string ResolveIncludePath(string includePath, string currentPath)
        {
            return source.ResolveIncludePath(includePath, currentPath);
        }

        /// <inheritdoc />
        public IEnumerable<string> ListFiles(string directory = "")
        {
            var prefix = ModPath.NormalizeDirectory(directory);
            var entries = new HashSet<string>(source.ListFiles(directory), StringComparer.Ordinal);
            foreach (var path in texts.Keys)
            {
                if (!path.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var slash = path.IndexOf('/', prefix.Length);
                entries.Add(slash < 0 ? path : path.Substring(0, slash + 1));
            }

            return entries;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
