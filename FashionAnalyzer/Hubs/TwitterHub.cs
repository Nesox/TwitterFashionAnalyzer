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
        private static ConcurrentDictionary<string, TwitterTaskData> CurrentTasks => _currentTasks ?? (_currentTasks = new ConcurrentDictionary<string, TwitterTaskData>());

        /// <summary> Starts the twitter live stream. </summary>
        /// <returns></returns>
        public async Task StartTwitterLive(string trackingFilter)
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
            string connectionId = Context.ConnectionId;

            var task = TwitterStream.Instance.StartStream(tokenSource.Token, connectionId, trackingFilter);
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