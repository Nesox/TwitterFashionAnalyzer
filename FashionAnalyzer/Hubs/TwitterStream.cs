using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Tweetinvi;
using Tweetinvi.Streaming;

namespace FashionAnalyzer.Hubs
{
    public class TwitterStream
    {
        private ISampleStream _stream;
        private readonly IHubContext _context = GlobalHost.ConnectionManager.GetHubContext<TwitterHub>();

        public async Task StartStream(CancellationToken token)
        {
            // *poof*

            if (_stream == null)
            {
                //_stream = Stream.CreateUserStream();
                _stream = Stream.CreateSampleStream();

                // Other events can be used. This is just YOUR twitter feed.
                _stream.TweetReceived += async (sender, args) =>
                //_stream.TweetCreatedByAnyone += async (sender, args) =>
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException e)
                    {
                        await _context.Clients.All.updateStatus("Stopped");
                        _stream.StopStream();
                    }

                    var embedTweet = Tweet.GetOEmbedTweet(args.Tweet);
                    await _context.Clients.All.updateTweet(embedTweet);
                };

                // if anything changes the state, update the UI.
                _stream.StreamPaused += async (sender, args) => { await _context.Clients.All.updateStatus("Paused."); };
                _stream.StreamResumed += async (sender, args) => { await _context.Clients.All.updateStatus("Streaming..."); };
                _stream.StreamStarted += async (sender, args) => { await _context.Clients.All.updateStatus("Started."); };
                _stream.StreamStopped += async (sender, args) => { await _context.Clients.All.updateStatus("Stopped (event)"); };

                // Start the stream.
                await _context.Clients.All.updateStatus("Started.");
                await _stream.StartStreamAsync();
            }

            // This condition will never be taken.
            else
            {
                _stream.ResumeStream();
            }
        }
    }
}