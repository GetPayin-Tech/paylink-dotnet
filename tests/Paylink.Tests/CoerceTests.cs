using Paylink;
using Paylink.Signing;
using Xunit;

namespace Paylink.Tests
{
    public sealed class CoerceTests
    {
        [Fact]
        public void NullBecomesEmptyString()
        {
            Assert.Equal("", Coerce.ToWire(null));
        }

        [Fact]
        public void BoolBecomesOneOrZero()
        {
            Assert.Equal("1", Coerce.ToWire(true));
            Assert.Equal("0", Coerce.ToWire(false));
        }

        [Fact]
        public void StringPassesThrough()
        {
            Assert.Equal("250.00", Coerce.ToWire("250.00"));
        }

        [Fact]
        public void IntAndLong()
        {
            Assert.Equal("250", Coerce.ToWire(250));
            Assert.Equal("250", Coerce.ToWire(250L));
        }

        [Fact]
        public void IntegerValuedDoubleHasNoTrailingZero()
        {
            Assert.Equal("250", Coerce.ToWire(250.0));
        }

        [Fact]
        public void FractionalDouble()
        {
            Assert.Equal("250.5", Coerce.ToWire(250.5));
        }

        [Fact]
        public void NonFiniteThrows()
        {
            Assert.Throws<PaylinkException>(() => Coerce.ToWire(double.PositiveInfinity));
        }
    }
}
