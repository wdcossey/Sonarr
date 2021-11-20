using System.Threading.Tasks;

namespace NzbDrone.Core.Extensions
{
    public static class TaskExtensions
    {
        public static void Forget(this Task task)
        {
            if (!task.IsCompleted || task.IsFaulted)
                _ = ForgetAwaited(task);

            static async Task ForgetAwaited(Task task)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
