using NUnit.Framework;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP/UdonUtils")]
    public class TestIntegerUtils
    {
        [Test]
        [TestCase(1, 0, 1, 1)]
        [TestCase(0, 0, 1, 1)]
        [TestCase(0, 9, 10, 1)]
        [TestCase(1, 9, 10, 2)]
        [TestCase(0, 8, 10, 12)]
        [TestCase(10, 0, 10, 0)]
        public void TestDecrementLoopingWorks(int value, int expected, int max, int decrement)
        {
            value.MoveIndexLeftLooping(max, decrement);
            Assert.AreEqual(value, expected);
        }

        [Test]
        [TestCase(1, 0, 1, 1)]
        [TestCase(0, 0, 1, 1)]
        [TestCase(9, 0, 10, 1)]
        [TestCase(9, 1, 10, 2)]
        [TestCase(0, 2, 10, 12)]
        [TestCase(10, 0, 10, 0)]
        public void TestIncrementLoopingWorks(int value, int expected, int max, int increment)
        {
            value.MoveIndexRightLooping(max, increment);
            Assert.AreEqual(value, expected);
        }
    }
}