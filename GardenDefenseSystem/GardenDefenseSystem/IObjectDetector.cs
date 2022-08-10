using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GardenDefenseSystem
{
    public interface IObjectDetector
    {
        public ImagePrediction Detect(byte[] image);
    }
}
