// HardwareService.cs
using System;
using System.Diagnostics;
using StarlightRotation; // 假设你的API类在这个命名空间下

namespace StarlightRotationWpf
{
    public class HardwareService
    {
        // 公开设备实例，以便ViewModel可以访问
        public StarlightDeviceApi Device0105 { get; private set; }
        public StarlightDeviceApi Device1266 { get; private set; }
        public DualAxisRotationDeviceApi DualAxisRotationDevice { get; private set; }
        public int FilterWheelHandle { get; private set; }

        public bool IsStarlightConnected => Device0105?.IsConnected == true && Device1266?.IsConnected == true;
        public bool IsRotationConnected => DualAxisRotationDevice?.isConnected == true;

        public HardwareService()
        {
            Device0105 = new StarlightDeviceApi();
            Device1266 = new StarlightDeviceApi();
            DualAxisRotationDevice = new DualAxisRotationDeviceApi();
        }

        public void InitializeDevices()
        {
            // --- 初始化滤光轮 ---
            Trace.WriteLine("Preparing Wheel....");
            FilterWheelHandle = FilterWheelApi.InitializeDeviceAndGetHandle("COM6");

            // --- 初始化星光模拟器 ---
            Device0105.Connect();
            Device1266.Connect();

            if (Device0105.SerialNumber == "1266" && Device1266.SerialNumber == "0105")
            {
                (Device0105, Device1266) = (Device1266, Device0105);
            }

            if (Device0105.SerialNumber != "0105" || Device1266.SerialNumber != "1266")
            {
                Trace.WriteLine("无法连接到星光模拟器！");
                // 可以在这里抛出异常或设置错误状态
            }
            else
            {
                Device1266.SetLightSourceCurrent(1, 0);
                Device1266.SetLightSourceCurrent(2, 0);
            }

            // --- 初始化转台 ---
            // DualAxisRotationDevice 的连接逻辑可能在它的构造函数或单独的方法里，这里假设它已处理
        }
    }
}