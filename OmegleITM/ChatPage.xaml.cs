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
using Omegle;
using System.Diagnostics;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Windows.UI;
using System.Windows.Media;
using System.IO.IsolatedStorage;
using System.Windows.Controls.Primitives;
using Microsoft.Phone.Tasks;
using System.Runtime.Serialization;

namespace Omeddle {
	public class ClientStyler {
		public Omegle.Client Instance { get; set; }
		public Coding4Fun.Toolkit.Controls.ChatBubbleDirection Direction { get; set; }
		public HorizontalAlignment ContainerAlignment { get; set; }
		public SolidColorBrush Color { get; set; }
		public int Number { get; set; }
		public string Name { get; set; }
		public Boolean isTyping { get; set; }
		public Boolean isConnecting { get; set; }

		public ClientStyler(Client instance, Coding4Fun.Toolkit.Controls.ChatBubbleDirection direction, HorizontalAlignment containerAlignment, System.Windows.Media.Color color, int number, string name) {
			this.Instance = instance;
			this.Direction = direction;
			this.ContainerAlignment = containerAlignment;
			this.Color = new SolidColorBrush(color);
			this.Number = number;
			this.Name = String.Format(name, number);
			this.isTyping = false;
			this.isConnecting = false;
		}

		public Boolean isConnected() {
			return this.Instance.isConnected();
		}
	}

	public class SpyStyler : ClientStyler {
		public ClientStyler stranger1Styler, stranger2Styler;

		public SpyStyler(ClientStyler connectionInfoStyler, ClientStyler stranger1, ClientStyler stranger2) :
			base(connectionInfoStyler.Instance, Coding4Fun.Toolkit.Controls.ChatBubbleDirection.LowerLeft, HorizontalAlignment.Center, 
			System.Windows.Media.Color.FromArgb(255, 255, 0, 255), 0, "") {
				stranger1Styler = stranger1;
				stranger2Styler = stranger2;

		}
	}

	[DataContract]
	public class Message {
		[DataMember]
		public string ParentName { get; set; }
		[DataMember]
		public string Text { get; set; }
		[DataMember]
		public string Date { get; set; }
		[DataMember]
		public string Time { get; set; }
		[DataMember]
		public Coding4Fun.Toolkit.Controls.ChatBubbleDirection Direction { get; set; }
		[DataMember]
		public System.Windows.Media.Color Color { get; set; }
		[IgnoreDataMember]
		public SolidColorBrush Brush { get; set; }
		[DataMember]
		public HorizontalAlignment ContainerAlignment { get; set; }

		public Message(ClientStyler instance, string text) {
			this.ParentName = String.Copy(instance.Name);
			this.Text = text;

			this.Date = DateTime.Now.ToString("MM/dd/yy");
			this.Time = DateTime.Now.ToString("HH:mm:ss");


			this.Direction = instance.Direction;
			this.ContainerAlignment = instance.ContainerAlignment;
			this.Color = instance.Color.Color;
			this.Brush = new SolidColorBrush(this.Color);
		}
	}

	[DataContract]
	public class ImgurImage {
		[DataMember]
		public string Id { get; set; }
		[DataMember]
		public string Url { get; set; }
		[DataMember]
		public string DeleteHash { get; set; }
		[IgnoreDataMember]
		public SolidColorBrush Highlight { get; set; } //for interface only

		public ImgurImage(string id, string url, string dh) {
			this.Id = id;
			this.Url = url;
			this.DeleteHash = dh;
			this.Highlight = new SolidColorBrush();
		}
	}

	[DataContract]
	public class Chat {
		[DataMember]
		public string Title { get; set; }
		[DataMember]
		public int Count { get; set; }
		[DataMember]
		public ObservableCollection<Message> Messages { get; set; }
		[DataMember]
		public int Hash { get; set; }

		public Chat(ObservableCollection<Message> messages) : 
			this(messages, String.Format("{0} - {1}", messages[0].Date, messages[0].Time)) { }

		public Chat(ObservableCollection<Message> messages, string title) {
			this.Messages = messages;

			if (messages.Count == 0) {
				throw new InvalidOperationException("Message list must contain at least one message");
			}

			this.Title = title;
			this.Count = messages.Count;
			this.Hash = this.GetHashCode();
		}
	}

	public partial class ChatPage : PhoneApplicationPage {
		private string dbcUser = "excelangue";
		private string dbcPass = "Jowypls0";

