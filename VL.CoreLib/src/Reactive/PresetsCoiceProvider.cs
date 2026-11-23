using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Reactive;

[assembly: ImportType(typeof(PresetsCoiceProvider), Category = "Reactive.Advanced")]

namespace VL.Lib.Reactive;

/// <summary>
/// Reads the preset names from a pin named "Presets" and offers them as choices. The Presets class is patched.
/// </summary>
public sealed class PresetsCoiceProvider : IChoiceProvider
{
    public const string PresetsPinName = "Presets";

    public async Task<IReadOnlyList<ChoiceEntry>> GetChoicesAsync(IChoiceContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Try to read the presets from the pin, if it exists
            return await context.ReadInputAsync(PresetsPinName, ReadPresets);
        }
        catch (ArgumentException)
        {
            // The pin doesn't exist, try to read the presets from the default instance of the Presets class
            return await context.ReadInstanceAsync(n =>
            {
                if (n is not IVLObject o)
                    return ImmutableArray<ChoiceEntry>.Empty;

                var presetsType = context.TypeRegistry.GetTypeByName("Presets [Application]");
                if (presetsType is null)
                    return ImmutableArray<ChoiceEntry>.Empty;

                var presets = presetsType.GetDefaultValue();
                if (presets is null)
                    return ImmutableArray<ChoiceEntry>.Empty;

                return ReadPresets(presets);
            });
        }

        static ImmutableArray<ChoiceEntry> ReadPresets(object instance)
        {
            // TODO: Use proper interface instead of dynamic
            var presets = (dynamic)instance;
            presets.GetPresetNames(out IEnumerable<string> presetNames);

            var choices = ImmutableArray.CreateBuilder<ChoiceEntry>();
            foreach (var presetName in presetNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                choices.Add(new ChoiceEntry(presetName));
            }
            return choices.ToImmutable();
        }
    }
}
