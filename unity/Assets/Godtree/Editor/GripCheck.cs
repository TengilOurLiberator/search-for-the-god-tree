using System.Collections.Generic;
using System.IO;
using Godtree.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Godtree.EditorTools
{
    /// <summary>
    /// Renders every weapon in the sandbox from above with its computed hold drawn on it:
    /// a red ball where the palm goes and a yellow bar pointing the way the weapon should
    /// point out of the fist. One sheet, docs/images/grip_check.png, so wrong holds can be
    /// spotted without a headset.
    /// Batch: -executeMethod Godtree.EditorTools.GripCheck.Render
    /// </summary>
    public static class GripCheck
    {
        [MenuItem("Godtree/Render Grip Check")]
        public static void Render()
        {
            EditorSceneManager.OpenScene("Assets/Godtree/Scenes/Sandbox.unity", OpenSceneMode.Single);
            GripPose[] poses = Object.FindObjectsByType<GripPose>(FindObjectsSortMode.None);
            System.Array.Sort(poses, (a, b) => string.CompareOrdinal(a.name, b.name));
            if (poses.Length == 0)
            {
                Debug.LogError("[Godtree] No GripPose in the scene");
                return;
            }

            const int cell = 320, columns = 5;
            int rows = Mathf.CeilToInt(poses.Length / (float)columns);
            var sheet = new Texture2D(columns * cell, rows * cell, TextureFormat.RGB24, false);
            var rt = new RenderTexture(cell, cell, 24);
            var tile = new Texture2D(cell, cell, TextureFormat.RGB24, false);

            var cameraObject = new GameObject("GripCheck camera");
            var cam = cameraObject.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.13f, 0.13f, 0.23f);
            cam.targetTexture = rt;
            cam.cullingMask = 1 << 31;

            Material red = Unlit(new Color(1f, 0.25f, 0.3f)), yellow = Unlit(new Color(1f, 0.85f, 0.3f));

            for (int i = 0; i < poses.Length; i++)
            {
                GripPose pose = poses[i];
                // Show only this weapon and its markers, on a private layer.
                var restore = new List<(GameObject, int)>();
                foreach (Transform t in pose.GetComponentsInChildren<Transform>(true))
                {
                    restore.Add((t.gameObject, t.gameObject.layer));
                    t.gameObject.layer = 31;
                }

                Vector3 grip = pose.transform.TransformPoint(pose.localGripPoint);
                Vector3 main = pose.transform.TransformDirection(pose.localMainAxis).normalized;

                GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.layer = 31;
                ball.transform.position = grip;
                ball.transform.localScale = Vector3.one * 0.06f;
                ball.GetComponent<MeshRenderer>().sharedMaterial = red;

                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.layer = 31;
                bar.transform.position = grip + main * 0.16f + Vector3.up * 0.05f;
                bar.transform.rotation = Quaternion.LookRotation(main);
                bar.transform.localScale = new Vector3(0.02f, 0.02f, 0.3f);
                bar.GetComponent<MeshRenderer>().sharedMaterial = yellow;

                Bounds bounds = new Bounds(grip, Vector3.zero);
                foreach (Renderer r in pose.GetComponentsInChildren<Renderer>())
                    bounds.Encapsulate(r.bounds);
                cam.transform.position = bounds.center + Vector3.up * 5f;
                cam.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                cam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.15f + 0.05f;
                cam.Render();

                RenderTexture.active = rt;
                tile.ReadPixels(new Rect(0, 0, cell, cell), 0, 0);
                tile.Apply();
                RenderTexture.active = null;
                int col = i % columns, row = rows - 1 - i / columns;
                sheet.SetPixels(col * cell, row * cell, cell, cell, tile.GetPixels());

                Object.DestroyImmediate(ball);
                Object.DestroyImmediate(bar);
                foreach ((GameObject go, int layer) in restore)
                    go.layer = layer;
                Debug.Log($"[Godtree] grip {i + 1}/{poses.Length} {pose.name} style={pose.style} row={i / columns + 1} col={col + 1}");
            }

            sheet.Apply();
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/images"));
            Directory.CreateDirectory(outDir);
            File.WriteAllBytes(Path.Combine(outDir, "grip_check.png"), sheet.EncodeToPNG());
            cam.targetTexture = null;
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(rt);
        }

        private static Material Unlit(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", color);
            return material;
        }
    }
}
