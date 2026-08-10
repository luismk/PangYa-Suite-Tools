using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaRoomLog
{
    public int Index { get; set; }

    public Guid? RoomId { get; set; }

    public string Name { get; set; }

    public int? MasterUid { get; set; }

    public int? NumberPlayers { get; set; }

    public int? MaxPlayers { get; set; }

    public int? GmEvent { get; set; }

    public int? Tipo { get; set; }

    public int? TipoEx { get; set; }

    public int? Modo { get; set; }

    public int? NaturalMode { get; set; }

    public int? ShotMode { get; set; }

    public int? QntdHole { get; set; }

    public int? Course { get; set; }

    public int? Hole { get; set; }

    public int? Uid { get; set; }

    public int? Character { get; set; }

    public int? Club { get; set; }

    public int? Mascot { get; set; }

    public int? Caddie { get; set; }

    public int? SpecialShot { get; set; }

    public decimal Score { get; set; }

    public int? Exp { get; set; }

    public long? Pang { get; set; }

    public long? BonusPang { get; set; }

    public int? TacadaNum { get; set; }

    public int? TotalTacadaNum { get; set; }

    public int? HioHit { get; set; }

    public int? AlbaHit { get; set; }

    public int? EagleHit { get; set; }

    public int? BirdieHit { get; set; }

    public int? ParHit { get; set; }

    public int? BogeyHit { get; set; }

    public int? DoubleBogeyHit { get; set; }

    public int? TripleBogeyHit { get; set; }

    public int? GiveUp { get; set; }

    public int? TimeOut { get; set; }

    public int? EnterAfterStarted { get; set; }

    public int? AssistFlag { get; set; }

    public int? Trofeu { get; set; }

    public int? FinishGame { get; set; }

    public DateTime? Data { get; set; }
}