		IsolatedStorageSettings appSettings;
		List<String> topics;
		ObservableCollection<Regexp> regexps;
		ObservableCollection<Chat> recentChatList, savedChatList;

		ChatMode chatMode;
		string chatModeText, chatLanguage, ask;
		Boolean canSaveQuestion, wantsSpy, usingSavedChat;
		ClientStyler userStyle, systemStyle;
		List<ClientStyler> clientList;
		ObservableCollection<Message> messageList;
		ObservableCollection<ImgurImage> imageList;
		Boolean isDying;
		int chatHash;

		Popup recaptchaBox;
		Canvas canvas;

		//menu definitions
		ApplicationBarIconButton sendAppBarButton, newChatAppBarButton, shareChatAppBarButton, saveChatAppBarButton, deleteChatAppBarButton;
		ApplicationBarIconButton sendS1AppBarButton, sendS2AppBarButton, takePhotoAppBarButton;
		ApplicationBarMenuItem insertImageMenuItem, terminateMenuItem, sendPhotoMenuItem;

		public ChatPage() {
			InitializeComponent();
			BuildLocalizedApplicationBar();

			clientList = new List<ClientStyler>();
			appSettings = IsolatedStorageSettings.ApplicationSettings;

			systemStyle = new ClientStyler(null, Coding4Fun.Toolkit.Controls.ChatBubbleDirection.UpperLeft,
						HorizontalAlignment.Center, System.Windows.Media.Colors.Gray, 0, AppResources.SystemText);

			recaptchaBox = new Popup();
			recaptchaBox.Margin = new Thickness(5,60,5,0);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);

			if (clientList.Count > 0) { unpauseAll(); return; } //came from choosertask

			//load data from save
			topics = new List<String>();
			if (appSettings.Contains("topics")) {
				foreach (Topic topic in appSettings["topics"] as ObservableCollection<Topic>) {
					if (topic.IsActive) {
						topics.Add(topic.Content);
						Debug.WriteLine("topic " + topic.Content);
					}
				}
			}

			recentChatList = new ObservableCollection<Chat>();
			if (appSettings.Contains("recents")) {
				recentChatList = appSettings["recents"] as ObservableCollection<Chat>;
			} else {
				appSettings["recents"] = recentChatList;
				appSettings.Save();
			}

			imageList = new ObservableCollection<ImgurImage>();
			if (appSettings.Contains("images")) {
				imageList = appSettings["images"] as ObservableCollection<ImgurImage>;
			} else {
				appSettings["images"] = imageList;
				appSettings.Save();
			}

			var param = "";
			//set mode from param passed
			if (NavigationContext.QueryString.TryGetValue("mode", out param)) {
				if (param.Equals(ChatMode.Chat.ToString())) {
					chatMode = ChatMode.Chat;
					chatModeText = AppResources.ChatModeText;
				} else if (param.Equals(ChatMode.AskQuestion.ToString())) {
					chatMode = ChatMode.AskQuestion;
					chatModeText = AppResources.QuestionModeText;
				} else if (param.Equals(ChatMode.AnswerQuestion.ToString())) {
					chatMode = ChatMode.AnswerQuestion;
					chatModeText = AppResources.QuestionModeText;
				} else if (param.Equals(ChatMode.Intercept.ToString())) {
					chatMode = ChatMode.Intercept;
					chatModeText = AppResources.InterceptModeText;
				} else if (param.Equals(ChatMode.ViewLog.ToString())) {
					chatMode = ChatMode.ViewLog;
					chatModeText = AppResources.LogPageText;
				}
			}

			string host = Client.defaultHost;
			if (appSettings.Contains("usingCustomHost") && appSettings.Contains("customHost")) {
				if ((bool)(appSettings["usingCustomHost"])) {
					host = (string)(appSettings["customHost"]);
				}
			}

			chatLanguage = "en";
			if (NavigationContext.QueryString.TryGetValue("lang", out param)) {
				//perhaps check for valid language
				chatLanguage = param;
				Debug.WriteLine(chatLanguage);
			}

			ask = "";
			if (NavigationContext.QueryString.TryGetValue("ask", out param)) { ask = param; }

			canSaveQuestion = false;
			if (NavigationContext.QueryString.TryGetValue("cansave", out param)) { canSaveQuestion = Boolean.Parse(param); }

			wantsSpy = false;
			if (NavigationContext.QueryString.TryGetValue("wantsspy", out param)) { wantsSpy = Boolean.Parse(param); }

