using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static MyLittleUI.MyLittleUI;

namespace MyLittleUI.Crafting
{
    public static class CraftNew
    {
        public static readonly HashSet<string> requiredItems = new HashSet<string>();

        public static bool IsEnabled => modEnabled.Value && craftingNewMarkEnabled.Value;

        public static bool IsItemToShow(ItemDrop.ItemData item) => Player.m_localPlayer && !Player.m_localPlayer.IsKnownMaterial(item.m_shared.m_name)
                                                                   && (!craftingNewMarkShowOnlyWithUnlocks.Value || requiredItems.Contains(item.m_shared.m_name));

        public static void UpdateCraftNewMark()
        {
            if (!IsEnabled)
                return;

            if (InventoryGui.instance?.InCraftTab() != true)
                return;

            if (InventoryGui.instance.m_availableRecipes.Count == 0 || requiredItems.Count == 0)
                return;

            foreach (var recipe in InventoryGui.instance.m_availableRecipes)
                if (recipe.ItemData == null && recipe.InterfaceElement != null && recipe.Recipe != null && IsItemToShow(recipe.Recipe.m_item.m_itemData))
                {
                    TMP_Text qualityText = recipe.InterfaceElement.transform.Find("QualityLevel")?.GetComponent<TMP_Text>();
                    if (!qualityText.gameObject.activeSelf)
                    {
                        qualityText.SetText(craftingNewMarkText.Value);
                        qualityText.fontStyle = craftingNewMarkFontStyle.Value;
                        qualityText.fontSize = craftingNewMarkFontSize.Value;
                        qualityText.color = craftingNewMarkColor.Value;
                        qualityText.gameObject.SetActive(true);
                    }
                }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList))]
        public static class InventoryGui_UpdateRecipeList_CraftNewRecipeMark
        {
            [HarmonyPriority(Priority.Last)]
            public static void Postfix() => UpdateCraftNewMark();
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        public static class Player_Load_FillRequiredItems
        {
            [HarmonyPriority(Priority.Last)]
            public static void Postfix() => FillRequiredItems();
        }

        public static void FillRequiredItems()
        {
            if (ObjectDB.instance)
            {
                foreach (var item in ObjectDB.instance.m_items)
                    if (item != null && item.GetComponent<ItemDrop>() is ItemDrop itemDrop && itemDrop.m_itemData is ItemDrop.ItemData itemData
                                     && itemData.m_shared is ItemDrop.ItemData.SharedData shared && shared.m_buildPieces is PieceTable pieceTable)
                        foreach (GameObject pieceObject in pieceTable.m_pieces)
                            if (pieceObject.GetComponent<Piece>() is Piece piece && piece.m_resources != null)
                                foreach (var itemRes in piece.m_resources)
                                    if (itemRes.m_resItem != null && itemRes.m_resItem.m_itemData is ItemDrop.ItemData itemDataRes
                                     && itemDataRes.m_shared is ItemDrop.ItemData.SharedData sharedRes)
                                        requiredItems.Add(sharedRes.m_name);

                foreach (var recipe in ObjectDB.instance.m_recipes)
                {
                    if (recipe.m_resources != null)
                        foreach (var itemRes in recipe.m_resources)
                            if (itemRes.m_resItem != null && itemRes.m_resItem.m_itemData is ItemDrop.ItemData itemDataRes
                             && itemDataRes.m_shared is ItemDrop.ItemData.SharedData sharedRes)
                                requiredItems.Add(sharedRes.m_name);
                }
            }
        }
    }
}
