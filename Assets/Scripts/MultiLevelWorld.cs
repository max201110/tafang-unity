using UnityEngine;

namespace Verdant
{
    public static class MultiLevelWorld
    {
        static readonly Color Stone=new Color(.22f,.30f,.32f);
        static readonly Color Deck=new Color(.33f,.47f,.48f);
        public static void Create(DefenseGame game)
        {
            Random.InitState(143);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.62f,.70f,.73f);
            var sun=new GameObject("Sun / bridge shadows").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.color=new Color(1,.93f,.8f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(53,-32,0);
            QualitySettings.shadowDistance=90;
            var cam=new GameObject("Orbit camera").AddComponent<Camera>();cam.tag="MainCamera";
            cam.orthographic=true;cam.orthographicSize=12.2f;cam.nearClipPlane=.1f;cam.farClipPlane=140;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.06f,.105f,.13f);cam.rect=new Rect(0,.18f,.75f,.65f);
            cam.gameObject.AddComponent<AudioListener>();game.WorldCamera=cam;cam.gameObject.AddComponent<BattleCamera>();
            var root=game.WorldRoot;
            Solid("Island rock",PrimitiveType.Cube,new Vector3(0,-1.1f,.5f),new Vector3(26,2,21),Stone,root);
            var ground=WorldBuilder.Shape("Meadow",PrimitiveType.Cube,new Vector3(0,-.13f,.5f),new Vector3(26.2f,.3f,21.2f),new Color(.25f,.38f,.31f),root,true);ground.layer=8;
            for(int x=-12;x<=12;x+=2)for(int z=-9;z<=9;z+=2){float v=Random.Range(-.02f,.02f);WorldBuilder.Shape("Moss tile",PrimitiveType.Cube,new Vector3(x,.028f,z),new Vector3(1.96f,.025f,1.96f),new Color(.29f+v,.43f+v,.34f+v),root);}
            // Actual vertical separation: ground 0 m, viaduct 3.6 m, summit 6.4 m.
            Solid("Summit escarpment",PrimitiveType.Cube,new Vector3(-7,3.15f,8),new Vector3(9.5f,6.3f,4.2f),new Color(.28f,.35f,.36f),root);
            Solid("Summit meadow",PrimitiveType.Cube,new Vector3(-7,6.3f,8),new Vector3(9.7f,.2f,4.4f),new Color(.4f,.5f,.39f),root);
            for(int i=0;i<7;i++){var ledge=Solid("Cliff strata",PrimitiveType.Cube,new Vector3(-11+i*1.3f,2.1f+(i%3)*.45f,5.95f),new Vector3(1.15f,3.8f,.6f),new Color(.23f,.30f,.32f),root);ledge.transform.localRotation=Quaternion.Euler(0,0,(i%2==0?4:-4));}
            game.Routes=new[]{
                new[]{P(-11,0,3.8f),P(-7,0,3.8f),P(-7,0,-2),P(-1,0,-2),P(-1,0,2.1f),P(6,0,2.1f),P(6,0,-5.8f),P(10,0,-5.8f)},
                new[]{P(-10.5f,3.6f,-6),P(-5,3.6f,-6),P(-5,3.6f,1),P(3,3.6f,1),P(3,3.6f,-3.3f),P(7,3.6f,-3.3f),P(10,0,-5.8f)},
                new[]{P(-10.5f,6.4f,7.4f),P(-4,6.4f,7.4f),P(0,3.6f,5.6f),P(6.2f,3.6f,5.6f),P(9,0,2),P(10,0,-5.8f)}
            };
            game.Route=game.Routes[0];
            for(int r=0;r<game.Routes.Length;r++) {
                var route=game.Routes[r];Color tint=game.RouteColors[r];
                for(int i=1;i<route.Length;i++) Road(route[i-1],route[i],r,tint,root);
                Gate(route[0],r,tint,root);
            }
            Vector3[] pads={
                new Vector3(-8.9f,0,1.1f),new Vector3(-5.3f,0,3.7f),new Vector3(-3.2f,0,-.2f),new Vector3(-3.4f,0,-3.6f),
                new Vector3(1.1f,0,.0f),new Vector3(4.2f,0,-.8f),new Vector3(7.8f,0,-.2f),new Vector3(7.6f,0,-6.9f),
                new Vector3(-7.5f,3.6f,-7.8f),new Vector3(-3.25f,3.6f,-4.6f),new Vector3(-3.2f,3.6f,2.5f),new Vector3(1.35f,3.6f,-.8f),
                new Vector3(5.15f,3.6f,-1.6f),new Vector3(3.6f,3.6f,7.2f),new Vector3(-8.2f,6.4f,8.9f),new Vector3(-3.6f,6.4f,8.9f),
                new Vector3(-.3f,3.6f,7.4f),new Vector3(10.5f,0,3.8f)
            };
            for(int i=0;i<pads.Length;i++) Pad(game,pads[i],i,root);
            Vector3 end=game.Route[game.Route.Length-1];
            WorldBuilder.Shape("Core pedestal",PrimitiveType.Cylinder,end,new Vector3(2.3f,.25f,2.3f),new Color(.68f,.73f,.6f),root);
            var crystal=WorldBuilder.Shape("Living core",PrimitiveType.Cube,end+Vector3.up*1.3f,new Vector3(.9f,1.9f,.9f),new Color(.4f,1,.75f),root,false,true);crystal.transform.rotation=Quaternion.Euler(0,45,15);crystal.AddComponent<CrystalMotion>();
            WorldBuilder.Ring("Core halo",end+Vector3.up*.5f,1.3f,new Color(.48f,1,.75f),.055f,root);
            Billboard(end+Vector3.up*2.65f,"CORE",new Color(.7f,1,.8f),root,.19f);
            for(int i=0;i<58;i++) {
                Vector3 p=new Vector3(Random.Range(-12,12),0,Random.Range(-8.8f,9.5f));
                if(p.z>5.6f && p.x<-2.3f)continue;
                bool clear=true;
                foreach(var route in game.Routes)for(int j=1;j<route.Length;j++)if(DistanceXZ(p,route[j-1],route[j])<1.5f)clear=false;
                foreach(var pad in pads)if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(pad.x,pad.z))<1.3f)clear=false;
                if(!clear)continue;
                Tree(p,Random.Range(.65f,1.1f),root);
            }
            Tree(new Vector3(-11,6.4f,9.3f),.8f,root);Tree(new Vector3(-5.6f,6.4f,9.2f),.9f,root);
            // Water channel and stone banks give the ground level its own landmark.
            for(int i=0;i<7;i++)WorldBuilder.Shape("Stream",PrimitiveType.Cube,new Vector3(-11.8f+i*.52f,.06f,-7.5f),new Vector3(.65f,.04f,2.1f),new Color(.2f,.49f,.56f),root);
        }
        static GameObject Solid(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, Transform root)
        {
            var obj=WorldBuilder.Shape(name,type,pos,scale,color,root,true);
            obj.layer=TerrainOcclusion.Layer;
            return obj;
        }
        static Vector3 P(float x,float y,float z){return new Vector3(x,y+.19f,z);}
        static float DistanceXZ(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;Vector3 d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude));}
        static void Road(Vector3 a,Vector3 b,int route,Color tint,Transform root)
        {
            Vector3 delta=b-a;Quaternion rot=Quaternion.LookRotation(delta.normalized,Vector3.up);
            Color surface=route==0?new Color(.67f,.62f,.45f):route==1?Deck:new Color(.45f,.40f,.51f);
            var deck=WorldBuilder.Shape("Route "+(char)('A'+route)+" deck",PrimitiveType.Cube,(a+b)/2-Vector3.up*.12f,new Vector3(1.4f,.23f,delta.magnitude+.3f),surface,root,true);deck.transform.rotation=rot;deck.layer=8;
            Vector3 side=Vector3.Cross(Vector3.up,delta).normalized;
            for(int sign=-1;sign<=1;sign+=2) {
                var rail=WorldBuilder.Shape("Route edge",PrimitiveType.Cube,(a+b)/2+side*sign*.64f+Vector3.up*.065f,new Vector3(.055f,.055f,delta.magnitude),tint,root,false,true);rail.transform.rotation=rot;
            }
            int count=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.9f));
            for(int j=0;j<count;j++){
                Vector3 p=Vector3.Lerp(a,b,(j+.5f)/count);
                var dash=WorldBuilder.Shape("Lane marker",PrimitiveType.Cube,p+Vector3.up*.025f,new Vector3(.09f,.025f,.25f),tint,root,false,true);dash.transform.rotation=rot;
                if(route>0 && j%3==0 && p.y>1.2f) {
                    Solid("Bridge pier",PrimitiveType.Cube,new Vector3(p.x,(p.y-.28f)/2,p.z),new Vector3(.32f,p.y-.28f,.42f),Stone,root);
                    for(int sign=-1;sign<=1;sign+=2)WorldBuilder.Shape("Balustrade light",PrimitiveType.Cylinder,p+side*sign*.67f+Vector3.up*.23f,new Vector3(.09f,.23f,.09f),tint,root,false,true);
                }
            }
            if(route>0 && Mathf.Abs(delta.y)<.1f) {
                var truss=Solid("Underdeck beam",PrimitiveType.Cube,(a+b)/2-Vector3.up*.36f,new Vector3(.38f,.37f,delta.magnitude),Stone,root);truss.transform.rotation=rot;
            }
        }
        static void Pad(DefenseGame game,Vector3 pos,int index,Transform root)
        {
            var o=new GameObject("Build pad "+(index+1)+" height "+pos.y);o.transform.SetParent(root);o.transform.position=pos;
            var pad=o.AddComponent<BuildPad>();pad.Index=index;game.Pads.Add(pad);
            Color tint=game.RouteColors[pos.y>6?2:pos.y>1?1:0];
            if(pos.y>1 && !(pos.y>6)) {
                Solid("Tower platform",PrimitiveType.Cube,pos+Vector3.down*.16f,new Vector3(1.8f,.28f,1.8f),Deck,root);
                Solid("Tower support",PrimitiveType.Cube,new Vector3(pos.x,(pos.y-.3f)/2,pos.z),new Vector3(.56f,pos.y-.3f,.56f),Stone,root);
            }
            WorldBuilder.Shape("Foundation",PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(1.36f,.12f,1.36f),new Color(.25f,.34f,.35f),o.transform,true);
            WorldBuilder.Shape("Build surface",PrimitiveType.Cylinder,new Vector3(0,.25f,0),new Vector3(1.15f,.025f,1.15f),new Color(.53f,.61f,.52f),o.transform,true);
            WorldBuilder.Ring("Build socket",pos+Vector3.up*.29f,.5f,tint,.04f,root);
            WorldBuilder.Shape("Plus",PrimitiveType.Cube,new Vector3(0,.29f,0),new Vector3(.42f,.025f,.07f),tint,o.transform);
            WorldBuilder.Shape("Plus",PrimitiveType.Cube,new Vector3(0,.29f,0),new Vector3(.07f,.025f,.42f),tint,o.transform);
        }
        static void Gate(Vector3 p,int route,Color tint,Transform root)
        {
            Solid("Entry platform",PrimitiveType.Cube,p-Vector3.up*.17f,new Vector3(1.8f,.28f,2.1f),Stone,root);
            for(int s=-1;s<=1;s+=2)WorldBuilder.Shape("Gate upright",PrimitiveType.Cube,p+new Vector3(0,.82f,s*.85f),new Vector3(.3f,1.8f,.3f),tint,root);
            WorldBuilder.Shape("Gate lintel",PrimitiveType.Cube,p+Vector3.up*1.7f,new Vector3(.33f,.25f,2),tint,root);
            Billboard(p+Vector3.up*2.25f,((char)('A'+route)).ToString()+" / "+(route==0?"0 M":route==1?"3.6 M":"6.4 M"),tint,root,.19f);
        }
        static void Tree(Vector3 pos,float size,Transform root)
        {
            WorldBuilder.Shape("Pine trunk",PrimitiveType.Cylinder,pos+Vector3.up*.38f*size,new Vector3(.19f,.38f,.19f)*size,new Color(.36f,.27f,.19f),root);
            WorldBuilder.Cone("Evergreen",pos+Vector3.up*.4f*size,.6f*size,1.35f*size,new Color(.13f,.31f,.26f),root);
            WorldBuilder.Cone("Evergreen tip",pos+Vector3.up*.95f*size,.4f*size,1.05f*size,new Color(.22f,.42f,.3f),root);
        }
        static void Billboard(Vector3 pos,string text,Color color,Transform parent,float size)
        {
            var obj=new GameObject(text);obj.transform.SetParent(parent);obj.transform.position=pos;
            var label=obj.AddComponent<TextMesh>();label.text=text;label.fontSize=48;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.color=color;
            obj.AddComponent<FaceCamera>();
        }
    }
    public class FaceCamera:MonoBehaviour
    {void LateUpdate(){if(DefenseGame.Instance!=null)transform.rotation=DefenseGame.Instance.WorldCamera.transform.rotation;}}
    public class BattleCamera:MonoBehaviour
    {
        float yaw=24,pitch=42,zoom=12.2f;Vector3 target=new Vector3(0,2,0);Camera cam;
        void Awake(){cam=GetComponent<Camera>();Position();}
        void LateUpdate()
        {
            var game=DefenseGame.Instance;
            if(game!=null && !game.ModalOpen && !game.RewardPending && !game.TestMode && cam.pixelRect.Contains(Input.mousePosition)) {
                if(Input.GetMouseButton(1)){yaw+=Input.GetAxis("Mouse X")*4;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*3,24,78);}
                zoom=Mathf.Clamp(zoom-Input.mouseScrollDelta.y*.75f,8,19);
            }
            if(Input.GetKeyDown(KeyCode.Home)){yaw=24;pitch=42;zoom=12.2f;target=new Vector3(0,2,0);}
            Position();
        }
        void Position(){transform.rotation=Quaternion.Euler(pitch,yaw,0);transform.position=target-transform.forward*44;cam.orthographicSize=zoom;}
    }
}
