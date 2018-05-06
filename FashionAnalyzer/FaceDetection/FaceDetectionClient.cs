using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace FashionAnalyzer.FaceDetection
{
    public static class FaceDetectionClient
    {
        private static readonly IFaceServiceClient _faceServiceClient;
        
        //private Face[] _detectedFaces;                   // The list of detected faces.
        //private String[] _faceDescriptions;      // The list of descriptions for the detected faces.
        //private double _resizeFactor;            // The resize factor for the displayed image.

        static FaceDetectionClient()
        {
            _faceServiceClient = new FaceServiceClient("", "https://westeurope.api.cognitive.microsoft.com/face/v1.0");
        }

        public static async Task<Face[]> DetectFaceAndAttributes(string imageUrl)
        {
            IEnumerable<FaceAttributeType> faceAttributes = new []
            {
                FaceAttributeType.Gender,
                FaceAttributeType.Age,
                FaceAttributeType.Smile,
                FaceAttributeType.Emotion,
                FaceAttributeType.Glasses,
                FaceAttributeType.Hair,
            };

            try
            {
                Face[] faces = await _faceServiceClient.DetectAsync(imageUrl, returnFaceAttributes: faceAttributes);
                return faces;
            }
            catch (FaceAPIException e)
            {
                return null;
            }
        }
    }
}