using StarlightRotation;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace StarlightRotationWpf
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // --- 私有字段 ---
        private StarlightDeviceApi device0105 = new StarlightDeviceApi();
        private StarlightDeviceApi device1266 = new StarlightDeviceApi();
        private DualAxisRotationDeviceApi dualAxisRotationDeviceApi = new DualAxisRotationDeviceApi();
        private int wheelhandle = 0;
        private DispatcherTimer _timer;

        private string _device1SerialNumber = "Connecting...";
        private string _device2SerialNumber = "Connecting...";
        private string _device1ReadingValue = "N/A";
        private string _device2ReadingValue = "N/A";
        private int _light1CurrencyMilliAmpere = 0;
        private int _light2CurrencyMilliAmpere = 0;

        private double _horizentalAngleInDegree = 0;
        private double _verticalAngleInDegree = 0;

        private double _horizentalStepSizeInDegree = 10;
        private double _verticalStepSizeInDegree = 10;

        private double _rotationSpeed = 10;

        private double _gotHorizentalAngleInDegree = 0;
        private double _gotVerticalAngleInDegree = 0;

        public ICommand ZeroDetector1Command { get; private set; }
        public ICommand ZeroDetector2Command { get; private set; }

        public ICommand FilterWheelRotateCommand { get; private set; }

        public ICommand DualAxisRotationGoHorizental { get; private set; }
        public ICommand DualAxisRotationGoVertical { get; private set; }
        public ICommand DualAxisRotationStop { get; private set; }
        public ICommand DualAxisRotationHorizenAdd { get; private set; }
        public ICommand DualAxisRotationHorizenMinus { get; private set; }
        public ICommand DualAxisRotationVerticalAdd { get; private set; }
        public ICommand DualAxisRotationVerticalMinus { get; private set; }


        // --- 公开属性 (供View绑定) ---




        public string Device1SerialNumber
        {
            get => _device1SerialNumber;
            set
            {
                _device1SerialNumber = value;
                OnPropertyChanged(); // 2. 当属性值改变时，发出通知
            }
        }

        public string Device2SerialNumber
        {
            get => _device2SerialNumber;
            set
            {
                _device2SerialNumber = value;
                OnPropertyChanged();
            }
        }

        public string Device1Reading
        {
            get => _device1ReadingValue;
            set
            {
                _device1ReadingValue = value;
                OnPropertyChanged();
            }
        }

        public string Device2Reading
        {
            get => _device2ReadingValue;
            set
            {
                _device2ReadingValue = value;
                OnPropertyChanged();
            }
        }

        public int Light1CurrencyMilliAmpere
        {
            get => _light1CurrencyMilliAmpere;
            set {
                if (value > 1000)
                    value = 1000;
                if (value < 0)
                    value = 0;
                device1266.SetLightSourceCurrent(1, value / 1000.0);
                _light1CurrencyMilliAmpere = value;
                OnPropertyChanged();
            }
        }

        public int Light2CurrencyMilliAmpere
        {
            get => _light2CurrencyMilliAmpere;
            set
            {
                if (value > 1000)
                    value = 1000;
                if (value < 0)
                    value = 0;
                device1266.SetLightSourceCurrent(2, value / 1000.0);
                _light2CurrencyMilliAmpere = value;
                OnPropertyChanged();
            }
        }

        public double HorizentalAngleInDegree
        {
            get => _horizentalAngleInDegree;
            set
            {
                _horizentalAngleInDegree = value;
                OnPropertyChanged();
            }
        }

        public double VerticalAngleInDegree
        {
            get => _verticalAngleInDegree;
            set
            {
                _verticalAngleInDegree = value;
                OnPropertyChanged();
            }
        }

        public double HorizentalStepSizeInDegree
        {
            get => _horizentalStepSizeInDegree;
            set
            {
                HorizentalStepSizeInDegree = value;
                OnPropertyChanged();
            }
        }

        public double VerticalStepSizeInDegree
        {
            get => _verticalStepSizeInDegree;
            set
            {
                VerticalStepSizeInDegree = value;
                OnPropertyChanged();
            }
        }

        public double GotHorizontalAngleInDegree
        {
            get => _gotHorizentalAngleInDegree;
            set
            {
                _gotHorizentalAngleInDegree= value;
                OnPropertyChanged();
            }
        }

        public double GotVerticalAngleInDegree
        {
            get => _gotVerticalAngleInDegree;
            set
            {
                _gotVerticalAngleInDegree= value;
                OnPropertyChanged();
            }
        }

        public double rotationSpeed
        {
            get => _rotationSpeed;
            set
            {
                _rotationSpeed = value;
                dualAxisRotationDeviceApi.Speed = value;
                OnPropertyChanged();
            }
        }
        // --- 构造函数 (初始化逻辑) ---
        public MainViewModel()
        {
            InitializeDevices();

            Trace.WriteLine("Preparing Wheel....");
            wheelhandle = FilterWheelApi.InitializeDeviceAndGetHandle("COM6");

            // 使用DispatcherTimer，它能确保Tick事件在UI线程上执行，避免跨线程问题
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(300);
            _timer.Tick += OnTimerTick;
            _timer.Start();

            _horizentalAngleInDegree = dualAxisRotationDeviceApi.getHorizentalRotationInDegree();
            _verticalAngleInDegree = dualAxisRotationDeviceApi.getVerticalRotationInDegree();

            ZeroDetector1Command = new AsyncRelayCommand<Object>(
                execute: (Object) => Task.Run(() =>
                {
                    try
                    {
                        device0105.ZeroDetector();
                    }
                    catch (Exception _)
                    {
                        {
                            Trace.WriteLine("ZeroDetector1 ERROR!");
                        }
                    }
                }),
                canExecute: (_) => device0105.IsConnected // 只有在设备连接时，按钮才可用
                );
            ZeroDetector2Command = new AsyncRelayCommand<Object>(
                execute: (_) => Task.Run(() => {
                    try
                    {
                        device1266.ZeroDetector();
                    }
                    catch(Exception _) {
                    {
                        Trace.WriteLine("ZeroDetector1 ERROR!");
                    }
                }}),
                canExecute: (_) => device1266.IsConnected // 只有在设备连接时，按钮才可用
                );
            FilterWheelRotateCommand = new AsyncRelayCommand<string>(
                execute: (s) => Task.Run(() => {
                    var result = FilterWheelApi.SetPosition(wheelhandle, int.Parse(s));
                    if(result != 0)
                    {
                        Trace.WriteLine(FilterWheelApi.GetErrorMessage(result));
                    }
                }),
                canExecute: (_) =>
                {
                    return true;
                });

            DualAxisRotationGoHorizental = new AsyncRelayCommand<string>(
            execute: (s) => Task.Run(() =>
            {
                dualAxisRotationDeviceApi.setHorizentalRotationInDegree(HorizentalAngleInDegree);
            }),
            canExecute: (_) =>
            {
                return dualAxisRotationDeviceApi.isAvaliable();
            });

            DualAxisRotationGoVertical= new AsyncRelayCommand<string>(
                execute: (s) => Task.Run(() =>
                {
                    dualAxisRotationDeviceApi.setVerticalRotationInDegree(VerticalAngleInDegree);
                }),
                canExecute: (_) =>
                {
                    return dualAxisRotationDeviceApi.isAvaliable();
                });

            DualAxisRotationStop = new AsyncRelayCommand<string>(
                execute: (s) => Task.Run(() =>
                {
                    dualAxisRotationDeviceApi.emergencyStop();
                }),
                canExecute: (_) =>
                {
                    return true;
                });

            DualAxisRotationHorizenAdd = new AsyncRelayCommand<string>(
                execute: (s) => Task.Run(() =>
                {
                    HorizentalAngleInDegree += HorizentalStepSizeInDegree;
                    dualAxisRotationDeviceApi.setHorizentalRotationInDegree(HorizentalAngleInDegree);
                }),
                canExecute: (_) =>
                {
                    return dualAxisRotationDeviceApi.isAvaliable();
                });


            DualAxisRotationHorizenMinus = new AsyncRelayCommand<string>(
                execute: (s) => Task.Run(() =>
                {
                    HorizentalAngleInDegree -= HorizentalStepSizeInDegree;
                    dualAxisRotationDeviceApi.setHorizentalRotationInDegree(HorizentalAngleInDegree);
                }),
                canExecute: (_) =>
                {
                    return dualAxisRotationDeviceApi.isAvaliable();
                });

            DualAxisRotationVerticalAdd = new AsyncRelayCommand<string>(
                execute: (s) => Task.Run(() =>
                {
                    VerticalAngleInDegree += VerticalStepSizeInDegree;
                    dualAxisRotationDeviceApi.setVerticalRotationInDegree(VerticalAngleInDegree);
                }),
                canExecute: (_) =>
                {
                    return dualAxisRotationDeviceApi.isAvaliable();
                });

            DualAxisRotationVerticalMinus = new AsyncRelayCommand<Object>(
    execute: (obj) => Task.Run(() =>
    {
        VerticalAngleInDegree -= VerticalStepSizeInDegree;
        dualAxisRotationDeviceApi.setVerticalRotationInDegree(VerticalAngleInDegree);
    }),
    canExecute: (_) =>
    {
        return dualAxisRotationDeviceApi.isAvaliable();
    });

        }


        // --- 核心逻辑方法 ---

        private void InitializeDevices()
        {
            // 这里可以添加try-catch来处理连接失败的情况
            // 为了简化，我们假设它能成功
            device0105.Connect();
            device1266.Connect();

            if (device0105.SerialNumber == "1266" && device1266.SerialNumber == "0105")
            {
                (device0105, device1266) = (device1266, device0105);
            }

            if (device0105.SerialNumber != "0105" || device1266.SerialNumber != "1266")
            {
                Device1SerialNumber = "Error!";
                Device2SerialNumber = "Error!";
                Trace.WriteLine("无法连接到星光模拟器！");
                return;
            }

           // device1266.SetLightSourceCurrent(1, 0.1);

            // 更新属性，UI会自动响应
            Device1SerialNumber = device0105.SerialNumber;
            Device2SerialNumber = device1266.SerialNumber;

            device1266.SetLightSourceCurrent(1,0);
            device1266.SetLightSourceCurrent(2, 0);

            

            // 立即读取一次数据
            UpdateReadings();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            UpdateReadings();
        }

        private void UpdateReadings()
        {
            if (device0105.IsConnected && device1266.IsConnected)
            {
                var d1Read = device1266.ReadDetectorValue();
                var d2Read = device0105.ReadDetectorValue();

                // 更新属性，而不是直接操作UI控件
                var d1Number = d1Read.Value * 27586.21;
                if (d1Number < 0.01)
                {
                    Device1Reading = $"{d1Number:E2}";
                }
                else
                {
                    Device1Reading = $"{d1Number:F2}";
                }

                Device2Reading = $"{d2Read.Value*202183822.7:F2}";



            }
            if (dualAxisRotationDeviceApi.isConnected)
            {
                GotHorizontalAngleInDegree = dualAxisRotationDeviceApi.getHorizentalRotationInDegree();
                GotVerticalAngleInDegree = dualAxisRotationDeviceApi.getVerticalRotationInDegree();
                Trace.WriteLine($"Dual: Got H A:{GotHorizontalAngleInDegree}");
                Trace.WriteLine($"Dual: Got V A:{GotVerticalAngleInDegree}");
            }
        }
        


        // --- INotifyPropertyChanged 的标准实现 ---
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public RelayCommand(Action execute, Func<bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

    public void Execute(object parameter) => _execute();
}

public class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T, Task> _execute;
    private readonly Func<T, bool> _canExecute;
    private bool _isExecuting;

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public AsyncRelayCommand(Func<T, Task> execute, Func<T, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        // 尝试将参数转换为泛型类型 T
        //return !_isExecuting && (_canExecute == null || _canExecute((T)parameter));
        return _canExecute((T)parameter);
    }

    public async void Execute(object parameter)
    {
        _isExecuting = true;
        try
        {
            // 尝试将参数转换为泛型类型 T
            await _execute((T)parameter);
        }
        finally
        {
            _isExecuting = false;
        }
    }
}