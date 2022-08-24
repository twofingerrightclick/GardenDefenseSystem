using Android.Graphics;
using GardenDefenseSystem.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Newtonsoft.Json;
using Plugin.SimpleAudioPlayer;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Mobile.BuildTools.Configuration;

namespace GardenDefenseSystem.ViewModels
{
    public class DetectionViewModel : BaseViewModel
    {
        bool UseCustomVisionApi { get; }

        CustomVisionApiConfig? CustomVisionApiConfig { get; }

        private ISimpleAudioPlayer Player { get; }

        VisionApiCallCount VisionApiCallCount { get; }

        string _PredictionResult = string.Empty;
        public string PredictionStatusMessage
        {
            get { return _PredictionResult; }
            set { SetProperty(ref _PredictionResult, value); }
        }

        string _RemainingHoursOfApiusage = string.Empty;
        public string RemainingHoursOfApiusage
        {
            get { return _RemainingHoursOfApiusage; }
            set { SetProperty(ref _RemainingHoursOfApiusage, value); }
        }

        double _ThresholdValue = 55;
        public double ThresholdValue
        {
            get { return _ThresholdValue; }
            set { SetProperty(ref _ThresholdValue, value); }
        }

        System.Drawing.Color _AlertButtonColor = System.Drawing.Color.Black;
        public System.Drawing.Color AlertButtonColor
        {
            get { return _AlertButtonColor; }
            set { SetProperty(ref _AlertButtonColor, value); }
        }

        private bool _TestModeSwitchToggleStatus;
        public bool TestModeSwitchToggleStatus
        {
            get { return _TestModeSwitchToggleStatus; }
            set { SetProperty(ref _TestModeSwitchToggleStatus, value); }
        }

        public DetectionViewModel()
        {
            Title = "Garden Defense System";

            TakePhotoCommand = new Command(async () => await ObserveAndDetect());
            VisionApiCallCount = VisionApiCallCount.Instance;
            StopPlayerCommand = new Command(() => StopPlayer());
            ObjectDetector = DependencyService.Get<IObjectDetector>();
            RemainingHoursOfApiusage =
                $"Remaining hrs for this month: {VisionApiCallCount.GetRemainingApiRuntimeHrs()}";
            var configManager = ConfigurationManager.Current;

            UseCustomVisionApi =
                configManager.AppSettings["UseCustomVisionApi"]?.Equals("true") ?? false
                    ? true
                    : false;

            if (UseCustomVisionApi)
            {
                CustomVisionApiConfig = new CustomVisionApiConfig(configManager);

                PredictionApiClient = AuthenticatePredictionClient(
                    CustomVisionApiConfig.PredictionKey,
                    CustomVisionApiConfig.VisionApiBaseAddress
                );
            }

            Player = LoadPlayer();
        }

        private void StopPlayer()
        {
            AlertButtonColor = System.Drawing.Color.Black;
            if (Player.IsPlaying)
            {
                Player.Stop();
            }
        }

        private void UpdateApiCallCount()
        {
            VisionApiCallCount.CallCount++;
            RemainingHoursOfApiusage =
                $"Remaining hrs for this month: {VisionApiCallCount.GetRemainingApiRuntimeHrs()}";
        }

        private ISimpleAudioPlayer LoadPlayer()
        {
            ISimpleAudioPlayer player = CrossSimpleAudioPlayer.Current;
            player.Load(GetStreamFromFile("chicken_cluck.mp3"));
            player.Volume = 90;
            player.Loop = true;
            return player;
        }

        public Image LatestCapture { get; set; } = new Image();

        public ICommand TakePhotoCommand { get; }
        public CustomVisionPredictionClient PredictionApiClient { get; }
        public ICommand StopPlayerCommand { get; }
        public IObjectDetector ObjectDetector { get; private set; }

        public async Task ObserveAndDetect()
        {
            if (NotDaytime(DateTime.Now.ToLocalTime()) && !TestModeSwitchToggleStatus)
            {
                return;
            }
            Device.BeginInvokeOnMainThread(async () => await Observe());
        }

