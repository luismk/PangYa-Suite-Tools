using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaApproachMission
{
    public long Numero { get; set; }

    public int Tipo { get; set; }

    public int RewardTipo { get; set; }

    public int Box { get; set; }

    public int Flag { get; set; }

    public short Active { get; set; }
}
