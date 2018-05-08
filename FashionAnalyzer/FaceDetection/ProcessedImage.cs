using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Razor.Generator;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Tweetinvi.Core.Extensions;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;

namespace FashionAnalyzer.FaceDetection
{
    public class ProcessedImage
    {
        private ProcessedImage()
        {
        }

        /// <summary> The tweet. </summary>
        public ITweet ParentTweet { get; private set; }

        /// <summary> The faces found in the picture. </summary>
        public Face[] Faces { get; private set; }

        /// <summary> The generated html for this image. </summary>
        public string GeneratedHtml { get; private set; }

        /// <summary> The media urls. </summary>
        public string MediaUrl { get; private set; }

        /// <summary> 
        /// If this image was processed successfully and has something we can use, if rate limiting from Azure's Face Api kicks in this will be set to true or if there are no faces in the image. 
        /// If any other exception was thrown when attempting to determine the faces in the picture.
        /// The string of the tuple will be set to the error that occured. </summary>
        public Tuple<bool, string> Success { get; private set; }

        private static Dictionary<string, ProcessedImage> _processedImages = new Dictionary<string, ProcessedImage>();

        /// <summary> Processes a tweet for images.</summary>
        /// <param name="tweet"> The tweet.</param>
        /// <returns>ProcessedImage object.</returns>
        public static async Task<List<ProcessedImage>> ProcessTweet(ITweet tweet)
        {
            List<ProcessedImage> images = new List<ProcessedImage>();
            if (tweet.Media != null)
            {
                foreach (IMediaEntity mediaEntity in tweet.Media)
                {
                    if (mediaEntity.MediaType == "photo")
                    {
                        string mediaUrl = mediaEntity.MediaURL;
                        if (!_processedImages.ContainsKey(mediaUrl))
                        {
                            ProcessedImage pi = new ProcessedImage
                            {
                                ParentTweet = tweet,
                                MediaUrl = mediaUrl,
                                Success = new Tuple<bool, string>(false, "")
                            };

                            try
                            {
                                var faces = await FaceDetectionClient.DetectFaceAndAttributes(mediaUrl);
                                if (faces.Length == 0)
                                    continue;

                                pi.Faces = faces;
                                pi.GeneratedHtml = GenerateHtml(pi);
                            }
                            catch (FaceAPIException e)
                            {
                                pi.Success = new Tuple<bool, string>(false, e.ErrorMessage);
                            }

                            if (_processedImages.TryAdd(mediaUrl, pi))
                            {
                                pi.Success = new Tuple<bool, string>(true, "");
                            }

                            images.Add(pi);
                        }
                        else
                        {
                            images.Add(_processedImages[mediaUrl]);
                        }
                    }
                }
            }

            return images;
        }

        private static string GenerateHtml(ProcessedImage image)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<div class=\"col-sm-4 tweet-item\">");
            sb.AppendLine(
                $"<img src=\"{image.MediaUrl}\" class=\"img-responsive img-rounded\" style=\"max-width: 400px;\">");

            foreach (Face f in image.Faces)
            {
                // Face info
                sb.AppendFormat("<p>Age: {0}, Gender: {1}, </p>\n", f.FaceAttributes.Age, f.FaceAttributes.Gender);
            }
            sb.AppendLine("</div>");

            return sb.ToString();
        }
    }
}