using System.Collections.Generic;

namespace ATLab.ViewModels;

/// <summary>Groups the step editors shown next to the step list.</summary>
public class WorkspaceEditorViewModel
{
    public TestHardwareRelayChannelsViewModel TestHardwareRelayChannels { get; }
    public TestStepConfiguratorViewModel TestStepConfigurator { get; }
    public ResponseMaskEditorViewModel ResponseMaskEditor { get; }
    public ScriptSelectorViewModel ScriptSelector { get; }
    public CommandEditorViewModel CommandEditor { get; }
    public ExternalProgramEditorViewModel ExternalProgramEditor { get; }
    public ExpressionEditorViewModel ExpressionEditor { get; }
    public FilePathEditorViewModel FilePathEditor { get; }
    public BoundaryScanStepEditorViewModel BoundaryScanStepEditor { get; }
    public BoundaryScanBusStepEditorViewModel BoundaryScanBusStepEditor { get; }
    public TestInterfaceCommunicationViewModel TestInterfaceCommunication { get; }

    public WorkspaceEditorViewModel(
        TestHardwareRelayChannelsViewModel testHardwareRelayChannels,
        TestStepConfiguratorViewModel testStepConfigurator,
        ResponseMaskEditorViewModel responseMaskEditor,
        ScriptSelectorViewModel scriptSelector,
        CommandEditorViewModel commandEditor,
        ExternalProgramEditorViewModel externalProgramEditor,
        ExpressionEditorViewModel expressionEditor,
        FilePathEditorViewModel filePathEditor,
        BoundaryScanStepEditorViewModel boundaryScanStepEditor,
        BoundaryScanBusStepEditorViewModel boundaryScanBusStepEditor,
        TestInterfaceCommunicationViewModel testInterfaceCommunication)
    {
        TestHardwareRelayChannels = testHardwareRelayChannels;
        TestStepConfigurator = testStepConfigurator;
        ResponseMaskEditor = responseMaskEditor;
        ScriptSelector = scriptSelector;
        CommandEditor = commandEditor;
        ExternalProgramEditor = externalProgramEditor;
        ExpressionEditor = expressionEditor;
        FilePathEditor = filePathEditor;
        BoundaryScanStepEditor = boundaryScanStepEditor;
        BoundaryScanBusStepEditor = boundaryScanBusStepEditor;
        TestInterfaceCommunication = testInterfaceCommunication;
    }

    public void LoadTestStep(
        TestStepViewModel vm,
        IEnumerable<TestStepViewModel> allSteps)
    {
        TestHardwareRelayChannels.MeasChannelViewModel.LoadActiveMeasChannels(vm.TestStep.MatrixState);
        TestHardwareRelayChannels.StimChannelViewModel.LoadRelayStates(vm.TestStep.LiveStimState);
        TestHardwareRelayChannels.ExtStimChannelViewModel.LoadRelayStates(vm.TestStep.LiveExtStimState);
        TestStepConfigurator.LoadTestStep(vm, allSteps);
        ScriptSelector.LoadTestStep(vm);
        CommandEditor.LoadTestStep(vm);
        TestInterfaceCommunication.LoadTestStep(vm);
        ExternalProgramEditor.LoadTestStep(vm.TestStep.ExternalProgram);
        ExpressionEditor.LoadTestStep(vm.TestStep);
        FilePathEditor.LoadTestStep(vm.TestStep);
        BoundaryScanStepEditor.LoadTestStep(vm.TestStep);
        BoundaryScanBusStepEditor.LoadTestStep(vm.TestStep);
        ResponseMaskEditor.LoadTestStep(vm);
    }
}