        public async Task Observe()
        {
            PredictionStatusMessage = " ";
            var photoFile = await TakePhoto();

            if (photoFile is null)
            {
                return;
            }

            using (var photoStream = await ResizeImageAndroidAsync(photoFile))
            {
                try
                {
                    ImagePrediction? result = null;

                    switch (UseCustomVisionApi)
                    {
                        case true:
                            PredictionStatusMessage = "Uploading photo...";
                            CancellationTokenSource uploadTaskTokenSource =
                                new CancellationTokenSource();
                            Task uploadTask = Task.Run(
                                async () =>
                                    result = await UploadPhotoToVisionApi_LollipopSafe(photoStream),
                                uploadTaskTokenSource.Token
                            );
                            // sometimes internet is too slow so check which task completed
                            if (await Task.WhenAny(uploadTask, Task.Delay(8000)) == uploadTask)
                            {
                                await uploadTask;
                                UpdateApiCallCount();
                            }
                            // timed out
                            else
                            {
                                uploadTaskTokenSource.Cancel();
                                PredictionStatusMessage = "photo upload timed out";
                            }
                            break;
                        case false:
                            PredictionStatusMessage = "Processing photo locally...";
                            result = ObjectDetector.Detect(photoStream.ToArray());
                            break;
                    }

                    var maxChickenPrediction = GetMaxPrediction(result!);

                    if (maxChickenPrediction != null)
                    {
                        PredictionStatusMessage =
                            $"Probabilty for {maxChickenPrediction.TagName}: {maxChickenPrediction.Probability}";

                        if (maxChickenPrediction.Probability > ThresholdValue / 100)
                        {
                            Alert(); //dont await
                        }
                        else
                        {
                            PredictionStatusMessage =
                                $"Max chicken object prob was: {maxChickenPrediction.Probability} ";
                        }
                    }
                    else
                    {
                        PredictionStatusMessage = $"No Chicken objects spotted.";
                    }
                }
                catch (Exception e)
                {
                    PredictionStatusMessage = e.Message;
                }
            }

            File.Delete(photoFile.FullPath);
        }

        public static PredictionModel GetMaxPrediction(ImagePrediction imagePrediction)
        {
            var maxChickenPrediction = imagePrediction.Predictions.Aggregate(
                (i, j) => i.Probability > j.Probability ? i : j
            );

            return maxChickenPrediction;
        }

        public static bool NotDaytime(DateTime now)
        {
            int sunrise = Sunrises[now.Month - 1];
            int sunset = Sunsets[now.Month - 1];

            int hoursSinceMidnight = now.TimeOfDay.Hours;
            var daytime = hoursSinceMidnight >= sunrise && hoursSinceMidnight < sunset;
            return !daytime;
        }

        private async Task Alert()
        {
            AlertButtonColor = System.Drawing.Color.LightGreen;
            Player.Play();
            await Task.Delay(TimeSpan.FromSeconds(30));
            Player.Stop();
            AlertButtonColor = System.Drawing.Color.Black;
        }

        Stream GetStreamFromFile(string filename)
        {
            var assembly = typeof(App).GetTypeInfo().Assembly;
            var stream = assembly.GetManifestResourceStream("GardenDefenseSystem." + filename);
            return stream;
        }

        public static async Task<MemoryStream> ResizeImageAndroidAsync(
            FileResult photoFile,
            float width = 1280,
            float height = 960
        )
        {
            using var stream = await photoFile.OpenReadAsync();

            // Load the bitmap

            Bitmap? resizedImage;

            int compressPercent = 60;

            if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.LollipopMr1)
            {
                Bitmap? originalImage = await BitmapFactory.DecodeStreamAsync(stream);
                if (originalImage is null)
                {
                    throw new Exception("original image is null");
                }
                //resize
                resizedImage = Bitmap.CreateScaledBitmap(
                    originalImage,
                    (int)width,
                    (int)height,
                    false
                );

                if (resizedImage is null)
                {
                    throw new Exception("resized image is null");
                }

                var matrix = new Matrix();
                matrix.PostRotate(90);
                //rotate (required for taking portrait shots)
                // resizedImage = Bitmap.CreateBitmap(resizedImage, 0, 0, resizedImage.Width, resizedImage.Height, matrix, true);

            }
            else
            {
                resizedImage = await DecodeStreamLowMemory(stream);
            }

