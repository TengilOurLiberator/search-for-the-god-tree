using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Godtree.EditorTools
{
    /// <summary>Prints compile messages for the project's own shaders. Batch: -executeMethod Godtree.EditorTools.ShaderCheck.Run</summary>
    public static class ShaderCheck
    {
        [MenuItem("Godtree/Check Shaders")]
        public static void Run()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets/Godtree" }))
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
                var material = new Material(shader);
                // Force every pass to compile so errors are reported now and not at first draw.
                for (int pass = 0; pass < material.passCount; pass++)
                    ShaderUtil.CompilePass(material, pass, true);
                ShaderMessage[] messages = ShaderUtil.GetShaderMessages(shader);
                Debug.Log($"[Godtree] Shader {shader.name}: supported={shader.isSupported} passes={material.passCount} messages={messages.Length}");
                foreach (ShaderMessage m in messages)
                    Debug.Log($"[Godtree]   {m.severity} line {m.line}: {m.message}");
                Object.DestroyImmediate(material);
            }
        }
    }
}