// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class ToggleCalculateFolderSizesAction : ObservableObject, IToggleAction
	{
		private readonly IFoldersSettingsService settings;

		public string Label
			=> Strings.CalculateFolderSizes.GetLocalizedResource();

		public string Description
			=> Strings.CalculateFolderSizes.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Show;

		public RichGlyph Glyph
			=> new("\uE8EF");

		public string AutomationId
			=> "ToggleFolderSizesButton";

		public bool IsOn
			=> settings.CalculateFolderSizes;

		public ToggleCalculateFolderSizesAction()
		{
			settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

			settings.PropertyChanged += Settings_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			settings.CalculateFolderSizes = !settings.CalculateFolderSizes;

			return Task.CompletedTask;
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.CalculateFolderSizes))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
