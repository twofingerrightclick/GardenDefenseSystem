using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GardenDefenseSystem.Models
{
    internal class VisionApiCallCount
    {
        private VisionApiCallCount() { }

        private static VisionApiCallCount _Instance = null;

        public static VisionApiCallCount Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new VisionApiCallCount();
                }
                return _Instance;
            }
        }

        public event PropertyChangedEventHandler ApiCountChanged;

        protected virtual void OnApiCountChanged(string propertyName)
        {
            ApiCountChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _VisionCallCount;
        public int CallCount
        {
            get => _VisionCallCount;
            set
            {
                _VisionCallCount = value;
                //reset on the first day of the month
                if (DateTime.Now.Day == 1)
                {
                    _VisionCallCount = 0;
                }
                OnApiCountChanged(nameof(CallCount));
            }
        }
        const int _maxApiCallCount = 10000;
        const int _numPhotosPerMinute = 4;

        public int GetRemainingApiRuntimeHrs()
        {
            var hrs = (_maxApiCallCount - CallCount) / (60 * _numPhotosPerMinute);
            return hrs;
        }
    }
}
