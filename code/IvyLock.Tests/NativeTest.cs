using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace IvyLock.Tests
{
    [TestClass]
    public class NativeTest
    {
        [TestMethod]
        public async Task ProcessInformation()
        {
            var z = Process.GetCurrentProcess();
            var zi = z.Id;
            var x = await Service.Native.NativeMethods.GetProcessInfo(0);
            var y = x.Where(spi => (int)spi.UniqueProcessId == zi).FirstOrDefault();
            Assert.IsNotNull(y);
            Assert.AreEqual(y.BasePriority, z.BasePriority);
        }
    }
}
