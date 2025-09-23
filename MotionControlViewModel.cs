// MotionControlViewModel.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace StarlightRotationWpf
{
    public class MotionControlViewModel : INotifyPropertyChanged
    {
        // --- 旧属性 (将被替换) ---
        // public ObservableCollection<double> Speeds { get; } = new ObservableCollection<double>(new double[8]);
        // public ObservableCollection<double> Accelerations { get; } = new ObservableCollection<double>(new double[8]);

        // --- 新属性：用于绑定Slider ---
        private double _speed;
        public double Speed
        {
            get => _speed;
            set { _speed = value; OnPropertyChanged(); }
        }

        private double _acceleration;
        public double Acceleration
        {
            get => _acceleration;
            set { _acceleration = value; OnPropertyChanged(); }
        }

        // --- 其他属性和命令保持不变 ---
        public string Title { get; }

        private double _targetAngle;
        public double TargetAngle
        {
            get => _targetAngle;
            set { _targetAngle = value; OnPropertyChanged(); }
        }

        private double _stepSize = 10.0;
        public double StepSize
        {
            get => _stepSize;
            set { _stepSize = value; OnPropertyChanged(); }
        }

        private double _zeroOffset;
        public double ZeroOffset
        {
            get => _zeroOffset;
            set { _zeroOffset = value; OnPropertyChanged(); }
        }

        public ICommand MoveCommand { get; }
        public ICommand SetZeroCommand { get; }
        public ICommand StepPositiveCommand { get; }
        public ICommand StepNegativeCommand { get; }
        public ICommand StopCommand { get; }

        public MotionControlViewModel(string title)
        {
            Title = title;

            // 初始化命令 (与之前相同)
            MoveCommand = new RelayCommand(() => System.Diagnostics.Trace.WriteLine($"{Title}: Moving to angle {TargetAngle}"));
            SetZeroCommand = new RelayCommand(() => System.Diagnostics.Trace.WriteLine($"{Title}: Setting zero offset to {ZeroOffset}"));
            StepPositiveCommand = new RelayCommand(() => TargetAngle += StepSize);
            StepNegativeCommand = new RelayCommand(() => TargetAngle -= StepSize);
            StopCommand = new RelayCommand(() => System.Diagnostics.Trace.WriteLine($"{Title}: Stop command issued"));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}