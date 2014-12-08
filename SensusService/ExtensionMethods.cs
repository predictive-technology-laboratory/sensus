using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SensusService
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Extension method for the WebClient class, which returns a Task.
        /// </summary>
        /// <param name="client">Client to use.</param>
        /// <param name="uri">URI to open.</param>
        /// <returns>Task returning a stream.</returns>
        public static Task<Stream> OpenReadTaskAsync(this WebClient client, Uri uri)
        {
            TaskCompletionSource<Stream> taskComplete = new TaskCompletionSource<Stream>();

            client.OpenReadCompleted += (o, e) =>
            {
                try { taskComplete.SetResult(e.Result); }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to open URI \"" + uri + "\":  " + ex.Message, LoggingLevel.Normal);
                    taskComplete.SetException(ex);
                }
            };

            client.OpenReadAsync(uri);

            return taskComplete.Task;
        }
    }
}
