using DeviceLink.Device.ConST326EX;
using DeviceLink.Tests.Helpers;
using DeviceLink.Transport;
using System.Threading.Tasks;
using Xunit;

namespace DeviceLink.Tests
{
    public class DPCEXBaseTests
    {
        /// <summary>
        /// 创建测试用 DPCEXBase 实例和配套的 LoopbackSettings
        /// </summary>
        /// <returns>(dpsex, settings) 元组</returns>
        private (DPCEXBase dpsex, LoopbackSettings settings) CreateTestDevice()
        {
            var settings = new LoopbackSettings();
            // 必须把 settings 交给设备 —— 原来这里建了 LoopbackSettings 却用蓝牙构造，
            // 用例实际在连真实硬件，无蓝牙适配器的机器上必然失败。
            var dPCEX = new DPCEXBase(settings);
            return (dPCEX, settings);
        }
        [Fact]
        public async Task GetVersion()
        {
            // Arrange
            var (dPCEX, settings) = CreateTestDevice();

            // 设置回环响应 - 模拟设备返回版本信息
            settings.Transport.OnSend += data =>
            {
                // 模拟 SCPI 响应
                var response = System.Text.Encoding.ASCII.GetBytes("ConST326EX V1.0.0\r\n");
                settings.Transport.EnqueueReceive(response);
            };

            await dPCEX.OpenAsync();

            // Act
            var version = await dPCEX.GetVersionAsync(Device.ConST326EX.Enums.VersionType.APPLication);

            // Assert
            Assert.NotNull(version);
        }
    }
}
