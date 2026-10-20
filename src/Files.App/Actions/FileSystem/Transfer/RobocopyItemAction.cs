// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class RobocopyItemAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private bool isRunning;

		public string Label => Strings.RobocopyLabel.GetLocalizedResource();
		public string Description => Strings.RobocopyDescription.GetLocalizedResource();
		public ActionCategory Category => ActionCategory.FileSystem;
		public RichGlyph Glyph => new(themedIconStyle: "App.ThemedIcons.Copy");
		public string AutomationId => "RobocopyButton";

		public override bool IsExecutable => base.IsExecutable && !isRunning && context.HasSelection
			&& context.SelectedItems.All(item => item.ItemPath is { } path && Path.IsPathFullyQualified(path)
				&& (File.Exists(path) || Directory.Exists(path)));

		public RobocopyItemAction()
		{
			context.PropertyChanged += (_, _) => OnPropertyChanged(nameof(IsExecutable));
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (!IsExecutable)
				return;

			var sources = context.SelectedItems.Select(item => Path.TrimEndingDirectorySeparator(Path.GetFullPath(item.GetRequiredPath()))).ToArray();
			var dialogs = Ioc.Default.GetRequiredService<ICommonDialogService>();
			if (!dialogs.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, true, [], Environment.SpecialFolder.Desktop, out var destination))
				return;

			destination = Path.GetFullPath(destination);
			foreach (var source in sources)
			{
				var name = Path.GetFileName(source);
				var target = Path.Combine(destination, name);
				if (string.IsNullOrEmpty(name) || target.Equals(source, StringComparison.OrdinalIgnoreCase)
					|| (Directory.Exists(source) && target.IsSubPathOf(source)))
				{
					await DialogDisplayHelper.ShowDialogAsync(Label, Strings.RobocopyInvalidDestination.GetLocalizedResource());
					return;
				}
			}

			if (!await DialogDisplayHelper.ShowDialogAsync(Label,
				Strings.RobocopyConfirm.GetLocalizedFormatResource(sources.Length, destination),
				Strings.Copy.GetLocalizedResource(), Strings.Cancel.GetLocalizedResource()))
				return;

			isRunning = true;
			OnPropertyChanged(nameof(IsExecutable));
			try
			{
				foreach (var source in sources)
				{
					bool isDirectory = Directory.Exists(source);
					var startInfo = new ProcessStartInfo
					{
						FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "robocopy.exe"),
						UseShellExecute = false,
					};
					startInfo.ArgumentList.Add(isDirectory ? source : Path.GetDirectoryName(source)!);
					startInfo.ArgumentList.Add(isDirectory ? Path.Combine(destination, Path.GetFileName(source)) : destination);
					if (isDirectory)
						startInfo.ArgumentList.Add("/E");
					else
						startInfo.ArgumentList.Add(Path.GetFileName(source));

					foreach (var option in new[] { "/XJ", "/SL", "/R:1", "/W:1", "/XC", "/XN", "/XO" })
						startInfo.ArgumentList.Add(option);

					using var process = Process.Start(startInfo) ?? throw new IOException(Strings.RobocopyFailed.GetLocalizedResource());
					await process.WaitForExitAsync();
					if (process.ExitCode >= 8 || process.ExitCode < 0)
					{
						await DialogDisplayHelper.ShowDialogAsync(Label,
							$"{Strings.RobocopyFailed.GetLocalizedResource()} ({process.ExitCode})\n{source}");
						return;
					}
				}

				await DialogDisplayHelper.ShowDialogAsync(Label, Strings.RobocopyCompleted.GetLocalizedFormatResource(destination));
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception)
			{
				await DialogDisplayHelper.ShowDialogAsync(Label, $"{Strings.RobocopyFailed.GetLocalizedResource()}\n{ex.Message}");
			}
			finally
			{
				isRunning = false;
				OnPropertyChanged(nameof(IsExecutable));
			}
		}
	}
}
