using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bigtree.Algorithm.HiddenMarkovModel.Learning.Unsupervised
{
    public partial interface IUnsupervisedLearning
    {
        double Run(double[][] observations_db);
    }
}
