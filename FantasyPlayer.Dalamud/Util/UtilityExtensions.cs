namespace FantasyPlayer.Dalamud.Util
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal static class UtilityExtensions
    {
        internal static string FlattenStringArray(this IEnumerable<string> lines)
        {
            return lines.Aggregate(string.Empty, (total, next) => string.IsNullOrEmpty(total) ? next : $"{total}\n{next}");
        }

        internal static void Forget(this Task t)
        {
            if (!t.IsCompleted || t.IsFaulted)
            {
                _ = ForgetAwaited(t);
            }

            async static Task ForgetAwaited(Task task)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch
                {
                    // Nothing to do here
                }
            }
        }
    }
}