using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGachaUserKey
{
    public int Uid { get; set; }

    public int CoinCountEntrou { get; set; }

    public short AttFlag { get; set; }

    public string Key { get; set; }

    public DateTime? DateKeyGeneration { get; set; }
}
