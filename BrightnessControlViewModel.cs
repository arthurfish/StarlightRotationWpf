// BrightnessControlViewModel.cs
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace StarlightRotationWpf
{
    public class BrightnessControlViewModel : INotifyPropertyChanged
    {
        private readonly HardwareService _hardwareService;
        // Properties for UI binding
        private double _inputMagnitude = -6;
        public double InputMagnitude
        {
            get => _inputMagnitude;
            set { _inputMagnitude = value; OnPropertyChanged(); CalculateBrightness(); }
        }

        private double _calculatedBrightness;
        public double CalculatedBrightness
        {
            get => _calculatedBrightness;
            set { _calculatedBrightness = value; OnPropertyChanged(); }
        }

        // 用于显示设备读数，由MainViewModel更新
        private string _currentBrightnessReading1 = "N/A";
        public string CurrentBrightnessReading1
        {
            get => _currentBrightnessReading1;
            set { _currentBrightnessReading1 = value; OnPropertyChanged(); }
        }

        private string _currentBrightnessReading2 = "N/A";
        public string CurrentBrightnessReading2
        {
            get => _currentBrightnessReading2;
            set { _currentBrightnessReading2 = value; OnPropertyChanged(); }
        }

        private double _smallSphereCurrent; // 小球光源电流 (mA)
        public double SmallSphereCurrent
        {
            get => _smallSphereCurrent;
            set
            {
                if (value > 1000) value = 1000;
                if (value < 0) value = 0;
                _smallSphereCurrent = value;
                // 假设小球是光源1
                _hardwareService.Device1266?.SetLightSourceCurrent(1, value / 1000.0);
                Trace.WriteLine("Light1 (Small Sphere) set mA: " + value);
                OnPropertyChanged();
            }
        }

        private double _largeSphereCurrent; // 大球光源电流 (mA)
        public double LargeSphereCurrent
        {
            get => _largeSphereCurrent;
            set
            {
                if (value > 1000) value = 1000;
                if (value < 0) value = 0;
                _largeSphereCurrent = value;
                // 假设大球是光源2
                _hardwareService.Device1266?.SetLightSourceCurrent(2, value / 1000.0);
                Trace.WriteLine("Light2 (Large Sphere) set mA: " + value);
                OnPropertyChanged();
            }
        }

        private double _currentStarSize = 0.014; // Default

        public BrightnessControlViewModel(HardwareService hardwareService)
        {
            _hardwareService = hardwareService;
            CalculateBrightness();
        }

        public void UpdateStarSize(double newSize)
        {
            if (newSize > 0)
            {
                _currentStarSize = newSize;
                CalculateBrightness();
            }
        }

        private void CalculateBrightness()
        {
            // The formula from your original ViewModel
            CalculatedBrightness = 0.00493888 * Math.Pow(2.512, -InputMagnitude) / (_currentStarSize * _currentStarSize);
        }

        public BrightnessControlViewModel()
        {
            CalculateBrightness();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
