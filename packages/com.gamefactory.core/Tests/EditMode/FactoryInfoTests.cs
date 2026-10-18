using NUnit.Framework;

namespace GameFactory.Core.Tests
{
    public class FactoryInfoTests
    {
        [Test]
        public void CoreVersion_IsSemanticVersion()
        {
            StringAssert.IsMatch(@"^\d+\.\d+\.\d+$", FactoryInfo.CoreVersion);
        }
    }
}
