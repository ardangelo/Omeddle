using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Scheduler;

namespace Omeddle {
	public partial class SettingsPage : PhoneApplicationPage {
		IsolatedStorageSettings appSettings;
		Boolean usingLiveTile, usingCustomHost;
		string customHost;

		public SettingsPage() {
			InitializeComponent();

			appSettings = IsolatedStorageSettings.ApplicationSettings;

			ApplicationBar = new ApplicationBar();

			ApplicationBarIconButton saveAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/save.png", UriKind.Relative));
			saveAppBarButton.Text = Omeddle.Resources.AppResources.SaveText;
			saveAppBarButton.Click += saveAppBarButton_Click;
			ApplicationBar.Buttons.Add(saveAppBarButton);
		}

		void saveAppBarButton_Click(object sender, EventArgs e) {
			appSettings["usingCustomHost"] = usingCustomHost;
			appSettings["customHost"] = CustomHostTextBox.Text;
			appSettings["usingLiveTile"] = usingLiveTile;

			appSettings.Save();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);
			
			if (!appSettings.Contains("usingLiveTile")) {
				appSettings["usingLiveTile"] = true;
				appSettings.Save();
			}

			usingLiveTile = (Boolean)(appSettings["usingLiveTile"]);
			UpdateLiveTileButton.IsEnabled = usingLiveTile;
			LiveTileCheckBox.IsChecked = usingLiveTile;

			if (!appSettings.Contains("usingCustomHost")) {
				appSettings["usingCustomHost"] = false;
				appSettings.Save();
			}

			if (!appSettings.Contains("customHost")) {
				appSettings["customHost"] = "omeddle.now.im";
				appSettings.Save();
			}

			usingCustomHost = (Boolean)(appSettings["usingCustomHost"]);
			customHost = (string)(appSettings["customHost"]);
			CustomHostContentControl.IsEnabled = usingCustomHost;
			CustomHostTextBox.Text = customHost;
			CustomHostCheckBox.IsChecked = usingCustomHost;
		}

		private void LiveTileCheckBox_Checked(object sender, RoutedEventArgs e) {
			usingLiveTile = (bool)((CheckBox)sender).IsChecked;
			UpdateLiveTileButton.IsEnabled = usingLiveTile;

			if (!(bool)((CheckBox)sender).IsChecked) {
				PeriodicTask tileUpdaterTask;
				string taskName = "TileUpdateAgent";
				tileUpdaterTask = ScheduledActionService.Find(taskName) as PeriodicTask;

				if (tileUpdaterTask != null) {
					ScheduledActionService.Remove(taskName);
				}
			}
		}

		private void UpdateLiveTileButton_Click(object sender, RoutedEventArgs e) {
			PeriodicTask tileUpdaterTask;
			string taskName = "TileUpdateAgent";
			tileUpdaterTask = ScheduledActionService.Find(taskName) as PeriodicTask;

			if (tileUpdaterTask != null) {
				ScheduledActionService.Remove(taskName);
			}

			tileUpdaterTask = new PeriodicTask(taskName);
			tileUpdaterTask.Description = "Omeddle Tile Updater";

			ScheduledActionService.Add(tileUpdaterTask);
			ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromSeconds(10));

			UpdateLiveTileButton.IsEnabled = false;
		}

		private void CustomHostCheckBox_Checked(object sender, RoutedEventArgs e) {
			usingCustomHost = (bool)((CheckBox)sender).IsChecked;
			CustomHostContentControl.IsEnabled = (bool)((CheckBox)sender).IsChecked;
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			NavigationService.Navigate(new Uri("/ImagesPage.xaml", UriKind.Relative));
		}
	}
}