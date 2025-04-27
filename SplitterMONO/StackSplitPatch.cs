using System.Reflection; 
using HarmonyLib;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.UI.Items;
using UnityEngine;

namespace Splitter
{
    public class StackSplitPatch : Core
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemUIManager), "Update")]
        private static void PostfixUpdate(ItemUIManager __instance)
        {
            HandleLeftClickSplit(__instance);
            HandleWheelSplit(__instance);
        }

        private static void HandleLeftClickSplit(ItemUIManager __instance)
        {
            bool flag = wheelCachedSlot?.ItemInstance?.GetItemData().ID.ToLower() == "cash";
            if (Input.GetMouseButtonDown(0) && Input.GetKey(SplitKey) && !flag)
            {
                leftClickCachedSlot = __instance.HoveredSlot?.assignedSlot;
                ItemInstance itemInstance = leftClickCachedSlot?.ItemInstance;
                ItemData itemData = itemInstance?.GetItemData();

                if (leftClickCachedSlot != null && itemData != null)
                {
                    int newAmount = (itemData.Quantity == 1) ? 1 
                    : RoundUp 
                        ? (int)Mathf.Ceil(itemData.Quantity / 2f)
                        : (int)Mathf.Floor(itemData.Quantity / 2f);
                        
                    
                    _setDraggedAmountMethod.Invoke(__instance, new object[] { newAmount });
                    // LogScrollAction(itemData.Quantity, newAmount, itemData.Quantity);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {                
                leftClickCachedSlot = null;
            }
        }

    private static void HandleWheelSplit(ItemUIManager __instance)
    {
        bool flag = wheelCachedSlot?.ItemInstance?.GetItemData().ID.ToLower() == "cash";
        if (Input.GetMouseButtonDown(1) && Input.GetKey(SplitKey))
        {
            wheelCachedSlot = __instance.HoveredSlot?.assignedSlot;
            wheelRightClickHeld = true;
        }
        else if (Input.GetMouseButtonUp(1) && !flag)
        {
            wheelRightClickHeld = false;
            wheelCachedSlot = null;
        }
        
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        int currentAmount = (int)_draggedAmount.GetValue(__instance);
        if (currentAmount <= 0) return;

        int maxSplit = 999;
        int direction = scroll > 0 ? 1 : -1;
        int step = SplitStep;

        ItemInstance itemInstance = wheelCachedSlot?.ItemInstance;
        if (itemInstance?.GetItemData() is ItemData itemData)
        {
            if (itemData.Quantity <= 1)
            {
                _setDraggedAmountMethod.Invoke(__instance, new object[] { 1 });
                return;
            }
            maxSplit = Mathf.Max(itemData.Quantity - 1, 1);            
        }
        
        int newAmount = CalculateNewAmount(currentAmount, direction, step, maxSplit);
        int maxQuantity = wheelCachedSlot?.ItemInstance?.GetItemData().Quantity ?? 0;
        // int itemInSlot = (int)_hoveredSlot.GetValue(__instance);

        if (!ConsumeAllIfBelowStep && direction > 0 && newAmount % step != 0)
        {
            newAmount =  currentAmount - 1;
            _setDraggedAmountMethod.Invoke(__instance, new object[] { newAmount });
            // LogScrollAction(newAmount, newAmount, maxQuantity+1);
            return;
        }
        else if (ConsumeAllIfBelowStep && !OneByOne && direction > 0 && newAmount % step != 0)
        {
            newAmount = Mathf.Min(currentAmount + maxSplit, maxQuantity);
            _setDraggedAmountMethod.Invoke(__instance, new object[] { newAmount });
            // LogScrollAction(currentAmount, newAmount, maxSplit);
            return;
        }
        else if (ConsumeAllIfBelowStep && direction > 0 && newAmount % step != 0)
        {
            return;
        }
        _setDraggedAmountMethod.Invoke(__instance, new object[] { newAmount });
        
        
        // if (DebugLogs)
            // LogScrollAction(currentAmount, newAmount, maxSplit);
    }
        private static int CalculateNewAmount(int current, int direction, int step, int max)
        {
            if (direction > 0)
            {
                if (current == 1)
                    return Mathf.Min(step, max);
                    
                int nextStep = ((current - 1) / step + 1) * step;
                return Mathf.Min(nextStep, max);
            }
            else
            {
                if (current <= step)
                    return 1;
                    
                int prevStep = ((current - 1) / step) * step;
                return Mathf.Max(prevStep, 1);
            }
        }

        private static void LogScrollAction(int oldAmount, int newAmount, int maxSplit)
        {
            // MelonLogger.Msg($"Cur Item | {oldAmount} → {newAmount} (max: {maxSplit})");
        }

        public StackSplitPatch()
        {
            // Logger = new MelonLogger.Instance("Splitter");
        }

        private static ItemSlot leftClickCachedSlot;
        
        private static ItemSlot wheelCachedSlot;
        private static bool wheelRightClickHeld;
        
        // private static MelonLogger.Instance Logger;

        private static readonly MethodInfo _setDraggedAmountMethod = typeof(ItemUIManager)
        .GetMethod("SetDraggedAmount", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo _draggedAmount = typeof(ItemUIManager).GetField(
            "draggedAmount", 
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        
        // private static readonly FieldInfo _hoveredSlot = typeof(ItemUIManager).GetField(
        //     "hoveredSlot", 
        //     BindingFlags.NonPublic | BindingFlags.Instance
        // );
    }
}