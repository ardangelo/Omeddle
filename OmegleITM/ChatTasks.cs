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
using System.Net.Http;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;

namespace Omeddle {
	public partial class ChatPage : PhoneApplicationPage {

		internal static string urlEncode(Dictionary<string, string> content) {
			Boolean isFirst = true;
			string construct = "";
			foreach (KeyValuePair<string, string> prop in content) {
				if (!isFirst) { construct += "&"; }
				construct += prop.Key + "=" + Uri.EscapeDataString(prop.Value);
				isFirst = false;
			}

			return construct;
		}

		//tasks
		public void updateTitleBar() { updateTitleBar(""); }
		public void updateTitleBar(string prefix) {
			string resultText = "";
			Boolean somethingHappening = false;

			if (prefix.Length > 0) {
				somethingHappening = true;
				resultText += prefix;
			}

			foreach (ClientStyler cs in clientList) {
				if (cs.isConnecting) {
					somethingHappening = true;
					if (chatMode == ChatMode.AskQuestion) {
						resultText += AppResources.StrangersConnectingText;
					} else {
						resultText += String.Format(AppResources.ConnectingText, cs.Number);
					}
					
				}

				if (cs.isTyping) {
					somethingHappening = true;
					resultText += String.Format(AppResources.TypingText, cs.Number);
				}
			}

			if (somethingHappening) {
				Dispatcher.BeginInvoke(() => {
					TypingIndicator.IsVisible = true;
					titleBar.Text = resultText.ToUpper();
				});
			} else {
				Dispatcher.BeginInvoke(() => {
					TypingIndicator.IsVisible = false;
					titleBar.Text = chatModeText.ToUpper();
				});
			}
		}

		private void generalSendTo(ClientStyler cs) {
			messageLLS.Focus();
			if (messageEntryBox.Text.Equals("")) { return; }
			
			cs.Instance.send(messageEntryBox.Text);

			generalMessageAdd(userStyle, String.Format("{0}:\n\"{1}\"", String.Format(AppResources.SendToText, cs.Number.ToString()), messageEntryBox.Text));
			messageEntryBox.Text = "";
		}

		private void generalMessageSend() {
			messageLLS.Focus();
			if (messageEntryBox.Text.Equals("")) { return; }

			foreach (ClientStyler cs in clientList) {
				if (cs.Instance.isConnected()) {
					cs.Instance.send(messageEntryBox.Text);
				}
			}

			generalMessageAdd(userStyle, messageEntryBox.Text);
			messageEntryBox.Text = "";
		}

		private void generalConnected() {
			if (chatMode != ChatMode.AskQuestion) {
				inputPanelControl.IsEnabled = true;
			}

			foreach (ApplicationBarMenuItem abmi in ApplicationBar.MenuItems) {
				abmi.IsEnabled = true;
			}

			ApplicationBar.Mode = ApplicationBarMode.Default;
			ApplicationBar.Buttons.Clear();

			if (chatMode == ChatMode.Intercept) {
				ApplicationBar.Buttons.Add(sendS1AppBarButton);
			}
			ApplicationBar.Buttons.Add(sendAppBarButton);
			if (Microsoft.Devices.Camera.IsCameraTypeSupported(Microsoft.Devices.CameraType.Primary) ||
				Microsoft.Devices.Camera.IsCameraTypeSupported(Microsoft.Devices.CameraType.FrontFacing)) {
					ApplicationBar.Buttons.Add(takePhotoAppBarButton);
			}
			if (chatMode == ChatMode.Intercept) {
				ApplicationBar.Buttons.Add(sendS2AppBarButton);
			}
		}

		private void generalDisconnect() {
			foreach (ClientStyler cs in clientList) {
				if (cs.Instance.isConnected()) {
					cs.Instance.disconnect();
				}
			}
			generalMessageAdd(systemStyle, AppResources.YouDisconnectedText);
		}

		private void generalMessageAdd(ClientStyler styler, string msg) {
			messageList.Add(new Message(styler, msg));
			messageLLS.ItemsSource = messageList;
			messageLLS.ScrollTo(messageList.LastOrDefault());
		}

		private async void showRecaptchaBox(ClientStyler cs, string challengeRequest) {
			canvas = new Canvas();
			System.Windows.Media.Color bgC = (System.Windows.Media.Color)Application.Current.Resources["PhoneAccentColor"];
			canvas.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, (byte)(bgC.R * .85), (byte)(bgC.G * .85), (byte)(bgC.B * .85)));
			canvas.Margin = new Thickness(-50, 0, 0, 0);
			canvas.Width = 1000;
			canvas.Opacity = .5;
			ContentPanel.Children.Add(canvas);
			
