using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NetSdrClientApp.Samples
{
    public class BinaryFileSampleSink : ISampleSink
    {
        private readonly string _filePath;
        private readonly object _syncRoot = new();

        public BinaryFileSampleSink()
            : this("samples.bin")
        {
        }

        public BinaryFileSampleSink(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
            }

            _filePath = filePath;
        }

        public void StoreSamples(IEnumerable<int> samples)
        {
            ArgumentNullException.ThrowIfNull(samples);

            var snapshot = samples.ToArray();
            if (snapshot.Length == 0)
            {
                return;
            }

            lock (_syncRoot)
            {
                using var fileStream = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                using var writer = new BinaryWriter(fileStream);

                foreach (var sample in snapshot)
                {
                    writer.Write((short)sample);
                }
            }
        }
    }
}

