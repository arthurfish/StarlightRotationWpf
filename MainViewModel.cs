// MainViewModel.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace StarlightRotationWpf
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly HardwareService _hardwareService;
        private readonly SettingsService _settingsService;
        private readonly DispatcherTimer _timer;

        // --- 子ViewModel ---
        public BrightnessControlViewModel BrightnessControl { get; }
        public StarSelectionViewModel StarSelection { get; }
        public MotionControlViewModel RotationControl { get; } // 水平/转动
        public MotionControlViewModel RollControl { get; }     // 垂直/滚动

        public MainViewModel()
        {
            // 1. 初始化服务
            _hardwareService = new HardwareService();
            _settingsService = new SettingsService(); // 创建实例
            _hardwareService.InitializeDevices();

            // 2. 创建子ViewModel
            BrightnessControl = new BrightnessControlViewModel(_hardwareService);
            StarSelection = new StarSelectionViewModel(_hardwareService);
            RotationControl = new MotionControlViewModel("转动控制", _hardwareService, AxisType.Horizontal);
            RollControl = new MotionControlViewModel("滚动控制", _hardwareService, AxisType.Vertical);

            // 3. 订阅事件
            StarSelection.PropertyChanged += OnStarSelectionChanged;
            RotationControl.SettingsChanged += OnMotionControlSettingsChanged; // 订阅水平轴的保存事件
            RollControl.SettingsChanged += OnMotionControlSettingsChanged;     // 订阅垂直轴的保存事件

            // 4. 加载持久化设置
            LoadAllSettings();

            // 5. 启动定时器
            _timer = new DispatcherTimer { /* ... */ };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }
        private void OnStarSelectionChanged(object sender, PropertyChangedEventArgs e)
        {
            // 当选择的星点发生变化时，更新亮度计算器所需的星点大小
            if (e.PropertyName == nameof(StarSelectionViewModel.SelectedStar) && StarSelection.SelectedStar != null)
            {
                BrightnessControl.UpdateStarSize(StarSelection.SelectedStar.Size);
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            // --- 更新亮度读数 ---
            if (_hardwareService.IsStarlightConnected)
            {
                var d1Read = _hardwareService.Device1266.ReadDetectorValue();
                var d2Read = _hardwareService.Device0105.ReadDetectorValue();

                var d1Number = d1Read.Value * 27586.21;
                BrightnessControl.CurrentBrightnessReading1 = d1Number < 0.01 ? $"{d1Number:E2}" : $"{d1Number:F2}";
                BrightnessControl.CurrentBrightnessReading2 = $"{d2Read.Value * 202183822.7:F2}";
            }

            // --- 更新转台角度读数 ---
            if (_hardwareService.IsRotationConnected)
            {
                RotationControl.CurrentAngle = _hardwareService.DualAxisRotationDevice.getHorizentalRotationInDegree() + RotationControl.ZeroOffset;
                RollControl.CurrentAngle = _hardwareService.DualAxisRotationDevice.getVerticalRotationInDegree() + RollControl.ZeroOffset;
            }
        }

        /// <summary>
        /// 当任何一个运动控制面板请求保存设置时调用此方法。
        /// </summary>
        private void OnMotionControlSettingsChanged(object sender, EventArgs e)
        {
            SaveAllSettings();
        }


        /// <summary>
        /// 在程序启动时加载所有设置。
        /// </summary>
        private void LoadAllSettings()
        {
            var settings = _settingsService.LoadSettings();
            RotationControl.ZeroOffset = settings.HorizontalZeroOffset;
            RollControl.ZeroOffset = settings.VerticalZeroOffset;
        }

        /// <summary>
        /// 保存所有需要持久化的设置。
        /// </summary>
        private void SaveAllSettings()
        {
            var settings = new AppSettings
            {
                HorizontalZeroOffset = RotationControl.ZeroOffset,
                VerticalZeroOffset = RollControl.ZeroOffset
            };
            _settingsService.SaveSettings(settings);
        }

        // INotifyPropertyChanged implementation...
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}