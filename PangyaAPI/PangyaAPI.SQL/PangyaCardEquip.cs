using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaCardEquip
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public int PartsId { get; set; }

    public int PartsTypeid { get; set; }

    public int CardTypeid { get; set; }

    public int Efeito { get; set; }

    public int EfeitoQntd { get; set; }

    public int Slot { get; set; }

    public DateTime? UseDt { get; set; }

    public DateTime? EndDt { get; set; }

    public int Tipo { get; set; }

    public short UseYn { get; set; }

    public DateTime Date { get; set; }
}
