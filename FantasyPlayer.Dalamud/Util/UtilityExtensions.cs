namespace FantasyPlayer.Dalamud.Util
{
    using System.Collections.Generic;
    using System.Linq;

    internal static class UtilityExtensions
    {
        internal static string FlattenStringArray(this IEnumerable<string> lines)
        {
            return lines.Aggregate(string.Empty, (total, next) => string.IsNullOrEmpty(total) ? next : $"{total}\n{next}");
        }
    }
}