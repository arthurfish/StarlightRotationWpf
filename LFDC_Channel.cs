using StarlightRotation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFDCPowerSupply;

public class LFDCQuadChannel
{
    private string expectedID = string.Empty;

    private SerialPort serialPort;

    private bool connected = false;

    private int baudRate = 115200;

    private string serialNumber = string.Empty;

    public bool IsConnected()
    {
        return connected;
    }

    public string SerialNumber()
    {
        if (connected)
        {
            return serialNumber;
        }

        return "未连接";
    }

    public bool Connect()
    {
        if (connected)
        {
            return true;
        }

        string[] portNames = SerialPort.GetPortNames();
        foreach (string portName in portNames)
        {
            serialPort = new SerialPort(portName, baudRate);
            try
            {
                serialPort.Open();
            }
            catch
            {
                continue;
            }

            if (!serialPort.IsOpen)
            {
                serialPort = null;
                continue;
            }

            serialPort.WriteTimeout = 200;
            serialPort.ReadTimeout = 200;
            try
            {
                serialNumber = RetrieveSerialNumber();
                if (serialNumber != string.Empty)
                {
                    connected = true;
                    return true;
                }
            }
            catch
            {
                serialPort.Close();
                serialPort.Dispose();
                connected = false;
            }
        }

        return false;
    }

    private string RetrieveSerialNumber()
    {
        if (serialPort.IsOpen)
        {
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();
            serialPort.WriteLine(LFPSDefinitions.READ_ID + LFPSDefinitions.TAIL);
            Thread.Sleep(100);
            string text = serialPort.ReadLine();
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();
            int num = text.LastIndexOf(":");
            return text.Substring(text.Length - 4);
        }

        return string.Empty;
    }

    public int ZeroPA()
    {
        if (serialPort == null)
        {
            return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
        }

        if (serialPort.IsOpen)
        {
            serialPort.ReadTimeout = 30000;
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();
            serialPort.WriteLine(LFPSDefinitions.ZERO_PA + LFPSDefinitions.TAIL);
            Thread.Sleep(100);
            try
            {
                string text = serialPort.ReadLine();
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                serialPort.ReadTimeout = 200;
                if (text.Contains(LFPSDefinitions.ZERO_PA_RESP))
                {
                    return LFPSDefinitions.RET_NO_ERROR;
                }

                return LFPSDefinitions.RET_WRONG_RESPONSE;
            }
            catch (Exception)
            {
                return LFPSDefinitions.RET_SERIAL_COM_ERROR;
            }
        }

        return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
    }

    public int TurnOnByChannel(int ch, double milliAmpere)
    {
        if (serialPort == null)
        {
            return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
        }

        if (ch < 1 || ch > 4)
        {
            return LFPSDefinitions.RET_INVALID_CHANNEL;
        }

        if (serialPort.IsOpen)
        {
            try
            {
                string text = LFPSDefinitions.SET_CURR0 + milliAmpere.ToString("F14") + LFPSDefinitions.TAIL;
                switch (ch)
                {
                    case 1:
                        text = LFPSDefinitions.SET_CURR0 + milliAmpere.ToString("F14") + LFPSDefinitions.TAIL;
                        break;
                    case 2:
                        text = LFPSDefinitions.SET_CURR1 + milliAmpere.ToString("F14") + LFPSDefinitions.TAIL;
                        break;
                    case 3:
                        text = LFPSDefinitions.SET_CURR2 + milliAmpere.ToString("F14") + LFPSDefinitions.TAIL;
                        break;
                    case 4:
                        text = LFPSDefinitions.SET_CURR3 + milliAmpere.ToString("F14") + LFPSDefinitions.TAIL;
                        break;
                    default:
                        return LFPSDefinitions.RET_INVALID_CHANNEL;
                }

                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                serialPort.WriteLine(text);
                Debug.WriteLine(text);
                Thread.Sleep(100);
                text = serialPort.ReadLine();
                if (text.Contains(LFPSDefinitions.SET_CURR_RESP))
                {
                    return LFPSDefinitions.RET_NO_ERROR;
                }

                return LFPSDefinitions.RET_WRONG_RESPONSE;
            }
            catch
            {
                return LFPSDefinitions.RET_SERIAL_COM_ERROR;
            }
        }

        return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
    }

