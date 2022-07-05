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

        public TensorflowObjectDetector()
        {
            var mappedByteBuffer = GetModelAsMappedByteBuffer();
            Interpreter = new Interpreter(mappedByteBuffer);
        }

        public Interpreter Interpreter { get; }

        public ImagePrediction Detect(byte[] image)
        {
            //To resize the image, we first need to get its required width and height
            var tensor = Interpreter.GetInputTensor(0);
            var shape = tensor.Shape();

            var width = shape[1];
            var height = shape[2];

            var byteBuffer = GetPhotoAsByteBuffer(image, width, height);

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

            //var output = new FloatBuffer[Interpreter.OutputTensorCount];


            int detectedBoxesOutputIndex = Interpreter.GetOutputIndex("detected_boxes"); // 0
            int detectedClassesOutputIndex = Interpreter.GetOutputIndex("detected_classes"); // 1
            int detectedScoresOutputIndex = Interpreter.GetOutputIndex("detected_scores"); // 2
            /*
                        for (int i = 0; i < output.Length; i++)
                        {
                            output[i] = FloatBuffer.Allocate(10000);
                        }*/

            var outputDict = new Dictionary<Java.Lang.Integer, Java.Lang.Object>();

            int firstDimensionOfOutputTensorArray = Interpreter.
                GetOutputTensor(Interpreter.GetOutputIndex("detected_boxes"))
                .NumElements() / 4;
            var boundingBoxes = new float[firstDimensionOfOutputTensorArray][];
            for (int i = 0; i < boundingBoxes.Length; i++)
            {
                boundingBoxes[i] = new float[4];
            }
            var boundingBoxesOutput = Java.Lang.Object.FromArray(boundingBoxes);

            outputDict.Add(new Java.Lang.Integer(detectedBoxesOutputIndex), boundingBoxesOutput);

            var detectedClasses = new int[64];
            var detectedClassesOutput = (Java.Lang.Object)  IntBuffer.Allocate(1000);

            outputDict.Add(new Java.Lang.Integer(detectedClassesOutputIndex), detectedClassesOutput);


            var scores = new float[64];
            var scoresOutput = (Java.Lang.Object)FloatBuffer.Allocate(1000);

            outputDict.Add(new Java.Lang.Integer(detectedScoresOutputIndex), scoresOutput);

            var input = new ByteBuffer[1] { byteBuffer };
            var inputs = (Java.Lang.Object[])(input);


            Interpreter.RunForMultipleInputsOutputs(inputs, outputDict);

            


            /*  for (int i = 0; i < Interpreter.OutputTensorCount; i++)
              {
                  outputLocations[i] = new float[Interpreter.GetOutputTensor(i).NumElements()];
              }*/



            // using FloatBuffer outputs = FloatBuffer.Allocate(Interpreter.GetOutputTensor(1).NumElements());

            //using FloatBuffer outputs = FloatBuffer.Allocate(10000);

            //var outputs = Java.Lang.Object<float[,,]>(outputLocations);



/*            output[0].Position(0);
            //outputs.ToArray<>

            StringBuilder stringBuilder = new StringBuilder();
            int count = 0;
            while (output[0].HasRemaining)
            {
                count++;
                stringBuilder.Append(output[0].Get() + ", ");

            }
             var x = stringBuilder.ToString();

            var classificationResult = outputs.ToArray<float>();*/
            var classificationResult = boundingBoxesOutput.ToArray<float[]>();

            //Map the classificationResult to the labels and sort the result to find which label has the highest probability

            var imagePrediction = new ImagePrediction();
            for (var i = 0; i < labels.Count; i++)
            {
                /*var label = labels[i];
                imagePrediction.Predictions.Add(
                    new PredictionModel(probability: classificationResult[0][i], tagName: label)
                );*/
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

        /*private Java.IO.File GetModelFile()
        {
            var assetDescriptor = Application.Context.Assets.OpenFd("model.tflite");
            FileDescriptor descriptor = assetDescriptor.FileDescriptor;

            Java.IO.File file = new Java.IO.File(assetDescriptor);

            return Java.IO.File.
        }*/

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
    }
}
