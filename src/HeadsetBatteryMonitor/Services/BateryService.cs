﻿using System.Globalization;
using System.Reflection;
using HeadsetBatteryMonitor.Models;
using HidApiAdapter;

namespace HeadsetBatteryMonitor.Services
{
    public class BateryService
    {
        private const int VoidBatteryMicup = 128;
        private static byte[] _dataReq = {0xC9, 0x64};

        public Device? Device { get; private set; }
        public int Pid => int.Parse(Device?.ProductId ?? "0", NumberStyles.HexNumber);
        public int Vid => int.Parse(Device?.VendorId ?? "0", NumberStyles.HexNumber);

        private int?[]? LastValues { get; set; }

        private const int FilterLength = 25;

        private HidDevice? _device;
        private IntPtr _devPtr;

        private HidDevice? GetHidDevice()
        {
            var devices = HidDeviceManager.GetManager().SearchDevices(Vid, Pid);
            foreach (var dev in devices)
            {
                if (dev.Path().Contains("col02"))
                {
                    return dev;
                }
            }

            if (devices.Count > 0)
                return devices.FirstOrDefault();
            return null;
        }

        public Task<byte[]?> GetBatteryStatusViaHid()
        {
            return Task.Run(() =>
            {
                _device = GetHidDevice();

                if (_device != null)
                {
                    _device.Connect();

                    //get handle via reflection, because its a private field (oof)
                    var field = typeof(HidDevice).GetField("m_DevicePtr", BindingFlags.NonPublic | BindingFlags.Instance);
                    _devPtr = (IntPtr)field?.GetValue(_device)!;

                    var buffer = new byte[5];
                    HidApi.hid_write(_devPtr, _dataReq, Convert.ToUInt32(_dataReq.Length));
                    HidApi.hid_read_timeout(_devPtr, buffer, Convert.ToUInt32(buffer.Length), 1000);
                    _device.Disconnect();
                    Thread.Sleep(250);
                    return buffer;
                }
                else
                {
                    return null;
                }
            });
        }

        private void HandleReport(byte[]? data)
        {
            if (data == null) return;
            try
            {
                // Charging
                if (data[4] == 0 || data[4] == 4 || data[4] == 5)
                {
                    Value = -2;
                    this.LastValues = new int?[FilterLength];
                    return;
                }

                // MicUp
                if (data[2] > VoidBatteryMicup)
                {
                    Value = (data[2] - VoidBatteryMicup);
                    return;
                }

                Value = (data[2]);
            }
            catch
            {
                Value = -1;
            }
        }

        private bool _running = true;

        public async void StartAsync(Device? device)
        {
            Device = device;
            while (_running)
            {
                var buffer = await GetBatteryStatusViaHid();
                HandleReport(buffer);
            }
        }

        public async void StopAsync()
        {
            _running = false;
            await Task.CompletedTask;
        }

        private decimal _value;

        public decimal Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    ValueChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        public event EventHandler? ValueChanged;
    }
}
