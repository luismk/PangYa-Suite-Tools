using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaCard
{
    public long CardItemid { get; set; }

    public int Uid { get; set; }

    public int CardTypeid { get; set; }

    public int? Qntd { get; set; }

    public DateTime? GetDt { get; set; }

    public DateTime? UseDt { get; set; }

    public DateTime? EndDt { get; set; }

    public int Slot { get; set; }

    public int Efeito { get; set; }

    public int EfeitoQntd { get; set; }

    public short CardType { get; set; }

    public string UseYn { get; set; }
}
