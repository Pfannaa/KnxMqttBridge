using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Models;
using Xunit;

namespace KnxMqttBridge.Tests
{
    public class GroupAddressInformationTests
    {
        #region GetByAddress Tests

        [Fact]
        public void GetByAddress_ExistingAddress_ReturnsGroupAddress()
        {
            var info = new GroupAddressInformation();
            info.GroupAddresses.Add(new KnxGroupAddress
            {
                Name = "Test Light",
                Address = "1/2/3",
                DataPointType = "DPST-1-1",
                Category = "Lighting",
                Subcategory = "Living Room",
                FullPath = "Lighting/Living Room"
            });

            var result = info.GetByAddress("1/2/3");

            Assert.NotNull(result);
            Assert.Equal("Test Light", result.Name);
            Assert.Equal("DPST-1-1", result.DataPointType);
        }

        [Fact]
        public void GetByAddress_NonExistingAddress_ReturnsNull()
        {
            var info = new GroupAddressInformation();
            info.GroupAddresses.Add(new KnxGroupAddress
            {
                Name = "Test Light",
                Address = "1/2/3",
                DataPointType = "DPST-1-1",
                Category = "Lighting",
                Subcategory = "Living Room",
                FullPath = "Lighting/Living Room"
            });

            var result = info.GetByAddress("9/9/9");

            Assert.Null(result);
        }

        #endregion

        #region GetByCategory Tests

        [Fact]
        public void GetByCategory_ExistingCategory_ReturnsGroupAddresses()
        {
            var info = new GroupAddressInformation();
            info.GroupAddresses.Add(new KnxGroupAddress
            {
                Name = "Light 1",
                Address = "1/2/3",
                DataPointType = "DPST-1-1",
                Category = "Lighting",
                Subcategory = "Living Room",
                FullPath = "Lighting/Living Room"
            });
            info.GroupAddresses.Add(new KnxGroupAddress
            {
                Name = "Light 2",
                Address = "1/2/4",
                DataPointType = "DPST-1-1",
                Category = "Lighting",
                Subcategory = "Bedroom",
                FullPath = "Lighting/Bedroom"
            });
            info.GroupAddresses.Add(new KnxGroupAddress
            {
                Name = "Temperature",
                Address = "2/1/1",
                DataPointType = "DPST-9-1",
                Category = "HVAC",
                Subcategory = "Sensors",
                FullPath = "HVAC/Sensors"
            });

            var result = info.GetByCategory("Lighting");

            Assert.Equal(2, result.Count());
            Assert.All(result, ga => Assert.Equal("Lighting", ga.Category));
        }

        [Fact]
        public void GetByCategory_NonExistingCategory_ReturnsEmpty()
        {
            var info = new GroupAddressInformation();
            info.GroupAddresses.Add(new KnxGroupAddress
            {
                Name = "Light 1",
                Address = "1/2/3",
                DataPointType = "DPST-1-1",
                Category = "Lighting",
                Subcategory = "Living Room",
                FullPath = "Lighting/Living Room"
            });

            var result = info.GetByCategory("NonExisting");

            Assert.Empty(result);
        }

        #endregion

        #region GetByDataPointType Tests

        [Fact]
        public void GetByDataPointType_ExistingDPT_ReturnsGroupAddresses()
        {
            var info = new GroupAddressInformation();
            info.GroupAddresses.Add(new KnxGroupAddress
            {
                Name = "Light 1",
                Address = "1/2/3",
                DataPointType = "DPST-1-1",
                Category = "Lighting",
                Subcategory = "Living Room",
                FullPath = "Lighting/Living Room"
            });
            info.GroupAddresses.Add(new KnxGroupAddress
            {
                Name = "Light 2",
                Address = "1/2/4",
                DataPointType = "DPST-1-1",
                Category = "Lighting",
                Subcategory = "Bedroom",
                FullPath = "Lighting/Bedroom"
            });
            info.GroupAddresses.Add(new KnxGroupAddress
            {
                Name = "Temperature",
                Address = "2/1/1",
                DataPointType = "DPST-9-1",
                Category = "HVAC",
                Subcategory = "Sensors",
                FullPath = "HVAC/Sensors"
            });

            var result = info.GetByDataPointType("DPST-1-1");

            Assert.Equal(2, result.Count());
            Assert.All(result, ga => Assert.Equal("DPST-1-1", ga.DataPointType));
        }

        [Fact]
        public void GetByDataPointType_NonExistingDPT_ReturnsEmpty()
        {
            var info = new GroupAddressInformation();
            info.GroupAddresses.Add(new KnxGroupAddress
            {
                Name = "Light 1",
                Address = "1/2/3",
                DataPointType = "DPST-1-1",
                Category = "Lighting",
                Subcategory = "Living Room",
                FullPath = "Lighting/Living Room"
            });

            var result = info.GetByDataPointType("DPST-999-999");

            Assert.Empty(result);
        }

        #endregion
    }
}
