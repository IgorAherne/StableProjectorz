using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bigtree.Algorithm.CRFLite
{
    public interface IGenerateFeature
    {
        bool Initialize();
        List<List<string>> GenerateFeature(string strText);
    }
}
