using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class ContasBetum
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public string NomeCompleto { get; set; }

    public DateTime? Birthday { get; set; }

    public string Email { get; set; }

    public short Sexo { get; set; }

    public string Pergunta { get; set; }

    public string Resposta { get; set; }

    public string LoginId { get; set; }

    public string Senha { get; set; }

    public Guid KeyUniq { get; set; }

    public byte FinishReg { get; set; }

    public DateTime? DateReg { get; set; }

    public string IpRegister { get; set; }

    public string Codigo { get; set; }

    public string ReferrerCode { get; set; }

    public string StatusReferal { get; set; }

    public string ProfileImage { get; set; }

    public string NewEmailPending { get; set; }

    public string EmailChangeKey { get; set; }

    public DateTime? RecoveryExpires { get; set; }
}