    public int TurnOnByChannel(int ch, double amps, int rampTime)
    {
        if (serialPort == null)
        {
            return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
        }

        if (ch < 1 || ch > 4)
        {
            return LFPSDefinitions.RET_INVALID_CHANNEL;
        }

        if (serialPort.IsOpen)
        {
            try
            {
                DateTime now = DateTime.Now;
                DateTime dateTime = DateTime.Now.AddMilliseconds(rampTime);
                while (DateTime.Now < dateTime)
                {
                    double num = amps * (DateTime.Now - now).TotalMilliseconds / (double)rampTime;
                    if (num < 0.1)
                    {
                        num = 0.1;
                    }

                    if (num > amps)
                    {
                        num = amps;
                    }

                    Debug.WriteLine(num.ToString("F2"));
                    int num2 = TurnOnByChannel(ch, num);
                    if (num2 != LFPSDefinitions.RET_NO_ERROR)
                    {
                        TurnOffByChannel(ch);
                        return num2;
                    }
                }

                int num3 = TurnOnByChannel(ch, amps);
                if (num3 != LFPSDefinitions.RET_NO_ERROR)
                {
                    TurnOffByChannel(ch);
                    return num3;
                }

                return num3;
            }
            catch
            {
                return LFPSDefinitions.RET_SERIAL_COM_ERROR;
            }
        }

        return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
    }

    public int TurnOnByChannelInParallel(double amps, int rampTime)
    {
        amps /= 2.0;
        if (serialPort == null)
        {
            return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
        }

        if (serialPort.IsOpen)
        {
            try
            {
                DateTime now = DateTime.Now;
                DateTime dateTime = DateTime.Now.AddMilliseconds(rampTime);
                while (DateTime.Now < dateTime)
                {
                    double num = amps * (DateTime.Now - now).TotalMilliseconds / (double)rampTime;
                    if (num < 0.1)
                    {
                        num = 0.1;
                    }

                    if (num > amps)
                    {
                        num = amps;
                    }

                    int num2 = TurnOnByChannel(1, num);
                    if (num2 != LFPSDefinitions.RET_NO_ERROR)
                    {
                        TurnOffByChannel(1);
                        return num2;
                    }
                }

                int num3 = TurnOnByChannel(1, amps);
                if (num3 != LFPSDefinitions.RET_NO_ERROR)
                {
                    TurnOffByChannel(1);
                    return num3;
                }

                now = DateTime.Now;
                dateTime = DateTime.Now.AddMilliseconds(rampTime);
                while (DateTime.Now < dateTime)
                {
                    double num4 = amps * (DateTime.Now - now).TotalMilliseconds / (double)rampTime;
                    if (num4 < 0.1)
                    {
                        num4 = 0.1;
                    }

                    if (num4 > amps)
                    {
                        num4 = amps;
                    }

                    int num5 = TurnOnByChannel(2, num4);
                    if (num5 != LFPSDefinitions.RET_NO_ERROR)
                    {
                        TurnOffByChannel(2);
                        return num5;
                    }
                }

                num3 = TurnOnByChannel(2, amps);
                if (num3 != LFPSDefinitions.RET_NO_ERROR)
                {
                    TurnOffByChannel(2);
                    return num3;
                }

                return num3;
            }
            catch
            {
                return LFPSDefinitions.RET_SERIAL_COM_ERROR;
            }
        }

        return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
    }

    public int TurnAllOn(double amps0, double amps1, double amps2, double amps3)
    {
        amps0 *= 1000.0;
        amps1 *= 1000.0;
        amps2 *= 1000.0;
        amps3 *= 1000.0;
        if (serialPort == null)
        {
            return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
        }

        if (serialPort.IsOpen)
        {
            try
            {
                string text = LFPSDefinitions.SET_CURR0 + amps0 + LFPSDefinitions.TAIL;
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                serialPort.WriteLine(text);
                Thread.Sleep(100);
                text = serialPort.ReadLine();
                if (!text.Contains(LFPSDefinitions.SET_CURR_RESP))
                {
                    return LFPSDefinitions.RET_WRONG_RESPONSE;
                }

                text = LFPSDefinitions.SET_CURR1 + amps1 + LFPSDefinitions.TAIL;
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                serialPort.WriteLine(text);
                Thread.Sleep(100);
                text = serialPort.ReadLine();
                if (!text.Contains(LFPSDefinitions.SET_CURR_RESP))
                {
                    return LFPSDefinitions.RET_WRONG_RESPONSE;
                }

                return LFPSDefinitions.RET_NO_ERROR;
            }
            catch
            {
                return LFPSDefinitions.RET_SERIAL_COM_ERROR;
            }
        }

        return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
    }

