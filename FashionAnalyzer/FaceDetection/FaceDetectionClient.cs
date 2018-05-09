using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace FashionAnalyzer.FaceDetection
{
    public static class FaceDetectionClient
    {
        private static readonly IFaceServiceClient _faceServiceClient;

        static FaceDetectionClient()
        {
            _faceServiceClient = new FaceServiceClient(APIKeys.AzureFaceApiKey,
                "https://westeurope.api.cognitive.microsoft.com/face/v1.0");
        }

        /// <summary> Queries Azure's Face API to determine how many faces there are in the picture. </summary>
        /// <param name="imageUrl"> The url for the image. </param>
        /// <returns></returns>
        public static async Task<Face[]> DetectFaceAndAttributes(string imageUrl)
        {
            IEnumerable<FaceAttributeType> faceAttributes = new[]
            {
                FaceAttributeType.Gender,
                FaceAttributeType.Age,
                FaceAttributeType.Smile,
                FaceAttributeType.Emotion,
                FaceAttributeType.Glasses,
                FaceAttributeType.Hair,
            };

            Face[] faces = await _faceServiceClient.DetectAsync(imageUrl, returnFaceAttributes: faceAttributes);
            return faces;
        }
    }

    public static class FaceExtensions
    {
        /// <summary> Determines if this face is a male. </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        public static bool IsMale(this Face face)
        {
            return face != null && face.FaceAttributes.Gender == "male";
        }

        /// <summary> Determines if this face is female. </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        public static bool IsFemale(this Face face)
        {
            return face != null && face.FaceAttributes.Gender == "female";
        }

        /// <summary> Gets the gender. </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        public static string GetGenderString(this Face face)
        {
            return face.FaceAttributes.Gender;
        }
    }
}