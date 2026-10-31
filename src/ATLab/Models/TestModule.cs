using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>Named group of test steps. Modules run in order; disabled modules are skipped in full runs.</summary>
public partial class TestModule : ObservableObject
{
    public TestModule()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    [JsonConstructor]
    public TestModule(string id)
    {
        Id = id;
    }

    [property: JsonPropertyOrder(1)]
    public string Id { get; }

    [ObservableProperty]
    [property: JsonPropertyOrder(2)]
    private string _name = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyOrder(3)]
    private bool _isEnabled = true;

    [ObservableProperty]
    [property: JsonPropertyOrder(4)]
    private string _description = string.Empty;

    [JsonPropertyOrder(5)]
    public ObservableCollection<TestStep> TestSteps { get; init; } = new();

    public void Renumber()
    {
        for (var i = 0; i < TestSteps.Count; i++)
            TestSteps[i].Number = i + 1;
    }

    // Step ids must stay unique across the whole project, so a duplicated module always gets new step ids.
    // Jump targets pointing inside the module are remapped to the cloned steps.
    public TestModule Clone(bool preserveIds = false)
    {
        var clone = new TestModule(preserveIds ? Id : Guid.NewGuid().ToString("N"))
        {
            Name = Name,
            IsEnabled = IsEnabled,
            Description = Description
        };

        var idMap = new Dictionary<string, string>();

        foreach (var step in TestSteps)
        {
            var stepClone = step.Clone(preserveIds);
            idMap[step.Id] = stepClone.Id;
            clone.TestSteps.Add(stepClone);
        }

        if (!preserveIds)
        {
            foreach (var step in clone.TestSteps)
            {
                if (idMap.TryGetValue(step.OnPass.JumpToId, out var passTarget))
                    step.OnPass.JumpToId = passTarget;

                if (idMap.TryGetValue(step.OnFail.JumpToId, out var failTarget))
                    step.OnFail.JumpToId = failTarget;
            }
        }

        return clone;
    }

    public static string CreateDefaultName(IEnumerable<TestModule> existing, string baseName)
    {
        var names = existing.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var index = 1;
        while (names.Contains($"{baseName} {index}"))
            index++;

        return $"{baseName} {index}";
    }
}