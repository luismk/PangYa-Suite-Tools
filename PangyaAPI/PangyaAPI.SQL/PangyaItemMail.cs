using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaItemMail
{
    public int MsgId { get; set; }

    public int ItemId { get; set; }

    public int ItemTypeid { get; set; }

    public short Flag { get; set; }

    public DateTime? GetDate { get; set; }

    public int QuantidadeItem { get; set; }

    public int QuantidadeDia { get; set; }

    public long Pang { get; set; }

    public long Cookie { get; set; }

    public int GmId { get; set; }

    public int FlagGift { get; set; }

    public string UccImgMark { get; set; }

    public short Type { get; set; }

    public short Valid { get; set; }
}
