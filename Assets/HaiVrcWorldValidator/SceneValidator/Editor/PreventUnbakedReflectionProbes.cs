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

using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDKBase.Editor.BuildPipeline;

namespace HaiVrcWorldValidator.SceneValidator.Editor
{
    /// <summary>
    /// This will fail a build if there is any unbaked reflection probe.
    /// </summary>
    public class PreventUnbakedReflectionProbes : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 50;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType == VRCSDKRequestedBuildType.Avatar) return true;

            var hasAtLeastOneUnbakedProbe = Object.FindObjectsOfType<ReflectionProbe>()
                .Where(probe => probe.mode == ReflectionProbeMode.Baked)
                .Any(probe => probe.bakedTexture == null);

            if (hasAtLeastOneUnbakedProbe)
            {
                ShowEditorNotification("Reflection probes must be baked before building.", 3f);
                return false;
            }

            return true;
        }

        private static void ShowEditorNotification(string text, float durationSeconds)
        {
            var editorWindows = Resources.FindObjectsOfTypeAll(typeof(SceneView)).Cast<EditorWindow>();
            foreach (var editorWindow in editorWindows)
            {
                editorWindow.ShowNotification(new GUIContent(text, ""), durationSeconds);
            }
        }
    }
}
