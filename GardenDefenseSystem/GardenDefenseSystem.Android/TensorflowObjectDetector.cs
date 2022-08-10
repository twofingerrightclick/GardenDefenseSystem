using Android.App;
using Android.Graphics;
using Java.IO;
using Java.Nio;
using Java.Nio.Channels;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Xamarin.TensorFlow.Lite;

[assembly: Dependency(typeof(GardenDefenseSystem.Droid.TensorflowObjectDetector))]

namespace GardenDefenseSystem.Droid
{
    public class TensorflowObjectDetector : IObjectDetector
    {
        //FloatSize is a constant with the value of 4 because a float value is 4 bytes
        const int FloatSize = 4;

        //PixelSize is a constant with the value of 3 because a pixel has three color channels: Red Green and Blue
        const int PixelSize = 3;

        // _OutputBoxes: array of shape [Batchsize, NUM_DETECTIONS,4]
        // contains the location of detected boxes
        private float[][] _OutputBoxes;
        public Java.Lang.Object OutputBoxes
        {
            get => Java.Lang.Object.FromArray(_OutputBoxes);
            set => _OutputBoxes = value.ToArray<float[]>();
        }

        // _OutputClasses: array of shape [NUM_DETECTIONS]
        // contains the classes of detected boxes
        private long[] _OutputClasses;
        public Java.Lang.Object OutputClasses
        {
            get => Java.Lang.Object.FromArray(_OutputClasses);
            set => _OutputClasses = value.ToArray<long>();
        }

        // _OutputScores: array of shape [NUM_DETECTIONS]
        // contains the scores of detected boxes
        private float[] _OutputScores;
        public Java.Lang.Object OutputScores
        {
            get => Java.Lang.Object.FromArray(_OutputScores);
            set => _OutputScores = value.ToArray<float>();
        }

        public Interpreter Interpreter { get; }

        public TensorflowObjectDetector()
        {
            var mappedByteBuffer = GetModelAsMappedByteBuffer();
            Interpreter = new Interpreter(mappedByteBuffer);
        }

        public ImagePrediction Detect(byte[] image)
        {
            //To resize the image, we first need to get its required width and height
            var tensor = Interpreter.GetInputTensor(0);
            var shape = tensor.Shape();

            var width = shape[1];
            var height = shape[2];

            var imageByteBuffer = GetPhotoAsByteBuffer(image, width, height);

            //use StreamReader to import the labels from labels.txt
            using var streamReader = new StreamReader(
                Android.App.Application.Context.Assets.Open("labels.txt")
            );

            //Transform labels.txt into List<string>
            var labels = streamReader
                .ReadToEnd()
                .Split('\n')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            // see this example of output https://github.com/tensorflow/examples/blob/75f4d66acd84b67fbee073186c9e031db5513e34/lite/examples/object_detection/android/app/src/main/java/org/tensorflow/lite/examples/detection/tflite/TFLiteObjectDetectionAPIModel.java#L178-L194
            int detectedBoxesOutputIndex = Interpreter.GetOutputIndex("detected_boxes"); // 0
            int detectedClassesOutputIndex = Interpreter.GetOutputIndex("detected_classes"); // 1
            int detectedScoresOutputIndex = Interpreter.GetOutputIndex("detected_scores"); // 2

            int numDetections = Interpreter
                .GetOutputTensor(detectedClassesOutputIndex)
                .NumElements();
 
            _OutputBoxes = CreateJaggedArray<float>(numDetections, 4);
            _OutputClasses = new long[numDetections];
            _OutputScores = new float[numDetections];

            // example of using buffer outputs
            //var buffer =  FloatBuffer.Allocate(1000);

            var javaizedOutputBoxes = OutputBoxes;
            var javaizedOutputClasses = OutputClasses;
            var javaizedOutputScores = OutputScores;

            Java.Lang.Object[] inputArray = { imageByteBuffer };

            var outputMap = new Dictionary<Java.Lang.Integer, Java.Lang.Object>();
            outputMap.Add(new Java.Lang.Integer(detectedBoxesOutputIndex), javaizedOutputBoxes);
            outputMap.Add(new Java.Lang.Integer(detectedClassesOutputIndex), javaizedOutputClasses);
            outputMap.Add(new Java.Lang.Integer(detectedScoresOutputIndex), javaizedOutputScores);
   

            Interpreter.RunForMultipleInputsOutputs(inputArray, outputMap);

            OutputBoxes = javaizedOutputBoxes;
            OutputClasses = javaizedOutputClasses;
            OutputScores = javaizedOutputScores;

            var imagePrediction = new ImagePrediction(predictions: new List<PredictionModel>());
            for (var i = 0; i < _OutputScores.Length; i++)
            {
                var label = labels[(int)_OutputClasses[i]];
                imagePrediction.Predictions.Add(
                    new PredictionModel(probability: _OutputScores[i], tagName: label)
                );
            }

            return imagePrediction;
        }

        //Convert model.tflite to Java.Nio.MappedByteBuffer , the depricated required type for Xamarin.TensorFlow.Lite.Interpreter
        private MappedByteBuffer GetModelAsMappedByteBuffer()
        {
            var assetDescriptor = Android.App.Application.Context.Assets.OpenFd("model.tflite");

            var inputStream = new FileInputStream(assetDescriptor.FileDescriptor);

            var mappedByteBuffer = inputStream.Channel.Map(
                FileChannel.MapMode.ReadOnly,
                assetDescriptor.StartOffset,
                assetDescriptor.DeclaredLength
            );

            return mappedByteBuffer;
        }

        //Resize the image for the TensorFlow interpreter
        private ByteBuffer GetPhotoAsByteBuffer(byte[] image, int width, int height)
        {
            var bitmap = BitmapFactory.DecodeByteArray(image, 0, image.Length);
            var resizedBitmap = Bitmap.CreateScaledBitmap(bitmap, width, height, true);

            var modelInputSize = FloatSize * height * width * PixelSize;
            var byteBuffer = ByteBuffer.AllocateDirect(modelInputSize);
            byteBuffer.Order(ByteOrder.NativeOrder());

            var pixels = new int[width * height];
            resizedBitmap.GetPixels(
                pixels,
                0,
                resizedBitmap.Width,
                0,
                0,
                resizedBitmap.Width,
                resizedBitmap.Height
            );

            var pixel = 0;

            //Loop through each pixels to create a Java.Nio.ByteBuffer
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var pixelVal = pixels[pixel++];

                    byteBuffer.PutFloat(pixelVal >> 16 & 0xFF);
                    byteBuffer.PutFloat(pixelVal >> 8 & 0xFF);
                    byteBuffer.PutFloat(pixelVal & 0xFF);
                }
            }

            bitmap.Recycle();

            return byteBuffer;
        }

        private static T[][][] CreateJaggedArray<T>(int lay1, int lay2, int lay3)
        {
            var arr = new T[lay1][][];

            for (int i = 0; i < lay1; i++)
            {
                arr[i] = CreateJaggedArray<T>(lay2, lay3);
            }
            return arr;
        }

        private static T[][] CreateJaggedArray<T>(int lay1, int lay2)
        {
            var arr = new T[lay1][];

            for (int i = 0; i < lay1; i++)
            {
                arr[i] = new T[lay2];
            }

            return arr;
        }
    }
}
