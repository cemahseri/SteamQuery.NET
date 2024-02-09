#if !NETCOREAPP2_0_OR_GREATER
namespace SteamQuery.Extensions;

internal static class TaskExtensions
{
    public static async Task<TResult> TimeoutAfterAsync<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (task == await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)))
        {
            return await task;
        }

        throw new TimeoutException();
    }
}
#endif