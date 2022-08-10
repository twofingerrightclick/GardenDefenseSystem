using System;
using Mobile.BuildTools.Configuration;

namespace GardenDefenseSystem.Models
{
    public class CustomVisionApiConfig
    {
        public string VisionApiBaseAddress { get; }
        public string PredictionKey { get; }
        public string VisionProjectName { get; }
        public string VisionModelName { get; }
        public string VisionPredictionEndpoint { get; }

        public CustomVisionApiConfig(IConfigurationManager configurationManager)
        {
            VisionApiBaseAddress = configurationManager.AppSettings["VisionApiBaseAddress"];
            PredictionKey = configurationManager.AppSettings["PredictionKey"];
            VisionProjectName = configurationManager.AppSettings["VisionProjectName"];
            VisionModelName = configurationManager.AppSettings["VisionModelName"];

            VisionPredictionEndpoint =
                $"/customvision/v3.0/Prediction/{VisionProjectName}/detect/iterations/{VisionModelName}/image";
        }
    }
}
