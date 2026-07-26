using System.Collections.ObjectModel;

namespace ATLab.ViewModels;

public class WorkspaceEditorViewModel
{
    public TestHardwareRelayChannelsViewModel TestHardwareRelayChannels { get; }
    public TestStepConfiguratorViewModel TestStepConfigurator { get; }
    public ResponseMaskEditorViewModel ResponseMaskEditor { get; }
    public ScriptSelectorViewModel ScriptSelector { get; }
    public CommandEditorViewModel CommandEditor { get; }
    public ShellCommandEditorViewModel ShellCommandEditor { get; }
    public ExpressionEditorViewModel ExpressionEditor { get; }
    public FilePathEditorViewModel FilePathEditor { get; }
    public TestInterfaceCommunicationViewModel TestInterfaceCommunication { get; }

    public WorkspaceEditorViewModel(
        TestHardwareRelayChannelsViewModel testHardwareRelayChannels,
        TestStepConfiguratorViewModel testStepConfigurator,
        ResponseMaskEditorViewModel responseMaskEditor,
        ScriptSelectorViewModel scriptSelector,
        CommandEditorViewModel commandEditor,
        ShellCommandEditorViewModel shellCommandEditor,
        ExpressionEditorViewModel expressionEditor,
        FilePathEditorViewModel filePathEditor,
        TestInterfaceCommunicationViewModel testInterfaceCommunication)
    {
        TestHardwareRelayChannels = testHardwareRelayChannels;
        TestStepConfigurator = testStepConfigurator;
        ResponseMaskEditor = responseMaskEditor;
        ScriptSelector = scriptSelector;
        CommandEditor = commandEditor;
        ShellCommandEditor = shellCommandEditor;
        ExpressionEditor = expressionEditor;
        FilePathEditor = filePathEditor;
        TestInterfaceCommunication = testInterfaceCommunication;
    }

    public void LoadTestStep(
        TestStepViewModel vm,
        ObservableCollection<TestStepViewModel> allSteps)
    {
        TestHardwareRelayChannels.MeasChannelViewModel.LoadActiveMeasChannels(vm.TestStep.MatrixState);
        TestHardwareRelayChannels.StimChannelViewModel.LoadRelayStates(vm.TestStep.LiveStimState);
        TestHardwareRelayChannels.ExtStimChannelViewModel.LoadRelayStates(vm.TestStep.LiveExtStimState);
        TestStepConfigurator.LoadTestStep(vm, allSteps);
        ScriptSelector.LoadTestStep(vm);
        CommandEditor.LoadTestStep(vm);
        TestInterfaceCommunication.LoadTestStep(vm);
        ShellCommandEditor.LoadTestStep(vm.TestStep.ShellCommand);
        ExpressionEditor.LoadTestStep(vm.TestStep);
        FilePathEditor.LoadTestStep(vm.TestStep);
        ResponseMaskEditor.LoadTestStep(vm);
    }
}