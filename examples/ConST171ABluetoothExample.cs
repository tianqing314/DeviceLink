using DeviceLink.Device.ConST171A;
using DeviceLink.DeviceBase;
using DeviceLink.Transport;
using System;
using System.Threading.Tasks;

namespace DeviceLink.Examples
{
    /// <summary>
    /// ConST171A 蓝牙通讯示例
    /// 
    /// 本示例展示如何通过蓝牙连接 ConST171A 压力控制器设备。
    /// 蓝牙设备地址：68:0a:e2:de:a5:2e
    /// </summary>
    public class ConST171ABluetoothExample
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== ConST171A 蓝牙通讯示例 ===\n");

            // 配置蓝牙选项
            var bluetoothOptions = new BluetoothOptions
            {
                DeviceAddress = "68:0a:e2:de:a5:2e",
                ServiceUuid = InTheHand.Net.Bluetooth.BluetoothService.SerialPort,
                ConnectTimeoutMs = 15000,  // 连接超时 15秒
                AutoPair = false,          // 已配对，不需要自动配对
            };

            // 创建蓝牙设置，增加超时时间
            var settings = new BluetoothSettings
            {
                BluetoothOptions = bluetoothOptions,
                ReceiveTimeoutMs = 10000,      // 接收超时 10秒
                ReceiveIdleTimeoutMs = 100,    // 空闲超时 100ms
                MaxRetryCount = 2,             // 重试2次
                RetryDelayMs = 500,            // 重试延迟 500ms
            };

            // 创建设备
            using var device = new ConST171Base(settings);

            try
            {
                Console.WriteLine("正在连接蓝牙设备...");
                await device.OpenAsync();
                Console.WriteLine("✓ 蓝牙设备已连接\n");

                // 读取设备标识
                Console.WriteLine("正在读取设备标识...");
                var idn = await device.GetIdentificationAsync();
                Console.WriteLine($"✓ 设备标识: {idn.Manufacturer} {idn.Model}");
                Console.WriteLine($"  序列号: {idn.SerialNumber}");
                Console.WriteLine($"  固件版本: {idn.FirmwareVersion}\n");

                // 读取序列号
                Console.WriteLine("正在读取序列号...");
                var serialNumber = await device.GetSerialNumberAsync();
                Console.WriteLine($"✓ 设备序列号: {serialNumber}\n");

                // 读取型号
                Console.WriteLine("正在读取设备型号...");
                var model = await device.GetModelAsync();
                Console.WriteLine($"✓ 设备型号: {model}\n");

                // 读取压力值
                Console.WriteLine("正在读取压力值...");
                var pressure = await device.GetPressureAsync();
                Console.WriteLine($"✓ 压力值:");
                Console.WriteLine($"  正压: {pressure.PositiveValue:F3} {pressure.PositiveUnit}");
                Console.WriteLine($"  真空: {pressure.VacuumValue:F3} {pressure.VacuumUnit}\n");

                // 读取正压值
                Console.WriteLine("正在读取正压值...");
                var positivePressure = await device.GetPressureAsync(SourceModule.Pressure);
                Console.WriteLine($"✓ 正压值: {positivePressure.Value:F3} {positivePressure.Unit}\n");

                // 读取真空值
                Console.WriteLine("正在读取真空值...");
                var vacuumPressure = await device.GetPressureAsync(SourceModule.Vacuum);
                Console.WriteLine($"✓ 真空值: {vacuumPressure.Value:F3} {vacuumPressure.Unit}\n");

                // 读取压力单位
                Console.WriteLine("正在读取压力单位...");
                var pressureUnit = await device.GetPressureUnitAsync(SourceModule.Pressure);
                var vacuumUnit = await device.GetPressureUnitAsync(SourceModule.Vacuum);
                Console.WriteLine($"✓ 压力单位:");
                Console.WriteLine($"  正压单位: {pressureUnit}");
                Console.WriteLine($"  真空单位: {vacuumUnit}\n");

                // 读取压力范围
                Console.WriteLine("正在读取压力范围...");
                var range = await device.GetPressureRangeAsync(SourceModule.Pressure);
                Console.WriteLine($"✓ 压力范围: {range.Min:F3} - {range.Max:F3}\n");

                Console.WriteLine("=== 所有操作完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"  内部错误: {ex.InnerException.Message}");
                }
            }
            finally
            {
                await device.CloseAsync();
                Console.WriteLine("\n蓝牙连接已关闭");
            }
        }
    }
}