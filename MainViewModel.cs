using StarlightRotation;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Printing.IndexedProperties;
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
        private double _light1CurrencyMilliAmpere = 0;
        private double _light2CurrencyMilliAmpere = 0;
        private double _expectedMagnitude = 0;
        private double _expectedBrightness = 0;

        private int _filterWheelPosition;
        private string _filterWheelPositionString;

        private double _horizentalAngleInDegree = 0;
        private double _verticalAngleInDegree = 0;

        private double _horizentalStepSizeInDegree = 10;
        private double _verticalStepSizeInDegree = 10;

        private double _horizentalRotationSpeed = 5;
        private double _verticalRotationSpeed = 5;

        private double _gotHorizentalAngleInDegree = 0;
        private double _gotVerticalAngleInDegree = 0;

        private double _horizentalBias = 0;
        private double _verticalBias = 0;


        public ICommand ZeroDetector1Command { get; private set; }
        public ICommand ZeroDetector2Command { get; private set; }
        public ICommand AutoTuneCurrency { get; private set; }

        public ICommand FilterWheelRotateCommand { get; private set; }

        public ICommand DualAxisRotationGoHorizental { get; private set; }
        public ICommand DualAxisRotationGoVertical { get; private set; }
        public ICommand DualAxisRotationStop { get; private set; }
        public ICommand DualAxisRotationHorizenAdd { get; private set; }
        public ICommand DualAxisRotationHorizenMinus { get; private set; }
        public ICommand DualAxisRotationVerticalAdd { get; private set; }
        public ICommand DualAxisRotationVerticalMinus { get; private set; }

        public ICommand DualAxisRotationSetHorizentalBias { get; private set; }
        public ICommand DualAxisRotationSetVerticalBias { get; private set; }


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

        public double Light1CurrencyMilliAmpere
        {
            get => _light1CurrencyMilliAmpere;
            set
            {
                if (value > 1000)
                    value = 1000;
                if (value < 0)
                    value = 0;
                device1266.SetLightSourceCurrent(1, value / 1000.0);
                _light1CurrencyMilliAmpere = value;
                Trace.WriteLine("Light1 set mA: " + value);
                OnPropertyChanged();
            }
        }

        public double Light2CurrencyMilliAmpere
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
                Trace.WriteLine("Light2 set mA: " + value);
                OnPropertyChanged();
            }
        }

        private double _filterPositionToSize(int pos)
        {
            if (pos == 1)
                return 0.014;
            else if (pos == 2)
                return 0.035;
            else if (pos == 3)
                return 0.056;
            else if (pos == 4)
                return 0.084;
            else if (pos == 5)
                return 0.11;
            else if (pos == 6)
                return 0.14;
            return 0;
        }

        public double ExpectedMagnitude
        {
            get => _expectedMagnitude;
            set
            {
                if (value > 9)
                    value = 9;
                if (value < -6)
                    value = -6;
                _expectedMagnitude = value;
                ExpectedBrightness = GetBrightnessByMagnitudeAndSize(_expectedMagnitude, _filterPositionToSize(FilterWheelPosition));
                OnPropertyChanged();
            }
        }

        public double ExpectedBrightness
        {
            get => _expectedBrightness;
            set
            {
                _expectedBrightness = value;
                OnPropertyChanged();
            }
        }

        public int FilterWheelPosition
        {
            get => _filterWheelPosition;
            set
            {
                if (value != _filterWheelPosition)
                {
                    FilterWheelPositionString = "当前位置：[ " + _filterPositionToString(value) + " ]";
                    ExpectedBrightness = GetBrightnessByMagnitudeAndSize(ExpectedMagnitude, _filterPositionToSize(FilterWheelPosition));
                    var result = FilterWheelApi.SetPosition(wheelhandle, value);

                    if (result != 0)
                    {
                        Trace.WriteLine(FilterWheelApi.GetErrorMessage(result));
                        return;
                    }
                    _filterWheelPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _filterPositionToString(int position) => position switch
        {
            1 => "0.002° (0.014mm)",
            2 => "0.005° (0.035mm)",
            3 => "0.008° (0.056mm)",
            4 => "0.012° (0.084mm)",
            5 => "0.016° (0.110mm)",
            6 => "0.020° (0.140mm)",
            _ => "Nothing",
        };

        public string FilterWheelPositionString
        {
            get => _filterWheelPositionString;
            set {
                _filterWheelPositionString = value;
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
                _gotHorizentalAngleInDegree = value;
                OnPropertyChanged();
            }
        }

        public double GotVerticalAngleInDegree
        {
            get => _gotVerticalAngleInDegree;
            set
            {
                _gotVerticalAngleInDegree = value;
                OnPropertyChanged();
            }
        }

        public double HorizentalRotationSpeed
        {
            get => _horizentalRotationSpeed;
            set
            {
                if(value < -8)
                {
                    value = -8;
                }
                if(value > 8)
                {
                    value = 8;
                }
                _horizentalRotationSpeed = value;
                dualAxisRotationDeviceApi.HorizentalSpeed = value;
                OnPropertyChanged();
            }
        }

        public double VerticalRotationSpeed
        {
            get => _verticalRotationSpeed;
            set
            {
                if (value < -10)
                    value = -10;
                if (value > 10)
                    value = 10;
                _verticalAngleInDegree = value;
                dualAxisRotationDeviceApi.VerticalSpeed = value;
                OnPropertyChanged();
            }
        }

        public double HorizentalBias
        {
            get => _horizentalBias;
            set
            {
                _horizentalBias = value;
                OnPropertyChanged();
            }
        }

        public double VerticalBias
        {
            get => _verticalBias;
            set
            {
                _verticalBias = value;
                OnPropertyChanged();
            }
        }

        double GetBrightnessByMagnitudeAndSize(double magnitude, double size)
        {
            var result = 0.00493888 * Math.Pow(2.512, -magnitude) / (size * size);
            Trace.WriteLine("Magnitude: " + magnitude + " , size: " + size + ", result: " + result);
            return result;
        }
        // --- 构造函数 (初始化逻辑) ---
        public MainViewModel()
        {
            InitializeDevices();

            var appSettings = new AppSettings();
            Trace.WriteLine("Loading Bias Data..............");
            appSettings.LoadSettings("settings.json");
            HorizentalBias = appSettings.HorizentalBias;
            VerticalBias = appSettings.VerticalBias;


            // 使用DispatcherTimer，它能确保Tick事件在UI线程上执行，避免跨线程问题
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(300);
            _timer.Tick += OnTimerTick;
            _timer.Start();

            Trace.WriteLine($"H:{HorizentalAngleInDegree} H_b:{HorizentalBias} V:{VerticalAngleInDegree} V_b:{VerticalBias}");
            HorizentalAngleInDegree = dualAxisRotationDeviceApi.getHorizentalRotationInDegree() + HorizentalBias;
            VerticalAngleInDegree = dualAxisRotationDeviceApi.getVerticalRotationInDegree() + VerticalBias;

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
                execute: (_) => Task.Run(() =>
                {
                    try
                    {
                        device1266.ZeroDetector();
                    }
                    catch (Exception _)
                    {
                        {
                            Trace.WriteLine("ZeroDetector1 ERROR!");
                        }
                    }
                }),
                canExecute: (_) => device1266.IsConnected // 只有在设备连接时，按钮才可用
                );
            FilterWheelRotateCommand = new AsyncRelayCommand<string>(
                execute: (s) => Task.Run(() =>
                {
                    FilterWheelPosition = int.Parse(s);
                }),
                canExecute: (_) =>
                {
                    return true;
                });

            DualAxisRotationGoHorizental = new AsyncRelayCommand<string>(
            execute: (s) => Task.Run(() =>
            {
                dualAxisRotationDeviceApi.setHorizentalRotationInDegree(HorizentalAngleInDegree - HorizentalBias);
            }),
            canExecute: (_) =>
            {
                return dualAxisRotationDeviceApi.isAvaliable();
            });

            DualAxisRotationGoVertical = new AsyncRelayCommand<string>(
                execute: (s) => Task.Run(() =>
                {
                    dualAxisRotationDeviceApi.setVerticalRotationInDegree(VerticalAngleInDegree - VerticalBias);
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
                    dualAxisRotationDeviceApi.setHorizentalRotationInDegree(HorizentalAngleInDegree - HorizentalBias);
                }),
                canExecute: (_) =>
                {
                    return dualAxisRotationDeviceApi.isAvaliable();
                });


            DualAxisRotationHorizenMinus = new AsyncRelayCommand<string>(
                execute: (s) => Task.Run(() =>
                {
                    HorizentalAngleInDegree -= HorizentalStepSizeInDegree;
                    dualAxisRotationDeviceApi.setHorizentalRotationInDegree(HorizentalAngleInDegree - HorizentalBias);
                }),
                canExecute: (_) =>
                {
                    return dualAxisRotationDeviceApi.isAvaliable();
                });

            DualAxisRotationVerticalAdd = new AsyncRelayCommand<string>(
                execute: (s) => Task.Run(() =>
                {
                    VerticalAngleInDegree += VerticalStepSizeInDegree;
                    dualAxisRotationDeviceApi.setVerticalRotationInDegree(VerticalAngleInDegree - VerticalBias);
                }),
                canExecute: (_) =>
                {
                    return dualAxisRotationDeviceApi.isAvaliable();
                });

            DualAxisRotationVerticalMinus = new AsyncRelayCommand<Object>(
                execute: (obj) => Task.Run(() =>
                {
                    VerticalAngleInDegree -= VerticalStepSizeInDegree;
                    dualAxisRotationDeviceApi.setVerticalRotationInDegree(VerticalAngleInDegree - VerticalBias);
                }),
                canExecute: (_) =>
                {
                    return dualAxisRotationDeviceApi.isAvaliable();
                });

            DualAxisRotationSetHorizentalBias = new AsyncRelayCommand<Object>(
                execute: (obj) => Task.Run(() =>
                {
                    if (HorizentalAngleInDegree != 0)
                    {
                        HorizentalBias = -HorizentalAngleInDegree + HorizentalBias;
                        HorizentalAngleInDegree = 0;
                        appSettings.HorizentalBias = HorizentalBias;
                        appSettings.SaveSettings("settings.json");
                    }
                }),
                canExecute: (_) =>
                {
                    return dualAxisRotationDeviceApi.isAvaliable();
                });

            DualAxisRotationSetVerticalBias = new AsyncRelayCommand<Object>(
                execute: (obj) => Task.Run(() =>
                {
                    if (VerticalAngleInDegree != 0)
                    {
                        VerticalBias = -VerticalAngleInDegree + VerticalBias;
                        VerticalAngleInDegree = 0;
                        appSettings.VerticalBias = VerticalBias;
                        appSettings.SaveSettings("settings.json");
                    }
                }),
                canExecute: (_) =>
                {
                    return dualAxisRotationDeviceApi.isAvaliable();
                });
        }


        // --- 核心逻辑方法 ---

        private void InitializeDevices()
        {
            Trace.WriteLine("Preparing Wheel....");
            wheelhandle = FilterWheelApi.InitializeDeviceAndGetHandle("COM6");
            var readPosition = 0;
            FilterWheelApi.GetPosition(wheelhandle, out readPosition);
            FilterWheelPosition = readPosition;
            Trace.WriteLine("Wheel Position: " + FilterWheelPosition + "f(pos): " + _filterPositionToSize(FilterWheelPosition));
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

            device1266.SetLightSourceCurrent(1, 0);
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

                Device2Reading = $"{d2Read.Value * 202183822.7:F2}";



            }
            if (dualAxisRotationDeviceApi.isConnected)
            {
                GotHorizontalAngleInDegree = dualAxisRotationDeviceApi.getHorizentalRotationInDegree() + HorizentalBias;
                GotVerticalAngleInDegree = dualAxisRotationDeviceApi.getVerticalRotationInDegree() + VerticalBias;
                //                Trace.WriteLine($"Dual: Got H A:{GotHorizontalAngleInDegree}");
                //                Trace.WriteLine($"Dual: Got V A:{GotVerticalAngleInDegree}");
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