using Db;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(WaveDefinition))]
    public class WaveDefinitionDrawer : PropertyDrawer
    {
        private const int FieldCount = 6;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * (FieldCount + 1);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enemyTypeProp = property.FindPropertyRelative("enemyType");
            var countProp = property.FindPropertyRelative("count");
            var spawnDelayProp = property.FindPropertyRelative("spawnDelay");
            var extraHealthProp = property.FindPropertyRelative("extraHealth");
            var extraSpeedProp = property.FindPropertyRelative("extraSpeed");
            var startTriggerProp = property.FindPropertyRelative("startTrigger");

            var enemyName = enemyTypeProp.enumDisplayNames[enemyTypeProp.enumValueIndex];
            var triggerName = startTriggerProp.enumDisplayNames[startTriggerProp.enumValueIndex];
            var summary = $"{enemyName} ×{countProp.intValue} ({TriggerLabel(startTriggerProp.enumValueIndex, triggerName)})";

            var lineHeight = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var foldoutRect = new Rect(position.x, position.y, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, summary, true);

            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;
            var y = position.y + lineHeight + spacing;

            void Line(SerializedProperty prop, string label2, string tooltip)
            {
                var rect = new Rect(position.x, y, position.width, lineHeight);
                EditorGUI.PropertyField(rect, prop, new GUIContent(label2, tooltip));
                y += lineHeight + spacing;
            }

            Line(enemyTypeProp, "Тип врага", "Какой враг будет спавниться в этой волне");
            Line(countProp, "Количество", "Сколько врагов этого типа заспавнится за волну");
            Line(spawnDelayProp, "Задержка спавна (сек)", "Пауза перед каждым врагом внутри волны");
            Line(extraHealthProp, "Доп. здоровье", "Добавляется к базовому здоровью врага из EnemyDefinition");
            Line(extraSpeedProp, "Доп. скорость", "Добавляется к базовой скорости врага из EnemyDefinition");
            Line(startTriggerProp, "Когда стартует волна",
                "Сразу / после порога 1-й звезды / после порога 2-й звезды — пороги задаются в LevelDefinition");

            EditorGUI.indentLevel--;
        }

        private static string TriggerLabel(int enumValueIndex, string fallback)
        {
            switch (enumValueIndex)
            {
                case 0: return "сразу";
                case 1: return "после 1-й звезды";
                case 2: return "после 2-й звезды";
                default: return fallback;
            }
        }
    }
}
