using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaAchievement
{
    public int IdAchievement { get; set; }

    public int Uid { get; set; }

    public string Nome { get; set; }

    public int TypeId { get; set; }

    public int Active { get; set; }

    /// <summary>
    /// 1 em agurado, 2 excluido, 3 ativo, 4 concluido
    /// </summary>
    public int Status { get; set; }
}
