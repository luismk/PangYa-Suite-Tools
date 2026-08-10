using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaTikiPoint
{
    public int Uid { get; set; }

    public long TikiPoints { get; set; }

    public DateTime RegDate { get; set; }

    public DateTime ModDate { get; set; }
}
