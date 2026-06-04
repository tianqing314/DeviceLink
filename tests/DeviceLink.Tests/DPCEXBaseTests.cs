using DeviceLink.Device.ConST326EX;
using DeviceLink.Tests.Helpers;
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
            var dPCEX = new DPCEXBase("COM4", 9600, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None);
            return (dPCEX, settings);
        }
        [Fact]
        public async Task GetVersion()
        {
            // Arrange
            var (dPCEX, settings) = CreateTestDevice();
            await dPCEX.OpenAsync();

            // Act
            var pressure = await dPCEX.GetVersion(Device.ConST326EX.Enums.VersionType.APPLication);
        }
    }
}
