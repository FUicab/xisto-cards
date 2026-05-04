using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Requirements))]
public class RequirementsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 1. Foldout inicial
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            // Obtener propiedades
            SerializedProperty requirement = property.FindPropertyRelative("requirement");
            SerializedProperty subtypeRequirement = property.FindPropertyRelative("subtypeRequirement");
            SerializedProperty factionRequirement = property.FindPropertyRelative("factionRequirement");
            SerializedProperty targetIs = property.FindPropertyRelative("targetIs");
            SerializedProperty attribute = property.FindPropertyRelative("attribute");
            SerializedProperty comparison = property.FindPropertyRelative("comparison");
            SerializedProperty attributeValue = property.FindPropertyRelative("attributeValue");
            SerializedProperty targetOfRequirementIsTargetOfAttack = property.FindPropertyRelative("targetOfRequirementIsTargetOfAttack");

            // 2. Dibujar el selector del Enum
            float currentY = position.y + EditorGUIUtility.singleLineHeight + 2;
            Rect enumRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(enumRect, requirement);
            currentY += EditorGUIUtility.singleLineHeight + 2;

            // 3. Lógica Condicional (Ajustar Cast según tus nombres de Enum)
            // Ejemplo: si RequirementTypes.Subtype es el índice 1, etc.
            if (requirement.enumValueIndex == (int)RequirementTypes.TargetHasSubtypesOrFactions) // Ajusta el nombre
            {
                float subTypeBoxHeight = EditorGUI.GetPropertyHeight(subtypeRequirement, true);
                EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, subTypeBoxHeight), subtypeRequirement, true);
                currentY += subTypeBoxHeight + 2;
                float factionBoxHeight = EditorGUI.GetPropertyHeight(factionRequirement, true);
                EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, factionBoxHeight), factionRequirement, true);
                currentY += factionBoxHeight + 2;
            } else if (requirement.enumValueIndex == (int)RequirementTypes.TargetIsNextTo) {
                float height = EditorGUI.GetPropertyHeight(targetIs, true);
                GUIContent customLabel = new GUIContent("Neighbor type");
                EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, height), targetIs, customLabel, true);
            } else if (requirement.enumValueIndex == (int)RequirementTypes.TargetAttributeIs) {
                float height = EditorGUI.GetPropertyHeight(attribute, true);
                EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, height), attribute, true);
                currentY += height + 2;
                EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, height), comparison, true);
                currentY += height + 2;
                EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, height), attributeValue, true);
                currentY += height + 2;
                EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, height), targetOfRequirementIsTargetOfAttack, true);
                currentY += height + 2;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;

        // Base: Foldout + Enum selector
        float height = (EditorGUIUtility.singleLineHeight + 2) * 2;

        SerializedProperty requirement = property.FindPropertyRelative("requirement");

        // Sumar altura de la lista activa
        if (requirement.enumValueIndex == (int)RequirementTypes.TargetHasSubtypesOrFactions) {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("subtypeRequirement"), true) + 2;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("factionRequirement"), true) + 2;
        } else if (requirement.enumValueIndex == (int)RequirementTypes.TargetIsNextTo) {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("targetIs"), true) + 2;
        } else if (requirement.enumValueIndex == (int)RequirementTypes.TargetAttributeIs) {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("attribute"), true) + 2;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("comparison"), true) + 2;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("attributeValue"), true) + 2;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("targetOfRequirementIsTargetOfAttack"), true) + 2;
        }

        return height;
    }
}
