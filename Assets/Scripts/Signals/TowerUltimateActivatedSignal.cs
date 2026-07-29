using System.Collections.Generic;
using Enums;
using UnityEngine;

namespace Signals
{
    /// <summary>
    /// Ультимейт башни сработал. Нужен, чтобы визуал не приходилось вшивать в TowerUltimateSystem:
    /// логика ульты остаётся там, отображение живёт в TowerUltimateVfxSystem.
    /// </summary>
    public class TowerUltimateActivatedSignal
    {
        public EMainTowerAttackType attackType;

        /// <summary>Сколько действует эффект. 0 — мгновенная ульта (Раскол).</summary>
        public float duration;

        public Vector3 worldPos;

        /// <summary>
        /// Позиции задетых врагов. Заполняется только Расколом — по ним рисуются осколочные лучи,
        /// чтобы игрок видел, кого именно накрыло. У остальных ульт null.
        /// </summary>
        public IReadOnlyList<Vector3> hitPoints;
    }
}
