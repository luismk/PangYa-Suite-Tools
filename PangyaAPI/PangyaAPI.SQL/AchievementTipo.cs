using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class AchievementTipo
{
    public int Uid { get; set; }

    public string Nome { get; set; }

    public int TypeId { get; set; }

    public int IdAchievement { get; set; }

    public short Tipo { get; set; }

    public int Option { get; set; }
}
