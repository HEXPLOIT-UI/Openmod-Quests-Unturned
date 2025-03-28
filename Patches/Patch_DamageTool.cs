using HarmonyLib;
using SDG.Unturned;

namespace Quests.Patches
{
    [HarmonyPatch(typeof(DamageTool))]
    internal static class Patch_DamageTool
    {
        internal static DamageZombieParameters s_CurrentDamageZombieParameters;
        internal static DamageAnimalParameters s_CurrentDamageAnimalParameters;

        [HarmonyPatch(nameof(DamageTool.damageZombie))]
        [HarmonyPrefix]
        private static void PreDamageZombie(DamageZombieParameters parameters)
        {
            s_CurrentDamageZombieParameters = parameters;
        }

        [HarmonyPatch(nameof(DamageTool.damageZombie))]
        [HarmonyPostfix]
        private static void PostDamageZombie()
        {
            s_CurrentDamageZombieParameters = default;
        }

        [HarmonyPatch(nameof(DamageTool.damageAnimal))]
        [HarmonyPrefix]
        private static void PreDamageAnimal(DamageAnimalParameters parameters)
        {
            s_CurrentDamageAnimalParameters = parameters;
        }

        [HarmonyPatch(nameof(DamageTool.damageAnimal))]
        [HarmonyPostfix]
        private static void PostDamageAnimal()
        {
            s_CurrentDamageAnimalParameters = default;
        }
    }
}
