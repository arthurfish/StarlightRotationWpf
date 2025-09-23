// MainViewModel.cs (Refactored)
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StarlightRotationWpf
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Sub-ViewModels for each panel
        public BrightnessControlViewModel BrightnessControl { get; }
        public StarSelectionViewModel StarSelection { get; }
        public MotionControlViewModel RotationControl { get; }
        public MotionControlViewModel RollControl { get; }

        // You would also keep properties for status, connection, etc. at this level
        private string _connectionStatus = "All Systems Nominal";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set { _connectionStatus = value; OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            // Initialize all the sub-viewmodels
            BrightnessControl = new BrightnessControlViewModel();
            StarSelection = new StarSelectionViewModel();
            RotationControl = new MotionControlViewModel("转动控制");
            RollControl = new MotionControlViewModel("滚动控制");

            // Example of connecting ViewModels:
            // When a new star is selected, update the brightness calculator.
            StarSelection.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(StarSelection.SelectedStar) && StarSelection.SelectedStar != null)
                {
                    BrightnessControl.UpdateStarSize(StarSelection.SelectedStar.Size);
                }
            };

            // Initialize hardware connections, timers, etc. here
            // This logic is now much cleaner as it just calls methods on the sub-viewmodels
            // or a dedicated hardware service class.
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}