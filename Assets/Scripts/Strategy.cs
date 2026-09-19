using System;
using System.Collections.Generic;
using UnityEngine;

namespace Verdant
{
    public enum Difficulty { Standard, Veteran, Nightmare }
    public enum TargetPriority { First, Strongest, Support }
    [Serializable] public class WavePlan
    {
        public string Name, Intel, Rule;
        public int Count; public float Gap; public int[] Roster; public bool Boss;
        public WavePlan(string name,string intel,string rule,int count,float gap,int[] roster,bool boss=false)
        {Name=name;Intel=intel;Rule=rule;Count=count;Gap=gap;Roster=roster;Boss=boss;}
    }
    public class Relic
    {
        public string Name, Description;public Relic(string n,string d){Name=n;Description=d;}
    }
    public static class Campaign
    {
        public static readonly WavePlan[] Waves = {
            new WavePlan("林缘试探","普通行者与斥候混编。不要把所有金币花在入口。","常规突袭",12,.83f,new[]{0,0,0,1}),
            new WavePlan("疾行穿插","斥候移动很快，减速比堆伤害更有效。","急行军 · 移速 +15%",16,.66f,new[]{1,0,1,0}),
            new WavePlan("钢甲前锋","重甲抵抗普通弹丸；花火爆破与穿甲进化可以克制。","重甲纵队",17,.78f,new[]{0,2,0,2,1}),
            new WavePlan("护盾回廊","蓝色护盾先于生命承伤。霜晶对护盾造成 3 倍伤害。","护盾护送",20,.66f,new[]{4,0,4,1,0}),
            new WavePlan("荆棘领主","首领半血狂暴并周期震荡，预警圈内的塔会被干扰。","首领战 · 震荡干扰",20,.76f,new[]{2,0,4,1},true),
            new WavePlan("复苏仪式","治疗师每 3 秒恢复附近敌人。将塔的优先级改为支援。","治疗阵线",24,.60f,new[]{0,5,2,0,4,1}),
            new WavePlan("暗夜奔袭","疾行敌群分两段涌入。霜晶控制配合天火。","急行军 · 移速 +15%",28,.48f,new[]{1,1,0,4,1,5}),
            new WavePlan("铁壁洪流","重甲与护盾并进。至少进化一座穿甲或攻城塔。","铁壁 · 重甲额外减伤",25,.64f,new[]{2,4,2,5,0}),
            new WavePlan("孢群狂潮","大量低生命敌人拥挤进场，燃烧区域可以持续阻截。","密集集群",38,.32f,new[]{6,6,0,6,5,1}),
            new WavePlan("双重压境","首领携带护盾护卫与治疗支援，提前准备主动技能。","首领战 · 震荡干扰",30,.59f,new[]{4,5,2,1,0},true),
            new WavePlan("破晓疾行","每一种敌人都将提速，前后两段都需要防御覆盖。","急行军 · 移速 +15%",34,.46f,new[]{1,4,1,5,2}),
            new WavePlan("再生军团","连续治疗与重甲盾墙。支援优先与霜裂易伤很重要。","治疗阵线",34,.55f,new[]{5,2,4,0,5,2}),
            new WavePlan("余烬潮汐","孢群掩护精锐推进，用范围火力清理前排。","密集集群",46,.31f,new[]{6,6,2,4,5,1}),
            new WavePlan("最后壁垒","高密度重甲护盾队。不要把技能全部留给最后一波。","铁壁 · 重甲额外减伤",38,.48f,new[]{2,4,5,2,4,1}),
            new WavePlan("永夜之王","最终首领半血后加速并强化干扰，支援单位必须优先处理。","最终战 · 狂暴首领",42,.44f,new[]{2,4,5,1,6,4},true)
        };
        public static readonly Relic[] Relics={
            new Relic("翠玉锋芒","所有防御塔伤害 +15%。可叠加。"),
            new Relic("远望透镜","所有防御塔射程 +0.45 米。"),
            new Relic("战地征税","击杀金币 +20%，向上取整。"),
            new Relic("复苏种子","恢复 6 点核心生命，并获得 65 金币。"),
            new Relic("星火回路","主动技能冷却减少 18%，可叠加，最低为原值的 45%。"),
            new Relic("共鸣脉冲","防御塔攻速 +14%。可叠加。"),
            new Relic("深度冻结","减速持续时间 +1 秒，霜晶伤害 +20%。"),
            new Relic("回收协议","出售返还 90% 总投入，并立即获得 90 金币。"),
            new Relic("陨星核心","天火伤害 +45%，爆炸半径 +0.7 米。")
        };
    }
    public partial class DefenseGame
    {
        public Difficulty Mode=Difficulty.Veteran;
        public float DamageBonus, RangeBonus, BountyBonus, HasteBonus, SlowBonus, FrostBonus, MeteorBonus;
        public float CooldownFactor=1, RefundRate=.7f;
        public float MeteorCooldown, FreezeCooldown, OverdriveCooldown, OverdriveUntil;
        public bool MeteorArmed, RewardPending;
        public readonly List<int> RelicHistory=new List<int>();
        public int[] RewardOptions=new int[3];
        public float ThreatMultiplier {get{return Mode==Difficulty.Standard?1:Mode==Difficulty.Veteran?1.24f:1.55f;}}
        public WavePlan CurrentPlan {get{return Campaign.Waves[Mathf.Clamp(Wave-1,0,Campaign.Waves.Length-1)];}}
        public WavePlan NextPlan {get{return Campaign.Waves[Mathf.Clamp(Wave,0,Campaign.Waves.Length-1)];}}
        public bool Overdriven {get{return Time.time<OverdriveUntil;}}
        public void SetDifficulty(int value)
        {if(Wave==0 && State==RunState.Planning && !ModalOpen)Mode=(Difficulty)Mathf.Clamp(value,0,2);}
        void TickStrategy(float dt)
        {
            if(!Running || State!=RunState.Wave)return;
            MeteorCooldown=Mathf.Max(0,MeteorCooldown-dt);FreezeCooldown=Mathf.Max(0,FreezeCooldown-dt);OverdriveCooldown=Mathf.Max(0,OverdriveCooldown-dt);
        }
        public void ArmMeteor()
        {
            if(!Running || State!=RunState.Wave || MeteorCooldown>0)return;
            MeteorArmed=!MeteorArmed;Blueprint=-1;Selected=null;
            Tell(MeteorArmed?"天火已就绪：点击地图指定轰炸区域，Esc 取消。":"已取消天火瞄准。");
        }
        public bool CastMeteor(Vector3 point)
        {
            if(!Running || State!=RunState.Wave || MeteorCooldown>0 || Mathf.Abs(point.x)>13 || Mathf.Abs(point.z)>11)return false;
            MeteorArmed=false;MeteorCooldown=26*CooldownFactor;
            var marker=WorldBuilder.Ring("Meteor warning",point+Vector3.up*.06f,3+MeteorBonus*1.56f,new Color(1,.43f,.23f),.07f,WorldRoot);
            var strike=marker.gameObject.AddComponent<MeteorStrike>();strike.Point=point;strike.Damage=(210+Wave*24)*(1+MeteorBonus);strike.Radius=3+MeteorBonus*1.56f;
            Tell("天火锁定！1 秒后轰击目标区域。");return true;
        }
        public bool CastFreeze()
        {
            if(!Running || State!=RunState.Wave || FreezeCooldown>0)return false;
            FreezeCooldown=36*CooldownFactor;
            foreach(var e in Enemies)if(e!=null)e.Freeze(3.2f);
            WorldBuilder.Burst(Vector3.up*.25f,new Color(.45f,.8f,1f),12);
            Tell("极寒领域：冻结全场敌人，首领冻结时间减半。");return true;
        }
        public bool CastOverdrive()
        {
            if(!Running || State!=RunState.Wave || OverdriveCooldown>0)return false;
            OverdriveCooldown=40*CooldownFactor;OverdriveUntil=Time.time+8;
            foreach(var t in Towers)WorldBuilder.Burst(t.transform.position+Vector3.up*.8f,new Color(1,.8f,.35f),1);
            Tell("全塔超频：8 秒内攻速提高 65%，并免疫首领干扰。");return true;
        }
        public bool EvolveSelected(int branch)
        {
            if(!Running || Selected==null || Selected.Level!=2 || branch<1 || branch>2)return false;
            int cost=Selected.UpgradeCost;if(Gold<cost){Tell("金币不足，暂时无法进化。");return false;}
            Gold-=cost;Selected.Invested+=cost;Selected.Level=3;Selected.Branch=branch;Selected.RefreshLevel();
            Tell("进化完成："+Selected.EvolutionName+"。进化路线无法撤销。");return true;
        }
        public void CyclePriority()
        {if(Selected!=null && Running)Selected.Priority=(TargetPriority)(((int)Selected.Priority+1)%3);}
        void OfferReward()
        {
            RewardPending=true;MeteorArmed=false;Selected=null;Blueprint=-1;
            int seed=Wave/3;
            // Deterministic draft keeps runs reproducible; distinct choices each draft.
            RewardOptions[0]=(seed*2-2)%Campaign.Relics.Length;
            RewardOptions[1]=(seed*2+1)%Campaign.Relics.Length;
            RewardOptions[2]=(seed*2+4)%Campaign.Relics.Length;
            Time.timeScale=0;
        }
        public bool ChooseRelic(int slot)
        {
            if(!RewardPending || slot<0 || slot>2)return false;
            int id=RewardOptions[slot];RelicHistory.Add(id);
            switch(id){
                case 0:DamageBonus+=.15f;break;
                case 1:RangeBonus+=.45f;break;
                case 2:BountyBonus+=.2f;break;
                case 3:Lives=Mathf.Min(20,Lives+6);Gold+=65;break;
                case 4:CooldownFactor=Mathf.Max(.45f,CooldownFactor*.82f);break;
                case 5:HasteBonus+=.14f;break;
                case 6:SlowBonus+=1;FrostBonus+=.2f;break;
                case 7:RefundRate=.9f;Gold+=90;break;
                case 8:MeteorBonus+=.45f;break;
            }
            RewardPending=false;Time.timeScale=Paused?0:Speed;Tell("获得遗物："+Campaign.Relics[id].Name);return true;
        }
        public string SkillLabel(int skill)
        {
            float cd=skill==0?MeteorCooldown:skill==1?FreezeCooldown:OverdriveCooldown;
            string name=skill==0?"Q  天火轰击":skill==1?"W  极寒领域":"E  全塔超频";
            return name+(cd>0?"  "+Mathf.CeilToInt(cd)+"s":skill==0&&MeteorArmed?"  瞄准中":"  就绪");
        }
    }
    public class MeteorStrike : MonoBehaviour
    {
        public Vector3 Point;public float Damage,Radius;float clock;
        void Update()
        {
            var game=DefenseGame.Instance;if(!game.Running)return;
            clock+=Time.deltaTime;if(clock<1)return;
            foreach(var e in game.Enemies.ToArray())if(e!=null && !e.Dead && Vector3.Distance(e.transform.position,Point)<Radius&&TerrainOcclusion.Clear(Point+Vector3.up*.2f,e.AimPosition))e.Hit(Damage,false,1);
            WorldBuilder.Burst(Point+Vector3.up*.4f,new Color(1,.56f,.25f),Radius);
            var falling=WorldBuilder.Shape("Meteor impact",PrimitiveType.Sphere,Point+Vector3.up*.5f,Vector3.one*.6f,new Color(1,.65f,.3f),game.WorldRoot,false,true);
            Destroy(falling,.35f);Destroy(gameObject);
        }
    }
    public class BurningGround : MonoBehaviour
    {
        public float Damage,Radius=1.7f;float elapsed,tick;
        public static void Spawn(Vector3 p,float damage)
        {
            var ring=WorldBuilder.Ring("Burning ground",p+Vector3.up*.08f,1.7f,new Color(1,.4f,.18f,.8f),.07f,DefenseGame.Instance.WorldRoot);
            var fire=ring.gameObject.AddComponent<BurningGround>();fire.Damage=damage;
        }
        void Update()
        {
            var game=DefenseGame.Instance;if(!game.Running)return;
            elapsed+=Time.deltaTime;tick-=Time.deltaTime;
            if(tick<=0){tick=.5f;
                Vector3 center=GetComponent<LineRenderer>().GetPosition(0)-Vector3.right*Radius;
                foreach(var e in game.Enemies.ToArray())if(e!=null && !e.Dead && Vector3.Distance(e.transform.position,center)<Radius&&TerrainOcclusion.Clear(center+Vector3.up*.15f,e.AimPosition))e.Hit(Damage*.5f,false,.6f);
            }
            if(elapsed>=3.5f)Destroy(gameObject);
        }
    }
}
