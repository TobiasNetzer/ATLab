using System;
using ATLab.CTIA;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Services;
using ATLab.ViewModels;
using ATLab.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ATLab;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackendServices(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<ISerialNumberDialogService, SerialNumberDialogService>();
        services.AddSingleton<IMessageBoxService, MessageBoxService>();
        services.AddSingleton<ISimulationService, SimulationStateService>();
        services.AddSingleton<IErrorService, ErrorService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IScriptRepository, FileScriptRepository>();
        services.AddSingleton<IScriptService, ScriptService>();
        services.AddSingleton<IScriptRunner, ScriptRunner>();
        services.AddSingleton<ICommandExecutor, CommandExecutor>();
        services.AddSingleton<ITestStepEvaluator, TestStepEvaluator>();
        services.AddSingleton<IResponseProcessor, ResponseProcessor>();
        services.AddSingleton<ICsvExportService, CsvExportService>();
        services.AddSingleton<IPdfExportService, PdfExportService>();
        services.AddSingleton<TestResultExportService>();
        services.AddSingleton<ProjectController>();
        services.AddSingleton<TestExecutionController>();
        services.AddSingleton<TestStepEditor>();
        services.AddSingleton<IDeviceIdentificationService, DeviceIdentificationService>();
        
        services.AddSingleton<ProjectSettings>();
        services.AddSingleton<ProjectDocumentation>();
        services.AddSingleton<DocumentLauncherService>();
        services.AddSingleton<DeviceUnderTestInfo>();
        
        services.AddSingleton<ITestStepRunner, TestStepRunner>();
        services.AddSingleton<ITestExecutor, TestExecutor>();

        services.AddTransient<CtiaCommunication>();
        services.AddTransient<CtiaHardware>();
        services.AddSingleton<TestHardwareSimulator>();
        
        services.AddSingleton<IHardwareAccessor, HardwareAccessor>();
        services.AddSingleton<ITestHardware>(sp => sp.GetRequiredService<IHardwareAccessor>().Hardware ?? throw new InvalidOperationException("Hardware not initialized"));
        services.AddSingleton<IHardwareInfo>(sp => sp.GetRequiredService<ITestHardware>().HardwareInfo);
        
        services.AddSingleton<IShellCommandRunner>(sp => ShellCommandRunnerFactory.Create());
        services.AddSingleton<ICommunicationFactory, CommunicationFactory>();

        return services;
    }

    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddSingleton<TestHardwareRelayChannelsViewModel>();
        services.AddSingleton<TestStepConfiguratorViewModel>();
        services.AddSingleton<DeviceManagerViewModel>();
        services.AddSingleton<ProjectSettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<TestingTabViewModel>();
        services.AddSingleton<ConfigTabViewModel>();
        services.AddSingleton<ScriptingTabViewModel>();
        services.AddSingleton<AboutTabViewModel>();
        services.AddSingleton<HardwareTabViewModel>();
        services.AddSingleton<DocumentationTabViewModel>();
        services.AddSingleton<TestHardwareInfoViewModel>();
        services.AddSingleton<ScriptSelectorViewModel>();
        services.AddSingleton<CommandEditorViewModel>();
        services.AddSingleton<TestHardwareConnectWindowViewModel>();
        services.AddSingleton<ShellCommandEditorViewModel>();
        services.AddSingleton<ExpressionEditorViewModel>();
        services.AddSingleton<ResponseMaskEditorViewModel>();
        services.AddSingleton<ProjectDocumentationViewModel>();
        services.AddSingleton<DeviceUnderTestInfoPanelViewModel>();
        services.AddSingleton<TestHardwareDiagnosticsViewModel>();
        services.AddSingleton<SerialNumberEntryWindowViewModel>();
        services.AddSingleton<RuntimeVariableEditorViewModel>();

        return services;
    }

    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddTransient<MainWindow>();
        services.AddTransient<TestHardwareConnectWindow>();
        return services;
    }
}
