using System;
using System.Collections.Generic;

namespace Kanomjeen.Core.Services
{
    public sealed class CommandRateLimiter
    {
        private readonly Dictionary<string, DateTime> nextAllowedUtc = new Dictionary<string, DateTime>(StringComparer.Ordinal);

        public bool TryConsume(string playerId, string action, DateTime nowUtc, TimeSpan interval, out TimeSpan remaining)
        {
            var key = (playerId ?? "") + "|" + (action ?? "");
            if (nextAllowedUtc.TryGetValue(key, out var next) && next > nowUtc)
            {
                remaining = next - nowUtc;
                return false;
            }

            nextAllowedUtc[key] = nowUtc.Add(interval < TimeSpan.Zero ? TimeSpan.Zero : interval);
            remaining = TimeSpan.Zero;
            return true;
        }

        public void RemovePlayer(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            var prefix = playerId + "|";
            var remove = new List<string>();
            foreach (var key in nextAllowedUtc.Keys)
                if (key.StartsWith(prefix, StringComparison.Ordinal)) remove.Add(key);
            foreach (var key in remove) nextAllowedUtc.Remove(key);
        }

        public void Clear() => nextAllowedUtc.Clear();
    }
}
