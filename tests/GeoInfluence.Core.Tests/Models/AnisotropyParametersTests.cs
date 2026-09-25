using GeoInfluence.Core.Models;

namespace GeoInfluence.Core.Tests.Models;

public sealed class AnisotropyParametersTests
{
    [Fact]
    public void Validate_WithValidValues_DoesNotThrow()
    {
        var parameters = new AnisotropyParameters(
            BearingDegrees: 125.5,
            MajorScale: 2.5,
            MinorScale: 0.8);

        parameters.Validate();
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_WithNonFiniteBearing_Throws(double bearing)
    {
        var parameters = new AnisotropyParameters(bearing, 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(parameters.Validate);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_WithNonFiniteMajorScale_Throws(double value)
    {
        var parameters = new AnisotropyParameters(0, value, 1);

        Assert.Throws<ArgumentOutOfRangeException>(parameters.Validate);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_WithNonFiniteMinorScale_Throws(double value)
    {
        var parameters = new AnisotropyParameters(0, 1, value);

        Assert.Throws<ArgumentOutOfRangeException>(parameters.Validate);
    }
}
