// BrightnessControlViewModel.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StarlightRotationWpf
{
    public class BrightnessControlViewModel : INotifyPropertyChanged
    {
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

        private double _currentBrightness; // This would be read from a device
        public double CurrentBrightness
        {
            get => _currentBrightness;
            set { _currentBrightness = value; OnPropertyChanged(); }
        }

        private int _smallSphereStep;
        public int SmallSphereStep
        {
            get => _smallSphereStep;
            set { _smallSphereStep = value; OnPropertyChanged(); }
        }

        private int _largeSphereStep;
        public int LargeSphereStep
        {
            get => _largeSphereStep;
            set { _largeSphereStep = value; OnPropertyChanged(); }
        }

        // This would need to be linked to the star size from another ViewModel
        // For simplicity, we can pass it in or use a shared service.
        private double _currentStarSize = 0.014; // Default to first star size

        public void UpdateStarSize(double newSize)
        {
            _currentStarSize = newSize;
            CalculateBrightness();
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
