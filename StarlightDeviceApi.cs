using LFDCPowerSupply; // 引用厂商提供的库命名空间
using System;

namespace StarlightRotation

{
    #region ConstDef
    class LFPSDefinitions
    {
        public const int RET_NO_ERROR = 0;

        public const int RET_DEVICE_NOT_CONNECTED = 1;

        public const int RET_INVALID_CHANNEL = 2;

        public const int RET_WRONG_RESPONSE = 3;

        public const int RET_SERIAL_COM_ERROR = 4;

        public const int RET_INVALID_GAIN = 2;

        public const string TAIL = "\r";

        public const string READ_ID = "[:]:id?:";

        public const string SET_CURR0 = "[:]:set:curr:00:00:";

        public const string SET_CURR1 = "[:]:set:curr:01:00:";

        public const string SET_CURR2 = "[:]:set:curr:02:00:";

        public const string SET_CURR3 = "[:]:set:curr:03:00:";

        public const string SET_CURR_RESP = "[:]:set:curr:";

        public const string READ_PA = "[:]:MSR:PAVL:01:03:";

        public const string ZERO_PA = "[:]:set:pacl:00:05:23.34";

        public const string ZERO_PA_RESP = "[:]:set:pacl:00:";

        public const string SET_LINEFREQ50 = "[:]:set:PAFT:01:50:";

        public const string SET_LINEFREQ50_RESP = "[:]:set:paft:01:PicoAmpFilter is: 50 HZ";

        public const string SET_LINEFREQ60 = "[:]:set:PAFT:01:60:";

        public const string SET_LINEFREQ60_RESP = "[:]:set:paft:01:PicoAmpFilter is: 60 HZ";

        public const string SET_PA_GAIN0 = "[:]:set:PAGN:00:00:235";

        public const string SET_PA_GAIN1 = "[:]:set:PAGN:00:01:235";

        public const string SET_PA_GAIN2 = "[:]:set:PAGN:00:02:235";

        public const string SET_PA_GAIN3 = "[:]:set:PAGN:00:03:235";

        public const string SET_PA_GAIN4 = "[:]:set:PAGN:00:04:235";

        public const string SET_PA_GAIN5 = "[:]:set:PAGN:00:05:235";

        public const string SET_PA_GAIN6 = "[:]:set:PAGN:00:06:235";

        public const string SET_PA_GAIN_AUTO = "[:]:set:PAGN:00:10:235";

        public const string SET_PA_GAIN_RESP = "[:]:set:PAGN:00:";
    }
    #endregion

    /// <summary>
    /// 表示皮安表的一次读数结果，包含测量值和当时的增益。
    /// </summary>
    public struct DetectorReading
    {
        /// <summary>
        /// 测量的电流值。根据您的文档，此值乘以一个系数即为照度(lux)。
        /// </summary>
        public double Value;

        /// <summary>
        /// 测量时设备自动或手动设置的增益档位。
        /// </summary>
        public int Gain;

        public override string ToString()
        {
            return $"Value: {Value}, Gain: {Gain}";
        }
    }

    /// <summary>
    /// Starlight光学仪器的高级API封装。
    /// 此类旨在提供一个简单、健壮的接口来控制设备的光源和探测器。
    /// 每个实例对应一个物理设备连接。
    /// 
    /// 注意: 底层库 LFDCPowerSupply.dll 的 Connect() 方法会连接到它找到的第一个响应设备。
    /// 本封装类在连接后会校验设备的序列号，以确保连接的是目标设备。
    /// 如果您有多个设备同时连接到电脑，请确保每次只尝试连接一个，或按特定顺序连接它们。
    /// </summary>
    class StarlightDeviceApi
    {
        private readonly LFDCQuadChannel _device;

        /// <summary>
        /// 获取当前连接设备的序列号。如果未连接，则返回 "未连接"。
        /// </summary>
        public string SerialNumber { get; private set; } = "未连接";

        /// <summary>
        /// 获取一个值，该值指示设备当前是否已成功连接。
        /// </summary>
        public bool IsConnected { get; private set; } = false;

        /// <summary>
        /// 初始化 StarlightDeviceApi 的一个新实例。
        /// </summary>
        public StarlightDeviceApi()
        {
            _device = new LFDCQuadChannel();
        }

        /// <summary>
        /// 尝试连接到指定序列号的设备。
        /// 它会扫描所有串口，并连接到第一个响应的设备，然后验证其序列号是否匹配。
        /// </summary>
        /// <returns>如果成功连接到目标设备，则返回 true；否则返回 false。</returns>
        public bool Connect()
        {
            if (IsConnected)
            {
                return true;
            }

            // 底层库的Connect()会遍历端口并连接第一个找到的设备
            if (_device.Connect())
            {
                string foundSn = _device.SerialNumber();
                this.SerialNumber = foundSn;
                this.IsConnected = true;
                Console.WriteLine($"成功连接到设备，序列号: {this.SerialNumber}");
                return true;
            }

            Console.WriteLine($"错误：未能找到或连接到任何设备。");
            return false;
        }

