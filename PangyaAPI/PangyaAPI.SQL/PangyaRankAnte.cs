using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaRankAnte
{
    public long Index { get; set; }

    public int Position { get; set; }

    public int Uid { get; set; }

    public short TipoRank { get; set; }

    public short TipoRankSeq { get; set; }

    public int Valor { get; set; }

    public DateTime? RegDate { get; set; }
}
