using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FashionAnalyzer.Hubs
{
    [HubName("twitterHub")]
    public class TwitterHub : Hub
    {
        private static ConcurrentDictionary<string, TwitterTaskData> _currentTasks;
        private ConcurrentDictionary<string, TwitterTaskData> CurrentTasks => _currentTasks ?? (_currentTasks = new ConcurrentDictionary<string, TwitterTaskData>());
        private readonly TwitterStream _twitterStream = new TwitterStream();

        /// <summary> Starts the twitter live stream. </summary>
        /// <returns></returns>
        public async Task StartTwitterLive()
        {
            var tokenSource = new CancellationTokenSource();
            var taskId = $"T-{Guid.NewGuid()}";
            CurrentTasks.TryAdd(taskId, new TwitterTaskData
            {
                CancellationToken = tokenSource,
                Id = taskId,
                Status = "Started."
            });
            await Clients.Caller.setTaskId(taskId);
            var task = _twitterStream.StartStream(tokenSource.Token);
            await task;
        }

        /// <summary> Stops the twitter stream with a specific task id.</summary>
        /// <param name="taskId"> The task id. </param>
        /// <returns></returns>
        public async Task StopTwitterLive(string taskId)
        {
            if (!string.IsNullOrWhiteSpace(taskId))
            {
                if (CurrentTasks.ContainsKey(taskId))
                    CurrentTasks[taskId].CancellationToken.Cancel();

                await Clients.Caller.updateStatus("Stopped.");
            }
        }
    }
}