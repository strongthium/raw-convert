namespace Strongthium.RawConvert.Tests;

/// <summary>Turning a picture by its orientation tag.</summary>
public sealed class OrientationTests
{
    // 3 by 2, each pixel numbered: 1 2 3 / 4 5 6.
    private static readonly RgbImage Small = new(3, 2, [.. new byte[] { 1, 2, 3, 4, 5, 6 }.SelectMany(n => new byte[] { n, n, n })]);

    private static byte[] Numbers(RgbImage image) => image.Pixels.Where((_, i) => i % 3 == 0).ToArray();

    [Theory]
    [InlineData(1, 3, new byte[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(2, 3, new byte[] { 3, 2, 1, 6, 5, 4 })]
    [InlineData(3, 3, new byte[] { 6, 5, 4, 3, 2, 1 })]
    [InlineData(4, 3, new byte[] { 4, 5, 6, 1, 2, 3 })]
    [InlineData(5, 2, new byte[] { 1, 4, 2, 5, 3, 6 })]
    [InlineData(6, 2, new byte[] { 4, 1, 5, 2, 6, 3 })]
    [InlineData(7, 2, new byte[] { 6, 3, 5, 2, 4, 1 })]
    [InlineData(8, 2, new byte[] { 3, 6, 2, 5, 1, 4 })]
    public void TurnsAsTheTagSays(int orientation, int width, byte[] expected)
    {
        var turned = Orientation.Apply(Small, orientation);

        Assert.Equal(width, turned.Width);
        Assert.Equal(6 / width, turned.Height);
        Assert.Equal(expected, Numbers(turned));
    }

    [Fact]
    public void AQuarterTurnEachWayIsNoTurnAtAll()
    {
        var image = Samples.Picture(17, 9);
        Assert.Equal(image.Pixels, Orientation.Apply(Orientation.Apply(image, 6), 8).Pixels);
        Assert.Equal(Orientation.Apply(image, 3).Pixels, Orientation.Apply(Orientation.Apply(image, 6), 6).Pixels);
    }

    [Fact]
    public void APictureTheDecoderTurnedAlreadyIsntTurnedAgain()
    {
        var metadata = new Metadata([], [], [], 6, 6000, 4000);

        Assert.Equal(6, Orientation.ToApply(6, Samples.Picture(60, 40), metadata));
        Assert.Equal(1, Orientation.ToApply(6, Samples.Picture(40, 60), metadata));
        Assert.Equal(3, Orientation.ToApply(3, Samples.Picture(60, 40), metadata));
    }
}
