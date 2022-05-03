internal static class Extensions
{
    public static async Task<int> ReadAsync(this StreamReader reader, char[] buffer, int index, int count,
                                            int millisecondsTimeout)
    {
        if (reader == null) {
            throw new ArgumentNullException(nameof(reader));
        }
        if (millisecondsTimeout <= -1) {
            throw new ArgumentOutOfRangeException($"{nameof(millisecondsTimeout)} must be greater than -1");
        }

        Task<int> readTask = reader.ReadAsync(buffer, index, count);
        await Task.WhenAny(readTask, Task.Delay(millisecondsTimeout));
        if (!readTask.IsCompleted) {
            throw new TimeoutException("Asynchronous read operation timed out.");
        }

        return await readTask;
    }
}