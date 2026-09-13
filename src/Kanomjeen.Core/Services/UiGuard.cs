using System;
using Logger = Rocket.Core.Logging.Logger;

namespace Kanomjeen.Core.Services
{
    /// <summary>
    /// Shared UI helpers used by every feature UI class.
    ///
    /// Two rules are deliberate here, both taken from a working reference plugin this project was
    /// benchmarked against: client presentation failures must never break gameplay, and button
    /// element names are matched by prefix + index so a row index is always parsed, never trusted.
    /// </summary>
    public static class UiGuard
    {
        /// <summary>Run a client UI operation, logging instead of throwing when it fails.</summary>
        public static void Run(string context, System.Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Logger.LogError("[Kanomjeen] UI " + context + " failed: " + ex.Message);
            }
        }

        /// <summary>Match a button element name such as <c>KJ_Waypoint_Track_3</c> against a prefix.</summary>
        public static bool TryParseIndex(string buttonName, string prefix, out int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(buttonName) || string.IsNullOrEmpty(prefix)) return false;
            if (!buttonName.StartsWith(prefix, StringComparison.Ordinal)) return false;
            return int.TryParse(buttonName.Substring(prefix.Length), out index);
        }
    }
}
