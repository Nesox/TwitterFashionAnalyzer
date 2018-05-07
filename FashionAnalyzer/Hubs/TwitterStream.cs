using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FashionAnalyzer.FaceDetection;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Security.Twitter;
using Microsoft.ProjectOxford.Face.Contract;
using Tweetinvi;
using Tweetinvi.Core.Extensions;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;
using Tweetinvi.Streaming;

namespace FashionAnalyzer.Hubs
{
    public class TwitterStream
    {
        private IFilteredStream _stream;
        //private readonly IHubContext _context = GlobalHost.ConnectionManager.GetHubContext<TwitterHub>();
        private readonly HashSet<string> _processedImages = new HashSet<string>();

        #region Singleton

        private static readonly Lazy<TwitterStream> _instance = new Lazy<TwitterStream>(
        () => new TwitterStream(GlobalHost.ConnectionManager.GetHubContext<TwitterHub>()));

        public static TwitterStream Instance => _instance.Value;

        #endregion

        private readonly IHubContext _context;
        private TwitterStream(IHubContext context)
        {
            _context = context;
        }

        internal static Task OnAuthenticated(TwitterAuthenticatedContext twitterAuthenticatedContext)
        {
            // Use the access token and secret from the twitter login.
            Auth.SetUserCredentials(
                "",                                                     // Consumer Key (API Key)
                "",                                                     // Consumer Secret (API Secret)   
                twitterAuthenticatedContext.AccessToken,                // Access Token
                twitterAuthenticatedContext.AccessTokenSecret           // Access Token Secret
            );

            return Task.FromResult(0);
        }

        private readonly HashSet<string> _trackingTags = new HashSet<string>();

        public async Task UpdateFilters(string filterQuery, string connectionId)
        {
            var client = _context.Clients.Client(connectionId);
            
            string[] filters = filterQuery.Split('#');
            if (filters.Length == 0)
            {
                await client.UpdateStatus("Invalid filter!");
                return;
            }
            
            _trackingTags.Clear();
            foreach (string s in filters)
            {
                if (!string.IsNullOrWhiteSpace(s))
                    _trackingTags.Add($"#{s}");
            }
        }

        public async Task StartStream(CancellationToken token, string connectionId)
        {
            var client = _context.Clients.Client(connectionId);
            if (Auth.Credentials == null)
            {
                await client.updateStatus("Please login with Twitter first");
                return;
            }

            if (_stream == null)
            {
                _stream = Stream.CreateFilteredStream();

                if (_trackingTags.Count == 0)
                {
                    await client.updateStatus("Please add at least one tracking tag!");
                    return;
                }

                // The stream won't start unless there's at least one track record.
                // Add all the hashtags we want to track.
                foreach (string s in _trackingTags)
                    _stream.AddTrack(s);

                // Raised when any tweet that matches any condition.
                _stream.MatchingTweetReceived += async (sender, args) =>
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException)
                    {
                        await client.updateStatus("Stopped");
                        _stream.StopStream();
                    }

                    //bool showTweet = false;
                    ITweet tweet = args.Tweet;
                    if (tweet.Media != null)
                    {
                        foreach (IMediaEntity mediaEntity in tweet.Media)
                        {
                            if (mediaEntity.MediaType == "photo")
                            {
                                string mediaUrl = mediaEntity.MediaURL;
                                if (!_processedImages.Contains(mediaUrl))
                                {
                                    //showTweet = true;
                                    _processedImages.Add(mediaUrl);
                                    Face[] faces = await FaceDetectionClient.DetectFaceAndAttributes(mediaUrl);

                                    string html;
                                    if (TryGenerateHtmlForProcessedImage(faces, mediaUrl, out html))
                                    {
                                        await client.updateTweetHtml(html);
                                    }
                                }
                            }
                        }
                    }

                    //if (showTweet)
                    //{ 
                    //    var embedTweet = Tweet.GetOEmbedTweet(args.Tweet);
                    //    await client.updateTweet(embedTweet);
                    //}
                };

                // if anything changes the state, update the UI.
                _stream.StreamPaused += async (sender, args) => { await client.updateStatus("Paused."); };
                _stream.StreamResumed += async (sender, args) => { await client.updateStatus("Streaming..."); };
                _stream.StreamStarted += async (sender, args) => { await client.updateStatus("Started."); };
                _stream.StreamStopped += async (sender, args) =>
                {
                    string status = "Stopped";
                    Exception e = args.Exception;
                    if (e != null)
                    {
                        status += ": " + e.Message;
                    }

                    await client.updateStatus(status);
                };

                // Start the stream.
                await client.updateStatus("Started.");
                await _stream.StartStreamMatchingAnyConditionAsync();
            }
            else
            {
                StreamState state = _stream.StreamState;
                switch (state)
                {
                    case StreamState.Pause:
                        _stream.ResumeStream();
                        break;
                    case StreamState.Stop:
                        await _stream.StartStreamMatchingAnyConditionAsync();
                        break;
                }
            }
        }
        
        private bool TryGenerateHtmlForProcessedImage(Face[] faces, string mediaUrl, out string html)
        {
            int numFace = faces.Length;
            if (numFace == 0)
            {
                html = "";
                return false;

            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<div class=\"col-sm-4\">");
            sb.AppendLine($"<img src=\"{mediaUrl}\" class=\"img-responsive img-rounded\" style=\"max-width: 400px;\">");

            foreach (Face f in faces)
            {
                // Face info
                sb.AppendFormat("<p>Age: {0}, Gender: {1}, </p>\n", f.FaceAttributes.Age, f.FaceAttributes.Gender);
            }
            sb.AppendLine("</div>");

            html = sb.ToString();
            return true;
        }
    }
}
 