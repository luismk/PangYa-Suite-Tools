using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaRecord
{
    public int Uid { get; set; }

    public short Tipo { get; set; }

    public short Course { get; set; }

    public short BestScore { get; set; }

    public long BestPang { get; set; }

    public int CharacterTypeid { get; set; }

    public short EventScore { get; set; }

    public int Tacada { get; set; }

    public int Putt { get; set; }

    public int Hole { get; set; }

    public int Fairway { get; set; }

    public int Puttin { get; set; }

    public int TotalScore { get; set; }

    public int Holein { get; set; }

    public short Assist { get; set; }
}
