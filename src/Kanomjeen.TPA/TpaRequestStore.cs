using System;
using System.Collections.Generic;

namespace Kanomjeen.TPA
{
    public enum TpaDirection { ToTarget, ToRequester }

    public sealed class TpaRequest
    {
        public string RequesterId;
        public string RequesterName;
        public string TargetId;
        public string TargetName;
        public TpaDirection Direction;
        public DateTime ExpiresUtc;
    }

    public sealed class TpaRequestStore
    {
        private readonly List<TpaRequest> requests = new List<TpaRequest>();
        private readonly HashSet<string> blocked = new HashSet<string>(StringComparer.Ordinal);

        public bool IsBlocked(string playerId) => blocked.Contains(playerId);
        public bool ToggleBlocked(string playerId) => blocked.Contains(playerId) ? !blocked.Remove(playerId) : blocked.Add(playerId);

        public int OutgoingCount(string playerId)
        {
            var count = 0; foreach (var r in requests) if (r.RequesterId == playerId) count++; return count;
        }

        public int IncomingCount(string playerId)
        {
            var count = 0; foreach (var r in requests) if (r.TargetId == playerId) count++; return count;
        }

        public bool HasDuplicate(string requesterId, string targetId)
        {
            foreach (var r in requests) if (r.RequesterId == requesterId && r.TargetId == targetId) return true;
            return false;
        }

        public void Add(TpaRequest request) => requests.Add(request);
        public void Remove(TpaRequest request) => requests.Remove(request);

        public TpaRequest NewestIncoming(string targetId, string requesterName = null)
        {
            TpaRequest best = null;
            for (var i = 0; i < requests.Count; i++)
            {
                var r = requests[i];
                if (r.TargetId != targetId) continue;
                if (!string.IsNullOrWhiteSpace(requesterName) && r.RequesterName.IndexOf(requesterName, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (best == null || r.ExpiresUtc > best.ExpiresUtc) best = r;
            }
            return best;
        }

        public List<TpaRequest> RemoveOutgoing(string requesterId)
        {
            var removed = new List<TpaRequest>();
            for (var i = requests.Count - 1; i >= 0; i--)
                if (requests[i].RequesterId == requesterId) { removed.Add(requests[i]); requests.RemoveAt(i); }
            return removed;
        }

        public List<TpaRequest> RemoveTouching(string playerId)
        {
            var removed = new List<TpaRequest>();
            for (var i = requests.Count - 1; i >= 0; i--)
                if (requests[i].RequesterId == playerId || requests[i].TargetId == playerId) { removed.Add(requests[i]); requests.RemoveAt(i); }
            return removed;
        }

        public List<TpaRequest> Sweep(DateTime nowUtc)
        {
            var expired = new List<TpaRequest>();
            for (var i = requests.Count - 1; i >= 0; i--)
                if (requests[i].ExpiresUtc <= nowUtc) { expired.Add(requests[i]); requests.RemoveAt(i); }
            return expired;
        }

        public void Clear() { requests.Clear(); blocked.Clear(); }
    }
}
