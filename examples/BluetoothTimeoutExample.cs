using DeviceLink.Device.ConST171A;
using DeviceLink.Transport;
using System;
using System.Threading.Tasks;

namespace DeviceLink.Examples
{
    /// <summary>
    /// 蓝牙超时配置示例
    /// 解决蓝牙通讯超时问题
    /// </summary>
    public class BluetoothTimeoutExample
    {
        public static async Task Main(string[] args)
        {
            // 配置蓝牙选项，增加超时时间
            var bluetoothOptions = new BluetoothOptions
            {
                DeviceAddress = "68:0a:e2:de:a5:2e",
                ServiceUuid = InTheHand.Net.Bluetooth.BluetoothService.SerialPort,
                ConnectTimeoutMs = 15000,  // 连接超时 15秒
                AutoPair = false,          // 已配对，不需要自动配对
            };

            // 创建蓝牙设置
            var settings = new BluetoothSettings
            {
                BluetoothOptions = bluetoothOptions,
                // 增加数据链路层超时
                ReceiveTimeoutMs = 10000,      // 接收超时 10秒
                ReceiveIdleTimeoutMs = 100,    // 空闲超时 100ms
                MaxRetryCount = 2,             // 重试2次
                RetryDelayMs = 500,            // 重试延迟 500ms
            };

            // 创建设备
            var device = new ConST171Base(settings);

            try
            {
                Console.WriteLine("正在连接蓝牙设备...");
                await device.OpenAsync();
                Console.WriteLine("蓝牙设备已连接");

                // 读取压力值
                Console.WriteLine("正在读取压力值...");
                var pressure = await device.GetPressureAsync();
                Console.WriteLine($"压力值: {pressure}");

                // 读取设备标识
                Console.WriteLine("正在读取设备标识...");
                var idn = await device.GetIdentificationAsync();
                Console.WriteLine($"设备标识: {idn}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
            }
            finally
            {
                await device.CloseAsync();
                Console.WriteLine("蓝牙连接已关闭");
            }
        }
    }
}