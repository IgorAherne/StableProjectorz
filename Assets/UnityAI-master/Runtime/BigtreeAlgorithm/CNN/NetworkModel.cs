using System;
using System.Collections.Generic;
using System.Text;

namespace Bigtree.Algorithm.CNN
{
    public class NetworkModel
    {
        public int FilterSize { get; set; }

        public NetworkModel()
        {
            FilterSize = 5;
        }

        public void Train()
        {
            Initialize();
        }

        private void Initialize(double scale = 1.0)
        {
            var fan_in = FilterSize * FilterSize * 1; // image depth
            var stddev = scale * Math.Sqrt(1.0 / fan_in);
        }
    }
}
