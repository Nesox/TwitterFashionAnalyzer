using System;
using System.Collections.Concurrent;
using System.Globalization;
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
            var task = TwitterStream.StartStream(tokenSource.Token);
            await task;
        }

        public async Task StopTwitterLive(string taskId)
        {
            if (CurrentTasks.ContainsKey(taskId))
            {
                CurrentTasks[taskId].CancellationToken.Cancel();
            }
            await Clients.Caller.updateStatus("Stopped.");
        }


        //public TwitterHub()
        //{
        //    //Create a task to update the clients every 3 seconds.
        //   var taskTimer = Task.Factory.StartNew(async () =>
        //   {
        //       while (true)
        //       {
        //           string timeNow = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        //            // Send server time to all the connected clients on the client method: SendServerTime()
        //            Clients.All.SendServerTime(timeNow);

        //           await Task.Delay(500);
        //       }
        //   }, TaskCreationOptions.LongRunning);
        //}

        //public void Hello()
        //{
        //    Clients.All.hello();
        //}
    }
}