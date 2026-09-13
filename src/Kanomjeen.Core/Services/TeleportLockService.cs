using System;
using System.Collections.Generic;

namespace Kanomjeen.Core.Services
{
    public sealed class TeleportLockService
    {
        private readonly HashSet<string> active = new HashSet<string>(StringComparer.Ordinal);

        public bool TryAcquire(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return false;
            return active.Add(playerId);
        }

        public bool IsActive(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId) && active.Contains(playerId);
        }

        public void Release(string playerId)
        {
            if (!string.IsNullOrWhiteSpace(playerId)) active.Remove(playerId);
        }

        public void Clear() => active.Clear();
    }
}
