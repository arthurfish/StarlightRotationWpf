using StarlightRotation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace StarlightRotationWpf
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // --- 私有字段 ---
        private StarlightDeviceApi device0105 = new StarlightDeviceApi();
        private StarlightDeviceApi device1266 = new StarlightDeviceApi();
        private DispatcherTimer _timer;

        private string _device1SerialNumber = "Connecting...";
        private string _device2SerialNumber = "Connecting...";
        private string _device1ReadingValue = "N/A";
        private string _device2ReadingValue = "N/A";
        private double _light1Currency = 0;
        private double _light2Currency = 0;

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

        public double Light1Currency
        {
            get => _light1Currency;
            set {
                if (value > 1)
                    value = 1;
                if (value < 0)
                    value = 0;
                device1266.SetLightSourceCurrent(1, value);
                _light1Currency = value;
                OnPropertyChanged();
            }
        }

        public double Light2Currency
        {
            get => _light2Currency;
            set
            {
                if (value > 1)
                    value = 1;
                if (value < 0)
                    value = 0;
                device1266.SetLightSourceCurrent(2, value);
                _light2Currency = value;
                OnPropertyChanged();
            }
        }




        // --- 构造函数 (初始化逻辑) ---
        public MainViewModel()
        {
            InitializeDevices();

            // 使用DispatcherTimer，它能确保Tick事件在UI线程上执行，避免跨线程问题
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += OnTimerTick;
            _timer.Start();
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
                Console.WriteLine("无法连接到星光模拟器！");
                return;
            }

            device1266.SetLightSourceCurrent(1, 0.1);

            // 更新属性，UI会自动响应
            Device1SerialNumber = device0105.SerialNumber;
            Device2SerialNumber = device1266.SerialNumber;

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
                var d1Read = device0105.ReadDetectorValue();
                var d2Read = device1266.ReadDetectorValue();

                // 更新属性，而不是直接操作UI控件
                Device1Reading = $"Value: {d1Read.Value:F4}, Gain: {d1Read.Gain}";
                Device2Reading = $"Value: {d2Read.Value:F4}, Gain: {d2Read.Gain}";
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
