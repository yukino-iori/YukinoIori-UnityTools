
using UnityEngine;
using UnityEditor;

public class BlendShapeAnimationCopier : EditorWindow
{
    private AnimationClip sourceClip;
    private GameObject targetObject;

    [MenuItem("Tools/YUKINO/BlendShape Animation Copier")]
    private static void OpenWindow()
    {
        GetWindow<BlendShapeAnimationCopier>(
            "BlendShape Copier"
        );
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "BlendShape Animation Copier",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space();

        sourceClip = (AnimationClip)EditorGUILayout.ObjectField(
            "Animation Clip",
            sourceClip,
            typeof(AnimationClip),
            false
        );

        targetObject = (GameObject)EditorGUILayout.ObjectField(
            "Target GameObject",
            targetObject,
            typeof(GameObject),
            true
        );

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Animation Clipの0秒地点にあるBlendShapeの値を、" +
            "名前が一致するコピー先のSkinned Mesh Rendererに適用します。\n" +
            "Hierarchyの階層パスは照合しません。",
            MessageType.Info
        );

        EditorGUILayout.Space();

        GUI.enabled = sourceClip != null && targetObject != null;

        if (GUILayout.Button(
            "Copy BlendShapes",
            GUILayout.Height(35)))
        {
            CopyBlendShapes();
        }

        GUI.enabled = true;
    }

    private void CopyBlendShapes()
    {
        var bindings =
            AnimationUtility.GetCurveBindings(sourceClip);

        var renderers =
            targetObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        int successCount = 0;
        int skipCount = 0;

        foreach (var binding in bindings)
        {
            if (binding.type != typeof(SkinnedMeshRenderer))
                continue;

            const string prefix = "blendShape.";

            if (!binding.propertyName.StartsWith(prefix))
                continue;

            string shapeName =
                binding.propertyName.Substring(prefix.Length);

            AnimationCurve curve =
                AnimationUtility.GetEditorCurve(
                    sourceClip,
                    binding
                );

            if (curve == null)
            {
                skipCount++;
                continue;
            }

            float value = curve.Evaluate(0f);

            bool found = false;

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null)
                    continue;

                int index =
                    renderer.sharedMesh.GetBlendShapeIndex(shapeName);

                if (index < 0)
                    continue;

                Undo.RecordObject(
                    renderer,
                    "Copy BlendShape Animation"
                );

                renderer.SetBlendShapeWeight(index, value);

                EditorUtility.SetDirty(renderer);

                successCount++;
                found = true;
            }

            if (!found)
            {
                skipCount++;
                Debug.LogWarning(
                    "コピー先にBlendShapeが見つかりません：" +
                    shapeName
                );
            }
        }

        Debug.Log(
            "BlendShapeコピー完了\n" +
            "適用数：" + successCount + "\n" +
            "スキップ数：" + skipCount
        );

        EditorUtility.DisplayDialog(
            "コピー完了",
            "BlendShapeのコピーが完了しました。\n\n" +
            "適用数：" + successCount + "\n" +
            "スキップ数：" + skipCount,
            "OK"
        );
    }
}
