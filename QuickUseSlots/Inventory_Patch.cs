﻿using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace QuickUseSlots
{
    [HarmonyPatch(typeof(Inventory), MethodType.Constructor, new Type[] { typeof(string), typeof(Sprite), typeof(int), typeof(int) })]
    public static class Inventory_Constructor_Patch
    {
        public static bool Prefix(string name, int w, ref int h)
        {
            if (name == "Inventory" || name == "Grave")
            {
                h += 1;
            }

            return true;
        }
    }

    //void GetBoundItems(List<ItemDrop.ItemData> bound)
    [HarmonyPatch(typeof(Inventory), "GetBoundItems", new Type[] { typeof(List<ItemDrop.ItemData>) })]
    public static class Inventory_GetBoundItems_Patch
    {
        public static void Postfix(List<ItemDrop.ItemData> bound, List<ItemDrop.ItemData> ___m_inventory)
        {
            foreach (ItemDrop.ItemData itemData in ___m_inventory)
            {
                if (QuickUseSlots.IsQuickSlot(itemData.m_gridPos))
                {
                    bound.Add(itemData);
                }
            }
        }
    }

    //public bool HaveEmptySlot() => this.m_inventory.Count < this.m_width * this.m_height;
    [HarmonyPatch(typeof(Inventory), "HaveEmptySlot")]
    public static class Inventory_HaveEmptySlot_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result)
        {
            if (__instance.GetName() != "Inventory")
            {
                return true;
            }

            __result = __instance.m_inventory.Count < ((__instance.m_width * __instance.m_height) - QuickUseSlots.EquipSlotCount);
            return false;
        }
    }

    //private Vector2i FindEmptySlot(bool topFirst)
    [HarmonyPatch(typeof(Inventory), "FindEmptySlot", new Type[] { typeof(bool) })]
    public static class Inventory_FindEmptySlot_Patch
    {
        public static bool Prefix(Inventory __instance, ref Vector2i __result, bool topFirst, int ___m_height, int ___m_width)
        {
            if (__instance.GetName() != "Inventory")
            {
                return true;
            }

            if (topFirst)
            {
                for (int index1 = 0; index1 < ___m_height; ++index1)
                {
                    for (int index2 = 0; index2 < ___m_width; ++index2)
                    {
                        if (QuickUseSlots.IsEquipmentSlot(index2, index1))
                        {
                            continue;
                        }

                        if (__instance.GetItemAt(index2, index1) == null)
                        {
                            __result = new Vector2i(index2, index1);
                            return false;
                        }
                    }
                }
            }
            else
            {
                // Skip the last row
                for (int index1 = ___m_height - 2; index1 >= 0; --index1)
                {
                    for (int index2 = 0; index2 < ___m_width; ++index2)
                    {
                        if (__instance.GetItemAt(index2, index1) == null)
                        {
                            __result = new Vector2i(index2, index1);
                            return false;
                        }
                    }
                }

                // Then do the bonus quick slots last
                for (int index = 0; index < QuickUseSlots.QuickUseSlotCount; ++index)
                {
                    var slotPosition = QuickUseSlots.GetQuickSlotPosition(index);
                    if (__instance.GetItemAt(slotPosition.x, slotPosition.y) == null)
                    {
                        __result = slotPosition;
                        return false;
                    }
                }
            }
            __result = new Vector2i(-1, -1);

            return false;
        }
    }
}
