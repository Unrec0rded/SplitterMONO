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
            if (__instance == null) return;

            bool flag = wheelCachedSlot?.ItemInstance?.GetItemData().ID.ToLower() == "cash";
            if (Input.GetMouseButtonDown(0) && !flag)
            {
                leftClickCachedSlot = __instance.HoveredSlot?.assignedSlot;
            }
            else if (Input.GetMouseButtonUp(0) || flag)
            {                
                leftClickCachedSlot = null;
            }

            if(!Input.GetKey(SplitKey)) return;
            
            if (leftClickCachedSlot == null) return;

            ItemInstance itemInstance = leftClickCachedSlot.ItemInstance;
            if (itemInstance == null || itemInstance is CashInstance) return;

            ItemData itemData = itemInstance.GetItemData();
            if (itemData == null) return;

            int newAmount = (itemData.Quantity == 1) 
                ? 1 
                : RoundUp 
                    ? Mathf.CeilToInt(itemData.Quantity / 2f) 
                    : Mathf.FloorToInt(itemData.Quantity / 2f);

            _setDraggedAmountMethod.Invoke(__instance, new object[] { newAmount });
        }

        private static void HandleWheelSplit(ItemUIManager __instance)
        {
            if (__instance == null) return;

            bool flag = wheelCachedSlot?.ItemInstance?.GetItemData().ID.ToLower() == "cash";
            if (Input.GetMouseButtonDown(1) && !flag)
            {
                wheelCachedSlot = __instance.HoveredSlot?.assignedSlot;
                wheelRightClickHeld = true;
            }
            else if (Input.GetMouseButtonUp(1) || flag)
            {
                wheelRightClickHeld = false;
                wheelCachedSlot = null;
            }
            
            if(!Input.GetKey(SplitKey)) return;
            
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            int currentAmount = (int)_draggedAmount.GetValue(__instance);
            if (currentAmount <= 0) return;

            if (wheelCachedSlot == null || wheelCachedSlot.ItemInstance == null)
            {
                return;
            }

            ItemInstance itemInstance = wheelCachedSlot.ItemInstance;
            if (itemInstance is CashInstance) return;

            ItemData itemData = itemInstance.GetItemData();
            if (itemData == null)
            {
                return;
            }

            int maxSplit = 999;
            if (itemData.Quantity >= 1)
            {
                maxSplit = Mathf.Max(itemData.Quantity - 1, 1);
            }

            if (itemData.Quantity == 1) return;

            int direction = scroll > 0 ? 1 : -1;
            int step = SplitStep;

            int newAmount = CalculateNewAmount(currentAmount, direction, step, maxSplit);

            if (!ConsumeAllIfBelowStep && newAmount % step != 0 && direction > 0)
            {
                _setDraggedAmountMethod.Invoke(__instance, new object[] { currentAmount - 1 });
                return;
            }
            else if (ConsumeAllIfBelowStep && !OneByOne && newAmount % step != 0 && direction > 0)
            {
                
                _setDraggedAmountMethod.Invoke(__instance, new object[] { Mathf.Min(currentAmount + maxSplit, itemData.Quantity) });
                return;
            }
            else if (ConsumeAllIfBelowStep && newAmount % step != 0 && direction > 0)
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