			chatHash = 0;
			if (NavigationContext.QueryString.TryGetValue("chathash", out param)) { chatHash = int.Parse(param); }

			usingSavedChat = false;
			if (NavigationContext.QueryString.TryGetValue("saved", out param)) { usingSavedChat = Boolean.Parse(param); }
			
			//setup page from mode
			switch (chatMode) {
				case ChatMode.Chat:
					userStyle = new ClientStyler(null, Coding4Fun.Toolkit.Controls.ChatBubbleDirection.LowerRight,
						HorizontalAlignment.Right, System.Windows.Media.Colors.Blue, 0, AppResources.YouText);
					clientList.Add(new ClientStyler(new Client(topics, chatLanguage, host),
						Coding4Fun.Toolkit.Controls.ChatBubbleDirection.UpperLeft,
						System.Windows.HorizontalAlignment.Left,
						System.Windows.Media.Colors.Red, 1, AppResources.StrangerText));

					//start in different thread as to not block the UI
					Task.Factory.StartNew(() => {
						updateTitleBar();
						chatLoop(clientList[0]);
					});

					break;

				case ChatMode.Intercept:

					userStyle = new ClientStyler(null, Coding4Fun.Toolkit.Controls.ChatBubbleDirection.LowerRight,
						HorizontalAlignment.Center, System.Windows.Media.Colors.Green, 0, AppResources.YouText);
					clientList.Add(new ClientStyler(new Client(topics, chatLanguage, host),
						Coding4Fun.Toolkit.Controls.ChatBubbleDirection.UpperLeft,
						System.Windows.HorizontalAlignment.Left,
						System.Windows.Media.Colors.Red, 1, AppResources.StrangerText));
					clientList.Add(new ClientStyler(new Client(topics, chatLanguage, host),
						Coding4Fun.Toolkit.Controls.ChatBubbleDirection.UpperLeft,
						System.Windows.HorizontalAlignment.Right,
						System.Windows.Media.Colors.Blue, 2, AppResources.StrangerText));

					if (appSettings.Contains("regexps")) {
						regexps = appSettings["regexps"] as ObservableCollection<Regexp>;
					}

					//start in different thread as to not block the UI
					Task.Factory.StartNew(() => {
						//very basic omegle client
						chatLoop(clientList[0]);
					});
					Task.Factory.StartNew(() => {
						updateTitleBar();
						chatLoop(clientList[1]);
					});

					break;

				case ChatMode.AskQuestion:
					userStyle = new ClientStyler(null, Coding4Fun.Toolkit.Controls.ChatBubbleDirection.LowerRight,
						HorizontalAlignment.Right, System.Windows.Media.Colors.Blue, 0, AppResources.YouText);
					clientList.Add(new SpyStyler(
						new ClientStyler(new Client(ask, canSaveQuestion, chatLanguage, host),
						Coding4Fun.Toolkit.Controls.ChatBubbleDirection.UpperLeft,
						System.Windows.HorizontalAlignment.Left, System.Windows.Media.Colors.Red, 1, AppResources.StrangerText),

						new ClientStyler(null, Coding4Fun.Toolkit.Controls.ChatBubbleDirection.LowerRight,
						HorizontalAlignment.Left, System.Windows.Media.Colors.Red, 1, AppResources.StrangerText),

						new ClientStyler(null, Coding4Fun.Toolkit.Controls.ChatBubbleDirection.UpperLeft,
						HorizontalAlignment.Right, System.Windows.Media.Colors.Blue, 2, AppResources.StrangerText)));

					//start in different thread as to not block the UI
					Task.Factory.StartNew(() => {
						updateTitleBar();
						chatLoop(clientList[0]);
					});

					break;
				case ChatMode.AnswerQuestion:
					userStyle = new ClientStyler(null, Coding4Fun.Toolkit.Controls.ChatBubbleDirection.LowerRight,
						HorizontalAlignment.Right, System.Windows.Media.Colors.Blue, 0, AppResources.YouText);
					clientList.Add(new ClientStyler(new Client(true, chatLanguage, host),
						Coding4Fun.Toolkit.Controls.ChatBubbleDirection.UpperLeft,
						System.Windows.HorizontalAlignment.Left, System.Windows.Media.Colors.Red, 1, AppResources.StrangerText));

					//start in different thread as to not block the UI
					Task.Factory.StartNew(() => {
						updateTitleBar();
						chatLoop(clientList[0]);
					});

					break;
				case ChatMode.ViewLog:
					string chatSpec;
					if (usingSavedChat) {
						chatSpec = "savedchats";
					} else {
						chatSpec = "recents";
					}

					ObservableCollection<Chat> chatList = new ObservableCollection<Chat>();

					if (appSettings.Contains(chatSpec)) {
						chatList = appSettings[chatSpec] as ObservableCollection<Chat>;
					} else {
						appSettings[chatSpec] = chatList;
						appSettings.Save();
					}

					Dispatcher.BeginInvoke(() => {
						ApplicationBar.Buttons.Clear();

						updateTitleBar();
						foreach (Chat chat in chatList) {
							if (chat.Hash == chatHash) {
								messageList = chat.Messages;
							}
						}

						foreach (Message msg in messageList) {
							msg.Brush = new SolidColorBrush(msg.Color);
						}

						messageLLS.ItemsSource = messageList;

						foreach (ApplicationBarMenuItem abmi in ApplicationBar.MenuItems) {
							abmi.IsEnabled = false;
						}

						newChatAppBarButton.IsEnabled = false;
						ApplicationBar.Buttons.Add(newChatAppBarButton);
						ApplicationBar.Buttons.Add(shareChatAppBarButton);
						if (usingSavedChat) {
							saveChatAppBarButton.IsEnabled = false;
						}
						ApplicationBar.Buttons.Add(saveChatAppBarButton);
						if (usingSavedChat) {
							ApplicationBar.Buttons.Add(deleteChatAppBarButton);
						}
					});

					break;
			}
		}
		
