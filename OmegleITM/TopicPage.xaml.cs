using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Runtime.Serialization;
using Omeddle.Resources;
using System.IO.IsolatedStorage;
using System.Collections.ObjectModel;

namespace Omeddle {
	[DataContract]
	public class Topic {
		[DataMember]
		public Boolean IsActive { get; set; }
		[DataMember]
		public string Content { get; set; }

		public Topic(string content) {
			IsActive = true;
			Content = content;
		}
	}

	public partial class TopicPage : PhoneApplicationPage {
		private ObservableCollection<Topic> targets;
		IsolatedStorageSettings appSettings;
		Boolean pickQuestions = false;

		public TopicPage() {
			InitializeComponent();
			BuildLocalizedApplicationBar();

			appSettings = IsolatedStorageSettings.ApplicationSettings;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);

			string q;
			if (NavigationContext.QueryString.TryGetValue("editqs", out q)) {
				Boolean.TryParse(q, out pickQuestions);
			}

			if (pickQuestions) {
				if (!appSettings.Contains("questions")) {
					targets = new ObservableCollection<Topic>();
					writeTargetsToStorage();
				} else {
					targets = appSettings["questions"] as ObservableCollection<Topic>;
				}

				pageTitle.Text = AppResources.QuestionListText;
			} else {
				//load data from save
				if (!appSettings.Contains("topics")) {
					targets = new ObservableCollection<Topic>();
					writeTargetsToStorage();
				} else {
					targets = appSettings["topics"] as ObservableCollection<Topic>;
				}
			}

			//update lls
			topicList.ItemsSource = targets;
		}

		private void BuildLocalizedApplicationBar() {
			// Set the page's ApplicationBar to a new instance of ApplicationBar.
			ApplicationBar = new ApplicationBar();

			// Create a new button and set the text value to the localized string from AppResources.
			ApplicationBarIconButton newTopicButton = new ApplicationBarIconButton(new Uri("/Assets/add.png", UriKind.Relative));
			newTopicButton.Text = AppResources.AddNewText;
			newTopicButton.Click += newTopicButton_Click;
			ApplicationBar.Buttons.Add(newTopicButton);

			ApplicationBarIconButton clearTopicsButton = new ApplicationBarIconButton(new Uri("/Assets/delete.png", UriKind.Relative));
			clearTopicsButton.Text = AppResources.ClearText;
			clearTopicsButton.Click += clearTopicsButton_Click;
			ApplicationBar.Buttons.Add(clearTopicsButton);
		}

		//handlers

		private void CheckBox_Checked(object sender, RoutedEventArgs e) {
			pageControl.IsEnabled = false;
			
			foreach (Topic topic in targets) {
				if (topic.Content.Equals(((CheckBox)sender).Content)) {
					topic.IsActive = !topic.IsActive;
				}
			}

			writeTargetsToStorage();
			pageControl.IsEnabled = true;
		}

		void newTopicButton_Click(object sender, EventArgs e) {
			if (topicEntryBox.Text.Equals("")) { return; }
			foreach (Topic topic in targets) {
				if (topic.Content.Equals(topicEntryBox.Text)) { return; }
			}

			pageControl.IsEnabled = false;
			targets.Add(new Topic(topicEntryBox.Text));
			writeTargetsToStorage();

			//topicList.ItemsSource = topics;
			topicEntryBox.Text = "";
			topicList.Focus();
			pageControl.IsEnabled = true;
		}

		void clearTopicsButton_Click(object sender, EventArgs e) {
			targets = new ObservableCollection<Topic>();
			writeTargetsToStorage();

			topicList.ItemsSource = targets;
			pageControl.IsEnabled = true;
		}

		private void writeTargetsToStorage() {
			if (pickQuestions) {
				appSettings["questions"] = targets;
			} else {
				appSettings["topics"] = targets;
			}
			
			appSettings.Save();
		}
	}
}