using GardenDefenseSystem.ViewModels;

namespace GardenDefenseSystemTests
{
    [TestClass]
    public class MethodTests
    {
        [TestMethod]
        public void NotDaytimeEvaluatesCorrectly()
        {
            Assert.IsTrue(DetectionViewModel.Sunsets.Length == 12);
            Assert.IsTrue(DetectionViewModel.Sunrises.Length == 12);

            var x = DetectionViewModel.NotDaytime(DateTime.Now.ToLocalTime());
        }

        [TestMethod]
        public void GetScale()
        {
          
            var x = DetectionViewModel.GetScale(3264,2448);
            Console.WriteLine("scale: " + x);
        }

    }
}