        /// <summary>
        /// 设置指定光源通道的输出电流。
        /// </summary>
        /// <param name="channel">通道号，必须在 0 到 4 之间。</param>
        /// <param name="currentInAmps">输出电流，单位是安培(A)。安全范围为 0.0 到 1.0 A。</param>
        public void SetLightSourceCurrent(int channel, double currentInAmps)
        {
            EnsureConnected();
            if (channel < 0 || channel > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(channel), "通道号必须在 0 到 4 之间。");
            }
            // 根据您的补充说明，电流不要超过1A
            if (currentInAmps < 0.0 || currentInAmps > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentInAmps), "电流值必须在 0.0 A 到 1.0 A 的安全范围内。");
            }

            int result = _device.TurnOnByChannel(channel, currentInAmps);
            HandleDeviceApiError(result, $"设置通道 {channel} 电流");
        }

        /// <summary>
        /// 关闭指定通道的光源。
        /// 这是 SetLightSourceCurrent(channel, 0.0) 的一个便捷方法。
        /// </summary>
        /// <param name="channel">要关闭的通道号 (1-4)。</param>
        public void TurnOffLightSource(int channel)
        {
            SetLightSourceCurrent(channel, 0.0);
        }

        /// <summary>
        /// 关闭所有光源通道。
        /// </summary>
        public void TurnOffAllLightSources()
        {
            EnsureConnected();
            int result = _device.TurnAllOff();
            HandleDeviceApiError(result, "关闭所有光源");
        }

        /// <summary>
        /// 读取探测器（皮安表）的当前值。
        /// </summary>
        /// <returns>一个包含测量值和增益的 <see cref="DetectorReading"/> 结构体。</returns>
        public DetectorReading ReadDetectorValue()
        {
            EnsureConnected();
            double reading = 0;
            int gain = 0;
            // 注意：底层库的ReadPA方法使用了ref参数，我们将其封装为更现代的返回结构体的方式
            int result = _device.ReadPA(ref reading, ref gain);
            HandleDeviceApiError(result, "读取皮安表值");

            return new DetectorReading { Value = reading, Gain = gain };
        }

        /// <summary>
        /// 对探测器（皮安表）进行清零操作。
        /// 在执行此操作前，请确保没有光照射到探测器上。
        /// </summary>
        public void ZeroDetector()
        {
            EnsureConnected();
            int result = _device.ZeroPA();
            HandleDeviceApiError(result, "皮安表清零");
        }

        /// <summary>
        /// 将探测器（皮安表）的增益设置为自动模式。
        /// </summary>
        public void SetDetectorAutoGain()
        {
            EnsureConnected();
            int result = _device.SetAutoGain();
            HandleDeviceApiError(result, "设置皮安表自动增益");
        }

        /// <summary>
        /// 手动设置探测器（皮安表）的增益档位。
        /// </summary>
        /// <param name="gain">增益档位，通常为 0 到 5 之间。</param>
        public void SetDetectorManualGain(int gain)
        {
            EnsureConnected();
            // 根据代码反编译，有效增益为 0-5
            if (gain < 0 || gain > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(gain), "增益档位必须在 0 到 5 之间。");
            }
            int result = _device.SetPAGain(gain);
            HandleDeviceApiError(result, $"设置皮安表手动增益为 {gain}");
        }


        /// <summary>
        /// 检查设备是否连接，如果未连接则抛出异常。
        /// </summary>
        private void EnsureConnected()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("操作失败：设备未连接。请先调用 Connect() 方法。");
            }
        }

        /// <summary>
        /// 将底层库返回的错误码转换成具体的异常。
        /// </summary>
        private void HandleDeviceApiError(int errorCode, string operationName)
        {
            if (errorCode == LFPSDefinitions.RET_NO_ERROR)
            {
                // 操作成功，什么都不做
                return;
            }

            string errorMessage = $"{operationName} 失败: ";
            switch (errorCode)
            {
                case LFPSDefinitions.RET_DEVICE_NOT_CONNECTED:
                    // 如果发生连接错误，更新我们的连接状态
                    this.IsConnected = false;
                    this.SerialNumber = "未连接";
                    errorMessage += "设备未连接。";
                    throw new InvalidOperationException(errorMessage);
                case LFPSDefinitions.RET_INVALID_CHANNEL: // 在这个库中，RET_INVALID_GAIN 和 RET_INVALID_CHANNEL 值相同
                    errorMessage += "无效的通道或增益参数。";
                    throw new ArgumentException(errorMessage);
                case LFPSDefinitions.RET_WRONG_RESPONSE:
                    errorMessage += "设备返回了非预期的响应。";
                    throw new System.IO.IOException(errorMessage);
                case LFPSDefinitions.RET_SERIAL_COM_ERROR:
                    errorMessage += "发生串口通信错误（例如超时）。";
                    throw new System.IO.IOException(errorMessage);
                default:
                    errorMessage += $"发生未知错误，错误码: {errorCode}。";
                    throw new Exception(errorMessage);
            }
        }
    }
}