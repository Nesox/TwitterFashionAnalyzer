using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <summary> Main logic for the TwitterStream that fetches all the data. </summary>
    public class TwitterStream
    {
        private IFilteredStream _stream;
        //private readonly IHubContext _context = GlobalHost.ConnectionManager.GetHubContext<TwitterHub>();

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
                APIKeys.TwitterConsumerKey,                             // Consumer Key (API Key)
                APIKeys.TwitterConsumerKeySecret,                       // Consumer Secret (API Secret)   
                twitterAuthenticatedContext.AccessToken,                // Access Token
                twitterAuthenticatedContext.AccessTokenSecret           // Access Token Secret
            );

            return Task.FromResult(0);
        }

        private readonly HashSet<string> _trackingTags = new HashSet<string>();
        /// <summary> Updates the hashtags to track. </summary>
        /// <param name="filterQuery">list of hashtags to process.</param>
        /// <param name="connectionId">The connection if from the TwitterHub.</param>
        /// <returns></returns>
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

        private int NumTweets { get; set; }
        private int NumFacesSeen { get; set; }
        private int NumFemaleFacesSeen { get; set; }
        private int NumMaleFacesSeen { get; set; }

        /// <summary>Stars the twitter stream, also handles stopping it.</summary>
        /// <param name="token">The cancelation token used for stopping the stream.</param>
        /// <param name="connectionId">The TwitterHub connection id.</param>
        /// <returns>Lé Task.</returns>
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

                _stream.ClearTracks();
                // The stream won't start unless there's at least one track record.
                // Add all the hashtags we want to track.
                foreach (string s in _trackingTags)
                    _stream.AddTrack(s);

                if (_stream.TracksCount == 0)
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
                    NumTweets++;
                    TweetsPerHour = CalculatePerHourAndUpdate(NumTweets, _tweetRecords);

                    try
                    {
                        token.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException)
                    {
                        await client.updateStatus("Stopped");
                        _stream.StopStream();
                    }

                    ITweet tweet = args.Tweet;
                    var images = await ProcessedImage.ProcessTweet(tweet);
                    foreach (ProcessedImage img in images)
                    {
                        if (img.Success.Item1)
                        {
                            NumFemaleFacesSeen += img.Faces.Count(f => f.IsFemale());
                            NumMaleFacesSeen += img.Faces.Count(f => f.IsMale());
                            NumFacesSeen += img.Faces.Length;

                            FacesPerHour = CalculatePerHourAndUpdate(NumFacesSeen, _faceRecords);
                            FemaleFacesPerHour = CalculatePerHourAndUpdate(NumFemaleFacesSeen, _femaleFacesRecords);
                            MaleFacesPerHour = CalculatePerHourAndUpdate(NumMaleFacesSeen, _maleFacesRecords);

                            string statsString =
                                string.Format("Tweets/h: {0:F1} Faces/h: {1:F1} Male/h: {2:F1} Female/h: {3:F1}",
                                    TweetsPerHour,
                                    FacesPerHour,
                                    MaleFacesPerHour,
                                    FemaleFacesPerHour
                                );

                            await client.updateStreamStats(statsString);
                            await client.updateTweetHtml(img.GeneratedHtml);
                        }
                    }
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

        #region Stats

        /// <summary> Number of tweets per hour. </summary>
        public double TweetsPerHour { get; set; }

        /// <summary> Number of male faces per hour. </summary>
        public double MaleFacesPerHour { get; set; }

        /// <summary> Number of female faces per hour. </summary>
        public double FemaleFacesPerHour { get; set; }

        /// <summary> Number of faces per hour.</summary>
        public double FacesPerHour { get; set; }

        private static int MinutesToMs(int minutes)
        {
            return minutes * 60 * 1000;
        }

        private readonly TimedRecordKeeper<long> _tweetRecords = new TimedRecordKeeper<long>(5 * 1000, MinutesToMs(60)); // Store records every 5 seconds for 60 minutes.
        private readonly TimedRecordKeeper<long> _maleFacesRecords = new TimedRecordKeeper<long>(5 * 1000, MinutesToMs(60)); // Store records every 5 seconds for 60 minutes.
        private readonly TimedRecordKeeper<long> _femaleFacesRecords = new TimedRecordKeeper<long>(5 * 1000, MinutesToMs(60)); // Store records every 5 seconds for 60 minutes.
        private readonly TimedRecordKeeper<long> _faceRecords = new TimedRecordKeeper<long>(5 * 1000, MinutesToMs(60)); // Store records every 5 seconds for 60 minutes.

        private double CalculatePerHourAndUpdate(long current, TimedRecordKeeper<long> recordKeeper)
        {
            recordKeeper.AddRecord(current);

            long valueAtTimeAgo;
            TimeSpan elapsedSinceTime;
            if (!recordKeeper.GetRecord(TimeSpan.FromMilliseconds(recordKeeper.StoreTimeMilliseconds), out valueAtTimeAgo, out elapsedSinceTime))
                return 0;

            double perHour = elapsedSinceTime.TotalHours > 0 ? (current - valueAtTimeAgo) / elapsedSinceTime.TotalHours : 0d;
            if (perHour < 0)
            {
                recordKeeper.Reset();
                return 0;
            }

            return perHour;
        }

        #region Nested type: TimedRecordKeeper

        /// <summary> A class that keeps record of an entry and a time at an interval for the last X milliseconds. </summary>
        public class TimedRecordKeeper<T>
        {
            // Note that we are using ints instead of TimeSpan's here to be able to divide properly.
            public TimedRecordKeeper(int storeIntervalMilliseconds, int storeTimeMilliseconds)
            {
                StoreIntervalMilliseconds = storeIntervalMilliseconds;
                StoreTimeMilliseconds = storeTimeMilliseconds;

                if (NumRecords < 0)
                    throw new ArgumentException("Invalid arguments passed!");

                _records = new Record[NumRecords];
                for (int i = 0; i < _records.Length; i++)
                    _records[i] = new Record { Timer = new Stopwatch() };

                _timer.Start();
            }

            /// <summary> Gets the interval, in milliseconds, that the record keeper stores records. </summary>
            public int StoreIntervalMilliseconds { get; private set; }

            /// <summary> Gets the amount of time the record keeper will keep records for. </summary>
            public int StoreTimeMilliseconds { get; private set; }

            /// <summary> Gets the amount of records this keeper keeps, based on the store interval and store time. </summary>
            public int NumRecords => StoreTimeMilliseconds / StoreIntervalMilliseconds;

            private readonly Stopwatch _timer = new Stopwatch();

            private class Record
            {
                public Stopwatch Timer { get; set; }
                public T Value { get; set; }
            }

            private readonly Record[] _records;

            /// <summary> Resets this record keeper to contain zero active records. </summary>
            public void Reset()
            {
                foreach (Record record in _records)
                {
                    record.Timer.Reset();
                }
            }

            /// <summary> Adds a record into the time keeper. </summary>
            /// <param name="value"></param>
            public void AddRecord(T value)
            {
                long index = _timer.ElapsedMilliseconds / StoreIntervalMilliseconds;
                index %= NumRecords;
                _records[index].Timer.Restart();
                _records[index].Value = value;
            }

            /// <summary> Find the record that has been stored for an amount of time closest to the passed time. </summary>
            /// <param name="timeAgo"></param>
            /// <param name="value"></param>
            /// <param name="elapsed"></param>
            /// <returns>A bool indicating whether any record was found.</returns>
            public bool GetRecord(TimeSpan timeAgo, out T value, out TimeSpan elapsed)
            {
                Record bestRecord = null;
                TimeSpan bestElapsed = TimeSpan.Zero;
                TimeSpan bestDifference = TimeSpan.MaxValue;
                foreach (Record record in _records)
                {
                    if (!record.Timer.IsRunning || record.Timer.ElapsedMilliseconds > StoreTimeMilliseconds)
                        continue;

                    TimeSpan recordElapsed = record.Timer.Elapsed;
                    TimeSpan difference = recordElapsed - timeAgo;
                    if (difference < TimeSpan.Zero)
                        difference = -difference;

                    if (difference < bestDifference)
                    {
                        bestDifference = difference;
                        bestRecord = record;
                        bestElapsed = recordElapsed;
                    }
                }

                if (bestRecord == null)
                {
                    value = default(T);
                    elapsed = TimeSpan.Zero;
                    return false;
                }

                value = bestRecord.Value;
                elapsed = bestElapsed;
                return true;
            }
        }

        #endregion

        #endregion
    }
}
 