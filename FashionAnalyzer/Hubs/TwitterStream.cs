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
    public static class TwitterStream
    {
        private static ISampleStream _stream;
        private static readonly IHubContext _context = GlobalHost.ConnectionManager.GetHubContext<TwitterHub>();

        public static async Task StartStream(CancellationToken token)
        {
            Auth.SetUserCredentials(
                "ao1UgCEX8ZVXauXUy7TQTll5X",                            // Consumer Key (API Key)
                "XxbPYV8N9nDrEaHyiNaxhOd3G7UHIJ1wEErlzoO0h7dVZ3V3ky",   // Consumer Secret (API Secret)
                "2260832391-euGoNd5qC9VC7T0NIQPLFnaRNC6tETcQ386abXx",   // Access Token
                "6DFkaFAkYmVY2MM7su7FsOt2CaKaq3bQXODtw9zVscNce"         // Access Token Secret
                );

            if (_stream == null)
            {
                //_stream = Stream.CreateUserStream();
                _stream = Stream.CreateSampleStream();

                // Other events can be used. This is just YOUR twitter feed.
                _stream.TweetReceived += async (sender, args) =>
                //_stream.TweetCreatedByAnyone += async (sender, args) =>
                {
                    if (token.IsCancellationRequested)
                    {
                        _stream.StopStream();
                        //token.ThrowIfCancellationRequested();
                    }

                    // use the embeded tweets from tweetinvi.
                    var embedTweet = Tweet.GetOEmbedTweet(args.Tweet);

                    await _context.Clients.All.updateTweet(embedTweet);
                };

                // if anything changes the state, update the UI.
                _stream.StreamPaused += async (sender, args) => { await _context.Clients.All.updateStatus("Paused."); };
                _stream.StreamResumed += async (sender, args) => { await _context.Clients.All.updateStatus("Streaming..."); };
                _stream.StreamStarted += async (sender, args) => { await _context.Clients.All.updateStatus("Started."); };
                _stream.StreamStopped += async (sender, args) => { await _context.Clients.All.updateStatus("Stopped (event)"); };

                // Start the stream.
                await _stream.StartStreamAsync();
            }
            else
            {
                _stream.ResumeStream();
            }

            await _context.Clients.All.updateStatus("Started.");
        }



    }
}