    public int SetLineFrequency(bool Line50Hz)
    {
        if (serialPort == null)
        {
            return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
        }

        if (serialPort.IsOpen)
        {
            try
            {
                if (Line50Hz)
                {
                    serialPort.Write(LFPSDefinitions.SET_LINEFREQ50);
                }
                else
                {
                    serialPort.Write(LFPSDefinitions.SET_LINEFREQ60);
                }

                Thread.Sleep(100);
                string text = serialPort.ReadLine();
                if (Line50Hz)
                {
                    if (text == LFPSDefinitions.SET_LINEFREQ50_RESP)
                    {
                        return LFPSDefinitions.RET_NO_ERROR;
                    }

                    return LFPSDefinitions.RET_WRONG_RESPONSE;
                }

                if (text == LFPSDefinitions.SET_LINEFREQ60_RESP)
                {
                    return LFPSDefinitions.RET_NO_ERROR;
                }

                return LFPSDefinitions.RET_WRONG_RESPONSE;
            }
            catch
            {
                return LFPSDefinitions.RET_SERIAL_COM_ERROR;
            }
        }

        return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
    }

    public int ReadPA(ref double reading, ref int gain)
    {
        if (serialPort == null)
        {
            return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
        }

        if (serialPort.IsOpen)
        {
            try
            {
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                serialPort.WriteLine(LFPSDefinitions.READ_PA + LFPSDefinitions.TAIL);
                Thread.Sleep(100);
                string[] array = serialPort.ReadLine().Split(new char[1] { ':' });
                try
                {
                    reading = Convert.ToDouble(array[^1]);
                    gain = Convert.ToInt32(array[^2]);
                    return LFPSDefinitions.RET_NO_ERROR;
                }
                catch
                {
                    return LFPSDefinitions.RET_WRONG_RESPONSE;
                }
            }
            catch
            {
                return LFPSDefinitions.RET_SERIAL_COM_ERROR;
            }
        }

        return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
    }

    public int TurnAllOff()
    {
        return TurnAllOn(0.0, 0.0, 0.0, 0.0);
    }

    public int TurnOffByChannel(int ch)
    {
        return TurnOnByChannel(ch, 0.0);
    }

    public int TurnOffByChannelInParallel()
    {
        int num = TurnOnByChannel(1, 0.0);
        return TurnOnByChannel(2, 0.0);
    }

    public int SetPAGain(int gain)
    {
        if (serialPort == null)
        {
            return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
        }

        if (gain < 0 || gain > 5)
        {
            return LFPSDefinitions.RET_INVALID_GAIN;
        }

        if (serialPort.IsOpen)
        {
            try
            {
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                string empty = string.Empty;
                switch (gain)
                {
                    case 0:
                        empty = LFPSDefinitions.SET_PA_GAIN0;
                        break;
                    case 1:
                        empty = LFPSDefinitions.SET_PA_GAIN1;
                        break;
                    case 2:
                        empty = LFPSDefinitions.SET_PA_GAIN2;
                        break;
                    case 3:
                        empty = LFPSDefinitions.SET_PA_GAIN3;
                        break;
                    case 4:
                        empty = LFPSDefinitions.SET_PA_GAIN4;
                        break;
                    case 5:
                        empty = LFPSDefinitions.SET_PA_GAIN5;
                        break;
                    default:
                        return LFPSDefinitions.RET_INVALID_GAIN;
                }

                serialPort.WriteLine(empty + LFPSDefinitions.TAIL);
                Thread.Sleep(100);
                empty = serialPort.ReadLine();
                if (empty.Contains(LFPSDefinitions.SET_PA_GAIN_RESP))
                {
                    return LFPSDefinitions.RET_NO_ERROR;
                }

                return LFPSDefinitions.RET_WRONG_RESPONSE;
            }
            catch
            {
                return LFPSDefinitions.RET_SERIAL_COM_ERROR;
            }
        }

        return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
    }

    public int SetAutoGain()
    {
        if (serialPort == null)
        {
            return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
        }

        if (serialPort.IsOpen)
        {
            try
            {
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                string sET_PA_GAIN_AUTO = LFPSDefinitions.SET_PA_GAIN_AUTO;
                serialPort.WriteLine(sET_PA_GAIN_AUTO + LFPSDefinitions.TAIL);
                Thread.Sleep(100);
                sET_PA_GAIN_AUTO = serialPort.ReadLine();
                if (sET_PA_GAIN_AUTO.Contains(LFPSDefinitions.SET_PA_GAIN_RESP))
                {
                    return LFPSDefinitions.RET_NO_ERROR;
                }

                return LFPSDefinitions.RET_WRONG_RESPONSE;
            }
            catch
            {
                return LFPSDefinitions.RET_SERIAL_COM_ERROR;
            }
        }

        return LFPSDefinitions.RET_DEVICE_NOT_CONNECTED;
    }
}