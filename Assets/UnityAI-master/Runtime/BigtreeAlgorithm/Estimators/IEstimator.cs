using Bigtree.Algorithm.Statistics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bigtree.Algorithm.Estimators
{
    public interface IEstimator
    {
        double Prob(List<Probability> dist, string sample);
    }
}
