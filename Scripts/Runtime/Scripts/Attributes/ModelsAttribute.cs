using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace DoubTech.ThirdParty.OpenAI
{
    public class ModelsAttribute : PropertyAttribute
    {
        public string ServerConfigFieldName { get; private set; }

        public ModelsAttribute(string serverConfigFieldName)
        {
            ServerConfigFieldName = serverConfigFieldName;
        }
    }
    
    #if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(ModelsAttribute))]
    public class ModelsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ModelsAttribute modelsAttribute = attribute as ModelsAttribute;

            // Get the target object the property belongs to
            object targetObject = property.serializedObject.targetObject;
            Type targetType = targetObject.GetType();

            // Use reflection to find the serverConfig field in the target object
            FieldInfo serverConfigField = targetType.GetField(modelsAttribute.ServerConfigFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (serverConfigField == null)
            {
                EditorGUI.LabelField(position, label.text, "Invalid server config field");
                return;
            }

            OpenAIServerConfig serverConfig = serverConfigField.GetValue(targetObject) as OpenAIServerConfig;
            if (serverConfig == null || serverConfig.models == null || serverConfig.models.Length == 0)
            {
                EditorGUI.TextField(position, label, property.stringValue); // Show text field if no models available
            }
            else
            {
                int currentIndex = Mathf.Max(0, Array.IndexOf(serverConfig.models, property.stringValue));
                currentIndex = EditorGUI.Popup(position, label.text, currentIndex, serverConfig.models);
                property.stringValue = serverConfig.models[currentIndex >= 0 ? currentIndex : 0];
            }
        }
    }

    #endif
}