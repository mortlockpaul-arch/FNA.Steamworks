#region License
/* FNA.Steamworks - XNA4 Xbox Live Reimplementation for Steamworks
 * Copyright 2016 Ethan "flibitijibibo" Lee
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.Collections.Generic;

using Steamworks;
#endregion

namespace Microsoft.Xna.Framework.GamerServices
{
	public static class GamerServicesDispatcher
	{
		#region Public Static Properties

		public static bool IsInitialized
		{
			get;
			private set;
		}

		public static IntPtr WindowHandle
		{
			get;
			set;
		}

		#endregion

		#region Private Static Variables

		private static object steamUpdateMutex = new object();

#pragma warning disable 0414
		private static Callback<GameOverlayActivated_t> overlayActivated;
		private static Callback<GamepadTextInputDismissed_t> textInputDismissed;
		private static Callback<GameLobbyJoinRequested_t> lobbyJoinRequested;
#pragma warning restore 0414

		#endregion

		#region Public Static Events

#pragma warning disable 0067
		// This should never happen, but lol XNA4 compliance -flibit
		public static event EventHandler<EventArgs> InstallingTitleUpdate;
#pragma warning restore 0067

		#endregion

		#region Public Static Methods

		public static void Initialize(IServiceProvider serviceProvider)
		{
            try
            {
                IsInitialized = SteamAPI.Init();

                if (!IsInitialized)
                {
                    Console.WriteLine(
                        "Steam is unavailable. Continuing without Steam services."
                    );
                }
            }
            catch (DllNotFoundException ex)
            {
                IsInitialized = false;
                Console.WriteLine($"Steamworks DLL unavailable: {ex.Message}");
            }
            catch (BadImageFormatException ex)
            {
                IsInitialized = false;
                Console.WriteLine($"Steamworks architecture mismatch: {ex.Message}");
            }
            catch (EntryPointNotFoundException ex)
            {
                IsInitialized = false;
                Console.WriteLine($"Steamworks entry point unavailable: {ex.Message}");
            }

            // [OBS] (half life scientist) everything..  seems to be in order

            AppDomain.CurrentDomain.ProcessExit += (o, e) =>
			{
                if (IsInitialized)
                {
                    SteamAPI.Shutdown();
                    IsInitialized = false;
                }
            };

			overlayActivated = Callback<GameOverlayActivated_t>.Create(Guide.OnOverlayActivated);
			textInputDismissed = Callback<GamepadTextInputDismissed_t>.Create(Guide.OnTextInputDismissed);
			lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(Net.NetworkSession.OnInviteAccepted);

            if (IsInitialized)
            {
                // SteamUserStats.RequestCurrentStats();
                // Removed for compatibility with newer Steamworks.NET.
            }
            else
            {
                Console.WriteLine(
                    "Steamworks unavailable. Skipping Steam user statistics."
                );
            }
            List<SignedInGamer> startGamers = new List<SignedInGamer>(4);

            bool steamAvailable = IsInitialized;

            // Use the real Steam account when available.
            // Otherwise, create an offline identity.
            CSteamID steamID = steamAvailable
                ? SteamUser.GetSteamID()
                : new CSteamID(0);

            string gamerName = steamAvailable
                ? SteamFriends.GetPersonaName()
                : "Player";

            // Create up to four local gamers.
            for (int i = 0; i < 4; i++)
            {
                PlayerIndex playerIndex = (PlayerIndex)i;

                string playerName = i == 0
                    ? gamerName
                    : $"{gamerName} ({i})";

                SignedInGamer gamer;

                if (i == 0)
                {
                    gamer = new SignedInGamer(
                        steamID,
                        playerName,
                        steamAvailable
                    );
                }
                else
                {
                    gamer = new SignedInGamer(
                        steamID,
                        playerName,
                        steamAvailable,
                        true,
                        playerIndex
                    );
                }

                startGamers.Add(gamer);
            }

            Gamer.SignedInGamers = new SignedInGamerCollection(startGamers);

            foreach (SignedInGamer gamer in Gamer.SignedInGamers)
            {
                SignedInGamer.OnSignIn(gamer);
            }
        }

		public static void Update()
		{
			lock (steamUpdateMutex)
			{
                if (IsInitialized)
                {
                    SteamAPI.RunCallbacks();
                }
            }

			// TODO: Guest hotplugging!
		}

		internal static bool UpdateAsync()
		{
			// If a thread's calling this after we quit, tell it to panic!
			if (IsInitialized)
			{
				Update();
			}
			return IsInitialized;
		}

		#endregion
	}
}
