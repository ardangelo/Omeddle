using System.Diagnostics;
using System.Windows;
using Omegle;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System.Linq;

namespace TileUpdateAgent {
	public class ScheduledAgent : ScheduledTaskAgent {
		/// <remarks>
		/// ScheduledAgent constructor, initializes the UnhandledException handler
		/// </remarks>
		static ScheduledAgent() {
			// Subscribe to the managed exception handler
			Deployment.Current.Dispatcher.BeginInvoke(delegate {
				Application.Current.UnhandledException += UnhandledException;
			});
		}

		/// Code to execute on Unhandled Exceptions
		private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e) {
			if (Debugger.IsAttached) {
				// An unhandled exception has occurred; break into the debugger
				Debugger.Break();
			}
		}

		/// <summary>
		/// Agent that runs a scheduled task
		/// </summary>
		/// <param name="task">
		/// The invoked task
		/// </param>
		/// <remarks>
		/// This method is called when a periodic or resource intensive task is invoked
		/// </remarks>
		protected override async void OnInvoke(ScheduledTask task) {
			//TODO: Add code to perform your task in background
			ShellTile appTile = ShellTile.ActiveTiles.First();
			FlipTileData tileData;

			try {
				string nu = await Omegle.Client.getNumberOfUsers() + "\nusers online";

				tileData = new FlipTileData() {
					BackContent = await Omegle.Client.getNumberOfUsers() + "\nusers online",
					BackTitle = "Omeddle",
					Title = "Omeddle",
					WideBackContent = await Omegle.Client.getNumberOfUsers() + "\nusers online"
				};
			} catch {
				tileData = new FlipTileData() {
					BackContent = "Connection error",
					BackTitle = "Omeddle",
					Title = "Omeddle",
					WideBackContent = "Connection error"
				};
			}
			
			appTile.Update(tileData);

			NotifyComplete();
		}
	}
}