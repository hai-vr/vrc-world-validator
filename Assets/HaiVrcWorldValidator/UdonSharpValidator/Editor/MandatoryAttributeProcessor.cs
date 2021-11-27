// MIT License
//
// Copyright (c) 2021 Haï~ (@vr_hai github.com/hai-vr)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Linq;
using System.Reflection;
using HaiVrcWorldValidator.UdonSharpValidator.Attributes;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;
using Object = UnityEngine.Object;

namespace HaiVrcWorldValidator.UdonSharpValidator.Editor
{
    /// <summary>
    /// This will fail a build according to the rules defined in Mandatory.cs,
    /// where a [Mandatory] UdonSharp field would be null at runtime.
    /// </summary>
    [InitializeOnLoad]
    public class MandatoryAttributeProcessor
    {
        static MandatoryAttributeProcessor()
        {
            EditorApplication.playModeStateChanged += WhenEnterPlayMode;
        }

        private static void WhenEnterPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            var missingAttributeNullable = FindFirstMissingAttributeOrNull(typeof(Mandatory), true);
            if (missingAttributeNullable != null)
            {
                EditorApplication.isPlaying = false;
                EditorGUIUtility.PingObject(missingAttributeNullable.sceneObject);
                ShowEditorNotification($"{missingAttributeNullable.fieldName} is mandatory on {missingAttributeNullable.typeName}, but a problem has occurred: {missingAttributeNullable.problem}", 6f);
                return;
            }

            missingAttributeNullable = FindFirstMissingAttributeOrNull(typeof(MandatoryElementsOrNone), false);
            if (missingAttributeNullable != null)
            {
                EditorApplication.isPlaying = false;
                EditorGUIUtility.PingObject(missingAttributeNullable.sceneObject);
                ShowEditorNotification($"{missingAttributeNullable.fieldName} is mandatory on {missingAttributeNullable.typeName}, but a problem has occurred: {missingAttributeNullable.problem}", 6f);
            }
        }

        public static MissingAttribute FindFirstMissingAttributeOrNull(Type attributeType, bool disallowEmptyArrays)
        {
            var udonBehaviours = Object.FindObjectsOfType<UdonBehaviour>();
            foreach (var udonBehaviour in udonBehaviours)
            {
                UdonSharpBehaviour proxyNullable = UdonSharpEditorUtility.FindProxyBehaviour(udonBehaviour);
                if (proxyNullable != null)
                {
                    var allFields = proxyNullable.GetType()
                        .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                        .ToList();

                    var allMandatoryProperties = allFields
                        .Where(info => info.IsDefined(attributeType, false))
                        .ToList();

                    if (allMandatoryProperties.Count > 0)
                    {
                        Debug.Log("Inspecting " + proxyNullable.GetType().Name + "...");
                        foreach (var mandatoryProperty in allMandatoryProperties)
                        {
                            var variableName = mandatoryProperty.Name;
                            udonBehaviour.publicVariables.TryGetVariableValue(variableName, out object value);
                            if (value == null)
                            {
                                return new MissingAttribute
                                {
                                    sceneObject = udonBehaviour,
                                    typeName = proxyNullable.GetType().Name,
                                    fieldName = variableName,
                                    problem = MissingAttributeProblem.IsNull
                                };
                            }

                            if (value is Component comp && comp.gameObject.CompareTag("EditorOnly"))
                            {
                                return new MissingAttribute
                                {
                                    sceneObject = udonBehaviour,
                                    typeName = proxyNullable.GetType().Name,
                                    fieldName = variableName,
                                    problem = MissingAttributeProblem.IsEditorOnly
                                };
                            }

                            if (value is Array arr)
                            {
                                if (disallowEmptyArrays && arr.Length == 0)
                                {
                                    return new MissingAttribute
                                    {
                                        sceneObject = udonBehaviour,
                                        typeName = proxyNullable.GetType().Name,
                                        fieldName = variableName,
                                        problem = MissingAttributeProblem.IsEmptyArray
                                    };
                                }

                                foreach (var item in arr)
                                {
                                    if (item == null)
                                    {
                                        return new MissingAttribute
                                        {
                                            sceneObject = udonBehaviour,
                                            typeName = proxyNullable.GetType().Name,
                                            fieldName = variableName,
                                            problem = MissingAttributeProblem.ArrayContainsNull
                                        };
                                    }

                                    if (item is Component itemComp && itemComp.gameObject.CompareTag("EditorOnly"))
                                    {
                                        return new MissingAttribute
                                        {
                                            sceneObject = udonBehaviour,
                                            typeName = proxyNullable.GetType().Name,
                                            fieldName = variableName,
                                            problem = MissingAttributeProblem.ArrayContainsEditorOnly
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public class MissingAttribute
        {
            public UdonBehaviour sceneObject;
            public string typeName;
            public string fieldName;
            public MissingAttributeProblem problem;
        }

        public enum MissingAttributeProblem
        {
            IsNull, IsEditorOnly, IsEmptyArray, ArrayContainsNull, ArrayContainsEditorOnly
        }

        private static void ShowEditorNotification(string text, float durationSeconds)
        {
            Debug.LogError($"[MandatoryAttributeProcessor] {text}");
            var editorWindows = Resources.FindObjectsOfTypeAll(typeof(SceneView)).Cast<EditorWindow>();
            foreach (var editorWindow in editorWindows)
            {
                editorWindow.ShowNotification(new GUIContent(text, ""), durationSeconds);
            }
        }
    }
}