			if (recaptchaBox.Child == null) {
				RecaptchaDialog daBox = new RecaptchaDialog();

				Uri requestChallengeUri = new Uri(String.Format(Client.recaptchaChallenge, challengeRequest));
				HttpClient browser = new HttpClient();
				HttpResponseMessage result;
				string challenge = "";

				HttpContent content = new StringContent("");
				content.Headers.ContentLength = 0;
				
				int autoSolveCredit = 0;
				if (appSettings.Contains("solvecredit")) {
					autoSolveCredit = (int)appSettings["solvecredit"];
				} else {
					appSettings["solvecredit"] = autoSolveCredit;
					appSettings.Save();
				}

				if (autoSolveCredit == 0) {
					daBox.AutoSolveButton.IsEnabled = false;
				}
				daBox.AutoSolveButton.Content = String.Format(AppResources.AutoSolveText, autoSolveCredit);

				try {
					result = await browser.PostAsync(requestChallengeUri, content);
					string response = result.Content.ReadAsStringAsync().Result;

					System.Text.RegularExpressions.Regex challengeRegex = new System.Text.RegularExpressions.Regex(Client.challengeRegex);
					System.Text.RegularExpressions.Match challengeMatch = challengeRegex.Match(response);
					challenge = challengeMatch.Value.Substring(challengeMatch.Value.IndexOf(" : ") + " : '".Length);
					challenge = challenge.Substring(0, challenge.Length - 1);
				} catch {}

				if (challenge.Equals("")) {
					MessageBox.Show(AppResources.ConnectionError);
				}

				daBox.recapchaImageBox.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(String.Format(Client.recaptchaImage, challenge), UriKind.Absolute));
				Debug.WriteLine(String.Format(Client.recaptchaImage, challenge));