		// intialize appbar
		private void BuildLocalizedApplicationBar() {
			// Set the page's ApplicationBar to a new instance of ApplicationBar.
			ApplicationBar = new ApplicationBar();

			// Create a new menu item with the localized string from AppResources.
			sendAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/send.message.png", UriKind.Relative));
			sendAppBarButton.Text = AppResources.SendText;
			sendAppBarButton.Click += sendAppBarButton_Click;
			
			takePhotoAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/camera.png", UriKind.Relative));
			takePhotoAppBarButton.Text = AppResources.TakePhotoText;
			takePhotoAppBarButton.Click += takePhotoAppBarButton_Click;

			newChatAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/refresh.png", UriKind.Relative));
			newChatAppBarButton.Text = AppResources.NewChatText;
			newChatAppBarButton.Click += newChatAppBarButton_Click;

			shareChatAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/share.png", UriKind.Relative));
			shareChatAppBarButton.Text = AppResources.ShareText;
			shareChatAppBarButton.Click += shareChatAppBarButton_Click;

			saveChatAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/save.png", UriKind.Relative));
			saveChatAppBarButton.Text = AppResources.SaveText;
			saveChatAppBarButton.Click += saveChatAppBarButton_Click;

			deleteChatAppBarButton = new ApplicationBarIconButton(new Uri("/Assets/delete.png", UriKind.Relative));
			deleteChatAppBarButton.Text = AppResources.DeleteText;
			deleteChatAppBarButton.Click += deleteChatAppBarButton_Click;

			sendS1AppBarButton = new ApplicationBarIconButton(new Uri("/Assets/left.png", UriKind.Relative));
			sendS1AppBarButton.Text = String.Format(AppResources.SendToText, "1");
			sendS1AppBarButton.Click += sendS1AppBarButton_Click;

			sendS2AppBarButton = new ApplicationBarIconButton(new Uri("/Assets/right.png", UriKind.Relative));
			sendS2AppBarButton.Text = String.Format(AppResources.SendToText, "2");
			sendS2AppBarButton.Click += sendS2AppBarButton_Click;

			sendPhotoMenuItem = new ApplicationBarMenuItem(String.Format("{0}...", AppResources.ShareFromLibraryText));
			sendPhotoMenuItem.Click += sendPhotoAppBarButton_Click;

			insertImageMenuItem = new ApplicationBarMenuItem(String.Format("{0}...", AppResources.InsertImageText));
			insertImageMenuItem.Click += insertImageMenuItem_Click;
			
			terminateMenuItem = new ApplicationBarMenuItem(AppResources.TerminateText);
			terminateMenuItem.Click += terminateMenuItem_Click;

			ApplicationBar.MenuItems.Add(sendPhotoMenuItem);
			ApplicationBar.MenuItems.Add(terminateMenuItem);
		}

