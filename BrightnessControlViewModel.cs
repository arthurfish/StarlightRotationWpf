// BrightnessControlViewModel.cs
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Threading; // 1. 引入DispatcherTimer所在的命名空间

namespace StarlightRotationWpf
{
    public class BrightnessControlViewModel : INotifyPropertyChanged
    {
        private readonly HardwareService _hardwareService;
        private readonly DispatcherTimer _updateTimer; // 2. 添加一个定时器成员变量

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

        // 用于显示设备读数，由定时器自动更新
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
                if (value > 1000000) value = 1000000;
                if (value < 0) value = 0;
                _smallSphereCurrent = value;
                // 假设小球是光源2 (根据您的HardwareService代码，在1266设备上)
                _hardwareService.Device1266?.SetLightSourceCurrent(2, value);
                Trace.WriteLine("Light1 (Small Sphere) set uA: " + value);
                OnPropertyChanged();
            }
        }

        private double _largeSphereCurrent; // 大球光源电流 (mA)
        public double LargeSphereCurrent
        {
            get => _largeSphereCurrent;
            set
            {
                if (value > 1000000) value = 1000000;
                if (value < 0) value = 0;
                _largeSphereCurrent = value;
                // 假设大球是光源1 (根据您的HardwareService代码，在1266设备上)
                _hardwareService.Device1266?.SetLightSourceCurrent(1, value);
                Trace.WriteLine("Light2 (Large Sphere) set uA: " + value);
                OnPropertyChanged();
            }
        }

        private double _currentStarSize = 0.014; // Default


        private string _currentBrightness;
        public string CurrentBrightness
        {
            get => _currentBrightness;
            set {
                _currentBrightness = value;
                OnPropertyChanged();
            }
        }

        // 主构造函数，用于运行时
        public BrightnessControlViewModel(HardwareService hardwareService)
        {
            _hardwareService = hardwareService;
            CalculateBrightness();

            // 3. 初始化并启动定时器
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(300); // 设置间隔为300毫秒
            _updateTimer.Tick += UpdateTimer_Tick; // 绑定Tick事件的处理方法
            _updateTimer.Start(); // 启动定时器
        }

        // 4. 定时器触发时执行的方法
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            // 确保硬件服务和设备都已连接
            if (_hardwareService == null || !_hardwareService.IsStarlightConnected)
            {
                CurrentBrightnessReading1 = "设备未连接";
                CurrentBrightnessReading2 = "设备未连接";
                return;
            }

            try
            {
                // 读取设备1 (0105) 的亮度值
                // StarlightDeviceApi.ReadDetectorValue() 返回一个 DetectorReading 结构体
                var reading1 = _hardwareService.Device0105.ReadDetectorValue();
                reading1.Value *= 202183822.7 * 1.3;
                // 将读数格式化为更友好的字符串并更新到UI属性
                CurrentBrightnessReading1 = $"{reading1.Value:F4} (增益: {reading1.Gain})";

                // 读取设备2 (1266) 的亮度值
                var reading2 = _hardwareService.Device1266.ReadDetectorValue();
                reading2.Value *= 27586.21*0.88;
                double validValue;
                if (Math.Abs(LargeSphereCurrent) <= 0.00001)
                {
                    validValue = reading2.Value;
                }
                else
                {
                    validValue = reading1.Value;
                }
                if (validValue < 0.01)
                {
                    CurrentBrightness = $"{validValue:E3}";
                }else
                {
                    CurrentBrightness = $"{validValue:F3}";
                }
//                CurrentBrightness = $"小球：{reading2.Value:E2} 大球：{reading1.Value:F2}";
//                Trace.WriteLine(CurrentBrightnessReading2);
            }
            catch (Exception ex)
            {
                CurrentBrightnessReading2 = "读取错误";
                Trace.WriteLine($"[Error] Reading Device: {ex.Message}");
            }

        }

        /// <summary>
        /// (可选，但建议) 提供一个方法来停止定时器，以防资源泄露。
        /// 可以在窗口关闭或控件卸载时调用。
        /// </summary>
        public void StopAutoUpdate()
        {
            _updateTimer?.Stop();
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
            CalculatedBrightness = 0.00493888 * Math.Pow(2.512, -InputMagnitude) / (_currentStarSize * _currentStarSize) * 100;
        }

        // 无参构造函数，主要用于XAML设计器
        public BrightnessControlViewModel()
        {
            CalculateBrightness();
            // 在设计模式下，我们不启动定时器
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}