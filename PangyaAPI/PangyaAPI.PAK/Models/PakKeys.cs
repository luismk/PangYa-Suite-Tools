//criado por LUISMK -> github.com/luismk
namespace PangyaAPI.PAK.Models
{
    public static class PakKeys
    {
        // Chaves Oficiais dos Servidores Base
        public static readonly uint[] GB = [0x03F607A9u, 0x036F5A3Eu, 0x011002B4u, 0x04AB00EAu];
        public static readonly uint[] TH = [0x050AD33Bu, 0x00BAFF09u, 0x0452FFDAu, 0x02CB4422u];
        public static readonly uint[] JP = [0x020A5FD4u, 0x01EEBDFFu, 0x02B3C6A0u, 0x04F6A3E1u];
        public static readonly uint[] KR = [0x0485B576u, 0x05148E02u, 0x05141D96u, 0x028FA9D6u];
        public static readonly uint[] ID = [0x01640DB7u, 0x01455A9Bu, 0x027F1AB7u, 0x05918B54u];
        public static readonly uint[] EU = [0x01E986D8u, 0x05818479u, 0x03D2B0BBu, 0x02C9B030u];
        public static readonly uint[] THIFF = [0x486d82bu, 0x148c72bu, 0x27eeafbu, 0x5a23814u];
        //SS DEV
        public static readonly uint[] SS = [0x087A0F82u, 0x1880DD08u, 0x85FA69CBu, 0xFF5808EAu];
        public static readonly IReadOnlyList<(string Label, uint[] Keys)> All = new[]
        { 
            ("Global",      GB),
            ("Thailand",    TH),
            ("Japan",     JP),
            ("Korea",     KR),
            ("Indonesian", ID),
            ("European",     EU),
            ("Thailand IFF",    THIFF),
            ("Super SS Dev", SS) //acabei desistindo:
            //de inicio eu conseguir, eu comecei fazendo, olhando bit por bit, mas descobrir outras coisas, preferir focar em entender, o conceito.
        }; 
    }
}