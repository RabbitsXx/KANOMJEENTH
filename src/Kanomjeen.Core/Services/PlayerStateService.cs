using System;
using System.Collections.Generic;

namespace Kanomjeen.Core.Services
{
    public sealed class PlayerRuntimeState
    {
        public DateTime CombatUntilUtc { get; set; } = DateTime.MinValue;
        public DateTime RaidUntilUtc { get; set; } = DateTime.MinValue;
        public DateTime LastDamageUtc { get; set; } = DateTime.MinValue;
        public bool IsTeleporting { get; set; }

        public bool IsInCombat(DateTime nowUtc) => CombatUntilUtc > nowUtc;
        public bool IsRaidTagged(DateTime nowUtc) => RaidUntilUtc > nowUtc;
    }

    public sealed class PlayerStateService
    {
        private readonly Dictionary<string, PlayerRuntimeState> states = new Dictionary<string, PlayerRuntimeState>(StringComparer.Ordinal);

        public PlayerRuntimeState GetOrCreate(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("playerId is required", nameof(playerId));
            if (!states.TryGetValue(playerId, out var state))
            {
                state = new PlayerRuntimeState();
                states[playerId] = state;
            }
            return state;
        }

        public void TagCombat(string playerId, DateTime nowUtc, TimeSpan duration)
        {
            var state = GetOrCreate(playerId);
            var until = nowUtc.Add(duration < TimeSpan.Zero ? TimeSpan.Zero : duration);
            if (until > state.CombatUntilUtc) state.CombatUntilUtc = until;
            state.LastDamageUtc = nowUtc;
        }

        public void TagRaid(string playerId, DateTime nowUtc, TimeSpan duration)
        {
            var state = GetOrCreate(playerId);
            var until = nowUtc.Add(duration < TimeSpan.Zero ? TimeSpan.Zero : duration);
            if (until > state.RaidUntilUtc) state.RaidUntilUtc = until;
        }

        public void Remove(string playerId)
        {
            if (!string.IsNullOrWhiteSpace(playerId)) states.Remove(playerId);
        }

        public void Clear() => states.Clear();
    }
}
