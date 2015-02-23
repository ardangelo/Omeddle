using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Omeddle.Resources;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Scheduler;

namespace Omeddle {
	public enum ChatMode { Chat, AskQuestion, AnswerQuestion, Intercept, ViewLog };

	public class MenuItem {
		public string Title { get; set; }
		public Uri ImageSource { get; set; }
		public int ImageHeight { get; set; }
		public ChatMode Mode { get; set; }

		public MenuItem(ChatMode mode, string title, Uri imageSource, int imageHeight) {
			Mode = mode;
			Title = title;
			ImageSource = imageSource;
			ImageHeight = imageHeight;
		}
	}

	public partial class MainPage : PhoneApplicationPage {
		ApplicationBarIconButton topicListButton, interceptAppBarButton, questionListAppBarButton;
		IsolatedStorageSettings appSettings;
		Boolean usingLiveTiles, alreadyGotUserCount;

		// Constructor
		public MainPage() {
			InitializeComponent();

			// Sample code to localize the ApplicationBar
			BuildLocalizedApplicationBar();

			appSettings = IsolatedStorageSettings.ApplicationSettings;
			alreadyGotUserCount = false;
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);
						
			if (MainMenuList.ItemsSource == null) {
				System.Collections.Generic.List<MenuItem> mainMenuItems = new System.Collections.Generic.List<MenuItem>();
				System.Collections.Generic.List<MenuItem> questionMenuItems = new System.Collections.Generic.List<MenuItem>();
				System.Collections.Generic.List<MenuItem> settingsMenuItems = new System.Collections.Generic.List<MenuItem>();

				int numberOfItems = 2;
				int menuHeight = 320;

				mainMenuItems.Add(new MenuItem(ChatMode.Chat, AppResources.ChatModeText, new Uri("Assets/message-01.png", UriKind.Relative), (int)(menuHeight / numberOfItems)));
				mainMenuItems.Add(new MenuItem(ChatMode.Intercept, AppResources.InterceptModeText, new Uri("Assets/intercept-01.png", UriKind.Relative), (int)(menuHeight / numberOfItems)));

				settingsMenuItems.Add(new MenuItem(ChatMode.ViewLog, AppResources.LogPageText, new Uri("Assets/book.png", UriKind.Relative), (int)(menuHeight / numberOfItems)));
				settingsMenuItems.Add(new MenuItem(ChatMode.Chat, AppResources.SettingsText, new Uri("Assets/settings.png", UriKind.Relative), (int)(menuHeight / numberOfItems)));

				questionMenuItems.Add(new MenuItem(ChatMode.AskQuestion, AppResources.AskQuestionText, new Uri("Assets/ask-01.png", UriKind.Relative), (int)(menuHeight / numberOfItems)));
				questionMenuItems.Add(new MenuItem(ChatMode.AnswerQuestion, AppResources.AnswerQuestionText, new Uri("Assets/answer-01.png", UriKind.Relative), (int)(menuHeight / numberOfItems)));

				MainMenuList.ItemsSource = mainMenuItems;
				QuestionMenuList.ItemsSource = questionMenuItems;
				SettingsMenuList.ItemsSource = settingsMenuItems;
			}

			Boolean usingCustomHost = false;
			string host = "omegle.com";
			if (appSettings.Contains("customHost") && appSettings.Contains("usingCustomHost")) {
				usingCustomHost = (Boolean)(appSettings["usingCustomHost"]);
				if (usingCustomHost) {
					host = (string)(appSettings["customHost"]);
				}
			}

			if (!alreadyGotUserCount) {
				try {
					//System.Diagnostics.Debug.WriteLine("Number of online users: " + Omegle.Client.getNumberOfUsers());
					mainPivot.Title = AppResources.ApplicationTitle + " - " + String.Format(AppResources.UsersOnlineText, await Omegle.Client.getNumberOfUsers(host));
				} catch {
					MessageBox.Show(AppResources.ConnectionError);
				}
			}

			usingLiveTiles = true;
			if (!appSettings.Contains("usingLiveTiles")) {
				appSettings["usingLiveTiles"] = usingLiveTiles;
				appSettings.Save();
			} else {
				usingLiveTiles = (Boolean)appSettings["usingLiveTiles"];
			}

			if (usingLiveTiles) {
				
				PeriodicTask tileUpdaterTask;
				string taskName = "TileUpdateAgent";
				tileUpdaterTask = ScheduledActionService.Find(taskName) as PeriodicTask;
				
				if (tileUpdaterTask != null) {
					ScheduledActionService.Remove(taskName);
				}

				tileUpdaterTask = new PeriodicTask(taskName);
				tileUpdaterTask.Description = "Omeddle Tile Updater";

				ScheduledActionService.Add(tileUpdaterTask);
				
				#if DEBUG
				//ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromSeconds(10));
				#endif 
			}
		}

