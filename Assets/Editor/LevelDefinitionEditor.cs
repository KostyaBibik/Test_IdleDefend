using Db;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(LevelDefinition))]
    public class LevelDefinitionEditor : UnityEditor.Editor
    {
        private static readonly string[] StageLabels =
        {
            "Этап 1 (старт → ★1)",
            "Этап 2 (★1 → ★2)",
            "Этап 3 (★2 → ★3)"
        };

        private static readonly string[] StageFieldNames = { "stage1", "stage2", "stage3" };

        private static readonly string[] StageValueFieldNames =
        {
            "spawnDelayMin",
            "spawnDelayMax",
            "extraHealth",
            "extraSpeed",
            "endOfWaveHealthMultiplier",
            "finalSpawnLeadSecondsOverride"
        };

        private ReorderableList _enemiesList;
        private SerializedProperty _levelEnemiesProp;

        private void OnEnable()
        {
            _levelEnemiesProp = serializedObject.FindProperty("levelEnemies");

            _enemiesList = new ReorderableList(serializedObject, _levelEnemiesProp, true, true, true, true)
            {
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawEnemyElement,
                elementHeightCallback = GetEnemyElementHeight,
                onAddCallback = OnAddEnemy
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                var scriptProp = serializedObject.FindProperty("m_Script");
                if (scriptProp != null)
                    EditorGUILayout.PropertyField(scriptProp);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("levelId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startCoins"));

            EditorGUILayout.Space();
            _enemiesList.DoLayoutList();
            EditorGUILayout.Space();

            DrawPropertiesExcluding(serializedObject, "m_Script", "levelId", "startCoins", "levelEnemies");

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Враги уровня");
        }

        private void OnAddEnemy(ReorderableList list)
        {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;

            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            ResetEnemyElement(element);
            list.index = index;
        }

        private static void ResetEnemyElement(SerializedProperty element)
        {
            element.FindPropertyRelative("enemyType").enumValueIndex = 0;
            element.isExpanded = true;

            for (var i = 0; i < StageFieldNames.Length; i++)
            {
                var stageProp = element.FindPropertyRelative(StageFieldNames[i]);
                stageProp.isExpanded = true;
                stageProp.FindPropertyRelative("enabled").boolValue = false;
                stageProp.FindPropertyRelative("spawnDelayMin").floatValue = 1f;
                stageProp.FindPropertyRelative("spawnDelayMax").floatValue = 1f;
                stageProp.FindPropertyRelative("extraHealth").intValue = 0;
                stageProp.FindPropertyRelative("extraSpeed").floatValue = 0f;
                stageProp.FindPropertyRelative("endOfWaveHealthMultiplier").floatValue = 1f;
                stageProp.FindPropertyRelative("finalSpawnLeadSecondsOverride").floatValue = -1f;
            }
        }

        private void DrawEnemyElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _levelEnemiesProp.GetArrayElementAtIndex(index);
            var enemyTypeProp = element.FindPropertyRelative("enemyType");

            var lineHeight = EditorGUIUtility.singleLineHeight;
            rect.y += 2;
            rect.x += 6; // отступ от "=" ручки reorder-списка, иначе стрелка foldout-а сливается с ней
            rect.width -= 6;

            var enumRect = new Rect(rect.x + rect.width - 150, rect.y, 150, lineHeight);
            var foldoutRect = new Rect(rect.x, rect.y, rect.width - 155, lineHeight);

            var label = ObjectNames.NicifyVariableName(
                enemyTypeProp.enumDisplayNames[Mathf.Clamp(enemyTypeProp.enumValueIndex, 0, enemyTypeProp.enumDisplayNames.Length - 1)]);
            element.isExpanded = EditorGUI.Foldout(foldoutRect, element.isExpanded, label, true);
            EditorGUI.PropertyField(enumRect, enemyTypeProp, GUIContent.none);

            if (!element.isExpanded)
                return;

            var y = rect.y + lineHeight + 4;
            for (var stageIndex = 0; stageIndex < StageFieldNames.Length; stageIndex++)
            {
                var stageProp = element.FindPropertyRelative(StageFieldNames[stageIndex]);
                y = DrawStage(rect, y, stageProp, stageIndex);
            }
        }

        // Отступ этапа явно больше ширины иконки foldout-стрелки врага (~14px), иначе стрелка
        // элемента-врага и чекбокс этапа визуально сливаются в один столбец.
        private const float StageIndent = 22f;
        private const float StageCheckboxWidth = 16f;
        private const float StageCheckboxToLabelGap = 8f;
        private const float StageLabelIndent = StageIndent + StageCheckboxWidth + StageCheckboxToLabelGap;
        private const float StageFieldIndent = StageLabelIndent + 12f;

        private static float DrawStage(Rect containerRect, float y, SerializedProperty stageProp, int stageIndex)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var enabledProp = stageProp.FindPropertyRelative("enabled");

            var toggleRect = new Rect(containerRect.x + StageIndent, y, StageCheckboxWidth, lineHeight);
            var labelRect = new Rect(containerRect.x + StageLabelIndent, y,
                containerRect.width - StageLabelIndent, lineHeight);

            enabledProp.boolValue = EditorGUI.Toggle(toggleRect, enabledProp.boolValue);

            if (!enabledProp.boolValue)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.LabelField(labelRect, StageLabels[stageIndex] + " — отключён");

                return y + lineHeight + 2;
            }

            stageProp.isExpanded = EditorGUI.Foldout(labelRect, stageProp.isExpanded, StageLabels[stageIndex], true);
            y += lineHeight + 2;

            if (!stageProp.isExpanded)
                return y;

            foreach (var fieldName in StageValueFieldNames)
            {
                var fieldProp = stageProp.FindPropertyRelative(fieldName);
                var fieldRect = new Rect(containerRect.x + StageFieldIndent, y,
                    containerRect.width - StageFieldIndent, lineHeight);
                EditorGUI.PropertyField(fieldRect, fieldProp);
                y += lineHeight + 2;
            }

            return y;
        }

        private float GetEnemyElementHeight(int index)
        {
            var element = _levelEnemiesProp.GetArrayElementAtIndex(index);
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var height = lineHeight + 6f;

            if (!element.isExpanded)
                return height;

            for (var stageIndex = 0; stageIndex < StageFieldNames.Length; stageIndex++)
            {
                var stageProp = element.FindPropertyRelative(StageFieldNames[stageIndex]);
                var enabledProp = stageProp.FindPropertyRelative("enabled");

                height += lineHeight + 2;

                if (enabledProp.boolValue && stageProp.isExpanded)
                    height += StageValueFieldNames.Length * (lineHeight + 2);
            }

            return height + 6f;
        }
    }
}
