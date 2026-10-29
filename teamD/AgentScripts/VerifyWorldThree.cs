using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class VerifyWorldThree {
 static Vector2Int P(JToken p)=>new Vector2Int((int)p[0],(int)p[1]);
 static int AssertFixed(TileGrid grid){var input=UnityEngine.Object.FindFirstObjectByType<TileGridController>();int count=0;foreach(var g in grid.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c is Warp||c is OnOffSwitch||c is OnOffFloor).Select(c=>c.GetComponentInParent<TileGroup>()).Distinct()){if(g==null||!g.IsStatic||input.BeginDragTile(g,null))throw new Exception("A fixed mechanic can be dragged");count++;}return count;}
 static void AssertFloors(TileGrid grid){foreach(var floor in grid.GetComponentsInChildren<OnOffFloor>(true)){var g=floor.GetComponent<TileGroup>();foreach(var c in g.Cells){var p=g.GridPosition+c.LocalPosition;if(floor.IsSolid){if(grid.GetCell(p)!=c)throw new Exception("Solid floor missing");}else if(grid.GetCell(p)!=null||grid.IsInPlacementArea(p))throw new Exception("Inactive floor not reserved");}}}
 static void Move(JToken action,TileGrid grid,TileGroup[] groups,Vector2Int occupied){var g=groups.Single(g=>g.name==(string)action["group"]);if(g.IsStatic||g.Cells.Any(c=>g.GridPosition+c.LocalPosition==occupied))throw new Exception("Moving reserved/fixed group");var old=g.GridPosition;grid.RemoveTile(g);if(!grid.SetTile(g,P(action["to"]))){grid.SetTile(g,old);throw new Exception("Invalid placement: "+g.name+" "+action["to"]);}}
 public static string CheckAssets(){var setup=EditorSceneManager.GetSceneManagerSetup();var log=new StringBuilder();try{for(int s=1;s<=4;s++){
  string pp=$"Assets/NormalStage/stage3-{s}/Stage3-{s}_Layout.prefab";var asset=PrefabUtility.LoadPrefabContents(pp);try{foreach(var g in asset.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c is Warp||c is OnOffSwitch||c is OnOffFloor).Select(c=>c.GetComponentInParent<TileGroup>()).Distinct())if(g==null||!g.IsStatic)throw new Exception("Prefab mechanic not fixed: "+(g==null?"missing owner":g.name));}finally{PrefabUtility.UnloadPrefabContents(asset);}
  EditorSceneManager.OpenScene($"Assets/NormalStage/stage3-{s}/stage3-{s}.unity",OpenSceneMode.Single);var grid=UnityEngine.Object.FindFirstObjectByType<TileGrid>();int count=AssertFixed(grid);var p=new SerializedObject(grid).FindProperty("_placementPositions");var allowed=Enumerable.Range(0,p.arraySize).Select(i=>p.GetArrayElementAtIndex(i).vector2IntValue).ToHashSet();foreach(var cell in grid.GetComponentsInChildren<TileCell>()){var pos=grid.WorldToGrid(cell.transform.position);for(int x=-2;x<=2;x++)for(int y=-2;y<=2;y++)if(!allowed.Contains(pos+new Vector2Int(x,y)))throw new Exception("Missing margin");}
  log.AppendLine($"3-{s}: {count} mechanic groups fixed in scene and prefab; BeginDragTile rejected each; full 2-cell margin verified.");
 }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}File.WriteAllText("Logs/WorldThree/asset-check.txt",log.ToString());return log.ToString();}
 public static string Prepare(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Playing");var setup=EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>s.isLoaded&&SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Unsaved scene");File.WriteAllLines("Logs/WorldThree/editor-scenes.txt",setup.Where(s=>s.isLoaded).Select(s=>(s.isActive?"1|":"0|")+s.path));EditorSceneManager.OpenScene("Assets/NormalStage/stage3-1/stage3-1.unity",OpenSceneMode.Single);return "Prepared";}
 public static string Restore(){EditorSceneManager.RestoreSceneManagerSetup(File.ReadAllLines("Logs/WorldThree/editor-scenes.txt").Select(l=>new SceneSetup{path=l.Substring(2),isActive=l.StartsWith("1|"),isLoaded=true}).ToArray());return "Restored editor scenes";}
 public static Task<string> Play()=>PlayFiles(false);
 public static Task<string> PlayAlternate()=>PlayFiles(true);
 static async Task<string> PlayFiles(bool alternate){
  if(!Application.isPlaying)throw new Exception("Not playing");bool background=Application.runInBackground;Application.runInBackground=true;var log=new StringBuilder();
  try{foreach(int s in alternate?new[]{3}:new[]{1,2,3,4}){
   Time.timeScale=1;var load=SceneManager.LoadSceneAsync($"Assets/NormalStage/stage3-{s}/stage3-{s}.unity");while(!load.isDone)await Task.Delay(10);await Task.Delay(100);
   var player=UnityEngine.Object.FindFirstObjectByType<Player>();var grid=player.Grid;var groups=grid.GetComponentsInChildren<TileGroup>();var controller=grid.GetComponentsInChildren<OnOffController>().Single();var plan=JObject.Parse(File.ReadAllText($"Logs/WorldThree/solution{s}"+(alternate?"-alternate":"")+".json"));int moves=0,warps=0,toggles=0,firstFrame=Time.frameCount;bool clear=false;
   Application.LogCallback cb=(message,stack,type)=>{if(message.Contains("クリア！！！"))clear=true;};Application.logMessageReceived+=cb;
   try{
    double startup=EditorApplication.timeSinceStartup+5;while(player.CurrnetPosition!=P(plan["steps"][0]["from"])){if(EditorApplication.timeSinceStartup>startup)throw new Exception("Player did not initialize");await Task.Delay(10);}AssertFixed(grid);AssertFloors(grid);
    foreach(var step in plan["steps"]){
     if(player.CurrnetPosition!=P(step["from"]))throw new Exception("Unexpected live path");foreach(var action in step["moves"]){Move(action,grid,groups,player.CurrnetPosition);moves++;}
     bool before=controller.IsOn;var next=P(step["position"]);double until=EditorApplication.timeSinceStartup+12;while(player.CurrnetPosition!=next){if(EditorApplication.timeSinceStartup>until)throw new Exception($"3-{s}: stalled {player.CurrnetPosition}, expected {next}");await Task.Delay(10);}
     if(player.Direction!=P(step["direction"]))throw new Exception("Direction mismatch");if(controller.IsOn!=(bool)step["state"])throw new Exception("ON/OFF mismatch");if(controller.IsOn!=before)toggles++;if((bool)step["warp"])warps++;AssertFloors(grid);
    }
    await Task.Delay(600);var goal=grid.GetCell(player.CurrnetPosition).GetComponent<GoalCell>();if(goal==null||!goal.HasRequiredKeys(player)||!clear||Time.frameCount-firstFrame<10)throw new Exception("Key/clear/frame verification failed");AssertFixed(grid);
    log.AppendLine($"3-{s}: normal-speed Play Mode clear; {moves} placements, {warps} warps, {toggles} state changes; fixed drag rejected; inactive floors reserved; {Time.frameCount-firstFrame} advancing frames.");File.WriteAllText(alternate?"Logs/WorldThree/alternate-play-check.txt":"Logs/WorldThree/play-check.txt",log.ToString());
   }finally{Application.logMessageReceived-=cb;Time.timeScale=1;}
  }}finally{Application.runInBackground=background;}return log.ToString();
 }
}
