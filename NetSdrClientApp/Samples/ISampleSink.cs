using System.Collections.Generic;

namespace NetSdrClientApp.Samples
{
    public interface ISampleSink
    {
        void StoreSamples(IEnumerable<int> samples);
    }
}

