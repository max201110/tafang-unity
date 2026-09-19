using UnityEngine;

namespace Verdant
{
    public class DefenseHUD : MonoBehaviour
    {
        DefenseGame game;Font font;Texture2D white;GUIStyle text,button;
        readonly Color bg=new Color(.055f,.096f,.103f), panel=new Color(.081f,.135f,.14f),line=new Color(.17f,.25f,.25f);
        readonly Color ink=new Color(.92f,.94f,.84f), muted=new Color(.67f,.76f,.7f),mint=new Color(.59f,.89f,.67f),gold=new Color(.98f,.78f,.43f);
        float h;
        void Awake(){game=GetComponent<DefenseGame>();font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Segoe UI","Arial"},18);white=Texture2D.whiteTexture;}
        void Init()
        {
            if(text!=null)return;
            text=new GUIStyle(GUI.skin.label){font=font,alignment=TextAnchor.MiddleLeft,wordWrap=false,padding=new RectOffset(0,0,0,0)};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=17,alignment=TextAnchor.MiddleCenter,border=new RectOffset(0,0,0,0),padding=new RectOffset(0,0,0,0)};
            button.normal.background=button.hover.background=button.active.background=white;
            button.normal.textColor=button.hover.textColor=button.active.textColor=ink;
        }
        void Box(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,white);GUI.color=old;}
        void Frame(Rect r,Color c){Box(new Rect(r.x,r.y,r.width,1),c);Box(new Rect(r.x,r.yMax-1,r.width,1),c);Box(new Rect(r.x,r.y,1,r.height),c);Box(new Rect(r.xMax-1,r.y,1,r.height),c);}
        void Label(float x,float y,float w,float height,string value,int size,Color color,FontStyle style=FontStyle.Normal,TextAnchor align=TextAnchor.MiddleLeft)
        {text.fontSize=Mathf.Max(13,size);text.normal.textColor=color;text.fontStyle=style;text.alignment=align;text.wordWrap=false;GUI.Label(new Rect(x,y,w,height),value,text);}
        void Paragraph(float x,float y,float w,float height,string value,int size,Color color)
        {text.fontSize=Mathf.Max(13,size);text.normal.textColor=color;text.fontStyle=FontStyle.Normal;text.alignment=TextAnchor.UpperLeft;text.wordWrap=true;GUI.Label(new Rect(x,y,w,height),value,text);text.wordWrap=false;}
        bool Button(Rect r,string title,Color fill,Color foreground,bool enabled=true)
        {
            Color old=GUI.backgroundColor;GUI.backgroundColor=enabled?fill:new Color(.16f,.21f,.21f);
            button.normal.textColor=button.hover.textColor=button.active.textColor=enabled?foreground:muted;
            bool oldEnabled=GUI.enabled;GUI.enabled=oldEnabled&&enabled;bool hit=GUI.Button(r,title,button);GUI.enabled=oldEnabled;GUI.backgroundColor=old;return hit;
        }
        void OnGUI()
        {
            Init();float scale=Screen.width/1600f;h=Screen.height/scale;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
            game.WorldCamera.rect=new Rect(.006f,205f/h,.739f,Mathf.Max(220,h-410)/h);
            GUI.enabled=!(confirmRestart||game.RewardPending||game.Paused||game.State==RunState.Victory||game.State==RunState.Defeat);
            // Four matte regions frame, rather than cover, the rendered world.
            Box(new Rect(0,0,1600,205),bg);Box(new Rect(0,h-205,1600,205),bg);
            Box(new Rect(1192,130,408,h-130),bg);Box(new Rect(0,0,12,h),bg);
            Box(new Rect(30,31,46,46),mint);Label(30,31,46,46,"V",30,bg,FontStyle.Bold,TextAnchor.MiddleCenter);
            Label(93,25,410,47,"翠境防线 · 纵深",29,ink,FontStyle.Bold);
            Label(96,72,470,25,"VERDANT  /  THREE FRONTS",11,muted);
            Stat(635,"WAVE / 波次",game.Wave.ToString("00")+" / "+game.TotalWaves,ink);
            Stat(843,"ENERGY / 金币",game.Gold.ToString(),gold);
            Stat(1052,"CORE / 生命",game.Lives+" / 20",game.Lives<=5?new Color(1,.48f,.4f):mint);
            if(Button(new Rect(1297,43,126,40),game.Sound?"声音  开":"声音  关",panel,muted))game.Sound=!game.Sound;
            if(Button(new Rect(1435,43,128,40),"重新开始",panel,muted))OpenRestartDialog();
            Box(new Rect(32,119,1536,1),line);
            Label(34,132,440,32,"01  /  雾松峡谷 · 三线立体防御",17,ink,FontStyle.Bold);
            Label(500,134,650,30,game.State==RunState.Wave?"●  敌人来袭     ·     剩余 "+(game.SpawnRemaining+game.Enemies.Count)+" 个目标":"●  战前部署     ·     点击空基座建造",13,game.State==RunState.Wave?gold:muted,FontStyle.Normal,TextAnchor.MiddleRight);
            RouteLegend();Sidebar();Bottom();
            GUI.enabled=true;
            if(game.RewardPending)RewardOverlay();
            if(game.Paused && !confirmRestart)Overlay("战术暂停","深呼吸，规划下一步。", "继续守护",()=>game.TogglePause());
            if(game.State==RunState.Victory)Overlay("翠境，安然无恙。",game.TotalWaves+" 波防守完成   /   击败 "+game.Kills+" 个敌人   /   得分 "+game.Score,"再次出发",()=>game.Restart());
            if(game.State==RunState.Defeat)Overlay("防线暂时失守","抵达第 "+game.Wave+" 波   /   击败 "+game.Kills+" 个敌人。试试减速塔与爆破塔的组合。","重整防线",()=>game.Restart());
            if(confirmRestart) {
                Box(new Rect(0,0,1600,h),new Color(.02f,.055f,.06f,.85f));Box(new Rect(520,h/2-125,560,250),panel);Frame(new Rect(520,h/2-125,560,250),line);
                Label(560,h/2-95,480,50,"重新开始这次守护？",25,ink,FontStyle.Bold);
                Label(560,h/2-40,480,35,"本局进度将重置，回到初始部署。",15,muted);
                if(Button(new Rect(560,h/2+40,220,48),"取消",line,ink))CloseRestartDialog();
                if(Button(new Rect(800,h/2+40,240,48),"确认重开",mint,bg))game.Restart();
            }
            GUI.matrix=Matrix4x4.identity;
        }
        bool confirmRestart;
        void OpenRestartDialog() { confirmRestart=true; game.ModalOpen=true; Time.timeScale=0; }
        void CloseRestartDialog() { confirmRestart=false; game.ModalOpen=false; Time.timeScale=game.Paused?0:game.Speed; }
        void Stat(float x,string caption,string value,Color c)
        {Label(x,28,196,25,caption,11,muted);Label(x,54,195,45,value,29,c,FontStyle.Bold);}
        void RouteLegend()
        {
            for(int i=0;i<3;i++) {
                int count=0;foreach(var e in game.Enemies)if(e.RouteIndex==i)count++;
                Label(34+i*272,169,265,29,((char)('A'+i))+"  "+(i==0?"地面 0m":i==1?"栈桥 3.6m":"山巅 6.4m")+"   /   "+count+" 敌",13,game.RouteColors[i]);
            }
            Label(841,170,322,25,"右键旋转 · 滚轮缩放 · Home 复位",13,muted,FontStyle.Normal,TextAnchor.MiddleRight);
        }
        void Sidebar()
        {
            float x=1212,w=352;
            Label(x,137,w,30,"防御工坊",22,ink,FontStyle.Bold);
            Label(x,170,w,23,"18 个塔位 / 3 层高度 / 6 条进化路线",13,muted);
            for(int i=0;i<3;i++) {
                var spec=DefenseGame.Specs[i];Rect r=new Rect(x,205+i*77,w,68);bool selected=game.Blueprint==i;
                Box(r,selected?new Color(.15f,.25f,.21f):panel);Frame(r,selected?mint:line);Box(new Rect(x,r.y,3,r.height),spec.color);
                Icon(new Rect(x+12,r.y+9,46,48),i,spec.color);
                Label(x+73,r.y+7,207,26,spec.name,17,ink,FontStyle.Bold);
                Label(x+73,r.y+35,244,23,spec.subtitle,12,muted);
                Label(x+w-75,r.y+7,58,27,spec.cost.ToString(),18,game.Gold>=spec.cost?gold:muted,FontStyle.Bold,TextAnchor.MiddleRight);
                if(GUI.Button(r,GUIContent.none,GUIStyle.none))game.Choose(i);
            }
            float sy=449;Box(new Rect(x,sy,w,1),line);
            if(game.Selected!=null) {
                Tower t=game.Selected;
                Label(x,sy+10,265,31,t.EvolutionName,20,ink,FontStyle.Bold);
                Label(x+275,sy+10,75,31,"Lv. "+t.Level,17,mint,FontStyle.Bold,TextAnchor.MiddleRight);
                Label(x,sy+45,w,25,"高度 "+t.Pad.transform.position.y.ToString("0.0")+"m  /  三维射程 "+t.Range.ToString("0.0")+"m",13,game.RouteColors[t.Pad.transform.position.y>6?2:t.Pad.transform.position.y>1?1:0]);
                Label(x,sy+74,w,23,"伤害 "+t.Damage.ToString("0")+"  ·  间隔 "+t.Interval.ToString("0.00")+"s"+(t.Jammed?"  [干扰中]":""),14,muted);
                if(Button(new Rect(x,sy+108,w,33),"目标 [Tab]  /  "+t.PriorityName,panel,ink,game.Running))game.CyclePriority();
                if(t.Level==2) {
                    for(int branch=1;branch<=2;branch++) {
                        float bx=x+(branch-1)*181;
                        if(Button(new Rect(bx,sy+154,171,39),t.BranchName(branch)+" "+t.UpgradeCost,branch==1?mint:new Color(.7f,.57f,.9f),bg,game.Gold>=t.UpgradeCost&&game.Running))game.EvolveSelected(branch);
                        Paragraph(bx,sy+202,170,43,t.BranchDescription(branch),13,muted);
                    }
                } else if(t.Level==1) {
                    if(Button(new Rect(x,sy+154,w,40),"升级至 Lv.2 [U]  /  "+t.UpgradeCost,mint,bg,game.Gold>=t.UpgradeCost&&game.Running))game.UpgradeSelected();
                    Paragraph(x,sy+205,w,40,"达到 Lv.2 后选择进化路线。高低层之间按真实距离索敌。",13,muted);
                } else Paragraph(x,sy+158,w,75,t.BranchDescription(t.Branch)+"。进化已锁定，改变索敌优先级可应对不同敌群。",14,mint);
                if(Button(new Rect(x,sy+255,w,33),"出售返还 "+t.SellValue+"  /  "+Mathf.RoundToInt(game.RefundRate*100)+"%",panel,gold,game.Running))game.SellSelected();
            } else {
                WavePlan plan=game.State==RunState.Wave?game.CurrentPlan:game.NextPlan;
                Label(x,sy+11,w,29,"战场情报 / "+plan.Name,18,ink,FontStyle.Bold);
                Label(x,sy+49,w,25,plan.Count+" 个敌人  ·  "+plan.Rule,13,gold);
                Paragraph(x,sy+85,w,68,plan.Intel,14,muted);
                Label(x,sy+155,w,27,game.Wave<2?"来袭入口 A + B / 第 3 波起开启 C":"A + B + C 三线同时来袭",14,mint);
                if(game.Wave==0) {
                    Label(x,sy+193,w,23,"战役难度 / 开战后锁定",13,muted);
                    for(int i=0;i<3;i++)if(Button(new Rect(x+i*120,sy+223,112,35),i==0?"标准":i==1?"老兵":"噩梦",(int)game.Mode==i?mint:panel,(int)game.Mode==i?bg:ink))game.SetDifficulty(i);
                } else {
                    Label(x,sy+197,w,25,"遗物 "+game.RelicHistory.Count+" 件 / 每 3 波三选一",14,muted);
                    if(game.RelicHistory.Count>0)Label(x,sy+233,w,25,"最近获得："+Campaign.Relics[game.RelicHistory[game.RelicHistory.Count-1]].Name,14,mint);
                }
            }
            float by=h-126;bool canStart=game.State==RunState.Planning&&game.Running;
            string title=game.State==RunState.Wave?"第 "+game.Wave+" 波 / "+(game.SpawnRemaining+game.Enemies.Count)+" 个目标":game.Wave==0?"开始三线守护 →":"下一波  "+(game.Wave+1).ToString("00")+" →";
            if(game.State==RunState.Victory)title="守护完成";if(game.State==RunState.Defeat)title="防线失守";
            if(Button(new Rect(x,by,w,58),title,mint,bg,canStart))game.StartWave();
            Label(x,by+65,w,28,"SPACE 开始 / 暂停  ·  ESC 取消",13,muted,FontStyle.Normal,TextAnchor.MiddleCenter);
        }
        void Icon(Rect r,int kind,Color color)
        {
            Color dark=new Color(.13f,.22f,.22f);
            Box(new Rect(r.x+6,r.y+36,35,8),muted);Box(new Rect(r.x+17,r.y+17,14,21),dark);
            if(kind==0){Box(new Rect(r.x+7,r.y+10,31,18),color);Box(new Rect(r.x+24,r.y+15,23,7),ink);}
            else if(kind==1){Box(new Rect(r.x+17,r.y,15,29),color);Box(new Rect(r.x+4,r.y+18,7,17),color*.7f);Box(new Rect(r.x+37,r.y+18,7,17),color*.7f);}
            else {Box(new Rect(r.x+8,r.y+19,32,14),dark);Box(new Rect(r.x+15,r.y+3,20,25),color);Box(new Rect(r.x+18,r.y+3,14,6),dark);}
        }
        void Bottom()
        {
            float y=h-191;
            Box(new Rect(32,y-9,1128,1),line);
            Label(34,y,1128,32,game.NoticeTimer>0?game.Notice:"高台不等于全图覆盖：注意高度差、道路交叉和坡道出口。",14,game.NoticeTimer>0?ink:muted);
            for(int i=0;i<3;i++) {
                float cd=i==0?game.MeteorCooldown:i==1?game.FreezeCooldown:game.OverdriveCooldown;
                Color color=i==0?new Color(.96f,.66f,.4f):i==1?new Color(.46f,.78f,.93f):mint;
                if(Button(new Rect(34+i*253,y+50,240,45),game.SkillLabel(i),cd>0?panel:color,cd>0?muted:bg,game.Running&&game.State==RunState.Wave&&cd<=0)) {if(i==0)game.ArmMeteor();else if(i==1)game.CastFreeze();else game.CastOverdrive();}
            }
            if(Button(new Rect(829,y+50,150,45),"暂停 Ⅱ",panel,ink,game.Running))game.TogglePause();
            if(Button(new Rect(991,y+50,166,45),"速度 ×"+game.Speed,panel,gold,game.Running))game.ToggleSpeed();
            Label(34,y+103,700,26,"天火：指定一层爆破  /  极寒：冻结全图  /  超频：攻速 +65%、免疫干扰",12,muted);
            for(int i=0;i<game.TotalWaves;i++){Color c=i<game.Wave?mint:line;if(i==game.Wave-1&&game.State==RunState.Wave)c=gold;Box(new Rect(34+i*33,y+149,25,5),c);}
            Label(570,y+133,587,29,"击败 "+game.Kills.ToString("000")+"   /   得分 "+game.Score.ToString("00000")+"   /   "+(game.Mode==Difficulty.Standard?"标准":game.Mode==Difficulty.Veteran?"老兵":"噩梦"),13,muted,FontStyle.Normal,TextAnchor.MiddleRight);
        }
        void RewardOverlay()
        {
            Box(new Rect(0,0,1600,h),new Color(.025f,.06f,.08f,.95f));
            Label(170,h/2-228,1260,38,"守护者遗物 / 选择一项永久强化",29,ink,FontStyle.Bold,TextAnchor.MiddleCenter);
            Label(170,h/2-174,1260,27,"每 3 波获得一次选择 · 本局生效 · 各项可叠加",15,muted,FontStyle.Normal,TextAnchor.MiddleCenter);
            for(int i=0;i<3;i++) {
                var relic=Campaign.Relics[game.RewardOptions[i]];float x=220+i*396;Rect r=new Rect(x,h/2-104,368,256);Box(r,panel);Frame(r,line);
                Label(x+26,r.y+24,316,30,"0"+(i+1)+" / RELIC",13,mint);
                Label(x+26,r.y+70,316,37,relic.Name,25,ink,FontStyle.Bold);
                Paragraph(x+26,r.y+126,316,54,relic.Description,16,muted);
                if(Button(new Rect(x+26,r.y+194,316,40),"获取强化 →",mint,bg))game.ChooseRelic(i);
            }
        }
        void Overlay(string title,string subtitle,string action,System.Action callback)
        {
            Box(new Rect(0,0,1600,h),new Color(.025f,.07f,.075f,.84f));Rect r=new Rect(370,h/2-160,860,320);Box(r,panel);Frame(r,line);
            Label(410,r.y+36,780,28,"V E R D A N T   /   翠 境 防 线",12,mint,FontStyle.Normal,TextAnchor.MiddleCenter);
            Label(410,r.y+87,780,60,title,36,ink,FontStyle.Bold,TextAnchor.MiddleCenter);
            Label(395,r.y+161,810,35,subtitle,15,muted,FontStyle.Normal,TextAnchor.MiddleCenter);
            if(Button(new Rect(640,r.y+228,320,54),action,mint,bg))callback();
        }
    }
}

