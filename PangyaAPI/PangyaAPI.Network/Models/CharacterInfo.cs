using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.IFF.JP.Models.Data;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.BinaryModels;
using PangyaAPI.Utilities.Log;
namespace PangyaAPI.Network.Models
{


    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 513)]
    public class CharacterInfo
    {
        public CharacterInfo()
        {
            clear();
        }

        public enum Stats : int
        {
            S_POWER,
            S_CONTROL,
            S_ACCURACY,
            S_SPIN,
            S_CURVE,
        }
        public uint _typeid { get; set; }
        public int id { get; set; }
        public byte default_hair { get; set; }
        public byte default_shirts { get; set; }
        public byte gift_flag { get; set; }
        public byte purchase { get; set; }
        /// <summary>
        /// Parts typeid, do 1 ao 24
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public uint[] parts_typeid { get; set; }
        /// <summary>
        /// Parts id, do 1 ao 24
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public uint[] parts_id { get; set; }
        /// <summary>
        ///Não sei bem direito o que é aqui
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 216)]
        public byte[] UccIndexList { get; set; }
        /// <summary>
        ///Auxiliar Parts 5, aqui fica anel
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public uint[] auxparts { get; set; }
        /// <summary>
        ///Cut-in, no primeiro mas acho que pode ser cut-in no resto
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] cut_in { get; set; }
        /// <summary>
        ///Aqui é o character stats, como controle, força, spin e etc
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] pcl { get; set; }
        /// <summary>
        /// Mastery, que aumenta os slot do stats do character
        /// </summary>
        public uint mastery { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] Card_Character { get; set; }				// 4 Slot de card Character
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] Card_Caddie { get; set; }             // 4 Slot de card Caddie
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] Card_NPC { get; set; }

        public void clear()
        {
            Card_NPC = new uint[4];
            Card_Character = new uint[4];
            Card_Caddie = new uint[4];
            parts_id = new uint[24];
            parts_typeid = new uint[24];
            auxparts = new uint[5];
            UccIndexList = new byte[216];
            cut_in = new uint[4];
            pcl = new byte[5];
        }

        public byte AngelEquiped()
        {
            uint typeId = _typeid & 0xFF;
            uint wing = Global.gacha_angel_wings.FirstOrDefault(el =>
                Singleton<IFFHandle>.getInstance().getItemCharIdentify(el) == typeId);
            uint slot;
            if (wing != 0
                && (slot = Singleton<IFFHandle>.getInstance().getItemCharPartNumber(wing)) < parts_typeid.Length
                && parts_typeid[slot] == wing)
            {
                return 1;
            }
            return 0;
        }

        public bool isEquipedPartSlotThirdCaddieCardSlot()
        {
            for (uint index = 0; index < parts_typeid.Length; index++)
            {
                var part = Singleton<IFFHandle>.getInstance().findPart(parts_typeid[index]);
                if (parts_id[index] != 0 && part != null && part._CardSlot.CaddieSlot != 0)
                    return true;
            }
            return false;
        }

        public bool isPartEquiped(uint partTypeId, int partId)
        {
            if (partTypeId == 0
                || Singleton<IFFHandle>.getInstance().getItemCharIdentify(partTypeId) != (_typeid & 0xFF))
            {
                return false;
            }

            uint slot = Singleton<IFFHandle>.getInstance().getItemCharPartNumber(partTypeId);
            return slot < parts_typeid.Length
                && parts_typeid[slot] == partTypeId
                && parts_id[slot] == partId;
        }

        public bool isPartEquiped(uint partTypeId)
        {
            if (partTypeId == 0
                || Singleton<IFFHandle>.getInstance().getItemCharIdentify(partTypeId) != (_typeid & 0xFF))
            {
                return false;
            }

            uint slot = Singleton<IFFHandle>.getInstance().getItemCharPartNumber(partTypeId);
            return slot < parts_typeid.Length && parts_typeid[slot] == partTypeId;
        }

        public bool isAuxPartEquiped(uint auxPartTypeId)
        {
            return auxPartTypeId != 0 && auxparts.Contains(auxPartTypeId);
        }

        public void unequipPart(Part part)
        {
            if (part == null)
            {
                PangyaLog.Write(
                    "[CharacterInfo::unequipPart][Error] IFF::Part* _part is invalid(null).",
                    LogDestination.Default);
                return;
            }

            for (uint index = 0; index < parts_typeid.Length; index++)
            {
                if (!part.position_mask.getSlot((int)index))
                    continue;

                uint defaultPartTypeId = ((index | (_typeid << 5)) << 13) | 0x8000400;
                var defaultPart = Singleton<IFFHandle>.getInstance().findPart(defaultPartTypeId);
                parts_typeid[index] = defaultPart != null && defaultPart.ID != 0 ? defaultPartTypeId : 0;
                parts_id[index] = 0;
            }
        }

        public void unequipPart(uint partTypeId)
        {
            if (partTypeId == 0)
                return;

            var part = Singleton<IFFHandle>.getInstance().findPart(partTypeId);
            if (part != null && part.ID != 0)
            {
                unequipPart(part);
                return;
            }

            PangyaLog.Write(
                $"[CharacterInfo::unequipPart][Warning] Part[TYPEID={partTypeId}] does not exist in the server IFF data.",
                LogDestination.Default);

            for (uint index = 0; index < parts_typeid.Length; index++)
            {
                if (parts_typeid[index] != partTypeId)
                    continue;

                uint defaultPartTypeId = ((index | (_typeid << 5)) << 13) | 0x8000400;
                var defaultPart = Singleton<IFFHandle>.getInstance().findPart(defaultPartTypeId);
                parts_typeid[index] = defaultPart != null && defaultPart.ID != 0 ? defaultPartTypeId : 0;
                parts_id[index] = 0;
                break;
            }
        }

        public void unequipAuxPart(uint auxPartTypeId)
        {
            if (auxPartTypeId == 0)
                return;

            for (var index = 0; index < auxparts.Length; index++)
            {
                if (auxparts[index] == auxPartTypeId)
                {
                    auxparts[index] = 0;
                    break;
                }
            }
        }

        public sbyte getSlotOfStatsFromsbyteEquipedPartItem(Stats stat)
        {
            sbyte value = 0;
            if (stat > Stats.S_CURVE)
                return -1;

            for (var index = 0; index < parts_typeid.Length; index++)
            {
                var part = parts_id[index] == 0
                    ? null
                    : Singleton<IFFHandle>.getInstance().findPart(parts_typeid[index]);
                if (part != null)
                    value += (sbyte)part.SlotStats.getSlot[(int)stat];
            }
            return value;
        }

        public sbyte getSlotOfStatsFromCharEquipedPartItem(Stats stat)
        {
            sbyte value = 0;
            if (stat > Stats.S_CURVE)
                return -1;

            for (var index = 0; index < parts_typeid.Length; index++)
            {
                var part = parts_id[index] == 0
                    ? null
                    : Singleton<IFFHandle>.getInstance().findPart(parts_typeid[index]);
                if (part == null)
                    continue;

                value += stat switch
                {
                    Stats.S_POWER => (sbyte)part.Stats.Power,
                    Stats.S_CONTROL => (sbyte)part.Stats.Control,
                    Stats.S_ACCURACY => (sbyte)part.Stats.Impact,
                    Stats.S_SPIN => (sbyte)part.Stats.Spin,
                    Stats.S_CURVE => (sbyte)part.Stats.Curve,
                    _ => (sbyte)0
                };
            }
            return value;
        }

        public sbyte getSlotOfStatsFromCharEquipedAuxPart(Stats stat)
        {
            sbyte value = 0;
            if (stat > Stats.S_CURVE)
                return -1;

            foreach (uint auxPartTypeId in auxparts)
            {
                var auxPart = auxPartTypeId == 0
                    ? null
                    : Singleton<IFFHandle>.getInstance().findAuxPart(auxPartTypeId);
                if (auxPart != null)
                    value += (sbyte)auxPart.slot[(int)stat];
            }
            return value;
        }

        public sbyte getSlotOfStatsFromSetEffectTable(Stats stat)
        {
            sbyte value = 0;
            var evaluatedSets = new List<uint>();
            if (stat > Stats.S_CURVE)
                return -1;

            foreach (uint partTypeId in parts_typeid)
            {
                if (partTypeId == 0)
                    continue;

                var set = Singleton<IFFHandle>.getInstance().findFirstItemInSetEffectTable(partTypeId);
                if (set == null || evaluatedSets.Contains(set.Index))
                    continue;

                evaluatedSets.Add(set.Index);
                bool equipped = true;
                foreach (uint itemTypeId in set.item.ID)
                {
                    if (itemTypeId == 0)
                        continue;

                    uint group = Singleton<IFFHandle>.getInstance().getItemGroupIdentify(itemTypeId);
                    if (group == Singleton<IFFHandle>.getInstance().PART && !isPartEquiped(itemTypeId)
                        || group == Singleton<IFFHandle>.getInstance().AUX_PART && !isAuxPartEquiped(itemTypeId))
                    {
                        equipped = false;
                        break;
                    }
                }

                if (!equipped)
                    continue;

                value += (sbyte)set.effect.effect.Count(effect => effect == 25);
                value += (sbyte)set.Slot[(int)stat];
            }
            return value;
        }

        public sbyte getSlotOfStatsFromCharEquipedCard(Stats stat)
        {
            sbyte value = 0;
            if (stat > Stats.S_CURVE)
                return -1;

            foreach (uint cardTypeId in Card_Character)
            {
                var card = cardTypeId == 0
                    ? null
                    : Singleton<IFFHandle>.getInstance().findCard(cardTypeId);
                if (card != null)
                    value += (sbyte)card.c[(int)stat];
            }
            return value;
        }

        public void initComboDef()
        {
            if (_typeid == 0)
                return;

            Array.Clear(parts_typeid, 0, parts_typeid.Length);
            Array.Clear(parts_id, 0, parts_id.Length);
            for (uint index = 0; index < parts_typeid.Length; index++)
            {
                uint partTypeId = (((_typeid << 5) | index) << 13) | 0x8000400;
                var part = Singleton<IFFHandle>.getInstance().findPart(partTypeId);
                if (part != null && part.ID == partTypeId)
                    parts_typeid[index] = partTypeId;
            }
        }

        /// <summary>
        /// size = 513 bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (var p = new PangyaBinaryWriter())
            {
                p.Write(_typeid);
                p.Write(id);
                p.Write(default_hair);
                p.Write(default_shirts);
                p.Write(gift_flag);
                p.Write(purchase);
                p.WriteUInt32(parts_typeid);
                p.WriteUInt32(parts_id);
                for (int i = 0; i < 216; i++)
                    p.WriteByte(0);
                p.WriteUInt32(auxparts);
                p.WriteUInt32(cut_in);
                p.WriteBytes(pcl);

                p.WriteUInt32(mastery);
                p.WriteUInt32(Card_Character);
                p.WriteUInt32(Card_Caddie);
                p.WriteUInt32(Card_NPC);

                return p.GetBytes;
            }
        }

        public CharacterInfo ToRead(packet r)
        {
            _typeid = r.ReadUInt32();
            id = r.ReadInt32();
            default_hair = r.ReadByte();
            default_shirts = r.ReadByte();
            gift_flag = r.ReadByte();
            purchase = r.ReadByte();

            parts_typeid = new uint[24];
            for (int i = 0; i < 24; i++)
                parts_typeid[i] = r.ReadUInt32();

            parts_id = new uint[24];
            for (int i = 0; i < 24; i++)
                parts_id[i] = r.ReadUInt32();

            UccIndexList = r.ReadBytes(216);

            auxparts = new uint[5];
            for (int i = 0; i < 5; i++)
                auxparts[i] = r.ReadUInt32();

            cut_in = new uint[4];
            for (int i = 0; i < 4; i++)
                cut_in[i] = r.ReadUInt32();

            pcl = r.ReadBytes(5);

            mastery = r.ReadUInt32();

            Card_Character = new uint[4];
            for (int i = 0; i < 4; i++)
                Card_Character[i] = r.ReadUInt32();

            Card_Caddie = new uint[4];
            for (int i = 0; i < 4; i++)
                Card_Caddie[i] = r.ReadUInt32();

            Card_NPC = new uint[4];
            for (int i = 0; i < 4; i++)
                Card_NPC[i] = r.ReadUInt32();

            return this;
        }

    }

}

