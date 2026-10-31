using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ATLab.Helpers;
using ATLab.Interfaces;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace ATLab.ViewModels;

/// <summary>
/// Application settings dialog: language, theme, simulation-only start and the development mode password. Edits a
/// copy of the settings and applies it on save. While a password is set, turning it off, changing it or changing the
/// simulation setting requires the current password.
/// </summary>
public partial class AppSettingsDialogViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localizationService;
    private readonly DialogManager _dialogManager;

    private bool _originalForceSimulation;

    public IReadOnlyList<CultureInfo> Languages => _localizationService.SupportedCultures;

    [ObservableProperty]
    private CultureInfo? _selectedLanguage;

    // Recreated on every Load so the names follow the current UI language
    [ObservableProperty]
    private IReadOnlyList<ThemeOption> _themes = [];

    [ObservableProperty]
    private ThemeOption? _selectedTheme;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCurrentPasswordRequired), nameof(ValidationMessage))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isForceSimulation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCurrentPasswordRequired), nameof(ValidationMessage))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isDevelopmentModePasswordEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCurrentPasswordRequired), nameof(ValidationMessage))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValidationMessage))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValidationMessage))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCurrentPasswordRequired))]
    private bool _hasPassword;

    [ObservableProperty]
    private string? _wrongPasswordMessage;

    /// <summary>True when a password is set and one of the settings it protects is about to change.</summary>
    public bool IsCurrentPasswordRequired =>
        HasPassword &&
        (!IsDevelopmentModePasswordEnabled || NewPassword.Length > 0 || IsForceSimulation != _originalForceSimulation);

    public string? ValidationMessage
    {
        get
        {
            if (IsDevelopmentModePasswordEnabled)
            {
                if (NewPassword.Length == 0 && ConfirmPassword.Length == 0 && !HasPassword)
                    return _localizationService["Text_EnterPassword"];

                if (NewPassword != ConfirmPassword)
                    return _localizationService["Text_PasswordsDoNotMatch"];
            }

            if (IsCurrentPasswordRequired && CurrentPassword.Length == 0)
                return _localizationService["Text_EnterCurrentPassword"];

            return null;
        }
    }

    public AppSettingsDialogViewModel(
        ISettingsService settingsService,
        ILocalizationService localizationService,
        DialogManager dialogManager)
    {
        _settingsService = settingsService;
        _localizationService = localizationService;
        _dialogManager = dialogManager;
    }

    /// <summary>Fills the dialog with the current settings, call before showing it.</summary>
    public void Load()
    {
        var settings = _settingsService.Settings;
        var currentLanguage = _localizationService.CurrentCulture.TwoLetterISOLanguageName;

        SelectedLanguage = Languages.FirstOrDefault(c => c.TwoLetterISOLanguageName == currentLanguage) ?? Languages[0];
        Themes =
        [
            new ThemeOption(ThemeVariant.Dark, _localizationService["Theme_Dark"]),
            new ThemeOption(ThemeVariant.Light, _localizationService["Theme_Light"])
        ];
        SelectedTheme = Application.Current!.RequestedThemeVariant == ThemeVariant.Light ? Themes[1] : Themes[0];
        _originalForceSimulation = settings.IsForceSimulation;
        IsForceSimulation = settings.IsForceSimulation;
        HasPassword = settings.IsDevelopmentModePasswordEnabled &&
                      !string.IsNullOrEmpty(settings.DevelopmentModePasswordHash);
        IsDevelopmentModePasswordEnabled = settings.IsDevelopmentModePasswordEnabled;
        ClearPasswords();
    }

    partial void OnCurrentPasswordChanged(string value) => WrongPasswordMessage = null;

    private bool CanSave() => ValidationMessage is null;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        var settings = _settingsService.Settings;

        if (IsCurrentPasswordRequired &&
            !PasswordHasher.Verify(CurrentPassword, settings.DevelopmentModePasswordHash, settings.DevelopmentModePasswordSalt))
        {
            WrongPasswordMessage = _localizationService["Text_WrongPassword"];
            return;
        }

        if (SelectedLanguage is not null &&
            SelectedLanguage.TwoLetterISOLanguageName != _localizationService.CurrentCulture.TwoLetterISOLanguageName)
        {
            _localizationService.ChangeCulture(SelectedLanguage);
        }
        settings.Language = _localizationService.CurrentCulture.TwoLetterISOLanguageName;

        if (SelectedTheme is not null)
        {
            Application.Current!.RequestedThemeVariant = SelectedTheme.Variant;
            settings.IsDarkMode = SelectedTheme.Variant == ThemeVariant.Dark;
        }

        settings.IsForceSimulation = IsForceSimulation;
        settings.IsDevelopmentModePasswordEnabled = IsDevelopmentModePasswordEnabled;

        if (!IsDevelopmentModePasswordEnabled)
        {
            settings.DevelopmentModePasswordHash = string.Empty;
            settings.DevelopmentModePasswordSalt = string.Empty;
        }
        else if (NewPassword.Length > 0)
        {
            (settings.DevelopmentModePasswordHash, settings.DevelopmentModePasswordSalt) =
                PasswordHasher.Hash(NewPassword);
        }

        _settingsService.Save();

        ClearPasswords();

        _dialogManager.Close(this, new CloseDialogOptions { Success = true });
    }

    [RelayCommand]
    private void Cancel()
    {
        ClearPasswords();

        _dialogManager.Close(this);
    }

    private void ClearPasswords()
    {
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        CurrentPassword = string.Empty;
        WrongPasswordMessage = null;
    }
}

/// <summary>Entry of the theme selection with its localized name.</summary>
public record ThemeOption(ThemeVariant Variant, string Name);