using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Verdant
{
    // Executes the real runtime components without UI input. Extra gold is only used
    // in lifecycle coverage, never in BalanceTest.
    public class SmokeTest : MonoBehaviour
    {
        DefenseGame game;string output;int assertions;float realStart;
        void Check(bool condition,string message){if(!condition)throw new Exception("SMOKE FAIL: "+message);assertions++;Debug.Log("SMOKE PASS: "+message);}
        Enemy Make(int kind,int route=0){Enemy e=WorldBuilder.CreateEnemy(kind,1,route);game.Enemies.Add(e);return e;}
        void ClearEnemies(){foreach(var e in game.Enemies.ToArray()){game.Enemies.Remove(e);Destroy(e.gameObject);}}
        void CheckOcclusion()
        {
            Physics.SyncTransforms();
            Check(!TerrainOcclusion.Clear(new Vector3(-7,2,5),new Vector3(-7,2,9)),"actual cliff blocks line of sight");
            Check(!TerrainOcclusion.Clear(new Vector3(-8,2,-6),new Vector3(-8,5,-6)),"actual viaduct deck blocks cross-level fire");
            Check(TerrainOcclusion.Clear(new Vector3(20,2,20),new Vector3(22,2,20)),"open air remains clear");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Occlusion test wall";wall.layer=TerrainOcclusion.Layer;
            wall.transform.position=new Vector3(101,1,100);wall.transform.localScale=new Vector3(.08f,1,2);
            Physics.SyncTransforms();
            Vector3 from=new Vector3(100,1,100),to=new Vector3(102,1,100),contact;
            Check(!TerrainOcclusion.CanFire(from,to,TowerKind.Bolt),"straight shots cannot cross thin wall");
            Check(TerrainOcclusion.CanFire(from,to,TowerKind.Bloom),"mortar arc clears low cover");
            Check(TerrainOcclusion.Sweep(wall.transform.position,to,.045f,out contact),"cast starting inside solid is blocked");
            wall.GetComponent<Collider>().isTrigger=true;
            Check(TerrainOcclusion.CanFire(from,to,TowerKind.Bolt),"trigger volumes do not block shots");
            wall.GetComponent<Collider>().isTrigger=false;wall.layer=0;
            Check(TerrainOcclusion.CanFire(from,to,TowerKind.Bolt),"non-terrain colliders do not block shots");
            wall.layer=TerrainOcclusion.Layer;wall.transform.localScale=new Vector3(.08f,8,2);Physics.SyncTransforms();
            Check(!TerrainOcclusion.CanFire(from,to,TowerKind.Bloom),"tall wall also blocks mortar arc");
            Enemy enemy=Make(0);enemy.transform.position=to-Vector3.up*.5f;float health=enemy.Health;
            var shot=Projectile.Launch(from,enemy,TowerKind.Bolt,10);shot.Advance(1);
            Check(shot.Resolved&&Mathf.Abs(enemy.Health-health)<.001f,"fast projectile swept collision prevents through-wall damage");
            shot=Projectile.Launch(from,enemy,TowerKind.Bloom,10);shot.Advance(1);
            Check(shot.Resolved&&Mathf.Abs(enemy.Health-health)<.001f,"blocked mortar does not explode through terrain");
            wall.SetActive(false);Physics.SyncTransforms();
            shot=Projectile.Launch(from,enemy,TowerKind.Bolt,10);shot.Advance(1);
            Check(shot.Resolved&&Mathf.Abs(enemy.Health-(health-10))<.001f,"unobstructed projectile applies damage");
            shot.Advance(1);Check(Mathf.Abs(enemy.Health-(health-10))<.001f,"resolved projectile cannot damage twice");
            Destroy(wall);ClearEnemies();
        }
        IEnumerator Start()
        {
            game=GetComponent<DefenseGame>();output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","smoke-result.json"));realStart=Time.realtimeSinceStartup;
            yield return null;
            try{
                CheckOcclusion();
                Check(game.Pads.Count==18,"18 build pads exist");Check(game.Routes.Length==3,"three independent routes exist");
                Check(game.Routes[0][0].y<.3f&&game.Routes[1][0].y>3.5f&&game.Routes[2][0].y>6.3f,"three physical elevation levels");
                for(int i=0;i<3;i++)Check(Vector3.Distance(game.Routes[i][game.Routes[i].Length-1],game.Route[game.Route.Length-1])<.01f,"route "+i+" terminates at core");
                Check(game.Gold==380&&game.Lives==20,"starting economy");
                var t=game.Build(game.Pads[9],TowerKind.Bolt);Check(t!=null&&game.Gold==290,"elevated tower builds and charges price");
                Check(game.Build(game.Pads[9],TowerKind.Bolt)==null&&game.Gold==290,"duplicate construction rejected");
                Enemy e=Make(0,1);e.transform.position=t.Head.position+Vector3.right*2-Vector3.up*.5f;
                Check(t.CanReach(e),"same-level enemy is targetable");e.transform.position+=Vector3.up*6;
                Check(!t.CanReach(e),"vertical distance excludes out-of-range enemy");
                e.transform.position=t.Head.position+Vector3.right*2+Vector3.down*2;
                Check(t.CanReach(e),"nearby cross-level enemy can be targeted");ClearEnemies();
                game.Selected=t;int cost=t.UpgradeCost;int gold=game.Gold;game.UpgradeSelected();Check(t.Level==2&&game.Gold==gold-cost,"level two upgrade");
                game.Gold=1000;game.Selected=t;Check(game.EvolveSelected(1)&&t.Level==3&&t.Branch==1,"sniper evolution");Check(!game.EvolveSelected(2),"evolution cannot be switched");
                int refund=t.SellValue;gold=game.Gold;game.SellSelected();Check(game.Pads[9].Occupant==null&&game.Gold==gold+refund,"sell clears elevated pad");
                game.Gold=0;Check(game.Build(game.Pads[0],TowerKind.Bolt)==null,"insufficient gold denied");
                game.Gold=10000;
                for(int k=0;k<3;k++)for(int branch=1;branch<=2;branch++){
                    t=game.Build(game.Pads[k*2+branch-1],(TowerKind)k);game.Selected=t;game.UpgradeSelected();Check(game.EvolveSelected(branch)&&t.Branch==branch,"evolution "+k+"/"+branch);
                }
                e=Make(4);float hp=e.Health,shield=e.Shield;e.Hit(10,true);Check(Mathf.Abs(e.Shield-(shield-30))<.01f&&e.Health==hp,"frost strips shield at triple strength");ClearEnemies();
                e=Make(2);hp=e.Health;e.Hit(10);Check(Mathf.Abs(hp-e.Health-5)<.01f,"armor reduces ordinary damage");hp=e.Health;e.Hit(10,false,1);Check(Mathf.Abs(hp-e.Health-10)<.01f,"armor piercing bypasses armor");ClearEnemies();
                game.TogglePause();Check(game.Paused&&Time.timeScale==0,"pause");game.TogglePause();game.ToggleSpeed();Check(Time.timeScale==2,"double speed");game.ToggleSpeed();
                game.RewardPending=true;game.RewardOptions=new[]{0,1,2};float bonus=game.DamageBonus;Check(game.ChooseRelic(0)&&game.DamageBonus>bonus&&!game.RewardPending,"draft applies permanent relic");Check(!game.ChooseRelic(0),"reward cannot be claimed twice");
                game.State=RunState.Wave;e=Make(0,2);Check(game.CastFreeze()&&e.Frozen,"global freeze affects upper route");Check(!game.CastFreeze(),"freeze cooldown enforced");
                t=game.Towers[0];t.Jam(4);Check(t.Jammed,"boss interference disables tower");Check(game.CastOverdrive()&&!t.Jammed,"overdrive counters interference");
                Check(game.CastMeteor(game.Routes[2][1]),"meteor can target mountain height");Check(!game.CastMeteor(game.Routes[2][1]),"meteor cooldown enforced");ClearEnemies();
                game.State=RunState.Planning;game.Gold=50000;game.DamageBonus=1.5f;game.Mode=Difficulty.Standard;
                for(int i=0;i<game.Pads.Count;i++){
                    t=game.Pads[i].Occupant;if(t==null)t=game.Build(game.Pads[i],(TowerKind)(i%3));game.Selected=t;
                    if(t.Level==1)game.UpgradeSelected();if(t.Level==2)game.EvolveSelected(i%2+1);
                }
                game.Selected=null;game.Blueprint=-1;game.Lives=20;
            }catch(Exception e){Fail(e);yield break;}
            DefenseGame.CaptureWorld(Path.GetFullPath(Path.Combine(Application.dataPath,"..","preview-three-fronts.png")));
            int[] seen=new int[3];bool sawRamp=false,sawBoss=false,sawSupport=false;
            while(game.State!=RunState.Victory&&game.State!=RunState.Defeat&&Time.realtimeSinceStartup-realStart<180){
                if(game.RewardPending)game.ChooseRelic(0);
                if(game.State==RunState.Planning)game.StartWave();Time.timeScale=25;
                foreach(var e in game.Enemies){seen[e.RouteIndex]++;if(e.Kind==3)sawBoss=true;if(e.Kind==5)sawSupport=true;if(e.RouteIndex==2&&e.transform.position.y<6.5f&&e.transform.position.y>4)sawRamp=true;}
                if(game.State==RunState.Wave&&game.Enemies.Count>5){game.CastFreeze();game.CastOverdrive();game.CastMeteor(game.Enemies[game.Enemies.Count/2].transform.position);}
                yield return null;
            }
            try{
                for(int i=0;i<3;i++)Check(seen[i]>0,"runtime spawns on route "+i);
                Check(sawRamp,"enemy physically traverses descending ramp");
                Check(sawBoss&&sawSupport,"boss and healer wave roster exercised");
                Check(game.State==RunState.Victory&&game.Wave==15,"fifteen-wave lifecycle reaches victory");
                Check(game.Enemies.Count==0,"no remaining enemies after victory");
                game.State=RunState.Wave;game.Lives=1;Enemy e=Make(3);game.RemoveEnemy(e,true);Destroy(e.gameObject);
                Check(game.State==RunState.Defeat&&game.Lives==0,"boss escape triggers defeat and clamps lives");
                File.WriteAllText(output,"{\"success\":true,\"assertions\":"+assertions+",\"kills\":"+game.Kills+",\"waves\":"+game.Wave+",\"routes\":3,\"rampObserved\":"+(sawRamp?"true":"false")+"}");
                Debug.Log("VERDANT_SMOKE_SUCCESS "+assertions);Application.Quit(0);
            }catch(Exception e){Fail(e);}
        }
        void Fail(Exception e){Debug.LogException(e);File.WriteAllText(output,"{\"success\":false,\"message\":\""+e.Message.Replace("\"","'")+"\"}");Application.Quit(1);}
    }
}
