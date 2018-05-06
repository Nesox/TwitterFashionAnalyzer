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
        private readonly IHubContext _context = GlobalHost.ConnectionManager.GetHubContext<TwitterHub>();
        private readonly HashSet<string> _processedImages = new HashSet<string>();

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
                //_stream.AddTrack("#face");
                //_stream.AddTrack("#ansikte");
                //_stream.AddTrack("#workout");
                 _stream.AddTrack("#facetestdebug");

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

                    bool showTweet = false;
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
                                    showTweet = true;
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
                        status += ": " + e.Message;

                    await client.updateStatus(status);
                };

                // Start the stream.
                await client.updateStatus("Started.");
                await _stream.StartStreamMatchingAnyConditionAsync();
            }

            // This condition will never be taken.
            else
            {
                _stream.ResumeStream();
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