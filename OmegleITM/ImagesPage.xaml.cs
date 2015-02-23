using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;
using System.Net.Http;
using Omeddle.Resources;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Omeddle {
	public partial class ImagesPage : PhoneApplicationPage {
		ObservableCollection<ImgurImage> images;
		IsolatedStorageSettings appSettings;
		ImgurImage selectedImage;

		ApplicationBarIconButton deleteButton, copyButton, openButton;

		public ImagesPage() {
			InitializeComponent();

			appSettings = IsolatedStorageSettings.ApplicationSettings;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);
			
			//load data from save
			if (!appSettings.Contains("images")) {
				images = new ObservableCollection<ImgurImage>();
				appSettings["images"] = images;
				appSettings.Save();
			} else {
				images = appSettings["images"] as ObservableCollection<ImgurImage>;
			}
			//update lls
			imagesList.ItemsSource = images;

			ApplicationBar = new ApplicationBar();
			ApplicationBar.Mode = ApplicationBarMode.Minimized;

			deleteButton = new ApplicationBarIconButton(new Uri("/Assets/delete.png", UriKind.Relative));
			deleteButton.Text = AppResources.DeleteText;
			deleteButton.Click += deleteButton_Click;

			openButton = new ApplicationBarIconButton(new Uri("/Assets/globe.png", UriKind.Relative));
			openButton.Text = AppResources.OpenText;
			openButton.Click += openButton_Click;

			copyButton = new ApplicationBarIconButton(new Uri("/Assets/clipboard.png", UriKind.Relative));
			copyButton.Text = AppResources.CopyImageText;
			copyButton.Click += copyButton_Click;

			ApplicationBarMenuItem deleteAllMenuItem = new ApplicationBarMenuItem(String.Format("{0}...", AppResources.DeleteAllImagesText));
			deleteAllMenuItem.Click += deleteAllMenuItem_Click;
			ApplicationBar.MenuItems.Add(deleteAllMenuItem);
		}
		
		async void deleteAllMenuItem_Click(object sender, EventArgs e) {
			if (images.Count == 0) { return; }

			var confirm = MessageBox.Show(AppResources.DeleteAllImagesConfirmation, AppResources.DeleteAllImagesText, MessageBoxButton.OKCancel);

			if (confirm == MessageBoxResult.OK) {
				Dispatcher.BeginInvoke(() => {
					TypingIndicator.IsVisible = true;
					titleBar.Text = AppResources.DeletingText.ToUpper();
				});

				foreach (ImgurImage image in images) {
					HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, String.Format("https://api.imgur.com/3/image/{0}", image.DeleteHash));
					request.Headers.Add("Authorization", "Client-ID " + AppResources.ImgurApiKey);
					HttpResponseMessage result = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
					try {
						//make request
						Task<HttpResponseMessage> resultTask = new HttpClient().SendAsync(request);
						result = await resultTask;
					} catch {
						Dispatcher.BeginInvoke(() => {
							TypingIndicator.IsVisible = false;
							titleBar.Text = AppResources.ApplicationTitle.ToUpper();
							MessageBox.Show(AppResources.ImageDeleteErrorText);
						});
						return;
					}
					Debug.WriteLine("Response: {0}", result.Content.ReadAsStringAsync().Result);

					if (result.Content.ReadAsStringAsync().Result.IndexOf("200") < 0) {
						Dispatcher.BeginInvoke(() => {
							TypingIndicator.IsVisible = false;
							titleBar.Text = AppResources.ApplicationTitle.ToUpper();
							MessageBox.Show(AppResources.ImageDeleteErrorText);
							return;
						});
					}
				}

				Dispatcher.BeginInvoke(() => {
					images = new ObservableCollection<ImgurImage>();
					imagesList.ItemsSource = images;
					appSettings["images"] = images;
					appSettings.Save();

					TypingIndicator.IsVisible = false;
					titleBar.Text = AppResources.ApplicationTitle.ToUpper();

					ApplicationBar.Mode = ApplicationBarMode.Minimized;
					ApplicationBar.Buttons.Clear();
				});
				
			}
		}

		private void openButton_Click(object sender, EventArgs e) {

			WebBrowserTask webBrowserTask = new WebBrowserTask();
			webBrowserTask.Uri = new Uri(selectedImage.Url, UriKind.Absolute);
			webBrowserTask.Show();
		}

		async void deleteButton_Click(object sender, EventArgs e) {
			Dispatcher.BeginInvoke(() => {
				TypingIndicator.IsVisible = true;
				titleBar.Text = AppResources.DeletingText.ToUpper();
				imagesList.IsEnabled = false;
			});

			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, String.Format("https://api.imgur.com/3/image/{0}", selectedImage.DeleteHash));
			request.Headers.Add("Authorization", "Client-ID " + AppResources.ImgurApiKey);
			HttpResponseMessage result = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
			try {
				//make request
				Task<HttpResponseMessage> resultTask = new HttpClient().SendAsync(request);
				result = await resultTask;
			} catch {
				Dispatcher.BeginInvoke(() => {
					TypingIndicator.IsVisible = false;
					titleBar.Text = AppResources.ApplicationTitle.ToUpper();
					MessageBox.Show(AppResources.ImageDeleteErrorText);
					imagesList.IsEnabled = true;
				});
				return;
			}
			Debug.WriteLine("Response: {0}", result.Content.ReadAsStringAsync().Result);

			if (result.Content.ReadAsStringAsync().Result.IndexOf("200") < 0) {
				Dispatcher.BeginInvoke(() => {
					TypingIndicator.IsVisible = false;
					titleBar.Text = AppResources.ApplicationTitle.ToUpper();
					MessageBox.Show(AppResources.ImageDeleteErrorText);
					imagesList.IsEnabled = true;
				});

				return;
			}
			
			Dispatcher.BeginInvoke(() => {
				TypingIndicator.IsVisible = false;
				titleBar.Text = AppResources.ApplicationTitle.ToUpper();
				imagesList.IsEnabled = true;

				images.Remove(selectedImage);
				imagesList.SelectedItem = null;
				selectedImage = null;
				appSettings["images"] = images;
				appSettings.Save();

				ApplicationBar.Mode = ApplicationBarMode.Minimized;
				ApplicationBar.Buttons.Clear();
			});
		}

		private void copyButton_Click(object sender, EventArgs e) {
			Clipboard.SetText(selectedImage.Url);
		}

		private void imagesList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (imagesList.SelectedItem == null) { return; }

			selectedImage = null;
			ApplicationBar.Mode = ApplicationBarMode.Minimized;
			ApplicationBar.Buttons.Clear();

			this.selectedImage = (ImgurImage)(imagesList.SelectedItem);

			ApplicationBar.Buttons.Add(copyButton);
			ApplicationBar.Buttons.Add(openButton);
			ApplicationBar.Buttons.Add(deleteButton);
			ApplicationBar.Mode = ApplicationBarMode.Default;

			imagesList.SelectedItem = null;
		}
	}
}