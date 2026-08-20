global using System;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.Diagnostics;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Net.Http.Headers;
global using System.Net.Http.Json;
global using System.Runtime.CompilerServices;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Threading;
global using System.Threading.Tasks;
global using Avalonia;
global using Avalonia.Controls;
global using Avalonia.Controls.ApplicationLifetimes;
global using Avalonia.Controls.Primitives;
global using Avalonia.Controls.Templates;
global using Avalonia.Data.Converters;
global using Avalonia.Input;
global using Avalonia.Interactivity;
global using Avalonia.Markup.Xaml;
global using Avalonia.Media;
global using Avalonia.Media.Imaging;
global using Avalonia.Platform.Storage;
global using Avalonia.VisualTree;
global using Avalonia.X11;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Markwardt.AssetPipeline.Client.Core.Models;
global using Markwardt.AssetPipeline.Client.Core.Serialization;
global using Markwardt.AssetPipeline.Client.Core.Services;
global using Markwardt.AssetPipeline.Client.Infrastructure;
global using Markwardt.AssetPipeline.Client.ViewModels;
global using Markwardt.AssetPipeline.Client.ViewModels.Dialogs;
global using Markwardt.AssetPipeline.Client.ViewModels.Infrastructure;
global using Markwardt.AssetPipeline.Client.Views;
global using Markwardt.AssetPipeline.Client.Views.Dialogs;

[assembly: InternalsVisibleTo("Tests")]

// Lets Moq (used by Tests) generate proxies for this assembly's internal interfaces - see
// https://github.com/moq/moq4/wiki/Quickstart#mocking-internal-types.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
