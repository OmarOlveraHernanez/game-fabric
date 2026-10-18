using __GAME__.Logic;
using NUnit.Framework;

namespace __GAME__.Tests
{
    public class ScoreKeeperTests
    {
        [Test]
        public void Add_AccumulatesAndTracksBest()
        {
            var s = new ScoreKeeper();
            s.Add(10); s.Add(5);
            Assert.AreEqual(15, s.Score);
            s.ResetRun(); s.Add(3);
            Assert.AreEqual(3, s.Score);
            Assert.AreEqual(15, s.Best);
        }
    }
}
