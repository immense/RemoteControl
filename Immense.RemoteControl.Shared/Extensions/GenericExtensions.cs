namespace Immense.RemoteControl.Shared.Extensions;
public static class GenericExtensions
{

    /// <summary>
    /// Returns an item wrapped in a completed <see cref="Task{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    /// <returns></returns>
    public static Task<T> AsTaskResult<T>(this T item)
    {
        return Task.FromResult(item);
    }
}
