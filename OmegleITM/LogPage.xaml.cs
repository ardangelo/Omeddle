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
using Omeddle.Resources;

namespace Omeddle {
	public partial class LogPage : PhoneApplicationPage {
		public LogPage() {
			InitializeComponent();

			appSettings = IsolatedStorageSettings.ApplicationSettings;
			mainPivot.Title = AppResources.LogPageText.ToUpper();
		}

		ObservableCollection<Chat> recentChatList, savedChatList;
		IsolatedStorageSettings appSettings;

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);

			recentChatList = new ObservableCollection<Chat>();
			if (appSettings.Contains("recents")) {
				recentChatList = appSettings["recents"] as ObservableCollection<Chat>;
			} else {
				appSettings["recents"] = recentChatList;
				appSettings.Save();
			}

			RecentChatList.ItemsSource = recentChatList;

			savedChatList = new ObservableCollection<Chat>();
			if (appSettings.Contains("savedchats")) {
				savedChatList = appSettings["savedchats"] as ObservableCollection<Chat>;
			} else {
				appSettings["savedchats"] = savedChatList;
				appSettings.Save();
			}

			SavedChatList.ItemsSource = savedChatList;
		}

		private void ChatList_SelectionChanged(object sender, SelectionChangedEventArgs e) {

			LongListSelector ChatList = ((LongListSelector)sender);
			Boolean savedChat = false;
			if (ChatList.Name.Equals("SavedChatList")) {
				savedChat = true;
			}

			if (ChatList.SelectedItem == null) { return; }

			var selectedItem = (Chat)ChatList.SelectedItem;
			NavigationService.Navigate(new Uri(String.Format("/ChatPage.xaml?mode={0}&saved={1}&chathash={2}",
				ChatMode.ViewLog.ToString(), savedChat, ((Chat)(ChatList.SelectedItem)).Hash), UriKind.Relative));

			ChatList.SelectedItem = null;
		}
	}
}