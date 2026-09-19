using System.Collections.Generic;
using UnityEngine;

namespace Verdant
{
    public class BuildPad : MonoBehaviour { public Tower Occupant; public int Index; }

    public static class WorldBuilder
    {
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static Material Mat(Color color, bool emission = false)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color) + emission;
            Material m;
            if(materials.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Shader.Find("Standard")); m.color = color;
            m.SetFloat("_Glossiness", .22f);
            if(emission) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * .6f); }
            materials[key] = m; return m;
        }
        public static GameObject Shape(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, Transform parent = null, bool collider = false, bool glow = false)
        {
            var o = GameObject.CreatePrimitive(type); o.name = name;
            if(parent != null) o.transform.SetParent(parent, false);
            o.transform.localPosition = pos; o.transform.localScale = scale;
            o.GetComponent<Renderer>().sharedMaterial = Mat(color, glow);
            if(!collider) Object.Destroy(o.GetComponent<Collider>());
            return o;
        }
        public static void Create(DefenseGame game)
        {
            MultiLevelWorld.Create(game);
        }
        static float DistanceToSegment(Vector3 p,Vector3 a,Vector3 b) { p.y=a.y; Vector3 d=b-a; return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude)); }
        static void Tree(Vector3 p,float size,Transform parent)
        {
            var root=new GameObject("Low-poly evergreen").transform; root.SetParent(parent); root.position=p; root.localScale=Vector3.one*size;
            Shape("Trunk",PrimitiveType.Cylinder,Vector3.up*.45f,new Vector3(.2f,.45f,.2f),new Color(.37f,.29f,.2f),root);
            Cone("Lower crown",new Vector3(0,.65f,0),.73f,1.3f,new Color(.13f,.32f,.25f),root);
            Cone("Upper crown",new Vector3(0,1.25f,0),.52f,1.15f,new Color(.22f,.44f,.31f),root);
        }
        public static GameObject Cone(string name,Vector3 pos,float radius,float height,Color color,Transform parent)
        {
            var o=new GameObject(name); o.transform.SetParent(parent,false); o.transform.localPosition=pos;
            var mesh=new Mesh(); var vertices=new List<Vector3>(); var triangles=new List<int>();
            const int n=7;
            for(int i=0;i<n;i++) {
                float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;
                int v=vertices.Count; vertices.Add(new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); vertices.Add(Vector3.up*height); vertices.Add(new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius));
                triangles.Add(v);triangles.Add(v+1);triangles.Add(v+2);
            }
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);return o;
        }
        public static Tower CreateTower(BuildPad pad,TowerKind kind)
        {
            var o=new GameObject(DefenseGame.Specs[(int)kind].name);o.transform.SetParent(DefenseGame.Instance.WorldRoot);o.transform.position=pad.transform.position;
            Tower t=o.AddComponent<Tower>();t.Kind=kind;t.Pad=pad;t.Invested=DefenseGame.Specs[(int)kind].cost;
            Color color=DefenseGame.Specs[(int)kind].color;
            Shape("Stone foot",PrimitiveType.Cylinder,new Vector3(0,.4f,0),new Vector3(.96f,.17f,.96f),new Color(.77f,.76f,.6f),o.transform);
            Shape("Armored stem",PrimitiveType.Cylinder,new Vector3(0,.78f,0),new Vector3(.58f,.32f,.58f),new Color(.22f,.32f,.3f),o.transform);
            var head=new GameObject("Rotating head").transform;head.SetParent(o.transform,false);head.localPosition=Vector3.up*1.12f;t.Head=head;
            if(kind==TowerKind.Bolt) {
                Shape("Turret housing",PrimitiveType.Cube,Vector3.zero,new Vector3(.69f,.42f,.67f),color,head);
                Shape("Barrel",PrimitiveType.Cube,new Vector3(0,.02f,.47f),new Vector3(.2f,.19f,.65f),new Color(.15f,.26f,.24f),head);
                Shape("Cap light",PrimitiveType.Sphere,new Vector3(0,.24f,0),Vector3.one*.2f,color,head,false,true);
            } else if(kind==TowerKind.Frost) {
                var gem=Shape("Frost prism",PrimitiveType.Cube,new Vector3(0,.25f,0),new Vector3(.49f,.8f,.49f),color,head,false,true);
                gem.transform.localRotation=Quaternion.Euler(0,45,15);
                for(int i=0;i<3;i++) { float a=i*Mathf.PI*2/3;Shape("Crystal prong",PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*.4f,0,Mathf.Sin(a)*.4f),new Vector3(.12f,.63f,.12f),new Color(.7f,.8f,.78f),head); }
            } else {
                Shape("Mortar cradle",PrimitiveType.Sphere,Vector3.zero,new Vector3(.82f,.5f,.82f),new Color(.32f,.3f,.25f),head);
                var barrel=Shape("Mortar barrel",PrimitiveType.Cylinder,new Vector3(0,.23f,.13f),new Vector3(.57f,.4f,.57f),color,head);barrel.transform.localRotation=Quaternion.Euler(33,0,0);
                Shape("Mortar opening",PrimitiveType.Sphere,new Vector3(0,.55f,.34f),new Vector3(.34f,.08f,.34f),new Color(.18f,.22f,.22f),head);
            }
            var collider=o.AddComponent<CapsuleCollider>();collider.center=Vector3.up*.8f;collider.height=1.65f;collider.radius=.6f;
            // Tower hits resolve through its foundation parent reference via this proxy.
            var proxy=o.AddComponent<BuildPad>();proxy.Occupant=t;
            t.RefreshLevel();return t;
        }
        public static Enemy CreateEnemy(int kind,int wave,int routeIndex=0)
        {
            var o=new GameObject("Invader " + kind);o.transform.SetParent(DefenseGame.Instance.WorldRoot);
            var e=o.AddComponent<Enemy>();e.Initialize(kind,wave,routeIndex);
            Color color=kind==0?new Color(.85f,.41f,.3f):kind==1?new Color(.94f,.67f,.33f):kind==2?new Color(.56f,.47f,.65f):kind==4?new Color(.25f,.65f,.88f):kind==5?new Color(.42f,.82f,.51f):kind==6?new Color(.85f,.53f,.66f):new Color(.67f,.27f,.35f);
            float scale=kind==3?1.35f:kind==2?1.05f:kind==1?.67f:kind==6?.55f:.85f;
            var body=new GameObject("Animated body").transform;body.SetParent(o.transform,false);body.localScale=Vector3.one*scale;e.Body=body;
            Shape("Body",PrimitiveType.Cube,new Vector3(0,.45f,0),new Vector3(.68f,.65f,.64f),color,body);
            Shape("Shell",PrimitiveType.Cube,new Vector3(0,.78f,-.08f),new Vector3(.77f,.21f,.59f),color*.8f,body);
            for(int s=-1;s<=1;s+=2) {
                Shape("Eye",PrimitiveType.Cube,new Vector3(s*.17f,.57f,.332f),new Vector3(.095f,.11f,.035f),new Color(1,.93f,.71f),body,false,true);
                Shape("Foot",PrimitiveType.Cube,new Vector3(s*.28f,.13f,0),new Vector3(.19f,.18f,.59f),new Color(.24f,.23f,.25f),body);
            }
            if(kind==2 || kind==3) for(int s=-1;s<=1;s+=2) Cone("Armored horn",new Vector3(s*.25f,.8f,0),.15f,kind==3?.6f:.28f,new Color(.95f,.75f,.51f),body);
            if(kind==4) {
                var shield=Shape("Shield fins",PrimitiveType.Cube,new Vector3(0,.55f,.42f),new Vector3(.94f,.88f,.1f),new Color(.35f,.8f,1),body,false,true);e.ShieldVisual=shield;
            }
            if(kind==5) {
                Shape("Healer cross",PrimitiveType.Cube,new Vector3(0,1,0),new Vector3(.5f,.12f,.13f),new Color(.75f,1,.6f),body,false,true);
                Shape("Healer cross",PrimitiveType.Cube,new Vector3(0,1,0),new Vector3(.12f,.5f,.13f),new Color(.75f,1,.6f),body,false,true);
            }
            e.HealthBack=Shape("Health background",PrimitiveType.Cube,new Vector3(0,scale+ .3f,0),new Vector3(.85f,.055f,.09f),new Color(.16f,.21f,.22f),o.transform).transform;
            e.HealthFill=Shape("Health",PrimitiveType.Cube,new Vector3(0,scale+.31f,-.005f),new Vector3(.83f,.06f,.1f),new Color(.96f,.59f,.35f),o.transform).transform;
            return e;
        }
        public static LineRenderer Ring(string name,Vector3 center,float radius,Color color,float width,Transform parent)
        {
            var o=new GameObject(name);o.transform.SetParent(parent);var line=o.AddComponent<LineRenderer>();
            line.sharedMaterial=new Material(Shader.Find("Sprites/Default"));line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;line.loop=true;line.positionCount=64;line.useWorldSpace=true;
            SetRing(line,center,radius);return line;
        }
        public static void SetRing(LineRenderer line,Vector3 center,float radius)
        { for(int i=0;i<line.positionCount;i++) {float a=i*Mathf.PI*2/line.positionCount;line.SetPosition(i,center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));} }
        public static void Burst(Vector3 pos,Color color,float radius)
        {
            var ring=Ring("Impact ring",pos,.1f,color,.055f,DefenseGame.Instance.WorldRoot);
            var effect=ring.gameObject.AddComponent<RingEffect>();effect.Radius=radius;
        }
    }
    public class CrystalMotion : MonoBehaviour
    {
        Vector3 origin;void Start(){origin=transform.position;}
        void Update(){transform.position=origin+Vector3.up*Mathf.Sin(Time.time*1.6f)*.1f;transform.Rotate(0,Time.deltaTime*17,0,Space.World);}
    }
    public class RingEffect : MonoBehaviour
    {
        public float Radius=1;float life;LineRenderer line;Vector3 center;
        void Start(){line=GetComponent<LineRenderer>();center=line.GetPosition(0)-new Vector3(.1f,0,0);}
        void Update(){life+=Time.deltaTime;WorldBuilder.SetRing(line,center,Mathf.Lerp(.1f,Radius,life/.4f));Color c=line.startColor;c.a=1-life/.4f;line.startColor=line.endColor=c;if(life>=.4f)Destroy(gameObject);}
    }
}
