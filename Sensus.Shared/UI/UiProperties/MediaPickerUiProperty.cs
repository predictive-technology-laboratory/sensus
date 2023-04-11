//using Plugin.Media;
//using Plugin.Media.Abstractions;
using Sensus.Exceptions;
using Sensus.UI.Inputs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
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
		public const string URL_CACHED = "Url (Cached)";
		public const string URL_EMBEDDED = "Url (Embed)";
		public const string NONE = "None";
		public const string CHOOSE = "Choose...";

		private string GetButtonText(MediaObject media)
		{
			if (media != null && string.IsNullOrWhiteSpace(media.Data) == false)
			{
				if (media.StorageMethod == MediaStorageMethods.Embed)
				{
					// using the byte count of the base64 string since it is what gets serialized to Unicode. 
					// The file size is likely smaller by almost half due to the base64 string being encoded in Unicode, but it would not represent the actual amount of data being stored.
					int size = Encoding.Unicode.GetByteCount(media.Data);

					return $"{media.Type} ({size} B)";
				}
				else if (media.StorageMethod == MediaStorageMethods.Cache)
				{
					return URL_CACHED;
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
			MediaStorageMethods storageMethod = currentMedia?.StorageMethod ?? MediaStorageMethods.URL;
			MediaInput input = (MediaInput)o;

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

			Label cacheLabel = new Label
			{
				Text = "Cache Mode:",
				FontSize = 20
			};

			Picker cachePicker = new Picker()
			{
				Title = "Select Cache Mode",
				HorizontalOptions = LayoutOptions.FillAndExpand
			};

			foreach (string cacheMode in Enum.GetNames(typeof(MediaCacheModes)))
			{
				cachePicker.Items.Add(cacheMode);
			}

			if (currentMedia != null)
			{
				cachePicker.SelectedItem = currentMedia.CacheMode.ToString();
			}

			cachePicker.SelectedIndexChanged += (s, e) =>
			{
				Enum.TryParse(typeof(MediaCacheModes), (string)cachePicker.SelectedItem, out object result);

				if (currentMedia != null)
				{
					currentMedia.CacheMode = (MediaCacheModes)result;
				}
			};

			StackLayout cacheLayout = new StackLayout()
			{
				IsVisible = false,
				Children = { cacheLabel, cachePicker }
			};

			StackLayout urlStack = new StackLayout
			{
				IsVisible = false,
				Children = { urlLabel, urlEntry, cacheLayout }
			};

			async Task setMediaObjectAsync(FileResult file)
			{
				if (file != null)
				{
					sourceButton.IsEnabled = false;
					sourceButton.Text = "Processing...";

					try
					{
						MediaObject media = await MediaObject.FromFileAsync(file.FullPath, await file.OpenReadAsync(), SensusServiceHelper.Get().GetMimeType(file.FullPath), input.CachePath);

						setMediaObject(media);
					}
					catch (Exception exception)
					{
						await SensusServiceHelper.Get().FlashNotificationAsync($"Failed to attach media to the {o.GetType().Name}");

						SensusServiceHelper.Get().Logger.Log("Failed to attach media: " + exception.Message, LoggingLevel.Normal, GetType());
					}

					sourceButton.IsEnabled = true;
				}
			}

			void setMediaObject(MediaObject media)
			{
				if (currentMedia != null)
				{
					currentMedia.ClearCache();
				}

				currentMedia = media;
				property.SetValue(o, media);

				if (media.StorageMethod == MediaStorageMethods.Cache)
				{
					cachePicker.SelectedItem = currentMedia.CacheMode.ToString();
				}

				Device.BeginInvokeOnMainThread(() =>
				{
					sourceButton.Text = GetButtonText(media);

					if (buttons.Contains(PREVIEW) == false)
					{
						buttons.Insert(0, PREVIEW);
					}

					if (media.StorageMethod == MediaStorageMethods.Embed)
					{
						urlStack.IsVisible = false;
					}
					else
					{
						urlStack.IsVisible = true;

						if (media.StorageMethod == MediaStorageMethods.Cache)
						{
							cacheLayout.IsVisible = true;
						}
					}
				});
			}

			urlEntry.Unfocused += async (s, e) =>
			{
				try
				{
					string mimeType = SensusServiceHelper.Get().GetMimeType(urlEntry.Text);

					if (mimeType?.StartsWith("video") == true && storageMethod == MediaStorageMethods.Embed)
					{
						storageMethod = MediaStorageMethods.Cache;

						sourceButton.Text = URL_CACHED;

						await SensusServiceHelper.Get().FlashNotificationAsync($"You cannot embed videos. The video will be cached instead.");
					}

					MediaObject media = await MediaObject.FromUrlAsync(urlEntry.Text, mimeType, storageMethod, input.CachePath);

					if (media.Type.StartsWith("video") && storageMethod == MediaStorageMethods.Embed)
					{
						storageMethod = MediaStorageMethods.Cache;

						sourceButton.Text = URL_CACHED;

						await SensusServiceHelper.Get().FlashNotificationAsync($"You cannot embed videos. The video will be cached instead.");
					}
					else
					{
						setMediaObject(media);
					}
				}
				catch (Exception exception)
				{
					await SensusServiceHelper.Get().FlashNotificationAsync($"Failed to attach media to the {o.GetType().Name}");

					SensusServiceHelper.Get().Logger.Log("Failed to attach media: " + exception.Message, LoggingLevel.Normal, GetType());
				}
			};

			if (currentMedia != null && string.IsNullOrWhiteSpace(currentMedia.Data) == false)
			{
				buttons.Add(PREVIEW);

				if (currentMedia.StorageMethod != MediaStorageMethods.Embed)
				{
					urlEntry.Text = currentMedia.Data;
					urlStack.IsVisible = true;

					if (currentMedia.StorageMethod == MediaStorageMethods.Cache)
					{
						cacheLayout.IsVisible = true;
					}
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

						MediaObject media = (MediaObject)property.GetValue(o);

						await media.CacheMediaAsync();

						await navigation.PushAsync(new MediaPreviewPage(media), true);
					}
					else if (source == CAPTURE_IMAGE)
					{
						FileResult file = await MediaPicker.CapturePhotoAsync();

						await setMediaObjectAsync(file);
					}
					else if (source == CHOOSE_IMAGE)
					{
						FileResult file = await MediaPicker.PickPhotoAsync();

						await setMediaObjectAsync(file);
					}
					else if (source == CAPTURE_VIDEO)
					{
						FileResult file = await MediaPicker.CaptureVideoAsync();

						await setMediaObjectAsync(file);
					}
					else if (source == CHOOSE_VIDEO)
					{
						FileResult file = await MediaPicker.PickVideoAsync();

						await setMediaObjectAsync(file);
					}
					else if (source == URL)
					{
						storageMethod = MediaStorageMethods.URL;

						sourceButton.Text = URL;

						urlStack.IsVisible = true;
						urlEntry.Focus();

						cacheLayout.IsVisible = false;
					}
					else if (source == URL_CACHED)
					{
						storageMethod = MediaStorageMethods.Cache;

						sourceButton.Text = URL_CACHED;

						urlStack.IsVisible = true;
						urlEntry.Focus();

						cacheLayout.IsVisible = true;
					}
					else if (source == URL_EMBEDDED)
					{
						storageMethod = MediaStorageMethods.Embed;

						sourceButton.Text = URL_EMBEDDED;

						urlStack.IsVisible = true;
						urlEntry.Focus();

						cacheLayout.IsVisible = false;
					}
					else if (source == NONE)
					{
						buttons.Remove(PREVIEW);

						if (currentMedia != null)
						{
							currentMedia.ClearCache();
						}

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

			if (MediaPicker.IsCaptureSupported)
			{
				buttons.Add(CAPTURE_IMAGE);
			}

			buttons.Add(CHOOSE_IMAGE);

			if (MediaPicker.IsCaptureSupported)
			{
				buttons.Add(CAPTURE_VIDEO);
			}

			buttons.Add(CHOOSE_VIDEO);

			buttons.Add(URL);
			buttons.Add(URL_CACHED);
			buttons.Add(URL_EMBEDDED);
			buttons.Add(NONE);

			StackLayout layout = new StackLayout()
			{
				Children = { sourceButton, urlStack }
			};

			return layout;
		}
	}
}
