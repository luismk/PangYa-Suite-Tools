using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGachaJpPlayerWin
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public int GachaNum { get; set; }

    public int Typeid { get; set; }

    public long Qnty { get; set; }

    public byte RarityType { get; set; }

    public byte SendMail { get; set; }

    public byte Valid { get; set; }

    public DateTime RegDate { get; set; }
}
