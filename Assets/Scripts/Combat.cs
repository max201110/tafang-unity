using UnityEngine;
namespace Verdant
{
    public class Enemy : MonoBehaviour
    {
        public int Kind,Reward,RouteIndex;
        public float Health,MaxHealth,Travelled,Speed,Shield,Armor;
        public Transform Body,HealthBack,HealthFill;
        public GameObject ShieldVisual;
        public bool Dead,Enraged;
        public Vector3[] Path;
        public float PathLength;
        public Vector3 AimPosition {get{return transform.position+Vector3.up*(Kind==3?.9f:.5f);}}
        public float RemainingDistance {get{return Mathf.Max(0,PathLength-Travelled);}}
        public bool Frozen {get{return Time.time<freezeUntil;}}
        public bool Slowed {get{return Time.time<slowUntil;}}
        public bool Vulnerable {get{return Time.time<vulnerableUntil;}}
        int segment=1;float slowUntil,slowFactor=.52f,freezeUntil,vulnerableUntil,phase,supportClock=2.5f,jamClock=5,warningClock;
        LineRenderer warning;Vector3 warningCenter;
        public void Initialize(int kind,int wave,int routeIndex=0)
        {
            var game=DefenseGame.Instance;Kind=kind;RouteIndex=Mathf.Clamp(routeIndex,0,game.Routes.Length-1);Path=game.Routes[RouteIndex];
            PathLength=0;for(int i=1;i<Path.Length;i++)PathLength+=Vector3.Distance(Path[i-1],Path[i]);
            float growth=1+(wave-1)*.22f+Mathf.Pow(Mathf.Max(0,wave-4),1.45f)*.04f;
            float basic=kind==0?77:kind==1?48:kind==2?142:kind==3?950:kind==4?89:kind==5?112:32;
            MaxHealth=basic*growth*game.ThreatMultiplier;Health=MaxHealth;
            Speed=(kind==0?1.23f:kind==1?2.05f:kind==2?.96f:kind==3?.72f:kind==4?1.22f:kind==5?1.13f:1.75f)*(1+wave*.009f);
            if(wave==2 || wave==7 || wave==11)Speed*=1.15f;
            Reward=kind==3?65:kind==2?13:kind==1?7:kind==4?12:kind==5?15:kind==6?4:8;
            Armor=kind==2?.5f:kind==3?.2f:0;if(kind==2 && (wave==8||wave==14))Armor=.65f;
            Shield=kind==4?MaxHealth*.85f:0;
            transform.position=Path[0];phase=Random.value*6;
        }
        void Update()
        {
            var game=DefenseGame.Instance;if(Dead || !game.Running)return;
            if(ShieldVisual!=null)ShieldVisual.SetActive(Shield>0);
            RefreshHealth();
            if(Frozen)return;
            if(Kind==5) {
                supportClock-=Time.deltaTime;
                if(supportClock<=0) {
                    supportClock=3;
                    foreach(var e in game.Enemies)if(e!=null && !e.Dead && e!=this && Vector3.Distance(e.transform.position,transform.position)<2.65f)e.Health=Mathf.Min(e.MaxHealth,e.Health+e.MaxHealth*.07f);
                    WorldBuilder.Burst(transform.position+Vector3.up*.25f,new Color(.5f,1,.55f),2.65f);
                }
            }
            if(Kind==3)BossAbility(game);
            float step=Speed*Time.deltaTime*(Slowed?slowFactor:1)*(Enraged?1.4f:1);
            while(step>0 && segment<Path.Length) {
                Vector3 delta=Path[segment]-transform.position;float length=delta.magnitude;
                Vector3 flat=new Vector3(delta.x,0,delta.z);
                if(flat.sqrMagnitude>.0001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(flat),Time.deltaTime*12);
                if(step>=length){transform.position=Path[segment];segment++;step-=length;Travelled+=length;}
                else{transform.position+=delta.normalized*step;Travelled+=step;step=0;}
            }
            if(Body!=null)Body.localPosition=Vector3.up*Mathf.Abs(Mathf.Sin(Time.time*Speed*7+phase))*.045f;
            if(segment>=Path.Length){Dead=true;game.RemoveEnemy(this,true);Destroy(gameObject);}
        }
        void BossAbility(DefenseGame game)
        {
            if(!Enraged && Health<MaxHealth*.5f){Enraged=true;game.Tell("首领进入狂暴！移速 +40%，干扰频率提高。");}
            if(warning!=null) {
                warningClock-=Time.deltaTime;
                if(warningClock<=0) {
                    foreach(var t in game.Towers)if(t!=null && Vector3.Distance(t.Head.position,warningCenter)<4.2f)t.Jam(2.5f);
                    WorldBuilder.Burst(warningCenter,new Color(.95f,.35f,.8f),4.2f);Destroy(warning.gameObject);warning=null;
                }
                return;
            }
            jamClock-=Time.deltaTime;
            if(jamClock<=0){jamClock=Enraged?6:10;warningClock=1.2f;warningCenter=transform.position+Vector3.up*.2f;warning=WorldBuilder.Ring("Boss interference warning",transform.position+Vector3.up*.2f,4.2f,new Color(.95f,.3f,.7f,.8f),.055f,transform);}
        }
        public void Freeze(float seconds){freezeUntil=Mathf.Max(freezeUntil,Time.time+seconds*(Kind==3?.5f:1));}
        public void Expose(float duration){vulnerableUntil=Mathf.Max(vulnerableUntil,Time.time+duration);}
        public void Hit(float damage,bool slow=false,float armorPierce=0,float slowStrength=.52f)
        {
            if(Dead)return;
            if(slow){slowUntil=Time.time+2+DefenseGame.Instance.SlowBonus;slowFactor=slowStrength;}
            damage*=1-Armor*(1-Mathf.Clamp01(armorPierce));if(Vulnerable)damage*=1.3f;
            if(Shield>0){float multiplier=slow?3:1;float absorbed=Mathf.Min(Shield,damage*multiplier);Shield-=absorbed;damage-=absorbed/multiplier;}
            Health-=Mathf.Max(0,damage);RefreshHealth();
            if(Health<=0){Dead=true;WorldBuilder.Burst(transform.position+Vector3.up*.3f,new Color(1,.76f,.41f),.65f);DefenseGame.Instance.RemoveEnemy(this,false);Destroy(gameObject);}
        }
        void RefreshHealth()
        {
            float fraction=Mathf.Clamp01(Health/MaxHealth);
            if(HealthFill!=null){HealthFill.localScale=new Vector3(.83f*fraction,.06f,.1f);Vector3 p=HealthFill.localPosition;p.x=-.415f*(1-fraction);HealthFill.localPosition=p;}
        }
    }
    public class Tower : MonoBehaviour
    {
        public TowerKind Kind;public BuildPad Pad;public Transform Head;
        public int Level=1,Invested,Shots,Branch;
        public TargetPriority Priority;
        public float JammedUntil;
        public bool Jammed {get{return Time.time<JammedUntil&&!DefenseGame.Instance.Overdriven;}}
        public float Damage {get{
            float factor=Kind==TowerKind.Bolt?(Branch==1?2.2f:Branch==2?.88f:1):Kind==TowerKind.Frost?(Branch==2?1.8f:1):(Branch==1?1.65f:Branch==2?.85f:1);
            return DefenseGame.Specs[(int)Kind].damage*Mathf.Pow(1.48f,Level-1)*factor*(1+DefenseGame.Instance.DamageBonus)*(Kind==TowerKind.Frost?1+DefenseGame.Instance.FrostBonus:1);
        }}
        public float Range {get{return DefenseGame.Specs[(int)Kind].range+(Level-1)*.3f+DefenseGame.Instance.RangeBonus+(Kind==TowerKind.Bolt&&Branch==1?1.65f:0);}}
        public float Interval {get{return DefenseGame.Specs[(int)Kind].interval*Mathf.Pow(.91f,Level-1)*(Kind==TowerKind.Bolt&&Branch==1?1.65f:Kind==TowerKind.Bloom&&Branch==1?1.25f:1)/(1+DefenseGame.Instance.HasteBonus)/(DefenseGame.Instance.Overdriven?1.65f:1);}}
        public int UpgradeCost {get{return Mathf.RoundToInt(DefenseGame.Specs[(int)Kind].cost*(Level==1?.85f:1.4f));}}
        public int SellValue {get{return Mathf.FloorToInt(Invested*DefenseGame.Instance.RefundRate);}}
        public string EvolutionName {get{return Branch==0?DefenseGame.Specs[(int)Kind].name:BranchName(Branch);}}
        public string BranchName(int branch){return Kind==TowerKind.Bolt?(branch==1?"鹰眼穿甲":"连锁闪电"):Kind==TowerKind.Frost?(branch==1?"永冻领域":"霜裂共鸣"):(branch==1?"攻城重炮":"熔火封锁");}
        public string BranchDescription(int branch){return Kind==TowerKind.Bolt?(branch==1?"射程 +1.65m / 穿透全部护甲":"弹射 2 个附近敌人"):Kind==TowerKind.Frost?(branch==1?"减速 68% / 短暂冻结":"范围易伤 / 受到伤害 +30%"):(branch==1?"2.2m 爆破 / 穿甲 85%":"留下持续 3.5 秒燃烧区");}
        public string PriorityName {get{return Priority==TargetPriority.First?"最接近终点":Priority==TargetPriority.Strongest?"生命最强":"治疗师优先";}}
        float cooldown;
        public void Jam(float duration){if(!DefenseGame.Instance.Overdriven)JammedUntil=Time.time+duration;}
        public void RefreshLevel()
        {
            if(Head!=null)Head.localScale=Vector3.one*(1+(Level-1)*.12f);
            for(int i=1;i<Level;i++)if(transform.Find("Level "+i)==null)WorldBuilder.Shape("Level "+i,PrimitiveType.Sphere,new Vector3(i==1?-.2f:.2f,.67f,-.31f),Vector3.one*.13f,new Color(1,.85f,.45f),transform,false,true);
            if(Branch>0 && transform.Find("Evolution crown")==null) {
                Color color=Branch==1?new Color(1,.82f,.39f):new Color(.74f,.49f,1);
                WorldBuilder.Shape("Evolution crown",PrimitiveType.Cylinder,new Vector3(0,.53f,0),new Vector3(1.15f,.035f,1.15f),color,transform,false,true);
                if(Kind==TowerKind.Bolt&&Branch==1)WorldBuilder.Shape("Extended railgun",PrimitiveType.Cube,new Vector3(0,.02f,.92f),new Vector3(.13f,.14f,.8f),color,Head);
                if(Branch==2)for(int i=-1;i<=1;i+=2)WorldBuilder.Shape("Evolution emitter",PrimitiveType.Sphere,new Vector3(i*.43f,.2f,0),Vector3.one*.23f,color,Head,false,true);
            }
        }
        public bool CanReach(Enemy enemy){return enemy!=null&&!enemy.Dead&&(enemy.AimPosition-Head.position).sqrMagnitude<=Range*Range&&TerrainOcclusion.CanFire(Head.position,enemy.AimPosition,Kind);}
        void Update()
        {
            var game=DefenseGame.Instance;if(!game.Running||Jammed)return;
            cooldown-=Time.deltaTime;Enemy target=null;float best=float.NegativeInfinity;
            foreach(var e in game.Enemies)if(CanReach(e)){
                float score=Priority==TargetPriority.Strongest?e.Health+e.Shield:-e.RemainingDistance;
                if(Priority==TargetPriority.Support&&e.Kind==5)score+=10000;
                if(score>best){target=e;best=score;}
            }
            if(target==null){if(Kind==TowerKind.Frost)Head.Rotate(0,Time.deltaTime*25,0);return;}
            Vector3 direction=target.AimPosition-Head.position;
            if(direction.sqrMagnitude>.001f)Head.rotation=Quaternion.Slerp(Head.rotation,Quaternion.LookRotation(direction),Time.deltaTime*10);
            if(cooldown>0)return;cooldown=Interval;Shots++;game.FireSound();
            Projectile.Launch(Head.position,target,Kind,Damage,Branch);
        }
    }
    public class Projectile : MonoBehaviour
    {
        public bool Resolved { get; private set; }
        Enemy target;TowerKind kind;float damage;int branch;Vector3 origin,destination;float progress,duration;
        public static Projectile Launch(Vector3 pos,Enemy target,TowerKind kind,float damage,int branch=0)
        {
            var o=WorldBuilder.Shape("Energy projectile",PrimitiveType.Sphere,pos,Vector3.one*(kind==TowerKind.Bloom?.22f:.13f),DefenseGame.Specs[(int)kind].color,DefenseGame.Instance.WorldRoot,false,true);
            var p=o.AddComponent<Projectile>();p.target=target;p.kind=kind;p.damage=damage;p.branch=branch;p.origin=pos;p.destination=target.AimPosition;
            p.duration=kind==TowerKind.Bloom?.56f:Mathf.Max(.08f,Vector3.Distance(pos,p.destination)/15);return p;
        }
        void Update()
        {
            if(!DefenseGame.Instance.Running)return;
            Advance(Time.deltaTime);
        }
        public void Advance(float deltaTime)
        {
            if(Resolved || deltaTime<=0)return;
            if(target!=null&&!target.Dead)destination=target.AimPosition;
            float end=Mathf.Min(1,progress+deltaTime/duration);
            // Sweep every segment, including the final frame: fast shots cannot tunnel through thin decks.
            while(progress<end) {
                float next=Mathf.Min(end,progress+1f/24);
                Vector3 nextPosition=TerrainOcclusion.ShotPoint(origin,destination,next,kind),contact;
                if(TerrainOcclusion.Sweep(transform.position,nextPosition,TerrainOcclusion.ShotRadius,out contact)) {
                    Resolved=true;WorldBuilder.Burst(contact,new Color(1,.72f,.35f),.3f);Destroy(gameObject);return;
                }
                transform.position=nextPosition;progress=next;
            }
            if(progress<1)return;
            Resolved=true;
            if(kind==TowerKind.Bolt){
                if(target!=null&&!target.Dead)target.Hit(damage,false,branch==1?1:0);
                if(branch==2){int hits=0;foreach(var e in DefenseGame.Instance.Enemies.ToArray())if(e!=null&&e!=target&&!e.Dead&&Vector3.Distance(e.AimPosition,destination)<2.2f&&TerrainOcclusion.Clear(destination,e.AimPosition)){e.Hit(damage*.65f);WorldBuilder.Burst(e.transform.position+Vector3.up*.4f,new Color(.74f,.49f,1),.6f);if(++hits==2)break;}}
            }else{
                float radius=kind==TowerKind.Bloom?(branch==1?2.2f:1.5f):(branch==2?1.8f:1.1f);
                foreach(var e in DefenseGame.Instance.Enemies.ToArray())if(e!=null&&!e.Dead&&Vector3.Distance(e.AimPosition,destination)<=radius&&TerrainOcclusion.Clear(destination,e.AimPosition)){
                    if(kind==TowerKind.Frost&&branch==2)e.Expose(3);
                    e.Hit(damage,kind==TowerKind.Frost,kind==TowerKind.Bloom?(branch==1?.85f:.6f):0,kind==TowerKind.Frost&&branch==1?.32f:.52f);
                    if(kind==TowerKind.Frost&&branch==1)e.Freeze(.35f);
                }
                Vector3 floor=destination-Vector3.up*.45f;
                WorldBuilder.Burst(floor,DefenseGame.Specs[(int)kind].color,radius);
                if(kind==TowerKind.Bloom&&branch==2)BurningGround.Spawn(floor,damage*.37f);
            }
            Destroy(gameObject);
        }
    }
}
