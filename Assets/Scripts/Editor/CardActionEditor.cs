using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CardAction))]
public class CardActionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 1. Dibujar el Foldout (la flechita para expandir/contraer)
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            // Obtener propiedades
            SerializedProperty actionType = property.FindPropertyRelative("actionType");
            SerializedProperty attacks = property.FindPropertyRelative("attacks");
            SerializedProperty buffs = property.FindPropertyRelative("buffs");
            SerializedProperty attackCountCanBeAugmented = property.FindPropertyRelative("attackCountCanBeAugmented");

            // 2. Dibujar ActionType
            Rect fieldRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(fieldRect, actionType);
            float currentY = fieldRect.y + EditorGUIUtility.singleLineHeight + 2;

            // 3. Dibujo Condicional
            if (actionType.enumValueIndex == (int)ActionTypes.Attack)
            {
                // Dibujar el bool debajo de la lista
                EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight), attackCountCanBeAugmented);
                currentY += EditorGUIUtility.singleLineHeight + 2;
                // Dibujar lista de ataques
                float listHeight = EditorGUI.GetPropertyHeight(attacks, true);
                EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, listHeight), attacks, true);

            }
            else if (actionType.enumValueIndex == (int)ActionTypes.Buff || actionType.enumValueIndex == (int)ActionTypes.ApplyDebuff)
            {
                float listHeight = EditorGUI.GetPropertyHeight(buffs, true);
                EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, listHeight), buffs, true);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    // ESTO ES LO QUE EVITA EL SOLAPAMIENTO
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;

        float height = EditorGUIUtility.singleLineHeight + 2; // Altura del Foldout
        height += EditorGUIUtility.singleLineHeight + 2;     // Altura del ActionType

        SerializedProperty actionType = property.FindPropertyRelative("actionType");

        if (actionType.enumValueIndex == (int)ActionTypes.Attack)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("attacks"), true) + 2;
            height += EditorGUIUtility.singleLineHeight + 2; // El bool attackCountCanBeAugmented
        }
        else if (actionType.enumValueIndex == (int)ActionTypes.Buff || actionType.enumValueIndex == (int)ActionTypes.ApplyDebuff)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("buffs"), true) + 2;
        }

        return height;
    }
}