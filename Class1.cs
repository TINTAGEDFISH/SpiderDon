using System.Collections.Generic;
using System.Diagnostics;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Linq;
using Il2CppInterop.Runtime.Injection;
using BepInEx.Logging;
using Utils;
using Il2CppSystem.Collections.Generic;
using System.Reflection;
using Il2CppSystem;
using SystemIntPtr = System.IntPtr;
using Il2CppIntPtr = Il2CppSystem.IntPtr;
using Il2CppInterop.Runtime;
using IntPtr = System.IntPtr;
using static MirrorDungeonSelectThemeUIPanel.UIResources;
using UnityEngine.Networking;
using SD;
using UnityEngine.UIElements;
using BattleUI;
using BattleUI.Dialog;
using StorySystem;
using System.Timers;
using BattleUI.Operation;

namespace SpiderDon
{
    // 自定义状态枚举
    public enum UnitState
    {
        Initial,
        Moving,
        Resting
    }

    // 自定义视图类（不继承任何类）
    public class CustomUnitView
    {
        public BattleUnitModel Model;
        public UnitState State = UnitState.Initial;
        public Vector3 TargetPosition;
        public int RestFramesRemaining;

        private const float MoveSpeed = 0.14f;
        private const int RestDuration = 15;

        public CustomUnitView(BattleUnitModel model)
        {
            Model = model;
            GenerateNewTarget();
        }

        public void Update()
        {
            BattleUnitModel_Abnormality abnormality =this.Model.TryCast<BattleUnitModel_Abnormality>();
            bool moving = true;
            if (abnormality != null)
            {
                Il2CppSystem.Collections.Generic.List<BattleUnitModel_Abnormality_Part> PartList = abnormality.PartList;
                foreach(BattleUnitModel_Abnormality_Part part in PartList)
                {
                    if (part is BattleUnitModel_Abnormality_Part && !((BattleUnitModel_Abnormality_Part)part).IsActionable())
                    {
                        moving = false;

                    }
                }
            }
            
            if ((moving|| this.Model.IsDead())&&this.Model.GetUnitID() == 1138103 )
            switch (State)
            {
                case UnitState.Initial:
                    TransitionToMoving();
                    break;

                case UnitState.Moving:
                    UpdateMovement();
                    break;

                case UnitState.Resting:
                    UpdateRest();
                    break;
            }
        }

        private void TransitionToMoving()
        {
            State = UnitState.Moving;
            GenerateNewTarget();
        }

        private void UpdateMovement()
        {
            var view = SingletonBehavior<BattleObjectManager>.Instance.GetView(Model);
            if (view == null) return;

            Vector3 currentPos = view.WorldPosition;
            Vector3 newPos = Vector3.MoveTowards(currentPos, TargetPosition, MoveSpeed);

            view.WorldPosition = newPos;

            if (Vector3.Distance(newPos, TargetPosition) < 0.01f)
            {
                State = UnitState.Resting;
                RestFramesRemaining = RestDuration;
            }
        }

        private void UpdateRest()
        {
            if (--RestFramesRemaining <= 0)
            {
                State = UnitState.Initial;
            }
        }

        private void GenerateNewTarget()
        {
            TargetPosition = new Vector3(
                UnityEngine.Random.Range(-17f, 17f),
                0f,
                UnityEngine.Random.Range(-15.5f, 9.5f)
            );
        }
    }

    internal class ChangeAppearanceTemp : MonoBehaviour
    {

        private static ChangeAppearanceTemp _instance;
        private float _updateInterval = 0.05f;
        private float _lastUpdateTime;

        // 存储所有定制单位的列表
        public static System.Collections.Generic.List<CustomUnitView> _activeUnits = new System.Collections.Generic.List<CustomUnitView>();
        public static System.Collections.Generic.HashSet<int> _trackedIDs = new System.Collections.Generic.HashSet<int>();

        public static bool allow = false;

        public static void Setup(Harmony harmony)
        {
            ClassInjector.RegisterTypeInIl2Cpp<ChangeAppearanceTemp>();

            GameObject obj = new GameObject("ChangeAppearanceController");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave;
            _instance = obj.AddComponent<ChangeAppearanceTemp>();
            _instance.enabled = true;
        }


        private void Update()
        {
            if(allow)
            {
                ProcessUnitsUpdate();
            }
            


        }
        private void ProcessUnitsUpdate()
        {
            // 更新所有单位状态
            foreach (var unit in _activeUnits)
            {
                try
                {
                    unit.Update();
                }
                catch
                {
                    allow = false;
                }
            }
        }
        public static void RefreshUnits(BattleObjectManager manager)
        {
            foreach (var model in manager.GetModelList())
            {
                if (IsValidUnit(model) && !_trackedIDs.Contains(model._instanceID))
                {
                    _activeUnits.Add(new CustomUnitView(model));
                    _trackedIDs.Add(model._instanceID);
                    
                }
            }
        }

        private static bool IsValidUnit(BattleUnitModel model)
        {
            return model.GetUnitID() == 1138103;
        }

    }




    [BepInPlugin(Guid, Name, Version)]
    public class Main : BasePlugin
    {
        public const string Guid = Author + "." + Name;
        public const string Name = "SpiderDon";
        public const string Version = "0.0.1";
        public const string Author = "Tintagedfish";
        public static ManualLogSource SharedLog;

        private static int cub = 1;

