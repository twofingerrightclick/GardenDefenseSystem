using GardenDefenseSystem.ViewModels;

namespace GardenDefenseSystemTests
{
    [TestClass]
    public class MethodTests
    {
        [TestMethod]
        public void NotDaytimeEvaluatesCorrectly()
        {
            Assert.IsTrue(AboutViewModel.Sunsets.Length == 12);
            Assert.IsTrue(AboutViewModel.Sunrises.Length == 12);

            var x = AboutViewModel.NotDayTime(DateTime.Now.ToLocalTime());
        }

        [TestMethod]
        public void GetScale()
        {
          
            var x = AboutViewModel.GetScale(3264,2448);
            Console.WriteLine("scale: " + x);
        }
    }
}