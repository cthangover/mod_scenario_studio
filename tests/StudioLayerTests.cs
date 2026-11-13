namespace Cthangover.Mods.ScenarioStudio.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Mods.Testing;
    using Cthangover.Core.Utils;
    using Godot;

    /// <summary>Checks the exported layer through the manifest format consumed by discovery and catalogs.</summary>
    public sealed class StudioLayerTests : IDisposable
    {
        private readonly StudioLayer layer;
        private readonly string modId;
        private readonly string target;

        private StudioLayerTests(ModTestContext context)
        {
            if (!GameEnvironment.IsRuntime)
                throw new InvalidOperationException("Studio export tests require the Godot runtime.");
            layer = new StudioLayer(new[] { "--studio-layer=" + context.SandboxDir });
            modId = "test_studio_export_" + Guid.NewGuid().ToString("N");
            target = Path.Combine(ProjectSettings.GlobalizePath("user://mods/"), modId);
        }

        [ModTest("layer/export_writes_current_category_metadata")]
        static void ExportManifest(ModTestContext context)
        {
            using var fixture = new StudioLayerTests(context);
            const string scenario = "scene: export_probe\n---\nExport probe\nend\n";
            fixture.layer.Write("scenarios/export_probe.scenario", scenario);
            var exported = fixture.layer.Export(fixture.modId);
            var text = File.ReadAllText(Path.Combine(exported, "manifest.json"));
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;

            ModAssert.False(root.TryGetProperty("category", out _), "legacy category field is absent");
            ModAssert.Equal(1, root.GetProperty("categories").GetArrayLength(), "one authored category");
            ModAssert.Equal("content", root.GetProperty("categories")[0].GetString(), "the exported category is retained");
            ModAssert.Equal("0.1.0", root.GetProperty("version").GetString(), "demo version");
            ModAssert.Equal(fixture.modId, root.GetProperty("id").GetString(), "exported mod identity");
            ModAssert.Equal(scenario, File.ReadAllText(Path.Combine(exported, "scenarios/export_probe.scenario")), "layer content is copied");

            var manifest = JsonSerializer.Deserialize<ModManifest>(text);
            var restored = JsonSerializer.Deserialize<ModManifest>(JsonSerializer.Serialize(manifest));
            ModAssert.True(restored.Categories.SequenceEqual(new[] { "content" }), "authored categories survive the manifest round-trip");
        }

        /// <summary>Removes both the scratch layer and the uniquely named user export, including after a failed assertion.</summary>
        public void Dispose()
        {
            if (Directory.Exists(target))
                Directory.Delete(target, true);
            if (Directory.Exists(layer.RootPath))
                Directory.Delete(layer.RootPath, true);
        }
    }
}
