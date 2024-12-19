using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    [Serializable]
    [Flags]
    public enum RoleMask
    {
        None = 0,
        Pawn = 1 << 0,
        Actor = 1 << 1,
        Tenant = 1 << 2,
        Worker = 1 << 3,
        Inspector = 1 << 5,
        Supervisor = 1 << 6,
        Trader = 1 << 7,
        Bishop = 1 << 8,
        Knight = 1 << 9,
        Rook = 1 << 10,
        Queen = 1 << 11,
        King = 1 << 12,
        Thief = 1 << 13,
        Courier = 1 << 14,
        Breeder = 1 << 15,
        Pet = 1 << 16,
        Predator = 1 << 17,
        Prey = 1 << 18,
        Police = 1 << 19,
        Judge = 1 << 20,
        Tank = 1 << 21,
        Soldier = 1 << 22,
        Captain = 1 << 23,
        General = 1 << 24,
        Wolf = 1 << 25,
        Sheep = 1 << 26,
        Dog = 1 << 27,
        Keeper = 1 << 28,
        Consumer = 1 << 29,
        Harvester = 1 << 30,
        Producer = 1 << 31
    }
}