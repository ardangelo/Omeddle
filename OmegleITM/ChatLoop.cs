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
using BugSense.Core.Model;

namespace Omeddle {
	public partial class ChatPage : PhoneApplicationPage {
		private Boolean usingAutoSolver = false;

		private void chatLoop(ClientStyler clientStyler) {
			clientStyler.isConnecting = true;
			clientStyler.isTyping = false;
			isDying = false; //when error event is recieved, set isDying so you don't get loads of msgboxes

			clientStyler.Instance.Start();

			Dispatcher.BeginInvoke(() => {
				foreach (ApplicationBarMenuItem abmi in ApplicationBar.MenuItems) {
					abmi.IsEnabled = false;
				}

				ApplicationBar.Buttons.Clear();
				ApplicationBar.Mode = ApplicationBarMode.Minimized;

				updateTitleBar();
				messageList = new ObservableCollection<Message>();
				messageLLS.ItemsSource = messageList;
			});

			while (true) {
				clientStyler.Instance.eventInQueueSignal.WaitOne();
				lock (clientStyler.Instance.eventQueue) {
					while (true) {
						Event currentEvent;

						//is there a better way to empty the queue?
						try {
							currentEvent = clientStyler.Instance.eventQueue.Dequeue();
						} catch {
							break;
						}

						//high level event handling
						switch (currentEvent.type) {
							case (EventType.Waiting):
								//sucessfully connected to omegle, waiting for stranger
								break;
							case (EventType.GotMessage):
								Dispatcher.BeginInvoke(() => {
									string msg = currentEvent.parameters[0];
									
									clientStyler.isTyping = false;
									updateTitleBar();

									if (chatMode == ChatMode.Intercept) {
										if (regexps != null) {
											foreach (Regexp regexp in regexps) {
												if (WordPage.IsValidRegex(regexp.Find)) {
													msg = System.Text.RegularExpressions.Regex.Replace(msg.ToLower(), regexp.Find, regexp.Replace);
												}
											}
										}

										foreach (ClientStyler cs in clientList) {
											if (cs != clientStyler) {
												cs.Instance.send(msg);
												Debug.WriteLine("Relayed message");
											}
										}
									}

									if (!msg.Equals(currentEvent.parameters[0])) {
										msg = String.Format("\"{0}\"\n{1}:\n\"{2}\"", currentEvent.parameters[0], AppResources.ChangedText, msg);
									}

									generalMessageAdd(clientStyler, msg);
								});
								break;
							case (EventType.Typing):
								Dispatcher.BeginInvoke(() => {
									clientStyler.isTyping = true;
									updateTitleBar();

									if (chatMode == ChatMode.Intercept) {
										foreach (ClientStyler cs in clientList) {
											if (!(cs.Equals(clientStyler))) {
												cs.Instance.typing();
											}
										}
									}
								});
								break;
							case (EventType.StoppedTyping):
								Dispatcher.BeginInvoke(() => {
									clientStyler.isTyping = false;
									updateTitleBar();

									if (chatMode == ChatMode.Intercept) {
										foreach (ClientStyler cs in clientList) {
											if (!(cs.Equals(clientStyler))) {
												cs.Instance.stoppedTyping();
											}
										}
									}
								});
								break;
							case (EventType.Connected):
								Dispatcher.BeginInvoke(() => {
									clientStyler.isConnecting = false;
									if (chatMode == ChatMode.AskQuestion) {
										generalMessageAdd(systemStyle, AppResources.StrangersConnectedText);
									} else {
										generalMessageAdd(systemStyle, String.Format(AppResources.ConnectedText, clientStyler.Number));
									}
									updateTitleBar();
									generalConnected();
								});
								break;
							case (EventType.StrangerDisconnected):
								Dispatcher.BeginInvoke(() => {
									clientStyler.Instance.disconnect();

									//clientStyler.Color = new SolidColorBrush(Colors.Gray);

									checkAllDone();

									generalMessageAdd(systemStyle, String.Format(AppResources.DisconnectedText, clientStyler.Number));
								});
								break;
							case (EventType.Question):
								Dispatcher.BeginInvoke(() => {
									generalMessageAdd(systemStyle, String.Format(AppResources.GotQuestionText, currentEvent.parameters[0]));
								});
								break;
							case (EventType.RecaptchaRequired):
								Dispatcher.BeginInvoke(() => {
									pauseAll();
									showRecaptchaBox(clientStyler, currentEvent.parameters[0]);
								});
								break;
							case (EventType.RecaptchaRejected):
								Dispatcher.BeginInvoke(() => {
									pauseAll();
									if (usingAutoSolver) {
										int autoSolveCredit = 0;
										if (appSettings.Contains("solvecredit")) {
											autoSolveCredit = (int)appSettings["solvecredit"];
										} else {
											appSettings["solvecredit"] = autoSolveCredit;
											appSettings.Save();
										}
										appSettings["solvecredit"] = autoSolveCredit + 1;
										appSettings.Save();
										MessageBox.Show(AppResources.AutoSolveFailedText);
										usingAutoSolver = false;
										//report failed solve to DBC
										return; return;
									}
									showRecaptchaBox(clientStyler, currentEvent.parameters[0]);
								});
								break;
							case (EventType.CommonLikes):
								Dispatcher.BeginInvoke(() => {
									string topics = ""; Boolean isFirst = true;
									foreach (string topic in currentEvent.parameters) {
										if (!isFirst) { topics += ","; isFirst = false; }
										topics += String.Format(" \"{0}\"", topic);
									}

									generalMessageAdd(systemStyle, String.Format(AppResources.YouBothLikeText, topics));
								});
								break;
							case (EventType.SpyMessage):
								Dispatcher.BeginInvoke(() => {
									int stranger = 0;
									int.TryParse(currentEvent.parameters[0].Substring(currentEvent.parameters[0].Length - 1), out stranger);
									string msg = currentEvent.parameters[1];

									if (stranger == 1) {
										generalMessageAdd(((SpyStyler)clientStyler).stranger1Styler, msg);
									} else if (stranger == 2) {
										generalMessageAdd(((SpyStyler)clientStyler).stranger2Styler, msg);
									} else {
										//internal error
										Debug.WriteLine("Stranger number not 1 or 2!");
									}
								});
								break;
							case (EventType.SpyDisconnected):
								Dispatcher.BeginInvoke(() => {
									
									int stranger = 0;
									int.TryParse(currentEvent.parameters[0].Substring(currentEvent.parameters[0].Length - 1), out stranger);

									clientStyler.Instance.disconnect();

									generalMessageAdd(systemStyle, String.Format(AppResources.DisconnectedText, stranger));

									checkAllDone();
								});
								break;
							case (EventType.Error):
								Dispatcher.BeginInvoke(() => {

									if ((((ErrorEvent)currentEvent).exception is System.Net.Http.HttpRequestException) &&
										(((System.Net.Http.HttpRequestException)(((ErrorEvent)currentEvent).exception)).InnerException is WebException) &&
											(((WebException)((System.Net.Http.HttpRequestException)(((ErrorEvent)currentEvent).exception)).InnerException).Status == WebExceptionStatus.RequestCanceled)) {
										Debug.WriteLine("threw out requestcancelled error");
										return; //no big deal, just try again on next eventthread trigger
									}

									clientStyler.Instance.Stop();

									if (isDying) { return; }
									isDying = true;

									checkAllDone();
									MessageBox.Show(AppResources.ConnectionError);

									LimitedCrashExtraDataList extrasExtraDataList = new LimitedCrashExtraDataList {
										new CrashExtraData("Omeddle", "Generic ErrorEvent")
									};
#if DEBUG
									throw ((ErrorEvent)currentEvent).exception;
#else
									BugSenseLogResult sendResult = BugSense.BugSenseHandler.Instance.LogException(((ErrorEvent)currentEvent).exception);
#endif

									
								});
								break;
						}
					}
				}
			}
		}
	}
}
