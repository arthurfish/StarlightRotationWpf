using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public static class FilterWheelApi
{
    private const string DllName = "FilterWheel102_win32.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int GetPorts(StringBuilder serialNoBuffer);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int Open(string serialNo, int nBaud, int timeout);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Close(int hdl);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetPosition(int hdl, out int pos);

    [DllImport(DllName, EntryPoint = "GetPositionCount", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetDevicePositionCount(int hdl, out int poscount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetPosition(int hdl, int pos);

    // --- Added functions based on fw_cmd_library.h ---

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetTimeout(int hdl, int timeout);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetPositionCount(int hdl, int count);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetSpeed(int hdl, int speed);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetTriggerMode(int hdl, int mode);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetMinVelocity(int hdl, int min);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetMaxVelocity(int hdl, int max);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetAcceleration(int hdl, int acceleration);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetSensorMode(int hdl, int mode);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Save(int hdl);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetSpeed(int hdl, out int speed);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetTriggerMode(int hdl, out int triggermode);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetMinVelocity(int hdl, out int minvelocity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetMaxVelocity(int hdl, out int maxvelocity);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetAcceleration(int hdl, out int acceleration);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetSensorMode(int hdl, out int sensormode);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int GetId(int hdl, StringBuilder idBuffer);

    // Helper to get error message string
    public static string GetErrorMessage(int errorCode)
    {
        return errorCode switch
        {
            0 => "Success",
            0xEA => "Command not defined (0xEA)",
            0xEB => "Timeout (0xEB)",
            0xED => "Invalid string buffer (0xED)",
            _ => $"Device error code: {errorCode} (0x{errorCode:X2})"
        };
    }

    private const int BAUD_RATE = 115200;
    private const int TIMEOUT_S = 5;
    private const int SERIAL_BUFFER_SIZE = 256;

    private static void ShowError(string s)
    {
        Trace.WriteLine(s);
    }


    public static int InitializeDeviceAndGetHandle(string _deviceSerialNumber)
    {
        Trace.WriteLine("Wheel: connecting....");
        try
        {
            StringBuilder snBuffer = new StringBuilder(256);
            int numDevices = FilterWheelApi.GetPorts(snBuffer);

            if (numDevices <= 0)
            {
                string errorMsg = numDevices < 0 ? $"Error getting ports (Code: {FilterWheelApi.GetErrorMessage(numDevices)})." : "No devices found.";
                ShowError(errorMsg + " Ensure device is connected and drivers are installed.");
                return -1;
            }

            string portList = snBuffer.ToString();
            Trace.WriteLine($"Wheel: PortList: {portList}");
//            _deviceSerialNumber = portList.Split(',').FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrEmpty(_deviceSerialNumber))
            {
                ShowError("Could not parse serial number from device list.");
                return -1;
            }

            var _deviceHandle = FilterWheelApi.Open(_deviceSerialNumber, BAUD_RATE, TIMEOUT_S);

            if (_deviceHandle < 0)
            {
                ShowError($"Failed to open port {_deviceSerialNumber} ({FilterWheelApi.GetErrorMessage(_deviceHandle)}). Check connection and ensure DLL is present.");
                _deviceHandle = -1;
                return -1;
            }

            Trace.WriteLine($"Connected to {_deviceSerialNumber}. Handle: {_deviceHandle}.");


            int positionCount;
            int result = FilterWheelApi.GetDevicePositionCount(_deviceHandle, out positionCount);

            if (result != 0)
            {
                ShowError($"Failed to get position count: {FilterWheelApi.GetErrorMessage(result)}");
                // Don't CleanupDevice here, port is open, but critical info missing. User might want to check settings.
                return -1; // Keep connected but indicate error
            }

            if (positionCount <= 0)
            {
                ShowError($"Device reported invalid position count: {positionCount}. Check Settings.");
                return -1; // Keep connected but indicate error
            }

            Trace.WriteLine($"Wheel: 设备已连接: {_deviceSerialNumber}. 当前位置: {positionCount}.");
            return _deviceHandle;
        }
        catch (DllNotFoundException)
        {
            ShowError($"Critical Error: FilterWheel102_win32.dll not found. Ensure it's in the application directory ({AppDomain.CurrentDomain.BaseDirectory}) and the application is running as x86.");
            return -1;
        }
        catch (Exception ex)
        {
            ShowError($"An unexpected error occurred during initialization: {ex.Message}");
            return -1;
        }
    }
}