            stream.Close();

            MemoryStream ms = new MemoryStream();

            resizedImage.Compress(Bitmap.CompressFormat.Jpeg, compressPercent, ms);

            ms.Position = 0;
            return ms;
        }

        static int IMAGE_MAX_SIZE = 2000;

        private static async Task<Bitmap?> DecodeStreamLowMemory(Stream stream)
        {
            Bitmap? b;

            //Decode image size
            BitmapFactory.Options o = new BitmapFactory.Options();
            o.InJustDecodeBounds = true;

            await BitmapFactory.DecodeStreamAsync(stream, null, o);

            int scale = 1;
            if (o.OutHeight > IMAGE_MAX_SIZE || o.OutWidth > IMAGE_MAX_SIZE)
            {
                scale = GetScale(o.OutHeight, o.OutHeight);
            }

            //Decode with inSampleSize
            BitmapFactory.Options o2 = new BitmapFactory.Options();
            //o2.InScaled = false;
            o2.InSampleSize = scale;
            stream.Position = 0;
            b = await BitmapFactory.DecodeStreamAsync(stream, null, o2);

            return b;
        }

        public static int GetScale(int outHeight, int outWidth)
        {
            // works well for (3264,2448);
            return 3;
            /*  return (int)Math.Pow(
                    2,
                    (int)Math.Ceiling(
                        Math.Log(IMAGE_MAX_SIZE / (double)Math.Max(outHeight,outWidth))
                            / Math.Log(0.5)
                    )
                );*/
        }

        private async Task<ImagePrediction> UploadPhotoToVisionApi_LollipopSafe(Stream photoStream)
        {
            using HttpClient client = new HttpClient()
            {
                BaseAddress = new Uri(CustomVisionApiConfig?.VisionApiBaseAddress)
            };

            StreamContent content = new(photoStream);
            content.Headers.Add("Prediction-Key", CustomVisionApiConfig?.PredictionKey);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            HttpResponseMessage response = await client.PostAsync(
                CustomVisionApiConfig?.VisionPredictionEndpoint,
                content
            );
            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<ImagePrediction>(json);
        }

        /// <summary>
        /// not working on android 5.1
        /// </summary>
        /// <param name="photoStream"></param>
        /// <returns></returns>
        public async Task<ImagePrediction> UploadToVisionPredictionApi(Stream photoStream)
        {
            ImagePrediction result = await PredictionApiClient.DetectImageAsync(
                new Guid(CustomVisionApiConfig?.VisionProjectName),
                CustomVisionApiConfig?.VisionModelName,
                photoStream
            );
            return result;
        }

        private static CustomVisionPredictionClient AuthenticatePredictionClient(
            string predictionKey,
            string visionApiBaseAddress
        )
        {
            // Create a prediction endpoint, passing in the obtained prediction key
            CustomVisionPredictionClient predictionApi = new CustomVisionPredictionClient(
                new ApiKeyServiceClientCredentials(predictionKey)
            )
            {
                Endpoint = visionApiBaseAddress
            };
            return predictionApi;
        }

        public async Task<FileResult> TakePhoto()
        {
            FileResult result = await MediaPicker.CapturePhotoAsync();

            return result;
        }

        public override void OnAppearing()
        {
            /* Task.Factory.StartNew(
                 () =>
                     Observe(
                         new TimeSpan(0, 0, 15),
                         _ObserveTaskTokenSource.Token
                     ),
                 TaskCreationOptions.LongRunning
             );*/
        }

        public override void OnDisappearing()
        {
            // CheckToCancelObserveTask();
        }

        // times based on Spokane (in hours)
        public static int[] Sunsets { get; set; } =
            new int[] { 17, 17, 19, 21, 22, 22, 21, 21, 20, 19, 18, 18 };

        public static int[] Sunrises { get; set; } =
            new int[] { 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7 };
    }
}
