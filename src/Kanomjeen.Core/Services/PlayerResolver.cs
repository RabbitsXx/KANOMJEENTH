using System;
using System.Collections.Generic;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace Kanomjeen.Core.Services
{
    public enum PlayerResolveStatus
    {
        Found,
        NotFound,
        Ambiguous
    }

    public sealed class PlayerResolveResult
    {
        public PlayerResolveStatus Status { get; private set; }
        public UnturnedPlayer Player { get; private set; }
        public int MatchCount { get; private set; }

        public static PlayerResolveResult Found(UnturnedPlayer player) => new PlayerResolveResult { Status = PlayerResolveStatus.Found, Player = player, MatchCount = 1 };
        public static PlayerResolveResult NotFound() => new PlayerResolveResult { Status = PlayerResolveStatus.NotFound };
        public static PlayerResolveResult Ambiguous(int count) => new PlayerResolveResult { Status = PlayerResolveStatus.Ambiguous, MatchCount = count };
    }

    public sealed class PlayerResolver
    {
        public PlayerResolveResult Resolve(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return PlayerResolveResult.NotFound();
            var needle = input.Trim();
            var partial = new List<UnturnedPlayer>();

            foreach (var steamPlayer in Provider.clients)
            {
                var player = UnturnedPlayer.FromSteamPlayer(steamPlayer);
                if (player == null || player.Player == null) continue;

                if (string.Equals(player.Id, needle, StringComparison.Ordinal) ||
                    string.Equals(player.DisplayName, needle, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(player.SteamName, needle, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(player.CharacterName, needle, StringComparison.OrdinalIgnoreCase))
                    return PlayerResolveResult.Found(player);

                if ((player.DisplayName ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (player.SteamName ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (player.CharacterName ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    partial.Add(player);
            }

            if (partial.Count == 1) return PlayerResolveResult.Found(partial[0]);
            if (partial.Count > 1) return PlayerResolveResult.Ambiguous(partial.Count);
            return PlayerResolveResult.NotFound();
        }

        public UnturnedPlayer FindById(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return null;
            foreach (var steamPlayer in Provider.clients)
            {
                var player = UnturnedPlayer.FromSteamPlayer(steamPlayer);
                if (player != null && player.Player != null && string.Equals(player.Id, playerId, StringComparison.Ordinal)) return player;
            }
            return null;
        }
    }
}
