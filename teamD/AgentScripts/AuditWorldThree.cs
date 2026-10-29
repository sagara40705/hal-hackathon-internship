using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class AuditWorldThree {
 public static string Run(){var setup=EditorSceneManager.GetSceneManagerSetup();var b=new StringBuilder();try{
 foreach(var path in new[]{"Assets/Gimmick/Prefab/TileGroup/Group_OnOffSwitch.prefab","Assets/Gimmick/Prefab/TileGroup/Group_OnOffFloor_On.prefab","Assets/Gimmick/Prefab/TileGroup/Group_OnOffFloor_Off.prefab"}){var p=AssetDatabase.LoadAssetAtPath<GameObject>(path);b.AppendLine(path+" "+string.Join(";",p.GetComponentsInChildren<Transform>(true).Select(t=>t.name+":"+t.localPosition+":"+string.Join(",",t.GetComponents<MonoBehaviour>().Select(c=>c.GetType().Name)))));}
 for(int s=1;s<=4;s++){
 EditorSceneManager.OpenScene($"Assets/NormalStage/stage3-{s}/stage3-{s}.unity",OpenSceneMode.Single);var grid=UnityEngine.Object.FindFirstObjectByType<TileGrid>();b.AppendLine("STAGE "+s+" root="+PrefabUtility.GetOutermostPrefabInstanceRoot(grid.GetComponentsInChildren<TileGroup>()[0].gameObject).name);
 foreach(var g in grid.GetComponentsInChildren<TileGroup>()){var cells=g.GetComponentsInChildren<TileCell>();b.AppendLine(g.name+" fixed="+g.IsStatic+" cells="+cells.Length+" types="+string.Join(",",g.GetComponentsInChildren<MonoBehaviour>(true).Select(c=>c.GetType().Name).Distinct()));foreach(var w in g.GetComponentsInChildren<Warp>())b.AppendLine("  WARP "+grid.WorldToGrid(w.transform.position)+" delta "+w.WarpAmount);}
 foreach(var c in grid.GetComponentsInChildren<OnOffController>())b.AppendLine("CONTROLLER "+c.name+" state="+c.IsOn);
 foreach(var k in UnityEngine.Object.FindObjectsByType<GridItem>(FindObjectsSortMode.None))b.AppendLine("KEY "+k.GridPosition);
 b.AppendLine("GOAL "+grid.WorldToGrid(grid.GetComponentInChildren<GoalCell>().transform.position));}
 }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}return b.ToString();}
}
