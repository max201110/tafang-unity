#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Verdant.Editor
{
    public static class ProjectSetup
    {
        [MenuItem("翠境防线/创建主场景")]
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Verdant / Game Controller").AddComponent<DefenseGame>();
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Verdant.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Verdant.unity",true)};
            PlayerSettings.companyName="Verdant Studio";PlayerSettings.productName="翠境防线 · 三线纵深";
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=1000;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            // Keep shaders used by procedural geometry in standalone builds.
            var graphics=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if(graphics.Length>0) {
                var so=new SerializedObject(graphics[0]);var shaders=so.FindProperty("m_AlwaysIncludedShaders");
                foreach(string name in new[]{"Standard","Sprites/Default"}) {
                    Shader shader=Shader.Find(name);bool found=false;
                    for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
                    if(!found){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            QualitySettings.antiAliasing=4;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;
            AssetDatabase.SaveAssets();Debug.Log("VERDANT_SCENE_READY");
        }
        [MenuItem("翠境防线/构建 Windows 版本")]
        public static void BuildWindows()
        {
            CreateScene();Directory.CreateDirectory("Builds/ThreeFronts");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Verdant.unity"},locationPathName="Builds/ThreeFronts/Verdant.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            Debug.Log("VERDANT_BUILD_SUCCESS "+report.summary.totalSize);
        }
    }
}
#endif


