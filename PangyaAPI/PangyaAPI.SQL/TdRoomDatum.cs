using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class TdRoomDatum
{
    public long MyroomId { get; set; }

    public int Uid { get; set; }

    public int RoomNo { get; set; }

    public int Typeid { get; set; }

    public float PosX { get; set; }

    public float PosY { get; set; }

    public float PosZ { get; set; }

    public float PosR { get; set; }

    public int ModSeq { get; set; }

    public string DisplayYn { get; set; }

    public string UseYn { get; set; }

    public DateTime? ModDt { get; set; }

    public byte? Valid { get; set; }
}