				daBox.NewCaptchaButton.Click += delegate(object sender, RoutedEventArgs e) {
					cs.Instance.recapcha(challenge, "0");

					if (recaptchaBox != null) {
						recaptchaBox.IsOpen = false;
						ContentPanel.Children.Remove(canvas);

						recaptchaBox.Child = null;
						daBox.recapchaImageBox = null;
						daBox.responseTextBox.Text = "";

						unpauseAll();
					}
				};
				daBox.SubmitCaptchaButton.Click += delegate(object sender, RoutedEventArgs e) {
					cs.Instance.recapcha(Uri.EscapeDataString(challenge), Uri.EscapeDataString(daBox.responseTextBox.Text));
					Debug.WriteLine(Uri.EscapeUriString(daBox.responseTextBox.Text));
					if (recaptchaBox != null) {
						recaptchaBox.IsOpen = false;
						ContentPanel.Children.Remove(canvas);

						recaptchaBox.Child = null;
						daBox.recapchaImageBox = null;
						daBox.responseTextBox.Text = "";

						unpauseAll();

						if (chatMode == ChatMode.Intercept) {
							generalDisconnect();
							generalReconnect();
						}
					}

				};
				/*daBox.AutoSolveButton.Click += async delegate(object sender, RoutedEventArgs e) {
					string dbcResponse = "";
					//change button: disabled and solving text
					//download captcha image and convert to base64
					//send to dbc api
					//poll every 4 seconds
					//submit attempt to omegle

					Debug.WriteLine(Uri.EscapeUriString(dbcResponse));
					if (recaptchaBox != null) {
						recaptchaBox.IsOpen = false;
						ContentPanel.Children.Remove(canvas);

						recaptchaBox.Child = null;
						daBox.recapchaImageBox = null;
						daBox.responseTextBox.Text = "";

						unpauseAll();

						if (chatMode == ChatMode.Intercept) {
							generalDisconnect();
							generalReconnect();
						}
					}
					return; return;
				};*/
				recaptchaBox.Child = daBox;
			}

			recaptchaBox.IsOpen = true;
		}

		private void checkAllDone() {
			//foreach (ClientStyler cs in clientList) {
			//	cs.Instance.disconnect();
			//}

			Boolean allDone = true;
			foreach (ClientStyler cs in this.clientList) {
				if (cs.isConnected()) {
					allDone = false;
				}
			} 

			if (allDone) {
				titleBar.Text = chatModeText.ToUpper();

				TypingIndicator.IsVisible = false;
				inputPanelControl.IsEnabled = false;

				foreach (ApplicationBarMenuItem abmi in ApplicationBar.MenuItems) {
					abmi.IsEnabled = false;
				}

				ApplicationBar.Buttons.Clear();
				ApplicationBar.Buttons.Add(newChatAppBarButton);
				ApplicationBar.Buttons.Add(shareChatAppBarButton);
				ApplicationBar.Buttons.Add(saveChatAppBarButton);

				if (recentChatList.Count == 10) {
					recentChatList.RemoveAt(9);
				}
				if (messageList.Count > 0) {
					recentChatList.Insert(0, new Chat(messageList));
				}

				appSettings["recents"] = recentChatList;
				appSettings.Save();
				
			}
		}

		private string chatListToString(ObservableCollection<Message> list) {
			string result = "";

			foreach (Message msg in list) {
				result += String.Format("[{0}] {1}: {2}\n", msg.Time, msg.ParentName, msg.Text);
			}

			return result;
		}

		private async Task<string> postPasteBin(ObservableCollection<Message> list) {
			updateTitleBar(AppResources.UploadingText);

			string apiKey = "89dc6a31f7a1bef226b83f031c4da346";
			string pasteTitle = String.Format(AppResources.ShareTitleFormatText, list[0].Date, list[0].Time);
			string content = String.Format("api_option={0}&api_dev_key={1}&api_paste_name={2}&api_paste_code={3}", "paste", apiKey, Uri.EscapeUriString(pasteTitle), Uri.EscapeUriString(chatListToString(list)));
			Debug.WriteLine(content);
			string baseUrl = "http://pastebin.com/api/api_post.php";

			HttpResponseMessage result = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
			HttpClient browser = new HttpClient();

			try {
				//make request
				Task<HttpResponseMessage> resultTask = browser.PostAsync(baseUrl, new StringContent(content, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"));
				result = await resultTask;
			} catch {
				updateTitleBar();
				return null;
			}
			Debug.WriteLine("Response: {0}", result.Content.ReadAsStringAsync().Result);

			updateTitleBar();
			return result.Content.ReadAsStringAsync().Result;
		}

		private async void postImgur(object sender, PhotoResult e) {
			string base64String = "";
			string baseUrl = "https://api.imgur.com/3/upload.xml";
			
			HttpClient browser = new HttpClient();
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, baseUrl);
			HttpResponseMessage result = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);

			if (e.TaskResult == TaskResult.OK) {
				updateTitleBar(AppResources.UploadingText);

				System.Windows.Media.Imaging.BitmapImage bmp = new System.Windows.Media.Imaging.BitmapImage();
				bmp.SetSource(e.ChosenPhoto);

				byte[] bytearray = null;
				using (var ms = new System.IO.MemoryStream()) {
					if (bmp != null) {
						var wbitmp = new WriteableBitmap((BitmapImage)bmp);

						wbitmp.SaveJpeg(ms, bmp.PixelWidth, bmp.PixelHeight, 0, 85);
						bytearray = ms.ToArray();
					}
				}
				if (bytearray != null) {
					base64String = Convert.ToBase64String(bytearray);
				} else { Debug.WriteLine("bytearray was null"); }

				request.Content = new StringContent(base64String);
				request.Headers.Add("Authorization", "Client-ID " + AppResources.ImgurApiKey);

				//make request
				try {
					//make request
					Task<HttpResponseMessage> resultTask = browser.SendAsync(request);
					result = await resultTask;
				} catch {
					updateTitleBar();
					return;
				}
				Debug.WriteLine("Response: {0}", result.Content.ReadAsStringAsync().Result);

				System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("<link>(.+?)</link>");
				System.Text.RegularExpressions.Match match = reg.Match(result.Content.ReadAsStringAsync().Result);
				string url = match.ToString().Replace("<link>", "").Replace("</link>", "");
				if (messageEntryBox.Text.Length > 0) {
					messageEntryBox.Text += url;
				} else {
					messageEntryBox.Text = url;
				}
				//nearly identical regexes :( fix
				reg = new System.Text.RegularExpressions.Regex("<id>(.+?)</id>");
				match = reg.Match(result.Content.ReadAsStringAsync().Result);
				string id = match.ToString().Replace("<id>", "").Replace("</id>", "");

				reg = new System.Text.RegularExpressions.Regex("<deletehash>(.+?)</deletehash>");
				match = reg.Match(result.Content.ReadAsStringAsync().Result);
				string dh = match.ToString().Replace("<deletehash>", "").Replace("</deletehash>", "");

				imageList.Add(new ImgurImage(id, url, dh));
				appSettings["images"] = imageList;
				appSettings.Save();

				updateTitleBar();
				return;
			}
		}

		public void generalReconnect() {
			string host = Client.defaultHost;
			if (appSettings.Contains("usingCustomHost") && appSettings.Contains("customHost")) {
				if ((bool)(appSettings["usingCustomHost"])) {
					host = (string)(appSettings["customHost"]);
				}
			}

			foreach (ClientStyler cs in clientList) {
				if (cs.Instance.isConnected()) {
					throw new InvalidOperationException("Tried to reconnect while already connected");
				}

				if (this.chatMode == ChatMode.Chat || this.chatMode == ChatMode.Intercept) {
					cs.Instance = new Client(topics, chatLanguage, host);
				} else if (this.chatMode == ChatMode.AskQuestion) {
					cs.Instance = new Client(ask, canSaveQuestion, chatLanguage, host);
				} else if (this.chatMode == ChatMode.AnswerQuestion) {
					cs.Instance = new Client(true, chatLanguage, host);
				}


				Task.Factory.StartNew(() => {
					chatLoop(cs);
				});
			}
		}

		public void pauseAll() {
			foreach (ClientStyler cs in clientList) {
				cs.Instance.Pause();
			}
		}

		public void unpauseAll() {
			foreach (ClientStyler cs in clientList) {
				try {
					cs.Instance.Unpause();
				} catch (NullReferenceException) {
					MessageBox.Show(AppResources.ConnectionError);
				}
			}
		}
	}
}
