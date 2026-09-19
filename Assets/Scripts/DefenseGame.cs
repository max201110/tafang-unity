using System;
using System.Collections.Generic;
using UnityEngine;

namespace Verdant
{
    public enum TowerKind { Bolt, Frost, Bloom }
    public enum RunState { Planning, Wave, Victory, Defeat }
    [Serializable] public class TowerSpec
    {
        public string name, subtitle, description;
        public int cost;
        public float damage, interval, range;
        public Color color;
        public TowerSpec(string n, string s, string d, int c, float hit, float rate, float radius, Color tint)
        { name = n; subtitle = s; description = d; cost = c; damage = hit; interval = rate; range = radius; color = tint; }
    }
    public partial class DefenseGame : MonoBehaviour
    {
        public static DefenseGame Instance;
        public static readonly TowerSpec[] Specs = {
            new TowerSpec("脉冲哨塔", "PULSE / 单体速射", "快速锁定单个目标，稳定守住每一道防线。", 90, 19, .56f, 3.7f, new Color(.45f,.91f,.67f)),
            new TowerSpec("霜晶塔", "FROST / 范围减速", "霜晶使目标及附近敌人减速 48%，持续 2 秒。", 125, 9, .95f, 3.4f, new Color(.39f,.79f,1f)),
            new TowerSpec("花火迫击炮", "BLOOM / 范围爆破", "炮弹在半径 1.5 米内造成伤害，适合密集敌群。", 155, 39, 1.6f, 4.2f, new Color(1f,.72f,.37f))
        };
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Tower> Towers = new List<Tower>();
        public readonly List<BuildPad> Pads = new List<BuildPad>();
        public Vector3[] Route;
        public Vector3[][] Routes;
        public readonly string[] RouteNames = { "A · 林间地道", "B · 空中栈桥", "C · 山巅坡道" };
        public readonly Color[] RouteColors = { new Color(1,.79f,.4f), new Color(.38f,.82f,1), new Color(.82f,.59f,1) };
        public Camera WorldCamera;
        public Transform WorldRoot;
        public int Gold = 380, Lives = 20, Wave, TotalWaves = 15, Kills, Score;
        public int Blueprint = 0;
        public Tower Selected;
        public BuildPad Hovered;
        public RunState State = RunState.Planning;
        public bool Paused, Sound = true, TestMode, ModalOpen;
        public int Speed = 1, SpawnRemaining;
        public string Notice = "选择防御塔，再点击地图中的发光基座建造。";
        public float NoticeTimer = 7;
        public float WaveElapsed;
        public float RouteLength { get; private set; }
        public float SessionTime { get; private set; }
        float spawnClock, settleClock;
        int spawnIndex;
        AudioSource audioSource;
        AudioClip fireClip, buildClip, hurtClip;
        LineRenderer rangeRing, rangeVerticalX, rangeVerticalZ;
        public bool Running { get { return !Paused && !ModalOpen && !RewardPending && State != RunState.Defeat && State != RunState.Victory; } }

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Time.timeScale = 1;
            WorldRoot = new GameObject("Procedural world").transform;
            WorldBuilder.Create(this);
            RouteLength = 0;
            for (int i=1; i<Route.Length; i++) RouteLength += Vector3.Distance(Route[i-1], Route[i]);
            gameObject.AddComponent<DefenseHUD>();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = .2f;
            fireClip = Tone(420, .055f);
            buildClip = Tone(740, .17f);
            hurtClip = Tone(110, .22f);
            rangeRing = WorldBuilder.Ring("Range preview", Vector3.zero, 1, new Color(.55f,1,.76f,.65f), .025f, WorldRoot);
            rangeRing.gameObject.SetActive(false);
            rangeVerticalX=WorldBuilder.Ring("3D range meridian X",Vector3.zero,1,new Color(.5f,1,.75f,.35f),.022f,WorldRoot);
            rangeVerticalZ=WorldBuilder.Ring("3D range meridian Z",Vector3.zero,1,new Color(.5f,1,.75f,.35f),.022f,WorldRoot);
            rangeVerticalX.gameObject.SetActive(false);rangeVerticalZ.gameObject.SetActive(false);
            TestMode = Array.IndexOf(Environment.GetCommandLineArgs(), "--smoke-test") >= 0;
            if (TestMode) gameObject.AddComponent<SmokeTest>();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--balance-test") >= 0) { TestMode=true; gameObject.AddComponent<BalanceTest>(); }
        }
        void Update()
        {
            float dt = Time.deltaTime;
            NoticeTimer -= Time.unscaledDeltaTime;
            if (Running) SessionTime += dt;
            TickStrategy(dt);
            if (ModalOpen || RewardPending) return;
            if (Input.GetKeyDown(KeyCode.F12)) StartCoroutine(SaveScreenshot());
            if (Input.GetKeyDown(KeyCode.Escape)) { Selected = null; Blueprint = -1; MeteorArmed=false; }
            if (Input.GetKeyDown(KeyCode.Q)) ArmMeteor();
            if (Input.GetKeyDown(KeyCode.W)) CastFreeze();
            if (Input.GetKeyDown(KeyCode.E)) CastOverdrive();
            if (Input.GetKeyDown(KeyCode.Tab)) CyclePriority();
            if (Input.GetKeyDown(KeyCode.Space)) { if(Paused) TogglePause(); else if(State == RunState.Planning) StartWave(); else if(State == RunState.Wave) TogglePause(); }
            if (Input.GetKeyDown(KeyCode.Alpha1)) Choose(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Choose(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Choose(2);
            if (Input.GetKeyDown(KeyCode.U)) UpgradeSelected();
            HandlePointer();
            UpdateRange();
            if (State != RunState.Wave || !Running) return;
            WaveElapsed += dt;
            spawnClock -= dt;
            if (SpawnRemaining > 0 && spawnClock <= 0) {
                int kind = EnemyKind(Wave, spawnIndex);
                SpawnEnemy(kind);
                spawnIndex++;
                SpawnRemaining--;
                spawnClock = CurrentPlan.Gap;
            }
            if (SpawnRemaining == 0 && Enemies.Count == 0) {
                settleClock += dt;
                if (settleClock > .7f) CompleteWave();
            }
        }
        void HandlePointer()
        {
            Hovered = null;
            if (!Running || TestMode) return;
            Vector3 mp = Input.mousePosition;
            if (!WorldCamera.pixelRect.Contains(mp)) return;
            RaycastHit hit;
            if(MeteorArmed) {
                Ray ray=WorldCamera.ScreenPointToRay(mp);
                if(Physics.Raycast(ray,out hit,150,1<<8) && Input.GetMouseButtonDown(0)) CastMeteor(hit.point);
                return;
            }
            if(Physics.Raycast(WorldCamera.ScreenPointToRay(mp), out hit, 100)) Hovered = hit.collider.GetComponentInParent<BuildPad>();
            if(Input.GetMouseButtonDown(0)) {
                if(Hovered != null) {
                    if(Hovered.Occupant != null) { Selected = Hovered.Occupant; Blueprint = -1; Play(buildClip); }
                    else if(Blueprint >= 0) Build(Hovered, (TowerKind)Blueprint);
                } else { Selected = null; }
            }
        }
        void UpdateRange()
        {
            Vector3 pos; float radius; Color color;
            if (Selected != null) { pos = Selected.transform.position; radius = Selected.Range; color = Specs[(int)Selected.Kind].color; }
            else if (Hovered != null && Hovered.Occupant == null && Blueprint >= 0) { pos = Hovered.transform.position; radius = Specs[Blueprint].range; color = Gold >= Specs[Blueprint].cost ? Specs[Blueprint].color : new Color(1,.35f,.3f); }
            else { rangeRing.gameObject.SetActive(false); rangeVerticalX.gameObject.SetActive(false); rangeVerticalZ.gameObject.SetActive(false); return; }
            rangeRing.gameObject.SetActive(true);
            Vector3 center=pos+Vector3.up*1.12f;
            WorldBuilder.SetRing(rangeRing,center,radius);
            rangeVerticalX.gameObject.SetActive(true);rangeVerticalZ.gameObject.SetActive(true);
            for(int i=0;i<64;i++) {float a=i*Mathf.PI*2/64;rangeVerticalX.SetPosition(i,center+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));rangeVerticalZ.SetPosition(i,center+new Vector3(0,Mathf.Sin(a)*radius,Mathf.Cos(a)*radius));}
            Color arc=color;arc.a=.22f;rangeVerticalX.startColor=rangeVerticalX.endColor=arc;rangeVerticalZ.startColor=rangeVerticalZ.endColor=arc;
            color.a = .55f; rangeRing.startColor = rangeRing.endColor = color;
        }
        public void Choose(int kind) { if (!Running || kind<0 || kind>=Specs.Length) return; Blueprint = kind; Selected = null; MeteorArmed=false; }
        public Tower Build(BuildPad pad, TowerKind kind)
        {
            if (!Running || pad == null || pad.Occupant != null) return null;
            TowerSpec spec = Specs[(int)kind];
            if(Gold < spec.cost) { Tell("金币不足，击败敌人可以获得更多金币。"); return null; }
            Gold -= spec.cost;
            Tower t = WorldBuilder.CreateTower(pad, kind);
            pad.Occupant = t; Towers.Add(t); Selected = t; Blueprint = -1;
            WorldBuilder.Burst(pad.transform.position + Vector3.up * .45f, spec.color, .8f);
            Play(buildClip); Tell(spec.name + " 已部署。点击空基座之前，可按 1 / 2 / 3 选择下一座塔。");
            return t;
        }
        public void UpgradeSelected()
        {
            if (!Running || Selected == null || Selected.Level >= 2) return;
            int cost = Selected.UpgradeCost;
            if (Gold < cost) { Tell("升级需要更多金币。"); return; }
            Gold -= cost; Selected.Invested += cost; Selected.Level++;
            Selected.RefreshLevel(); Play(buildClip);
            WorldBuilder.Burst(Selected.transform.position+Vector3.up, Specs[(int)Selected.Kind].color, 1);
            Tell("升级完成：伤害、射程与射速已提升。");
        }
        public void SellSelected()
        {
            if (!Running || Selected == null) return;
            Tower t = Selected; Gold += t.SellValue;
            t.Pad.Occupant = null; Towers.Remove(t); Selected = null;
            Destroy(t.gameObject); Play(buildClip); Tell("已回收防御塔，返还总投入的 "+Mathf.RoundToInt(RefundRate*100)+"%。");
        }
        public void StartWave()
        {
            if(State != RunState.Planning || !Running) return;
            Wave++; State = RunState.Wave; WaveElapsed = 0;
            SpawnRemaining = CurrentPlan.Count; spawnIndex = 0; spawnClock = .5f; settleClock = 0;
            Tell("第 "+Wave+" 波 · "+CurrentPlan.Name+" / "+CurrentPlan.Rule);
        }
        public static int EnemyKind(int wave, int index)
        {
            var plan=Campaign.Waves[Mathf.Clamp(wave-1,0,Campaign.Waves.Length-1)];
            if(plan.Boss && index==plan.Count-1)return 3;
            return plan.Roster[index%plan.Roster.Length];
        }

        void SpawnEnemy(int kind)
        {
            var enemy = WorldBuilder.CreateEnemy(kind, Wave, spawnIndex % (Wave >= 3 ? 3 : 2));
            Enemies.Add(enemy);
        }
        public void RemoveEnemy(Enemy enemy, bool escaped)
        {
            if (!Enemies.Remove(enemy)) return;
            if (escaped) {
                Lives = Mathf.Max(0, Lives - (enemy.Kind == 3 ? 4 : 1)); Play(hurtClip);
                WorldBuilder.Burst(Route[Route.Length-1]+Vector3.up*.6f, new Color(1,.35f,.32f), 1.3f);
                if(Lives == 0) { State = RunState.Defeat; Paused = false; Time.timeScale = 1; Tell("核心已失守。调整火力布局，再试一次。"); }
            } else { Gold += Mathf.CeilToInt(enemy.Reward*(1+BountyBonus)); Kills++; Score += enemy.Reward * 10; }
        }
        void CompleteWave()
        {
            int bonus = 28 + Wave * 3; Gold += bonus; Score += Wave * 100;
            if(Wave == TotalWaves) { State = RunState.Victory; Time.timeScale = 1; Tell("全部波次已清除，翠境恢复宁静。"); }
            else { State = RunState.Planning; if(Wave%3==0)OfferReward(); Tell("防线稳固！波次奖励 +" + bonus + " 金币。准备好后开始下一波。"); }
        }
        public void TogglePause()
        {
            if(ModalOpen || RewardPending || State == RunState.Victory || State == RunState.Defeat) return;
            Paused = !Paused; Time.timeScale = Paused ? 0 : Speed;
        }
        public void ToggleSpeed() { if(ModalOpen || RewardPending)return; Speed = Speed == 1 ? 2 : 1; if(!Paused && Running) Time.timeScale = Speed; }
        public void Restart() { Time.timeScale = 1; UnityEngine.SceneManagement.SceneManager.LoadScene(0); }
        public void Tell(string text) { Notice = text; NoticeTimer = 5; }
        public void FireSound() { if(UnityEngine.Random.value < .25f) Play(fireClip); }
        void Play(AudioClip clip) { if(Sound && !TestMode && audioSource != null) audioSource.PlayOneShot(clip); }
        AudioClip Tone(float frequency, float duration)
        {
            const int rate = 22050; float[] data = new float[(int)(rate * duration)];
            for(int i=0;i<data.Length;i++) { float t = (float)i / rate; data[i] = Mathf.Sin(2*Mathf.PI*frequency*t) * Mathf.Pow(1-(float)i/data.Length,2) * .45f; }
            var clip = AudioClip.Create("Synth " + frequency, data.Length, 1, rate, false); clip.SetData(data,0); return clip;
        }
        System.Collections.IEnumerator SaveScreenshot()
        {
            yield return new WaitForEndOfFrame();
            string path=System.IO.Path.Combine(Application.persistentDataPath,"Verdant-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png");
            CaptureFrame(path); Tell("截图已保存："+path);
        }
        public static void CaptureWorld(string path)
        {
            var camera=Instance.WorldCamera;
            var previous=camera.targetTexture;var rect=camera.rect;var active=RenderTexture.active;
            var rt=new RenderTexture(1600,1000,24);var shot=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try {camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);camera.Render();RenderTexture.active=rt;shot.ReadPixels(new Rect(0,0,1600,1000),0,0);shot.Apply();System.IO.File.WriteAllBytes(path,shot.EncodeToPNG());}
            finally{camera.targetTexture=previous;camera.rect=rect;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(shot);}
        }
        public static void CaptureFrame(string path)
        {
            Texture2D shot=ScreenCapture.CaptureScreenshotAsTexture();
            if(shot==null) { Debug.LogWarning("Frame capture unavailable"); return; }
            System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(path),shot.EncodeToPNG());
            Destroy(shot);
        }
        void OnDestroy() { Time.timeScale = 1; if(Instance == this) Instance = null; }
    }
}

