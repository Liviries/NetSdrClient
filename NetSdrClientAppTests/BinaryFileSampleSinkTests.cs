using NetSdrClientApp.Samples;

namespace NetSdrClientAppTests;

[TestFixture]
public class BinaryFileSampleSinkTests
{
    private static readonly int[] BatchOneSamples = { 1, 2 };
    private static readonly int[] BatchTwoSamples = { 3 };
    private static readonly byte[] ExpectedBytesSequence = { 1, 0, 2, 0, 3, 0 };
    private static readonly int[] DefaultSamples = { 42 };

    [Test]
    public void StoreSamples_AppendsBinaryContent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bin");

        try
        {
            var sink = new BinaryFileSampleSink(path);

            sink.StoreSamples(BatchOneSamples);
            sink.StoreSamples(BatchTwoSamples);

            var bytes = File.ReadAllBytes(path);

            CollectionAssert.AreEqual(ExpectedBytesSequence, bytes);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void StoreSamples_EmptyEnumeration_DoesNotCreateFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bin");

        try
        {
            var sink = new BinaryFileSampleSink(path);
            sink.StoreSamples(Array.Empty<int>());

            Assert.That(File.Exists(path), Is.False);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void StoreSamples_NullSamples_Throws()
    {
        var sink = new BinaryFileSampleSink(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bin"));

        Assert.Throws<ArgumentNullException>(() => sink.StoreSamples(null!));
    }

    [Test]
    public void DefaultConstructor_WritesToSamplesBinInWorkingDirectory()
    {
        var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}"));
        var originalDirectory = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = tempDir.FullName;
            var sink = new BinaryFileSampleSink();

            sink.StoreSamples(DefaultSamples);

            var defaultFile = Path.Combine(tempDir.FullName, "samples.bin");
            Assert.That(File.Exists(defaultFile), Is.True);
            CollectionAssert.AreEqual(new byte[] { 42, 0 }, File.ReadAllBytes(defaultFile));
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            if (Directory.Exists(tempDir.FullName))
            {
                Directory.Delete(tempDir.FullName, recursive: true);
            }
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Constructor_WithInvalidPath_Throws(string? invalidPath)
    {
        Assert.Throws<ArgumentException>(() => new BinaryFileSampleSink(invalidPath!));
    }
}

