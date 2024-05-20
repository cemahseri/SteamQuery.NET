namespace SteamQuery.Extensions;

internal static class TaskExtensions
{
    public static async Task<TResult> TimeoutAfterAsync<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (timeout.TotalMilliseconds == 0.0d)
        {
            timeout = Timeout.InfiniteTimeSpan;
        }

#if NETCOREAPP2_0_OR_GREATER
        return await task.WaitAsync(timeout, cancellationToken);
#else
        if (task == await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)))
        {
            return await task;
        }

        throw new TimeoutException();
#endif
    }
}