		void deleteChatAppBarButton_Click(object sender, EventArgs e) {
			deleteChatAppBarButton.IsEnabled = false;

			ObservableCollection<Chat> savedChatList = new ObservableCollection<Chat>();
			if (appSettings.Contains("savedchats")) {
				savedChatList = appSettings["savedchats"] as ObservableCollection<Chat>;
			}

			foreach (Chat chat in savedChatList) {
				if (chat.Hash == chatHash) {
					savedChatList.Remove(chat);
					NavigationService.GoBack();
					break;
				}
			}

			appSettings["savedchats"] = savedChatList;
			appSettings.Save();
		}

		void saveChatAppBarButton_Click(object sender, EventArgs e) {
			saveChatAppBarButton.IsEnabled = false;

			ObservableCollection<Chat> savedChatList = new ObservableCollection<Chat>();
			if (appSettings.Contains("savedchats")) {
				savedChatList = appSettings["savedchats"] as ObservableCollection<Chat>;
			}

			savedChatList.Add(new Chat(messageList));
			appSettings["savedchats"] = savedChatList;
			appSettings.Save();
		}

		void sendS1AppBarButton_Click(object sender, EventArgs e) {
			generalSendTo(clientList[0]);
		}

		void sendS2AppBarButton_Click(object sender, EventArgs e) {
			generalSendTo(clientList[1]);
		}

		void insertImageMenuItem_Click(object sender, EventArgs e) {
			pauseAll();
			NavigationService.Navigate(new Uri("/ImagesPage.xaml", UriKind.Relative));
		}

		void sendPhotoAppBarButton_Click(object sender, EventArgs e) {
			pauseAll();
			PhotoChooserTask photoChooserTask = new PhotoChooserTask();
			photoChooserTask.Completed += new EventHandler<PhotoResult>(postImgur);
			photoChooserTask.Show();
			return;
		}

		void takePhotoAppBarButton_Click(object sender, EventArgs e) {
			pauseAll();
			CameraCaptureTask cameraCaptureTask = new CameraCaptureTask();
			cameraCaptureTask.Completed += new EventHandler<PhotoResult>(postImgur);
			cameraCaptureTask.Show();
			return;
		}

		async void shareChatAppBarButton_Click(object sender, EventArgs e) {
			ShareLinkTask sLT = new ShareLinkTask();
			sLT.Title = String.Format(AppResources.ShareTitleFormatText, messageList[0].Date, messageList[0].Time);
			string pasteUrl = await postPasteBin(messageList);
			if (pasteUrl == null) {
				MessageBox.Show(AppResources.ShareErrorText);
				return;
			}
			sLT.LinkUri = new Uri(pasteUrl, UriKind.Absolute);
			sLT.Message = AppResources.ShareLinkText;

			sLT.Show();

			return;
		}

		//handlers
		protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e) {

			if (recaptchaBox != null && recaptchaBox.IsOpen) {
				recaptchaBox.IsOpen = false;
				ContentPanel.Children.Remove(canvas);
				e.Cancel = true;

				return;
			}

			foreach (ClientStyler cs in clientList) {
				if (cs.Instance.isConnected()) {
					var result = MessageBox.Show(AppResources.ConfirmPrompt, AppResources.ConfirmTitle, MessageBoxButton.OKCancel);

					if (result == MessageBoxResult.OK) {
						generalDisconnect();

						break;
					} else {
						e.Cancel = true;
					}

					return;

				} else {
					cs.Instance.Stop();
				}

			}

			if (NavigationService.CanGoBack) {
				NavigationService.GoBack();
			} else {
				NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
			}
		}

		void newChatAppBarButton_Click(object sender, EventArgs e) {
			generalReconnect();
		}

		void sendAppBarButton_Click(object sender, EventArgs e) {
			generalMessageSend();
		}

		void terminateMenuItem_Click(object sender, EventArgs e) {
			generalDisconnect();
			checkAllDone();
		}

		private void messageEntryBox_GotFocus(object sender, RoutedEventArgs e) {
			foreach (ClientStyler cs in clientList) {
				cs.Instance.typing();
			}
		}

		private void messageEntryBox_LostFocus(object sender, RoutedEventArgs e) {
			foreach (ClientStyler cs in clientList) {
				cs.Instance.stoppedTyping();
			}
		}

		private void messageEntryBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
			if (e.Key == System.Windows.Input.Key.Enter) {
				generalMessageSend();
			}
		}
	}
}