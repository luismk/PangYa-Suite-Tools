using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaEventSite
{
    public int Id { get; set; }

    public string NomeEvento { get; set; }

    public string Status { get; set; }

    public string TipoEvento { get; set; }

    public DateTime DataInicial { get; set; }

    public DateTime DataFim { get; set; }

    public DateTime? DataRegistro { get; set; }

    public bool Itens { get; set; }

    public virtual ICollection<PangyaEventItensSite> PangyaEventItensSites { get; set; } = new List<PangyaEventItensSite>();
}
