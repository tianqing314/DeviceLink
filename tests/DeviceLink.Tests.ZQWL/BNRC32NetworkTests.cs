using DeviceLink.Device.ZQWL;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace DeviceLink.Tests.ZQWL
{
    /// <summary>
    /// BNRC32 网络继电器控制器（32路）网口通讯集成测试
    /// 需要实际的 BNRC32 设备连接到网络
    /// 
    /// 使用方法：
    /// 1. 设置环境变量 BNRC32_IP 和 BNRC32_PORT（可选，默认端口 5000）
    /// 2. 运行测试：dotnet test --filter "Category=Network"
    /// 
    /// 示例：
    ///   set BNRC32_IP=192.168.1.100
    ///   set BNRC32_PORT=5000
    ///   dotnet test --filter "Category=Network"
    /// </summary>
    [Trait("Category", "Network")]
    public class BNRC32NetworkTests
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly byte _address;

        public BNRC32NetworkTests()
        {
            // 从环境变量读取配置
            _ipAddress = Environment.GetEnvironmentVariable("BNRC32_IP") ?? "192.168.45.103";
            _port = int.TryParse(Environment.GetEnvironmentVariable("BNRC32_PORT"), out int port) ? port : 1030;
            _address = 1; // 默认设备地址
        }

        #region 连接测试

        [Fact]
        public async Task ConnectAsync_ShouldSucceed()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);

            // Act
            await device.OpenAsync();

            // Assert
            Assert.True(device.IsOpen);

            // Cleanup
            await device.CloseAsync();
        }

        [Fact]
        public async Task DisconnectAsync_ShouldSucceed()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);
            await device.OpenAsync();

            // Act
            await device.CloseAsync();

            // Assert
            Assert.False(device.IsOpen);
        }

        #endregion

        #region 设备信息测试

        [Fact]
        public async Task GetVersionAsync_ShouldReturnNonEmptyString()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);
            await device.OpenAsync();

            // Act
            string version = await device.GetVersionAsync();

            // Assert
            Assert.False(string.IsNullOrEmpty(version));
            Assert.Contains("BN", version); // 版本号应包含 "BN"

            // Cleanup
            await device.CloseAsync();
        }

        [Fact]
        public async Task IsExistAsync_ShouldReturnTrue()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);
            await device.OpenAsync();

            // Act
            bool exists = await device.IsExistAsync();

            // Assert
            Assert.True(exists);

            // Cleanup
            await device.CloseAsync();
        }

        #endregion

        #region 输出控制测试

        [Fact]
        public async Task SetOutputAsync_Channel1_ShouldSucceed()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);
            await device.OpenAsync();

            // Act & Assert - 不应抛出异常
            await device.SetOutputAsync(1, true);
            await Task.Delay(100); // 等待状态稳定

            bool state = await device.GetOutputAsync(1);
            Assert.True(state, "设置输出后，读取状态应为 true");

            // Cleanup - 关闭输出
            await device.SetOutputAsync(1, false);
            await device.CloseAsync();
        }

        [Fact]
        public async Task SetOutputAsync_Channel32_ShouldSucceed()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);
            await device.OpenAsync();

            // Act & Assert - 不应抛出异常
            await device.SetOutputAsync(32, true);
            await Task.Delay(100); // 等待状态稳定

            // 验证状态
            bool state = await device.GetOutputAsync(32);
            Assert.True(state);

            // Cleanup - 关闭输出
            await device.SetOutputAsync(32, false);
            await device.CloseAsync();
        }

        [Fact]
        public async Task CloseAllAsync_ShouldTurnOffAllOutputs()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);
            await device.OpenAsync();

            // 先打开一些输出
            await device.SetOutputAsync(1, true);
            await device.SetOutputAsync(2, true);
            await Task.Delay(100);

            // Act
            await device.CloseAllAsync();
            await Task.Delay(100);

            // Assert - 检查所有输出是否关闭
            var statuses = await device.GetAllStatusesAsync();
            Assert.All(statuses, state => Assert.False(state));

            // Cleanup
            await device.CloseAsync();
        }

        [Fact]
        public async Task OpenAllAsync_ShouldTurnOnAllOutputs()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);
            await device.OpenAsync();

            // 先关闭所有输出
            await device.CloseAllAsync();
            await Task.Delay(100);

            // Act
            await device.OpenAllAsync();
            await Task.Delay(100);

            // Assert - 检查所有输出是否打开
            var statuses = await device.GetAllStatusesAsync();
            Assert.All(statuses, state => Assert.True(state));

            // Cleanup - 关闭所有输出
            await device.CloseAllAsync();
            await device.CloseAsync();
        }

        #endregion

        #region 输入读取测试

        [Fact]
        public async Task GetInputAsync_Channel1_ShouldReturnBooleanValue()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);
            await device.OpenAsync();

            // Act
            bool inputState = await device.GetInputAsync(1);

            // Assert - 只验证返回值类型，不验证具体值（因为不知道实际输入状态）
            Assert.IsType<bool>(inputState);

            // Cleanup
            await device.CloseAsync();
        }

        [Fact]
        public async Task GetAllStatusesAsync_ShouldReturn32States()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);
            await device.OpenAsync();

            // Act
            var statuses = await device.GetAllStatusesAsync();

            // Assert
            Assert.NotNull(statuses);
            Assert.Equal(32, statuses.Count);

            // Cleanup
            await device.CloseAsync();
        }

        #endregion

        #region 模拟量读取测试

        [Fact]
        public async Task GetAnalogInputAsync_Channel1_ShouldReturnValue()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);
            await device.OpenAsync();

            // Act
            int analogValue = await device.GetAnalogInputAsync(1);

            // Assert - 模拟量值应在合理范围内（0-20mA 对应 0-10000 左右）
            Assert.InRange(analogValue, 0, 100000);

            // Cleanup
            await device.CloseAsync();
        }

        #endregion

        #region 综合测试

        [Fact]
        public async Task FullWorkflow_TurnOnOff_ShouldSucceed()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);
            await device.OpenAsync();

            // Act & Assert - 完整的工作流程
            // 1. 关闭所有输出
            await device.CloseAllAsync();
            await Task.Delay(100);

            // 2. 打开第1路输出
            await device.SetOutputAsync(1, true);
            await Task.Delay(100);
            bool state1 = await device.GetOutputAsync(1);
            Assert.True(state1);

            // 3. 打开第16路输出
            await device.SetOutputAsync(16, true);
            await Task.Delay(100);
            bool state16 = await device.GetOutputAsync(16);
            Assert.True(state16);

            // 4. 关闭第1路输出
            await device.SetOutputAsync(1, false);
            await Task.Delay(100);
            state1 = await device.GetOutputAsync(1);
            Assert.False(state1);

            // 5. 打开所有输出
            await device.OpenAllAsync();
            await Task.Delay(100);
            var allStatuses = await device.GetAllStatusesAsync();
            Assert.All(allStatuses, state => Assert.True(state));

            // 6. 关闭所有输出
            await device.CloseAllAsync();
            await Task.Delay(100);
            allStatuses = await device.GetAllStatusesAsync();
            Assert.All(allStatuses, state => Assert.False(state));

            // Cleanup
            await device.CloseAsync();
        }

        [Fact]
        public async Task SetOutputAsync_AllChannels_ShouldSucceed()
        {
            // Arrange
            using var device = new BNRC32(IPAddress.Parse(_ipAddress), _port, _address);
            await device.OpenAsync();

            // Act & Assert - 测试所有有效通道
            for (int channel = 1; channel <= 32; channel++)
            {
                // 打开通道
                await device.SetOutputAsync(channel, true);
                await Task.Delay(50);

                // 验证状态
                bool state = await device.GetOutputAsync(channel);
                Assert.True(state, $"通道 {channel} 打开后状态应为 true");

                // 关闭通道
                await device.SetOutputAsync(channel, false);
                await Task.Delay(50);

                state = await device.GetOutputAsync(channel);
                Assert.False(state, $"通道 {channel} 关闭后状态应为 false");
            }

            // Cleanup
            await device.CloseAsync();
        }

        #endregion
    }
}