using Newtonsoft.Json;
using Sensus.Anonymization.Anonymizers;
using Sensus.Context;
using Sensus.UI.UiProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sensus.UI
{
	public class AnonymizerPage : ContentPage
	{
		private readonly Anonymizer _anonymizer;
		private readonly string _serializedAnonymizer;
		private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
		{
			PreserveReferencesHandling = PreserveReferencesHandling.None,
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			TypeNameHandling = TypeNameHandling.All,
			ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
		};

		public AnonymizerPage(Anonymizer anonymizer)
		{
			_anonymizer = anonymizer;
			_serializedAnonymizer = JsonConvert.SerializeObject(anonymizer, _serializerSettings);
			Title = anonymizer.DisplayText;

			StackLayout contentLayout = new StackLayout
			{
				Orientation = StackOrientation.Vertical,
				VerticalOptions = LayoutOptions.FillAndExpand,
				Children =
				{
					new Label
					{
						Text = Title,
						FontSize = 20,
						HorizontalOptions = LayoutOptions.CenterAndExpand
					}
				}
			};

			foreach (StackLayout stack in UiProperty.GetPropertyStacks(anonymizer))
			{
				contentLayout.Children.Add(stack);
			}

			Button okayButton = new Button()
			{
				Text = "OK",
				FontSize = 20,
				HorizontalOptions = LayoutOptions.FillAndExpand
			};

			Button cancelButton = new Button()
			{
				Text = "Cancel",
				FontSize = 20,
				HorizontalOptions = LayoutOptions.FillAndExpand
			};

			okayButton.Clicked += async (o, e) =>
			{
				try
				{
					if (anonymizer.Validate(out string errorMessage))
					{
						await Navigation.PopModalAsync(true);
					}
					else
					{
						await DisplayAlert(anonymizer.DisplayText, errorMessage, "OK");
					}
				}
				catch (Exception error)
				{
					await DisplayAlert(anonymizer.DisplayText, error.Message, "OK");
				}
			};

			cancelButton.Clicked += async (o, e) =>
			{
				JsonConvert.PopulateObject(_serializedAnonymizer, anonymizer, _serializerSettings);

				await Navigation.PopModalAsync(true);
			};

			StackLayout buttonLayout = new StackLayout
			{
				Orientation = StackOrientation.Horizontal,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Children = { okayButton, cancelButton }
			};

			contentLayout.Children.Add(buttonLayout);

			Content = contentLayout;
		}

		protected override bool OnBackButtonPressed()
		{
			if (_anonymizer.Validate(out string errorMessage) == false)
			{
				SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
				{
					await DisplayAlert(_anonymizer.DisplayText, errorMessage, "OK");
				});

				return true;
			}

			return false;
		}
	}
}