        public override void Load()
        {
            // 打印日志信息
            SharedLog = Log;
            Harmony harmony = new Harmony("SpiderDon");

            Harmony.CreateAndPatchAll(typeof(Main));
            ChangeAppearanceTemp.Setup(harmony);
            Log.LogInfo("Hello LimbusConpanay!!!This is SpiderDon MOD!!!");
            Log.LogWarning("This is Warning!!!From SpiderDon MOD.");
        }
        [HarmonyPatch(typeof(BattleObjectManager), "OnRoundStart_View")]
        [HarmonyPostfix]
        private static void BattleObjectManager_OnRoundStart_Model(BattleObjectManager __instance)
        {
            ChangeAppearanceTemp._activeUnits.Clear();
            ChangeAppearanceTemp._trackedIDs.Clear();
            ChangeAppearanceTemp.allow = true;
            Main.SharedLog.LogWarning("?????????????????????????????????");
            ChangeAppearanceTemp.RefreshUnits(__instance);
            Main.SharedLog.LogWarning("?????????????????????????????????");
            foreach (var model in __instance.GetModelList())
            {
                if (model.GetUnitID() == 1138103)
                {
                    Vector3 TargetPosition = new Vector3(
                    UnityEngine.Random.Range(-17f, 17f),
                    0f,
                    UnityEngine.Random.Range(-15.5f, 9.5f)

                    );
                    SingletonBehavior<BattleObjectManager>.Instance.GetView(model).WorldPosition = TargetPosition;
                }
            }
        }
        
        [HarmonyPatch(typeof(BattleObjectManager), "OnStageEnd")]
        [HarmonyPostfix]
        private static void BattleObjectManager_OnStageEnd(BattleObjectManager __instance)
        {
            ChangeAppearanceTemp.allow = false;
            Main.SharedLog.LogWarning("?????????????????????????????????");
            ChangeAppearanceTemp.RefreshUnits(__instance);
            ChangeAppearanceTemp._activeUnits.Clear();
            ChangeAppearanceTemp._trackedIDs.Clear();
            
            Main.SharedLog.LogWarning("?????????????????????????????????");
        }

        [HarmonyPatch(typeof(BattleUnitView), "Start")]
        [HarmonyPrefix]
        private static void StartPostfix(BattleUnitView __instance)
        {
            if (__instance.unitModel.GetUnitID() == 1138103)
            {
                float scaleRate = UnityEngine.Random.Range(0.7f, 1.5f);
                //Main.SharedLog.LogWarning("(!#!#!#!#)" + __instance.UIManager._upperUICanvasGroup.transform.localPosition);
                __instance.Appearance.transform.localScale = Vector3.one * scaleRate;
                __instance.UIManager._upperUICanvasGroup.transform.localScale = Vector3.one * 0.01f*scaleRate;
                __instance.UIManager._batonUI.transform.localScale= Vector3.one * 0.01f * scaleRate;
                //Vector3 newPosition = new Vector3(0f, 0f, -10f);
                //Main.SharedLog.LogWarning("(!#!#!#!#)" + __instance.UIManager._upperUICanvasGroup.transform.localPosition);
                
            }
            
        }
        [HarmonyPatch(typeof(PassiveDetail), "OnBattleStart")]
        [HarmonyPostfix]
        private static void Postfix_PassiveDetail_OnBattleStart(BATTLE_EVENT_TIMING timing, PassiveDetail __instance)
        {
            ChangeAppearanceTemp.allow = false;
        }
        private static bool BanAutoSelect()
        {
            bool hasDon = false;
            BattleObjectManager battleObjectManager = Singleton<SinManager>.Instance._battleObjectManager;
            foreach (BattleUnitModel model in battleObjectManager.GetModelList())
            {
                if (model.GetUnitID() == 1138103)
                {
                    hasDon = true;
                    break;
                }
            }
            if (Singleton<StageController>.Instance.GetCurrentRound() >= 2 && hasDon)
            {
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(NewOperationAutoSelectManager), "GetKeyboardInput")]
        [HarmonyPrefix]
        private static bool NewOperationAutoSelectManagerGetKeyboardInput(NewOperationAutoSelectManager __instance)
        {

            return BanAutoSelect();
        }

        [HarmonyPatch(typeof(NewOperationAutoSelectManager), "SetWinRateToggle")]
        [HarmonyPrefix]
        private static bool NewOperationAutoSelectManagerSetWinRateToggle(NewOperationAutoSelectManager __instance)
        {
            
            return BanAutoSelect();
        }
        [HarmonyPatch(typeof(NewOperationAutoSelectManager), "SetDamageToggle")]
        [HarmonyPrefix]
        private static bool NewOperationAutoSelectManagerSetDamageToggle(NewOperationAutoSelectManager __instance)
        {
            
            return BanAutoSelect();
        }
        [HarmonyPatch(typeof(PassiveDetail), "OnRoundStart_After_Event")]
        [HarmonyPostfix]
        private static void Postfix_PassiveDetail_OnRoundStart_After_Event(BATTLE_EVENT_TIMING timing, PassiveDetail __instance)
        {
            if (__instance._owner.GetUnitID() == 1138103 || __instance._owner.GetUnitID() == 113810301)
            {
                UnlockInformationManager instance7 = Singleton<UnlockInformationManager>.Instance;
                foreach (PassiveModel passiveModel2 in __instance._owner.GetPassiveList())
                {
                    instance7.UnlockPassiveStatus(__instance._owner.GetOriginUnitID(), 113810321);
                }
                foreach (SkillModel skill in __instance._owner.GetSkillList())
                {
                    instance7.UnlockSkill(__instance._owner.GetOriginUnitID(), skill);
                }
            }
        }



    }
}