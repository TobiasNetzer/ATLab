using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATLab.Analyzer.Analysis;
using ATLab.Analyzer.Controls;
using ATLab.Analyzer.Localization;
using ATLab.Analyzer.Services;
using ATLab.Results;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.Analyzer.ViewModels;

public enum ResultFilter
{
    All,
    Passed,
    Failed
}

/// <summary>
/// Imported runs, the filters and everything computed from the filtered runs: key figures, step statistics and the
/// charts of the selected step.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly Dictionary<string, LoadedRun> _runsByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<LoadError> _errors = [];
    private readonly AnalyzerSettings? _settings;
    private bool _suspendFilters;

    /// <summary>Builds the status text, so it can be rebuilt in another language.</summary>
    private Func<string> _status = () => Localizer.Get("Status_Start");

    /// <param name="settings">Language and theme to start with and to save changes to; null in tests.</param>
    public MainWindowViewModel(AnalyzerSettings? settings = null)
    {
        Languages = Localizer.Instance.SupportedCultures.Select(c => new LanguageOption(c)).ToList();
        if (settings?.Language is { } language)
            Localizer.Instance.ChangeCulture(new CultureInfo(language));
        _selectedLanguage = Languages.First(l => l.Culture.TwoLetterISOLanguageName == Localizer.Instance.Culture.TwoLetterISOLanguageName);

        _isDarkMode = settings?.IsDarkMode ?? true;
        _settings = settings;

        _projects = [new ProjectOption(null)];
        _selectedProject = _projects[0];

        Localizer.Instance.CultureChanged += (_, _) => OnCultureChanged();
    }

    public IFileDialogs? FileDialogs { get; set; }


    // ----- Loading -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRuns), nameof(ShowWelcome))]
    private int _loadedRunCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenFilesCommand), nameof(OpenFolderCommand), nameof(ClearCommand))]
    private bool _isLoading;

    public string StatusText => _status();

    private void SetStatus(Func<string> status)
    {
        _status = status;
        OnPropertyChanged(nameof(StatusText));
    }

    /// <summary>Files that could not be read, e.g. other JSON files in an opened folder.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLoadErrors), nameof(LoadErrorsText))]
    private IReadOnlyList<LoadError> _loadErrors = [];

    public bool HasLoadErrors => LoadErrors.Count > 0;
    public string LoadErrorsText => Localizer.Format("Status_FilesSkipped", LoadErrors.Count);
    public bool HasRuns => LoadedRunCount > 0;
    public bool ShowWelcome => LoadedRunCount == 0;

    // ----- Filters -----

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ResultFilter _resultFilter = ResultFilter.All;
    [ObservableProperty] private ProjectOption _selectedProject;
    [ObservableProperty] private bool _excludeSimulator;
    [ObservableProperty] private bool _latestPerDevice;
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private IReadOnlyList<ProjectOption> _projects;

    /// <summary>Replaced on a language change, so the dropdown shows its entries in the new language.</summary>
    [ObservableProperty] private IReadOnlyList<ResultFilter> _resultFilters = Enum.GetValues<ResultFilter>();

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnResultFilterChanged(ResultFilter value) => ApplyFilters();
    partial void OnSelectedProjectChanged(ProjectOption value) => ApplyFilters();
    partial void OnExcludeSimulatorChanged(bool value) => ApplyFilters();
    partial void OnLatestPerDeviceChanged(bool value) => ApplyFilters();
    partial void OnFromDateChanged(DateTime? value) => ApplyFilters();
    partial void OnToDateChanged(DateTime? value) => ApplyFilters();

    // ----- Results of the filtered runs -----

    [ObservableProperty] private IReadOnlyList<LoadedRun> _runs = [];
    [ObservableProperty] private RunSummary _summary = RunSummary.Empty;
    [ObservableProperty] private IReadOnlyList<StepRow> _steps = [];
    [ObservableProperty] private IReadOnlyList<StepRow> _topFailures = [];
    [ObservableProperty] private IReadOnlyList<Fact> _summaryFacts = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsYieldPerWeek))]
    private IReadOnlyList<YieldBucket> _yieldHistory = [];

    /// <summary>True when the yield chart groups the runs by week because they span many days.</summary>
    public bool IsYieldPerWeek => YieldHistory.Count > 0 && YieldHistory[0].IsWeek;

    public bool HasTopFailures => TopFailures.Count > 0;
    partial void OnTopFailuresChanged(IReadOnlyList<StepRow> value) => OnPropertyChanged(nameof(HasTopFailures));

    // ----- Selected run -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedRun))]
    [NotifyCanExecuteChangedFor(nameof(ExportSelectedRunCommand))]
    private LoadedRun? _selectedRun;

    [ObservableProperty] private IReadOnlyList<RunStepRow> _selectedRunSteps = [];
    [ObservableProperty] private IReadOnlyList<Fact> _selectedRunFacts = [];

    public bool HasSelectedRun => SelectedRun is not null;

    partial void OnSelectedRunChanged(LoadedRun? value)
    {
        SelectedRunSteps = value?.File.Steps.Select(s => new RunStepRow(s)).ToList() ?? [];
        SelectedRunFacts = value is null ? [] : RunFacts(value);
    }

    // ----- Selected step -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedStep))]
    [NotifyCanExecuteChangedFor(nameof(ExportSelectedStepCommand))]
    private StepRow? _selectedStep;

    [ObservableProperty] private IReadOnlyList<double> _selectedStepValues = [];
    [ObservableProperty] private IReadOnlyList<TrendPoint> _selectedStepTrend = [];
    [ObservableProperty] private IReadOnlyList<Fact> _selectedStepFacts = [];
    // Chart options, each in its chart's panel
    [ObservableProperty] private bool _includeLimitsInHistogram = true;
    [ObservableProperty] private bool _includeLimitsInTrend = true;
    [ObservableProperty] private bool _showNormalCurve = true;
    [ObservableProperty] private bool _showSigmaBand = true;

    // ----- Histogram bins -----

    /// <summary>Replaced on a language change, so the dropdown shows its entries in the new language.</summary>
    [ObservableProperty] private IReadOnlyList<BinRule> _binRules = Enum.GetValues<BinRule>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBinCountRule), nameof(IsBinWidthRule))]
    private BinRule _binRule = BinRule.Auto;

    /// <summary>Number of bins for the Count rule; double for the slider.</summary>
    [ObservableProperty] private double _binCount = 20;

    /// <summary>Bin width as typed, e.g. "5m" or "5 mV".</summary>
    [ObservableProperty] private string _binWidthText = string.Empty;

    [ObservableProperty] private BinSettings _binSettings = BinSettings.Default;

    [ObservableProperty] private bool _isBinWidthInvalid;

    public bool IsBinCountRule => BinRule == BinRule.Count;
    public bool IsBinWidthRule => BinRule == BinRule.Width;
    public int MaxBinCount => Histogram.MaxCount;

    /// <summary>The width the automatic rule uses for the selected step, as example for the width input.</summary>
    public string BinWidthPlaceholder =>
        Histogram.Create(SelectedStepValues).BinWidth is var w && w > 0
            ? Localizer.Format("Placeholder_BinWidth", ValueFormatter.Format(w, SelectedStep?.Unit, 2))
            : string.Empty;

    partial void OnBinRuleChanged(BinRule value)
    {
        // Start the manual modes where the automatic rule was, so the chart does not jump
        var auto = Histogram.Create(SelectedStepValues);
        if (value == BinRule.Count && auto.Bins.Count > 0)
            BinCount = Math.Clamp(auto.Bins.Count, 2, Histogram.MaxCount);
        if (value == BinRule.Width && string.IsNullOrWhiteSpace(BinWidthText) && auto.BinWidth > 0)
            BinWidthText = ValueFormatter.Format(auto.BinWidth, SelectedStep?.Unit, 2);

        UpdateBinSettings();
    }

    partial void OnBinCountChanged(double value) => UpdateBinSettings();
    partial void OnBinWidthTextChanged(string value) => UpdateBinSettings();

    private void UpdateBinSettings()
    {
        var widthValid = ValueFormatter.TryParse(BinWidthText, SelectedStep?.Unit, out var width) && width > 0;
        IsBinWidthInvalid = BinRule == BinRule.Width && !widthValid;

        BinSettings = BinRule switch
        {
            BinRule.Count => new BinSettings(BinRule.Count, Count: (int)Math.Round(BinCount)),
            // An invalid width keeps the automatic bins until the input is fixed
            BinRule.Width => widthValid ? new BinSettings(BinRule.Width, Width: width) : BinSettings.Default,
            _ => new BinSettings(BinRule)
        };
    }

    public bool HasSelectedStep => SelectedStep is not null;

    /// <summary>0 overview, 1 steps, 2 runs.</summary>
    [ObservableProperty] private int _selectedTabIndex;

    /// <summary>Opens the charts of a step, e.g. from the failure list of the overview.</summary>
    [RelayCommand]
    private void ShowStep(StepRow? row)
    {
        if (row is null) return;
        SelectedStep = row;
        SelectedTabIndex = 1;
    }

    partial void OnSelectedStepChanged(StepRow? value)
    {
        if (value is null)
        {
            SelectedStepValues = [];
            SelectedStepTrend = [];
            SelectedStepFacts = [];
            return;
        }

        var measured = value.Series.Samples.Where(s => s.IsMeasured).ToList();
        SelectedStepValues = measured.Select(s => s.Result.Value!.Value).ToList();
        SelectedStepTrend = measured
            .Select(s => new TrendPoint(
                s.Result.Value!.Value,
                s.IsPassed,
                $"{s.Run.SerialNumber ?? s.Run.FileName}  {s.Run.LocalStartTime:yyyy-MM-dd HH:mm}"))
            .ToList();
        SelectedStepFacts = StepFacts(value);

        // The width input refers to the unit of the step
        UpdateBinSettings();
        OnPropertyChanged(nameof(BinWidthPlaceholder));
    }

    // ----- Theme -----

    [ObservableProperty] private bool _isDarkMode;

    partial void OnIsDarkModeChanged(bool value)
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;

        if (_settings is not null)
        {
            _settings.IsDarkMode = value;
            _settings.Save();
        }
    }

    // ----- About -----

    public AboutViewModel About { get; } = new();

    [ObservableProperty] private bool _isAboutOpen;

    [RelayCommand]
    private void ToggleAbout() => IsAboutOpen = !IsAboutOpen;

    [RelayCommand]
    private void CloseAbout() => IsAboutOpen = false;

    // ----- Language -----

    public IReadOnlyList<LanguageOption> Languages { get; }

    [ObservableProperty] private LanguageOption _selectedLanguage;

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        Localizer.Instance.ChangeCulture(value.Culture);

        if (_settings is not null)
        {
            _settings.Language = value.Culture.TwoLetterISOLanguageName;
            _settings.Save();
        }
    }

    /// <summary>Rebuilds the texts that are made in code; texts in XAML update through their bindings.</summary>
    private void OnCultureChanged()
    {
        var binRule = BinRule;
        BinRules = Enum.GetValues<BinRule>();
        BinRule = binRule;
        OnPropertyChanged(nameof(BinRule));
        OnPropertyChanged(nameof(BinWidthPlaceholder));

        var resultFilter = ResultFilter;
        _suspendFilters = true;
        ResultFilters = Enum.GetValues<ResultFilter>();
        ResultFilter = resultFilter;
        OnPropertyChanged(nameof(ResultFilter));
        _suspendFilters = false;

        RefreshProjects();
        SummaryFacts = BuildSummaryFacts(Summary);
        OnSelectedRunChanged(SelectedRun);
        OnSelectedStepChanged(SelectedStep);
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(LoadErrorsText));
    }

    [RelayCommand]
    private void ToggleTheme() => IsDarkMode = !IsDarkMode;

    // ----- Commands -----

    private bool CanLoad() => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private async Task OpenFilesAsync()
    {
        if (FileDialogs is null) return;
        var files = await FileDialogs.OpenResultFilesAsync();
        if (files.Count > 0)
            await LoadPathsAsync(files);
    }

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private async Task OpenFolderAsync()
    {
        if (FileDialogs is null) return;
        var folder = await FileDialogs.OpenFolderAsync();
        if (folder is not null)
            await LoadPathsAsync([folder]);
    }

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private void Clear()
    {
        _runsByPath.Clear();
        _errors.Clear();
        LoadedRunCount = 0;
        SetStatus(() => Localizer.Get("Status_Start"));
        OnLoadErrorsChanged();
        RefreshProjects();
        ApplyFilters();
    }

    [RelayCommand]
    private void ResetFilters()
    {
        _suspendFilters = true;
        SearchText = string.Empty;
        ResultFilter = ResultFilter.All;
        SelectedProject = Projects[0];
        ExcludeSimulator = false;
        LatestPerDevice = false;
        FromDate = null;
        ToDate = null;
        _suspendFilters = false;
        ApplyFilters();
    }

    [RelayCommand]
    private async Task ExportStepStatisticsAsync() =>
        await ExportAsync(Localizer.Get("Dialog_ExportStepStatistics"), Localizer.Get("File_StepStatistics"),
            () => CsvExporter.StepStatistics(Steps.Select(s => s.Series)));

    [RelayCommand]
    private async Task ExportRunsAsync() =>
        await ExportAsync(Localizer.Get("Dialog_ExportRuns"), Localizer.Get("File_Runs"), () => CsvExporter.Runs(Runs));

    /// <summary>The selected step in every filtered run that executed it.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedStep))]
    private async Task ExportSelectedStepAsync()
    {
        if (SelectedStep is not { } step) return;
        await ExportAsync(Localizer.Get("Dialog_ExportSelectedStep"), FileName($"{step.Module} {step.Number} {step.Name}"),
            () => CsvExporter.StepValues(step.Series));
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRun))]
    private async Task ExportSelectedRunAsync()
    {
        if (SelectedRun is not { } run) return;
        await ExportAsync(Localizer.Get("Dialog_ExportSelectedRun"),
            FileName($"{run.SerialNumber ?? "run"}_{run.LocalStartTime:yyyy-MM-dd_HH-mm-ss}"),
            () => CsvExporter.RunSteps(run));
    }

    [RelayCommand]
    private async Task ExportValueMatrixAsync() =>
        await ExportAsync(Localizer.Get("Dialog_ExportValueMatrix"), Localizer.Get("File_ValueMatrix"),
            () => CsvExporter.ValueMatrix(Runs, Steps.Select(s => s.Series).ToList()));

    /// <summary>Suggested file name without characters Windows does not allow.</summary>
    private static string FileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return (clean.Length > 0 ? clean : "export") + ".csv";
    }

    /// <summary>
    /// Adds the files to the imported runs; folders are searched including subfolders. Files that are already
    /// imported are read again, so a reload picks up changes.
    /// </summary>
    public async Task LoadPathsAsync(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0 || IsLoading) return;

        IsLoading = true;
        SetStatus(() => Localizer.Get("Status_Reading"));

        try
        {
            var result = await Task.Run(() => ResultFileLoader.LoadAsync(paths));

            foreach (var run in result.Runs)
                _runsByPath[run.FilePath] = run;

            // Errors of files that now loaded are outdated
            _errors.RemoveAll(e => _runsByPath.ContainsKey(e.FilePath) || result.Errors.Any(n => n.FilePath == e.FilePath));
            _errors.AddRange(result.Errors);

            LoadedRunCount = _runsByPath.Count;
            var (read, skipped, imported) = (result.Runs.Count, result.Errors.Count, LoadedRunCount);
            SetStatus(() => skipped > 0
                ? Localizer.Format("Status_LoadedWithSkipped", read, skipped, imported)
                : Localizer.Format("Status_Loaded", read, imported));
        }
        catch (Exception ex)
        {
            SetStatus(() => Localizer.Format("Status_ReadFailed", ex.Message));
        }
        finally
        {
            IsLoading = false;
        }

        OnLoadErrorsChanged();
        RefreshProjects();
        ApplyFilters();
    }

    private void OnLoadErrorsChanged() => LoadErrors = _errors.ToList();

    private void RefreshProjects()
    {
        var names = _runsByPath.Values
            .Select(r => r.ProjectName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.CurrentCultureIgnoreCase);

        var selected = SelectedProject.Name;
        _suspendFilters = true;
        Projects = [new ProjectOption(null), .. names.Select(n => new ProjectOption(n))];
        SelectedProject = Projects.FirstOrDefault(p => p.Name == selected) ?? Projects[0];
        _suspendFilters = false;
    }

    private async Task ExportAsync(string title, string suggestedName, Func<string> content)
    {
        if (FileDialogs is null) return;

        var path = await FileDialogs.SaveTextFileAsync(title, suggestedName);
        if (path is null) return;

        try
        {
            // UTF-8 with BOM so Excel detects the encoding
            await File.WriteAllTextAsync(path, content(), new UTF8Encoding(true));
            SetStatus(() => Localizer.Format("Status_Exported", path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus(() => Localizer.Format("Status_ExportFailed", ex.Message));
        }
    }

    // ----- Filtering and evaluation -----

    internal IEnumerable<LoadedRun> Filter(IEnumerable<LoadedRun> runs)
    {
        var search = SearchText.Trim();

        var filtered = runs.Where(r =>
            (ResultFilter == ResultFilter.All || (ResultFilter == ResultFilter.Passed) == r.IsPassed) &&
            (SelectedProject.Name is not { } project || string.Equals(r.ProjectName, project, StringComparison.OrdinalIgnoreCase)) &&
            (!ExcludeSimulator || !r.IsSimulator) &&
            (FromDate is not { } from || r.LocalStartTime >= from.Date) &&
            (ToDate is not { } to || r.LocalStartTime < to.Date.AddDays(1)) &&
            (search.Length == 0 || Matches(r, search)));

        // Latest per device applies after the other filters, e.g. the last passed run of every device
        return LatestPerDevice ? RunSummary.LatestPerDevice(filtered) : filtered;
    }

    private static bool Matches(LoadedRun run, string search) =>
        new[] { run.SerialNumber, run.FileName, run.ProjectName, run.DeviceName, run.Variant, run.Operator, run.Station }
            .Any(v => v?.Contains(search, StringComparison.CurrentCultureIgnoreCase) == true);

    private void ApplyFilters()
    {
        if (_suspendFilters) return;

        var runs = Filter(_runsByPath.Values).OrderByDescending(r => r.StartTime).ToList();
        var selectedRun = SelectedRun;
        var selectedStepId = SelectedStep?.StepId;

        Runs = runs;
        Summary = RunSummary.Compute(runs);
        SummaryFacts = BuildSummaryFacts(Summary);
        YieldHistory = Analysis.YieldHistory.Build(runs);

        var steps = StepSeries.Build(runs).Select(s => new StepRow(s)).ToList();
        Steps = steps;

        var failures = steps.Where(s => s.Failed > 0).OrderByDescending(s => s.Failed).ThenBy(s => s.Name).Take(10).ToList();
        var maxFailures = failures.Count > 0 ? failures[0].Failed : 1;
        foreach (var row in failures)
            row.FailShareOfMax = (double)row.Failed / maxFailures;
        TopFailures = failures;

        // Keep the selection when it is still part of the filtered data
        SelectedRun = selectedRun is not null && runs.Contains(selectedRun) ? selectedRun : runs.FirstOrDefault();
        SelectedStep = steps.FirstOrDefault(s => s.StepId == selectedStepId)
                       ?? steps.FirstOrDefault(s => s.Series.HasValues)
                       ?? steps.FirstOrDefault();
    }

    private static IReadOnlyList<Fact> BuildSummaryFacts(RunSummary s)
    {
        if (s.Runs == 0)
            return [];

        return
        [
            new(Localizer.Get("Fact_Period"), $"{s.From!.Value.LocalDateTime:yyyy-MM-dd HH:mm} – {s.To!.Value.LocalDateTime:yyyy-MM-dd HH:mm}"),
            new(Localizer.Get("Fact_Devices"), s.Devices.ToString(CultureInfo.CurrentCulture)),
            new(Localizer.Get("Fact_FirstPassYield"), s.FirstPassYield?.ToString("P1") ?? "–"),
            new(Localizer.Get("Fact_FinalYield"), s.FinalYield?.ToString("P1") ?? "–"),
            new(Localizer.Get("Fact_MeanTestTime"), s.MeanDurationSeconds is { } d ? FormatDuration(d) : "–")
        ];
    }

    private static IReadOnlyList<Fact> StepFacts(StepRow row)
    {
        var s = row.Series;
        var st = s.Statistics;
        var unit = s.Unit;

        var facts = new List<Fact>
        {
            new(Localizer.Get("Fact_Module"), s.Module),
            new(Localizer.Get("Fact_Limits"),
                $"{Limit(row.LowerLimitText)} … {Limit(row.UpperLimitText)}" + (s.LimitsChanged ? "  " + Localizer.Get("Fact_LimitsChanged") : "")),
            new(Localizer.Get("Fact_Executed"), Localizer.Format("Fact_ExecutedValue", s.Executed, s.Passed, s.Failed)),
            new(Localizer.Get("Fact_FailRate"), row.FailRateText),
            new(Localizer.Get("Fact_MeasuredValues"), s.MeasuredCount.ToString(CultureInfo.CurrentCulture))
        };

        // Broken down by the statuses that occurred, so a status added to the file format shows up without changes here
        if (s.Invalid > 0)
        {
            var byStatus = s.Samples
                .Where(x => x.Result.Status is not (TestValueStatus.Measured or TestValueStatus.NoValue))
                .GroupBy(x => x.Result.Status)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{Localizer.Enum(g.Key)} {g.Count()}");
            facts.Add(new(Localizer.Get("Fact_Invalid"), $"{s.Invalid}  ({string.Join(", ", byStatus)})"));
        }

        if (st is not null)
        {
            facts.Add(new(Localizer.Get("Fact_Mean"), ValueFormatter.Format(st.Mean, unit)));
            facts.Add(new(Localizer.Get("Fact_Median"), ValueFormatter.Format(st.Median, unit)));
            facts.Add(new(Localizer.Get("Fact_StdDev"), st.StdDev is { } sd ? ValueFormatter.Format(sd, unit) : "–"));
            facts.Add(new(Localizer.Get("Fact_MinMax"), $"{ValueFormatter.Format(st.Min, unit)} / {ValueFormatter.Format(st.Max, unit)}"));
            facts.Add(new(Localizer.Get("Fact_Range"), ValueFormatter.Format(st.Range, unit)));
            facts.Add(new(Localizer.Get("Fact_CpCpk"), $"{st.Cp?.ToString("0.00") ?? "–"} / {st.Cpk?.ToString("0.00") ?? "–"}"));
        }

        return facts;

        static string Limit(string text) => text.Length == 0 ? "–" : text;
    }

    private static IReadOnlyList<Fact> RunFacts(LoadedRun r)
    {
        var f = r.File;
        var facts = new List<Fact>
        {
            new(Localizer.Get("Fact_SerialNumber"), r.SerialNumber ?? "–"),
            new(Localizer.Get("Fact_Result"), r.IsPassed ? Localizer.Enum(r.Result) : Localizer.Format("Fact_ResultFail", r.FailedSteps)),
            new(Localizer.Get("Fact_Start"), $"{r.LocalStartTime:yyyy-MM-dd HH:mm:ss}"),
            new(Localizer.Get("Fact_Duration"), FormatDuration(r.DurationSeconds)),
            new(Localizer.Get("Fact_RunMode"), r.LoopIndex is { } loop
                ? Localizer.Format("Fact_RunModeLoop", Localizer.Enum(r.RunMode), loop)
                : Localizer.Enum(r.RunMode)),
            new(Localizer.Get("Fact_Operator"), r.Operator ?? "–"),
            new(Localizer.Get("Fact_Station"), r.Station ?? "–"),
            new(Localizer.Get("Fact_Project"), f.Project.Name ?? "–"),
            new(Localizer.Get("Fact_ProjectFile"), f.Project.FileHash is null
                ? Localizer.Get("Fact_ProjectNotSaved")
                : f.Project.HasUnsavedChanges ? Localizer.Get("Fact_ProjectUnsaved") : Short(f.Project.FileHash)),
            new(Localizer.Get("Fact_AtlabVersion"), f.Project.AtlabVersion ?? "–"),
            new(Localizer.Get("Fact_Device"), Join(f.DeviceUnderTest.DeviceName, f.DeviceUnderTest.Variant, f.DeviceUnderTest.Revision)),
            new(Localizer.Get("Fact_PartNumber"), f.DeviceUnderTest.PartNumber ?? "–"),
            new(Localizer.Get("Fact_TestHardware"), f.TestHardware.IsSimulator
                ? Localizer.Get("Fact_Simulator")
                : Join(f.TestHardware.Name, f.TestHardware.SerialNumber is { } sn ? $"SN {sn}" : null,
                    f.TestHardware.FirmwareVersion is { } fw ? $"FW {fw}" : null))
        };

        facts.AddRange(f.TestEquipment.Select(e => new Fact(e.Name, e.Identification ?? Localizer.Get("Fact_NoAnswer"))));

        if (!string.IsNullOrWhiteSpace(f.Notes))
            facts.Add(new(Localizer.Get("Fact_Notes"), f.Notes));

        facts.Add(new(Localizer.Get("Fact_File"), r.FilePath));
        return facts;

        static string Join(params string?[] parts)
        {
            var text = string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            return text.Length == 0 ? "–" : text;
        }

        static string Short(string hash) => hash.Length > 19 ? hash[..19] + "…" : hash;
    }

    internal static string FormatDuration(double seconds)
    {
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t:mm\\:ss}" : t.TotalMinutes >= 1 ? $"{t:m\\:ss} min" : $"{seconds:0.0} s";
    }
}