using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Verdant
{
    // Natural economy only. Uses player-facing actions and no stat/gold grants.
    public class BalanceTest : MonoBehaviour
    {
        IEnumerator Start()
        {
            var game=GetComponent<DefenseGame>();float started=Time.realtimeSinceStartup;game.Sound=false;
            bool passive=Array.IndexOf(Environment.GetCommandLineArgs(),"--passive-test")>=0;
            bool standard=Array.IndexOf(Environment.GetCommandLineArgs(),"--standard")>=0;
            game.SetDifficulty(standard?0:1);
            game.Build(game.Pads[2],TowerKind.Bolt);game.Build(game.Pads[9],TowerKind.Bolt);game.Build(game.Pads[10],TowerKind.Frost);
            int[] order={2,9,14,10,13,5,11,8,7,16,4,15,0,12,6,17,1,3};
            TowerKind[] kinds={TowerKind.Bolt,TowerKind.Bloom,TowerKind.Bolt,TowerKind.Bloom,TowerKind.Bolt,TowerKind.Frost,TowerKind.Bolt,TowerKind.Bolt,TowerKind.Bloom,TowerKind.Bolt,TowerKind.Frost,TowerKind.Bloom,TowerKind.Bolt,TowerKind.Bloom,TowerKind.Bolt,TowerKind.Bolt,TowerKind.Frost,TowerKind.Bolt};
            int casts=0;
            while(game.State!=RunState.Victory&&game.State!=RunState.Defeat&&Time.realtimeSinceStartup-started<180){
                if(game.RewardPending)game.ChooseRelic(0);
                if(game.State==RunState.Planning){
                    Debug.Log("BALANCE wave="+game.Wave+" gold="+game.Gold+" lives="+game.Lives+" towers="+game.Towers.Count);
                    if(game.Wave>0){
                        if(game.Pads[14].Occupant==null)game.Build(game.Pads[14],TowerKind.Bolt);
                        // Upgrades at the three main junctions, then layered coverage.
                        foreach(int index in order){
                            Tower t=game.Pads[index].Occupant;
                            if(t!=null&&t.Level<3&&(index==2||index==9||index==14||game.Towers.Count>=9)){
                                game.Selected=t;if(t.Level==1)game.UpgradeSelected();else game.EvolveSelected(t.Kind==TowerKind.Frost?2:1);
                            }
                        }
                        foreach(int index in order)if(game.Pads[index].Occupant==null&&game.Gold>=DefenseGame.Specs[(int)kinds[index]].cost)game.Build(game.Pads[index],kinds[index]);
                        foreach(var t in game.Towers)if(t.Kind==TowerKind.Bolt)t.Priority=TargetPriority.Support;
                    }
                    game.Selected=null;game.StartWave();
                }
                Time.timeScale=25;
                if(!passive&&game.State==RunState.Wave&&game.Enemies.Count>4){
                    Enemy target=null;float best=float.NegativeInfinity;
                    foreach(var e in game.Enemies){
                        float score=0;foreach(var other in game.Enemies)if(Vector3.Distance(other.transform.position,e.transform.position)<3)score+=Mathf.Min(other.Health,210+game.Wave*24);
                        if(e.RemainingDistance<12)score*=1.7f;if(score>best){best=score;target=e;}
                    }
                    if(target!=null&&game.CastMeteor(target.transform.position))casts++;
                    if(game.Enemies.Count>7&&game.CastOverdrive())casts++;
                    if(game.Enemies.Count>8&&game.CastFreeze())casts++;
                }
                yield return null;
            }
            bool won=game.State==RunState.Victory;
            string result="{\"success\":"+(won?"true":"false")+",\"wave\":"+game.Wave+",\"lives\":"+game.Lives+",\"gold\":"+game.Gold+",\"kills\":"+game.Kills+",\"towers\":"+game.Towers.Count+",\"casts\":"+casts+",\"passive\":"+(passive?"true":"false")+",\"difficulty\":\""+game.Mode+"\"}";
            string name=passive?"balance-passive.json":standard?"balance-standard.json":"balance-veteran.json";
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"..",name)),result);Debug.Log("VERDANT_BALANCE_RESULT "+result);Application.Quit(won?0:1);
        }
    }
}
