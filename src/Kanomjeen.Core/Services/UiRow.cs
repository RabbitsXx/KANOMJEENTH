namespace Kanomjeen.Core.Services
{
    /// <summary>
    /// One display-ready row of a reusable client UI list. Feature plugins build these from their
    /// authoritative state and hand them to the matching UI class, which keeps presentation classes
    /// free of gameplay/storage knowledge.
    /// </summary>
    public readonly struct UiRow
    {
        public UiRow(string name, string detail = null)
        {
            Name = name ?? string.Empty;
            Detail = detail;
        }

        /// <summary>Primary column (row title).</summary>
        public string Name { get; }

        /// <summary>Secondary column such as a cooldown or distance; may be null.</summary>
        public string Detail { get; }
    }
}
