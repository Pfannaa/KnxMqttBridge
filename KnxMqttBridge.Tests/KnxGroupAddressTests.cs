using KnxMqttBridge.Models;
using Xunit;

namespace KnxMqttBridge.Tests
{
    public class KnxGroupAddressTests
    {
        #region ParseAddress Tests

        [Theory]
        [InlineData("0/0/1", 0, 0, 1)]
        [InlineData("1/2/3", 1, 2, 3)]
        [InlineData("31/7/255", 31, 7, 255)]
        [InlineData("5/1/100", 5, 1, 100)]
        public void ParseAddress_ValidThreeLevelAddress_ReturnsCorrectParts(
            string address, int expectedMain, int expectedMiddle, int expectedSub)
        {
            var groupAddress = new KnxGroupAddress
            {
                Name = "Test",
                Address = address,
                DataPointType = "DPST-1-1",
                Category = "Test",
                Subcategory = "Test",
                FullPath = "Test/Test"
            };

            var (main, middle, sub) = groupAddress.ParseAddress();

            Assert.Equal(expectedMain, main);
            Assert.Equal(expectedMiddle, middle);
            Assert.Equal(expectedSub, sub);
        }

        [Theory]
        [InlineData("0/1", 0, 1)]
        [InlineData("2/71", 2, 71)]
        [InlineData("31/255", 31, 255)]
        public void ParseAddress_ValidTwoLevelAddress_ReturnsNullMiddle(
            string address, int expectedMain, int expectedSub)
        {
            var groupAddress = new KnxGroupAddress
            {
                Name = "Test",
                Address = address,
                DataPointType = "DPST-1-1",
                Category = "Test",
                Subcategory = "Test",
                FullPath = "Test/Test"
            };

            var (main, middle, sub) = groupAddress.ParseAddress();

            Assert.Equal(expectedMain, main);
            Assert.Null(middle);
            Assert.Equal(expectedSub, sub);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            var groupAddress = new KnxGroupAddress
            {
                Name = "Living Room Light",
                Address = "1/2/3",
                DataPointType = "DPST-1-1",
                Category = "Lighting",
                Subcategory = "Living Room",
                FullPath = "Lighting/Living Room"
            };

            var result = groupAddress.ToString();

            Assert.Equal("1/2/3 - Living Room Light (DPST-1-1)", result);
        }

        #endregion
    }
}