		// Sample code for building a localized ApplicationBar
		private void BuildLocalizedApplicationBar() {
			// Set the page's ApplicationBar to a new instance of ApplicationBar.
			ApplicationBar = new ApplicationBar();

			// Create a new button and set the text value to the localized string from AppResources.
			topicListButton = new ApplicationBarIconButton(new Uri("/Assets/manage.png", UriKind.Relative));
			topicListButton.Text = AppResources.AppBarButtonText;
			topicListButton.Click += topicListButton_Click;
			
			interceptAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/edittext.png", UriKind.Relative));
			interceptAppBarButton.Text = AppResources.InterceptListText;
			interceptAppBarButton.Click += interceptAppBarButton_Click;

			questionListAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/edittext.png", UriKind.Relative));
			questionListAppBarButton.Text = AppResources.QuestionListText;
			questionListAppBarButton.Click += editQuestionAppBarButton_Click;

			ApplicationBarMenuItem aboutMenuItem = new ApplicationBarMenuItem(String.Format("{0}...", AppResources.AboutText).ToLower());
			aboutMenuItem.Click += aboutMenuItem_Click;
			ApplicationBar.MenuItems.Add(aboutMenuItem);

			ApplicationBarMenuItem feedbackMenuItem = new ApplicationBarMenuItem(String.Format("{0}...", AppResources.SendFeedbackText));
			feedbackMenuItem.Click += feedbackMenuItem_Click;
			ApplicationBar.MenuItems.Add(feedbackMenuItem);
		}

		void settingsMenuItem_Click(object sender, EventArgs e) {
			NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
		}

		void feedbackMenuItem_Click(object sender, EventArgs e) {
			Microsoft.Phone.Tasks.EmailComposeTask emailComposeTask = new Microsoft.Phone.Tasks.EmailComposeTask();

			emailComposeTask.Subject = "Omeddle Feedback";
			emailComposeTask.Body = "";
			emailComposeTask.To = "dangeloandrew@outlook.com";

			emailComposeTask.Show();
		}

		void editQuestionAppBarButton_Click(object sender, EventArgs e) {
			NavigationService.Navigate(new Uri("/TopicPage.xaml?editqs=true", UriKind.Relative));
		}

		void aboutMenuItem_Click(object sender, EventArgs e) {
			MessageBox.Show(AppResources.AboutApp);
		}

		void topicListButton_Click(object sender, EventArgs e) {
			NavigationService.Navigate(new Uri("/TopicPage.xaml", UriKind.Relative));
		}

		void interceptAppBarButton_Click(object sender, EventArgs e) {
			NavigationService.Navigate(new Uri("/WordPage.xaml", UriKind.Relative));
		}

		private void MainMenuList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (MainMenuList.SelectedItem == null) { return; }

			var selectedItem = (MenuItem)MainMenuList.SelectedItem;
			var lang = System.Globalization.CultureInfo.CurrentCulture.Name.Substring(0, 2);
			//create topic string
			NavigationService.Navigate(new Uri(String.Format("/ChatPage.xaml?mode={0}&lang={1}", selectedItem.Mode.ToString(), lang), UriKind.Relative));

			MainMenuList.SelectedItem = null;
		}

		private void QuestionMenuList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (QuestionMenuList.SelectedItem == null) { return; }

			var selectedItem = (MenuItem)QuestionMenuList.SelectedItem;
			var lang = System.Globalization.CultureInfo.CurrentCulture.Name.Substring(0, 2);

			if (selectedItem.Mode == ChatMode.AnswerQuestion) {
				NavigationService.Navigate(new Uri(String.Format("/ChatPage.xaml?mode={0}&lang={1}&wantsspy=true", selectedItem.Mode.ToString(), lang), UriKind.Relative));
			} else if (selectedItem.Mode == ChatMode.AskQuestion) {
				//if no question navigate to	question page and notify
				if (!appSettings.Contains("questions")) {
					MessageBox.Show(AppResources.NoQuestionsError);
					return;
				}
				ObservableCollection<Topic> questions = new ObservableCollection<Topic>();
				foreach (Topic question in appSettings["questions"] as ObservableCollection<Topic>) {
					if (question.IsActive) {
						questions.Add(question);
					}
				}
				if (questions.Count == 0) {
					MessageBox.Show(AppResources.NoQuestionsError);
					return;
				}
				NavigationService.Navigate(new Uri(String.Format("/ChatPage.xaml?mode={0}&lang={1}&ask={2}&cansave={3}", 
					selectedItem.Mode.ToString(), lang, questions[new Random().Next(questions.Count)].Content, "true"), UriKind.Relative));
			} 

			MainMenuList.SelectedItem = null;

			return;
		}

		private void Pivot_LoadedPivotItem(object sender, PivotItemEventArgs e) {
			if (((Pivot)sender).SelectedItem.Equals(mainMenuPivotItem)) {
				ApplicationBar.Buttons.Add(topicListButton);
				ApplicationBar.Buttons.Add(interceptAppBarButton);
			} else if (((Pivot)sender).SelectedItem.Equals(questionMenuPivotItem)) {
				ApplicationBar.Buttons.Add(questionListAppBarButton);
			}
			
			if (((Pivot)sender).SelectedItem.Equals(settingsMenuPivotItem)) {
				ApplicationBar.Mode = ApplicationBarMode.Minimized;
			} else {
				ApplicationBar.Mode = ApplicationBarMode.Default;
			}
		}

		private void Pivot_LoadingPivotItem(object sender, PivotItemEventArgs e) {
			ApplicationBar.Buttons.Clear();
		}

		private void SettingsMenuList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (SettingsMenuList.SelectedItem == null) { return; }

			var selectedItem = (MenuItem)SettingsMenuList.SelectedItem;
			if (selectedItem.Mode == ChatMode.ViewLog) {
				NavigationService.Navigate(new Uri("/LogPage.xaml", UriKind.Relative));
			} else {
				NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
			}

			SettingsMenuList.SelectedItem = null;
			return;
		}
	}
}