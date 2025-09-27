// MotionControlViewModel.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace StarlightRotationWpf
{
    public enum AxisType { Horizontal, Vertical }
    public class MotionControlViewModel : INotifyPropertyChanged
    {
        // --- 旧属性 (将被替换) ---
        // public ObservableCollection<double> Speeds { get; } = new ObservableCollection<double>(new double[8]);
        // public ObservableCollection<double> Accelerations { get; } = new ObservableCollection<double>(new double[8]);
        private readonly HardwareService _hardwareService;
        private readonly AxisType _axis;
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

        public event EventHandler SettingsChanged;

        private double _zeroOffset;
        public double ZeroOffset
        {
            get => _zeroOffset;
            set { _zeroOffset = value; OnPropertyChanged(); }
        }

        private double _currentAngle;
        public double CurrentAngle
        {
            get => _currentAngle;
            set { _currentAngle = value; OnPropertyChanged(); }
        }

        public ICommand MoveCommand { get; }
        public ICommand SetZeroCommand { get; }
        public ICommand StepPositiveCommand { get; }
        public ICommand StepNegativeCommand { get; }
        public ICommand StopCommand { get; }

        public MotionControlViewModel(string title, HardwareService hardwareService, AxisType axis)
        {
            Title = title;
            _hardwareService = hardwareService;
            _axis = axis;

            MoveCommand = new RelayCommand(MoveToTargetAngle, CanExecuteMotion);
            SetZeroCommand = new RelayCommand(ExecuteSetZero, CanExecuteMotion);
            StepPositiveCommand = new RelayCommand(() => {
                TargetAngle += StepSize;
                MoveToTargetAngle();
            }, CanExecuteMotion);
            StepNegativeCommand = new RelayCommand(() => { 
                TargetAngle -= StepSize; 
                MoveToTargetAngle();
            }, CanExecuteMotion);
            StopCommand = new RelayCommand(ExecuteStop, CanExecuteMotion);
        }

        private bool CanExecuteMotion() => _hardwareService.IsRotationConnected;

        private void MoveToTargetAngle()
        {
            // 根据轴类型调用不同的API方法
            if (_axis == AxisType.Horizontal)
            {
                _hardwareService.DualAxisRotationDevice.setHorizentalRotationInDegree(TargetAngle - ZeroOffset);
            }
            else
            {
                _hardwareService.DualAxisRotationDevice.setVerticalRotationInDegree(TargetAngle - ZeroOffset);
            }
        }

        private void ExecuteSetZero()
        {
            // 逻辑来自您原来的代码，现在更清晰
            if (CurrentAngle != 0)
            {
                // 计算新的偏移量
                double newOffset = -CurrentAngle + ZeroOffset;

                // 更新UI和内部状态
                ZeroOffset = newOffset;
                TargetAngle = 0; // 将目标设置为新的零点

                // 触发事件，通知 MainViewModel 保存新的偏移量
                SettingsChanged?.Invoke(this, EventArgs.Empty);

                // 移动到新的零点
                MoveToTargetAngle();
            }
        }


        private void ExecuteStop()
        {
            _hardwareService.DualAxisRotationDevice.emergencyStop();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}