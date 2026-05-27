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

// Toolkit promotions (issue #32, §18.1): types provided by MZikmund.Toolkit.WinUI replace the
// template's former duplicates. Dialog coordinator types are aliased rather than imported via the
// whole MZikmund.Toolkit.WinUI.Services namespace to avoid colliding with the template's own
// IPreferences/Preferences (which remain local until the toolkit interface gains
// ContainsKey/Remove/Clear).
global using MZikmund.Toolkit.WinUI.Infrastructure;
global using IDialogCoordinator = MZikmund.Toolkit.WinUI.Services.IDialogCoordinator;
global using DialogCoordinator = MZikmund.Toolkit.WinUI.Services.DialogCoordinator;
