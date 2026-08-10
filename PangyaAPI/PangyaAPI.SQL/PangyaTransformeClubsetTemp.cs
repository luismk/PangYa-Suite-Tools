using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaTransformeClubsetTemp
{
    public long TransIndex { get; set; }

    public int Uid { get; set; }

    public int TaqueiraId { get; set; }

    public int State { get; set; }

    public int Mastery { get; set; }

    public int State2 { get; set; }

    public short Flag { get; set; }

    public int CardTypeid { get; set; }

    public int CardQntd { get; set; }

    public int TaqueiraTransTypeid { get; set; }
}
