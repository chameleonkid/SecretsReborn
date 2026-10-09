using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class SpellFoundationSetup
    {
        static SpellFoundationSetup()=>EditorApplication.update+=Requested;
        private static void Requested()
        {
            const string request="Temp/SetupSpellFoundation.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request); try { Setup(); } catch (Exception e) { File.WriteAllText("Temp/SpellFoundationReport.txt","FAIL: "+e); Debug.LogException(e); }
        }
        [MenuItem("SecretsReborn/Magic/Prepare spell foundation")]
        public static void Setup()
        {
            const string folder="Assets/Resources/Magic/Spells"; Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            foreach (bool healing in new[] { false,true })
            {
                string id=healing ? "heal" : "fireball",path=folder+"/"+id+".asset";
                string previous="Assets/World/Magic/Spells/"+id+".asset";
                if (AssetDatabase.LoadAssetAtPath<SpellDefinition>(path)==null && AssetDatabase.LoadAssetAtPath<SpellDefinition>(previous)!=null)
                { string error=AssetDatabase.MoveAsset(previous,path); if (!string.IsNullOrEmpty(error)) throw new Exception(error); }
                if (AssetDatabase.LoadAssetAtPath<SpellDefinition>(path)!=null) continue;
                var ranks=new SpellRankDefinition[3]; int[] budgets=healing ? new[] { 2,4,6 } : new[] { 5,8,12 };
                for (int i=0;i<3;i++) ranks[i]=new SpellRankDefinition { totalBudget=budgets[i],visualProjectiles=!healing && i==2 ? 2 : 1,manaCost=10+i*5,castTime=.6f,cooldown=2,range=8 };
                var definition=ScriptableObject.CreateInstance<SpellDefinition>(); definition.Configure(id,healing ? "Heilung" : "Feuerball",healing ? SpellElement.Light : SpellElement.Fire,healing ? SpellTargetKind.LivingAlly : SpellTargetKind.Enemy,ranks);
                AssetDatabase.CreateAsset(definition,path);
            }
            var world=new WorldSessionState("spell-foundation-test"); world.CharacterInventory("mage"); world.CharacterSpells("mage").Learn("fireball",3); world.CharacterSpells("mage").Learn("fireball",3);
            string save=Path.GetFullPath("Temp/SpellFoundation.es3");
            try
            {
                SaveGameStore.Save(world,save); if (SaveGameStore.Load(save).CharacterSpells("mage").Rank("fireball")!=2) throw new Exception("Spell file roundtrip failed");
                var old=world.Capture(); old.version=16; foreach (var actor in old.characters) actor.spells=null;
                if (WorldSessionState.Restore(old).CharacterSpells("mage").Rank("fireball")!=0) throw new Exception("Spell migration failed");
            }
            finally { if (ES3.FileExists(save)) ES3.DeleteFile(save); if (ES3.FileExists(save+".bac")) ES3.DeleteFile(save+".bac"); }
            AssetDatabase.SaveAssets(); File.WriteAllText("Temp/SpellFoundationReport.txt","PASS: Fire/Light spell definitions, native spell-rank file roundtrip and format-16 migration. Ring UI and effect animations remain pending.");
        }
    }
}
