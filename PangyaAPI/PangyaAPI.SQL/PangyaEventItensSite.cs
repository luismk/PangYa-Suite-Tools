using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaEventItensSite
{
    public int ItemId { get; set; }

    public int EventoId { get; set; }

    public string NomeItem { get; set; }

    public int QntJogada { get; set; }

    public int Typeid { get; set; }

    public int QntItem { get; set; }

    public virtual PangyaEventSite Evento { get; set; }
}
