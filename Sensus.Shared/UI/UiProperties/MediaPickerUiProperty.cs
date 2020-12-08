using Plugin.Media;
using Plugin.Media.Abstractions;
using Sensus.Exceptions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sensus.UI.UiProperties
{
	public class MediaPickerUiProperty : UiProperty
	{
		public MediaPickerUiProperty(string labelText, bool editable, int order) : base(labelText, editable, order, false)
		{

		}

		public const string PREVIEW = "Preview";
		public const string CAPTURE_IMAGE = "Capture Image";
		public const string CHOOSE_IMAGE = "Choose Image";
		public const string CAPTURE_VIDEO = "Capture Video";
		public const string CHOOSE_VIDEO = "Choose Video";
		public const string URL = "Url";
		public const string FROM_URL = "From Url";
		public const string NONE = "None";
		public const string CHOOSE = "Choose...";

		//private async Task<MediaObject> GetMediaObjectAsync(MediaFile file)
		//{
		//	MediaObject media = new MediaObject();

		//	byte[] data = await SensusServiceHelper.ReadAllBytesAsync(file.GetStream());

		//	media.Data = Convert.ToBase64String(data);
		//	media.Type = SensusServiceHelper.Get().GetMimeType(file.Path);
		//	media.Embeded = true;

		//	return media;
		//}

		//private async Task<MediaObject> GetMediaObjectAsync(string data, bool embed)
		//{
		//	MediaObject media = new MediaObject();

		//	string mimeType = SensusServiceHelper.Get().GetMimeType(data);

		//	if (embed)
		//	{
		//		using (HttpClient client = new HttpClient())
		//		{
		//			using (HttpResponseMessage response = await client.GetAsync(data))
		//			{
		//				if (string.IsNullOrEmpty(mimeType))
		//				{
		//					mimeType = response.Content.Headers.ContentType.MediaType.ToLower();
		//				}

		//				data = Convert.ToBase64String(await response.Content.ReadAsByteArrayAsync());
		//			}
		//		}
		//	}

		//	media.Data = data;
		//	media.Type = mimeType;
		//	media.Embeded = embed;

		//	return media;
		//}

		private string GetButtonText(MediaObject media)
		{
			if (media != null && string.IsNullOrWhiteSpace(media.Data) == false)
			{
				if (media.Embeded)
				{
					// using the byte count of the base64 string since it is what gets serialized to Unicode. 
					// The file size is likely smaller by almost half due to the base64 string being encoded in Unicode, but it would not represent the actual amount of data being stored.
					int size = Encoding.Unicode.GetByteCount(media.Data);

					return $"{media.Type} ({size} B)";
				}
				else
				{
					return URL;
				}
			}

			return CHOOSE;
		}

		public override View GetView(PropertyInfo property, object o, out BindableProperty bindingProperty, out IValueConverter converter)
		{
			if (property.PropertyType != typeof(MediaObject))
			{
				throw new ArgumentException($"The property provided must have a type of {nameof(MediaObject)}.", nameof(property));
			}

			bindingProperty = null;
			converter = null;

			List<string> buttons = new List<string>();
			StackLayout urlLayout = new StackLayout();
			MediaObject currentMedia = (MediaObject)property.GetValue(o);
			bool embed = false;

			Button sourceButton = new Button()
			{
				Text = GetButtonText(currentMedia),
				HorizontalOptions = LayoutOptions.FillAndExpand,
				FontSize = 20
			};

			Label urlLabel = new Label
			{
				Text = "Url:",
				FontSize = 20
			};

			Entry urlEntry = new Entry
			{
				Keyboard = Keyboard.Url,
				HorizontalOptions = LayoutOptions.FillAndExpand
			};

			StackLayout urlStack = new StackLayout
			{
				IsVisible = false,
				Children = { urlLabel, urlEntry }
			};

			async Task setMediaObjectAsync(MediaFile file)
			{
				if (file != null)
				{
					sourceButton.IsEnabled = false;
					sourceButton.Text = "Processing...";

					MediaObject media = await MediaObject.FromFileAsync(file.GetStream(), SensusServiceHelper.Get().GetMimeType(file.Path));

					setMediaObject(media);
					sourceButton.IsEnabled = true;
				}
			}

			void setMediaObject(MediaObject media)
			{
				property.SetValue(o, media);

				Device.BeginInvokeOnMainThread(() =>
				{
					sourceButton.Text = GetButtonText(media);

					if (buttons.Contains(PREVIEW) == false)
					{
						buttons.Insert(0, PREVIEW);
					}

					if (media.Embeded)
					{
						urlStack.IsVisible = false;
					}
					else
					{
						urlStack.IsVisible = true;
					}
				});
			}

			urlEntry.Unfocused += async (s, e) =>
			{
				try
				{
					string mimeType = SensusServiceHelper.Get().GetMimeType(urlEntry.Text);

					MediaObject media = await MediaObject.FromUrlAsync(urlEntry.Text, mimeType, embed);

					setMediaObject(media);
				}
				catch (Exception exception)
				{
					await SensusServiceHelper.Get().FlashNotificationAsync($"Failed to attach media to the {o.GetType().Name}");

					SensusException.Report(exception);
				}
			};

			if (currentMedia != null && string.IsNullOrWhiteSpace(currentMedia.Data) == false)
			{
				buttons.Add(PREVIEW);

				if (currentMedia.Embeded == false)
				{
					urlEntry.Text = currentMedia.Data;
					urlStack.IsVisible = true;
				}
			}

			sourceButton.Clicked += async (s, e) =>
			{
				try
				{
					string source = await Application.Current.MainPage.DisplayActionSheet("Media Source", "Cancel", null, buttons.ToArray());

					if (source == PREVIEW)
					{
						INavigation navigation = (Application.Current as App).DetailPage.Navigation;

						await navigation.PushAsync(new MediaPreviewPage((MediaObject)property.GetValue(o)), true);
					}
					else if (source == CAPTURE_IMAGE)
					{
						MediaFile file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
						{
							CustomPhotoSize = 50,
							CompressionQuality = 50,
							SaveMetaData = false,
							SaveToAlbum = true
						});

						await setMediaObjectAsync(file);
					}
					else if (source == CHOOSE_IMAGE)
					{
						MediaFile file = await CrossMedia.Current.PickPhotoAsync(new PickMediaOptions()
						{
							CustomPhotoSize = 50,
							CompressionQuality = 50,
							SaveMetaData = false
						});

						await setMediaObjectAsync(file);
					}
					else if (source == CAPTURE_VIDEO)
					{
						MediaFile file = await CrossMedia.Current.TakeVideoAsync(new StoreVideoOptions
						{
							CustomPhotoSize = 50,
							CompressionQuality = 50,
							SaveMetaData = false,
							SaveToAlbum = true
						});

						await setMediaObjectAsync(file);
					}
					else if (source == CHOOSE_VIDEO)
					{
						MediaFile file = await CrossMedia.Current.PickVideoAsync();

						await setMediaObjectAsync(file);
					}
					else if (source == URL)
					{
						embed = false;

						urlStack.IsVisible = true;
						urlEntry.Focus();
					}
					else if (source == FROM_URL)
					{
						embed = true;

						urlStack.IsVisible = true;
						urlEntry.Focus();
					}
					else if (source == NONE)
					{
						buttons.Remove(PREVIEW);

						property.SetValue(o, null);

						urlStack.IsVisible = false;
						sourceButton.Text = CHOOSE;
					}
				}
				catch (Exception exception)
				{
					await SensusServiceHelper.Get().FlashNotificationAsync($"Failed to attach media to the {o.GetType().Name}");

					SensusException.Report(exception);
				}
			};

			if (CrossMedia.Current.IsTakePhotoSupported)
			{
				buttons.Add(CAPTURE_IMAGE);
			}

			if (CrossMedia.Current.IsPickPhotoSupported)
			{
				buttons.Add(CHOOSE_IMAGE);
			}

			if (CrossMedia.Current.IsTakeVideoSupported)
			{
				buttons.Add(CAPTURE_VIDEO);
			}

			if (CrossMedia.Current.IsPickVideoSupported)
			{
				buttons.Add(CHOOSE_VIDEO);
			}

			buttons.Add(URL);
			buttons.Add(FROM_URL);
			buttons.Add(NONE);

			StackLayout layout = new StackLayout()
			{
				Children = { sourceButton, urlStack }
			};

			return layout;
		}
	}
}
