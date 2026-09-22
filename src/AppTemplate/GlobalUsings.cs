global using System.Collections.Immutable;
global using AppTemplate.Models;
global using AppTemplate.Core.Services;
global using AppTemplate.Services;
global using AppTemplate.Services.Navigation;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Localization;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using ApplicationExecutionState = Windows.ApplicationModel.Activation.ApplicationExecutionState;

// Shared infrastructure from MZikmund.Toolkit.WinUI replaces the app's former hand-written
// duplicates (see docs/toolkit-migration.md). The Infrastructure namespace is imported globally
// (only IXamlRootProvider lives there). The Services types are added as global using aliases rather
// than importing the whole MZikmund.Toolkit.WinUI.Services namespace, which would collide with the
// app's own IAppRatingService/AppRatingService (a different, dialog-driven rating contract kept local).
global using MZikmund.Toolkit.WinUI.Infrastructure;
global using IDialogCoordinator = MZikmund.Toolkit.WinUI.Services.IDialogCoordinator;
global using DialogCoordinator = MZikmund.Toolkit.WinUI.Services.DialogCoordinator;
global using IPreferences = MZikmund.Toolkit.WinUI.Services.IPreferences;
global using Preferences = MZikmund.Toolkit.WinUI.Services.Preferences;
