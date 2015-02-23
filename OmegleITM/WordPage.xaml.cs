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
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;

namespace Omeddle {
		
	[DataContract]
	public class Regexp {
		[DataMember]
		public Boolean IsActive { get; set; }
		[DataMember]
		public string Find { get; set; }
		[DataMember]
		public string Replace { get; set; }

		public Regexp(string find, string replace) {
			IsActive = true;
			Find = find;
			Replace = replace;
		}
	}

	public partial class WordPage : PhoneApplicationPage {
		ObservableCollection<Regexp> regexps;
		IsolatedStorageSettings appSettings;
		System.Windows.Media.SolidColorBrush defaultColor;
		ApplicationBarIconButton deleteButton;
		ApplicationBarMenuItem clearRegexpItem;

		public WordPage() {
			InitializeComponent();
			BuildLocalizedApplicationBar();

			appSettings = IsolatedStorageSettings.ApplicationSettings;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);

			//load data from save
			if (!appSettings.Contains("regexps")) {
				regexps = new ObservableCollection<Regexp>();
				writeRegexpsToStorage();
			} else {
				regexps = appSettings["regexps"] as ObservableCollection<Regexp>;
			}
			//update lls
			regexpList.ItemsSource = regexps;

			if (regexps.Count == 0) {
				clearRegexpItem.IsEnabled = false;
			}
		}

		private void BuildLocalizedApplicationBar() {
			// Set the page's ApplicationBar to a new instance of ApplicationBar.
			ApplicationBar = new ApplicationBar();

			deleteButton = new ApplicationBarIconButton(new Uri("/Assets/delete.png", UriKind.Relative));
			deleteButton.Text = AppResources.DeleteText;
			deleteButton.Click += Button_Click;

			// Create a new button and set the text value to the localized string from AppResources.
			ApplicationBarIconButton newRegexpButton = new ApplicationBarIconButton(new Uri("/Assets/add.png", UriKind.Relative));
			newRegexpButton.Text = AppResources.AddNewText;
			newRegexpButton.Click += newRegexpButton_Click;
			ApplicationBar.Buttons.Add(newRegexpButton);

			clearRegexpItem = new ApplicationBarMenuItem(AppResources.ClearText);
			clearRegexpItem.Click += clearRegexpButton_Click;
			ApplicationBar.MenuItems.Add(clearRegexpItem);

			ApplicationBarIconButton helpButton = new ApplicationBarIconButton(new Uri("/Assets/question.png", UriKind.Relative));
			helpButton.Text = AppResources.ExampleText;
			helpButton.Click += helpButton_Click;
			ApplicationBar.Buttons.Add(helpButton);
		}

		public static bool IsValidRegex(string pattern) {
			if (string.IsNullOrEmpty(pattern)) return false;

			try {
				System.Text.RegularExpressions.Regex test = new System.Text.RegularExpressions.Regex(pattern);
			} catch (ArgumentException) {
				return false;
			}

			return true;
		}
		
		//handlers
		void helpButton_Click(object sender, EventArgs e) {
			MessageBox.Show(AppResources.RegexExampleText);
		}

		void newRegexpButton_Click(object sender, EventArgs e) {

			pageControl.IsEnabled = false;
			regexps.Add(new Regexp("Find", "Replace"));
			writeRegexpsToStorage();

			regexpList.ItemsSource = regexps;
			pageControl.IsEnabled = true;

			clearRegexpItem.IsEnabled = true;
		}

		void clearRegexpButton_Click(object sender, EventArgs e) {
			regexps = new ObservableCollection<Regexp>();
			writeRegexpsToStorage();

			regexpList.ItemsSource = regexps;
			regexpList.SelectedItem = null;
			pageControl.IsEnabled = true;
		}

		private void writeRegexpsToStorage() {
			appSettings["regexps"] = regexps;
			appSettings.Save();
		}

		private void TextBox_ValidateRegex(object sender, RoutedEventArgs e) {
			if (!IsValidRegex(((TextBox)sender).Text)) {
				((TextBox)sender).BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
			} else {
				((TextBox)sender).BorderBrush = defaultColor;
			}

			TextBox_LostFocus(sender, e);
		}

		private void TextBox_Loaded(object sender, RoutedEventArgs e) {
			defaultColor = (System.Windows.Media.SolidColorBrush)((TextBox)sender).BorderBrush;
		}

		private void TextBox_LostFocus(object sender, RoutedEventArgs e) {
			writeRegexpsToStorage();
		}

		private void Button_Click(object sender, EventArgs e) {
			if (regexpList.SelectedItem == null) { return; }

			regexps.Remove((Regexp)regexpList.SelectedItem);
			writeRegexpsToStorage();

			regexpList.SelectedItem = null;

			if (regexps.Count == 0) {
				clearRegexpItem.IsEnabled = false;
			}
		}

		private void regexpList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (ApplicationBar.Buttons.Contains(deleteButton)) {
				ApplicationBar.Buttons.Remove(deleteButton);
			}
			if (regexpList.SelectedItem == null) { return; }

			ApplicationBar.Buttons.Add(deleteButton);
		}
	}
}