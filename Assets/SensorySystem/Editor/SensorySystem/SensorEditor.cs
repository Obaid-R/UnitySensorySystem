﻿using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEditor.SceneManagement;

namespace UnitySensorySystem
{
    [CustomEditor(typeof(SensorObject))]
    public class SensorEditor : Editor
    {
        static List<MethodInfo> methods;
        static string[] ignoreMethods = new string[] { "Start", "Update", "LateUpdate", "FixedUpdate" };

        public override void OnInspectorGUI()
        {
            SensorObject sensorObj = (SensorObject)target;
            if (sensorObj.sensor == null)
                return;


            GUILayout.Box("Sensor General Options", EditorStyles.helpBox);

            sensorObj.sensor.Sense = (SenseType)EditorGUILayout.EnumPopup("Sense type", sensorObj.sensor.Sense);
            sensorObj.sensor.CoolDownSeconds = EditorGUILayout.FloatField("Full Cooldown in seconds", sensorObj.sensor.CoolDownSeconds);
            AddCallbackGUI(sensorObj);
            AddCustomDistanceGUI(sensorObj);
            AddLineOfSightGUI(sensorObj);

            EditorGUILayout.Space();

            if (sensorObj.sensor.Sense == SenseType.Vision)
            {
                
                bool val = EditorGUILayout.Toggle("Preview View Cones", sensorObj.sensor.DrawCones);
                if (val != sensorObj.sensor.DrawCones)
                {
                    Undo.RecordObject(sensorObj, "Preview cones");
                    sensorObj.sensor.DrawCones = val;
                    MarkSceneDirty();
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Add new View Cone"))
                {
                    Undo.RecordObject(sensorObj, "Add new View Cone");
                    sensorObj.sensor.AddViewCone();
                    MarkSceneDirty();
                    SceneView.RepaintAll();
                }


                if (sensorObj.sensor.ViewCones != null)
                {
                    for (int i = 0; i < sensorObj.sensor.ViewCones.Count; i++)
                    {
                        EditorGUILayout.Space();
                        GUILayout.Box("View Cone " + (i + 1), EditorStyles.helpBox);

                        int value = EditorGUILayout.IntSlider("FoV (degrees)", sensorObj.sensor.ViewCones[i].FoVAngle, 0, 180);
                        if (value != sensorObj.sensor.ViewCones[i].FoVAngle)
                        {
                            Undo.RecordObject(sensorObj, "Change View Cone");
                            MarkSceneDirty();
                            sensorObj.sensor.ViewCones[i].FoVAngle = value;
                            SceneView.RepaintAll();
                        }

                        value = EditorGUILayout.IntSlider("Range of sight", (int)sensorObj.sensor.ViewCones[i].Range, 0, 15);
                        if (value != sensorObj.sensor.ViewCones[i].Range)
                        {
                            Undo.RecordObject(sensorObj, "Change View Cone");
                            MarkSceneDirty();
                            sensorObj.sensor.ViewCones[i].Range = value;
                            SceneView.RepaintAll();
                        }

                        Awareness aw = (Awareness)EditorGUILayout.EnumPopup("Awareness Level", sensorObj.sensor.ViewCones[i].AwarenessLevel);
                        if (aw != sensorObj.sensor.ViewCones[i].AwarenessLevel)
                        {
                            Undo.RecordObject(sensorObj, "Change View Cone");
                            MarkSceneDirty();
                            sensorObj.sensor.ViewCones[i].AwarenessLevel = aw;
                            SceneView.RepaintAll();
                        }

                        value = EditorGUILayout.IntSlider("Horizontal Offset", sensorObj.sensor.ViewCones[i].HorizontalOffset, 0, 360);
                        if (value != sensorObj.sensor.ViewCones[i].HorizontalOffset)
                        {
                            Undo.RecordObject(sensorObj, "Change View Cone");
                            MarkSceneDirty();
                            sensorObj.sensor.ViewCones[i].HorizontalOffset = value;
                            SceneView.RepaintAll();
                        }

                        value = EditorGUILayout.IntSlider("Recognition delay", sensorObj.sensor.ViewCones[i].RecognitionDelayFrames, 0, 60);
                        if (value != sensorObj.sensor.ViewCones[i].RecognitionDelayFrames)
                        {
                            Undo.RecordObject(sensorObj, "Change Recognition delay");
                            MarkSceneDirty();
                            sensorObj.sensor.ViewCones[i].RecognitionDelayFrames = value;
                            SceneView.RepaintAll();
                        }

                        Color col = EditorGUILayout.ColorField("Scene color", sensorObj.sensor.ViewCones[i].SceneColor);
                        if (col != sensorObj.sensor.ViewCones[i].SceneColor)
                        {
                            Undo.RecordObject(sensorObj, "Change View Cone");
                            MarkSceneDirty();
                            sensorObj.sensor.ViewCones[i].SceneColor = col;
                            SceneView.RepaintAll();
                        }

                        bool drawCone = EditorGUILayout.Toggle("Draw", sensorObj.sensor.ViewCones[i].DrawCone);
                        if (drawCone != sensorObj.sensor.ViewCones[i].DrawCone)
                        {
                            Undo.RecordObject(sensorObj, (drawCone ? "Enable" : "Disable") + " Draw Cone");
                            sensorObj.sensor.ViewCones[i].DrawCone = drawCone;
                            MarkSceneDirty();
                            SceneView.RepaintAll();
                        }


                        if (GUILayout.Button("Remove"))
                        {
                            Undo.RecordObject(sensorObj, "Remove View Cone");
                            sensorObj.sensor.RemoveViewCone(i);
                            MarkSceneDirty();
                            SceneView.RepaintAll();
                        }
                    }
                }
            }

        }

        private void AddCallbackGUI(SensorObject sensorObj)
        {
            methods = new List<MethodInfo>();
            sensorObj.resolveCallbackMethods(methods, typeof(SenseLink));

            if (sensorObj.sensor != null)
            {
                int index = 0;
                sensorObj.ResolveCallbacks(); //otherwise the next method info won't be resolved
                MethodInfo methodInfo = sensorObj.sensor.delegateSignalDetected != null ? sensorObj.sensor.delegateSignalDetected.Method : null;

                if (methodInfo != null && methods != null)
                {
                    try
                    { // resolve the index of the already selected method, if any
                        index = methods
                            .Select((v, i) => new { Method = v, Index = i })
                            .OrderBy(m => (m.Method == methodInfo && m.Method.DeclaringType == methodInfo.DeclaringType) ? 0 : 1)
                            .First().Index;
                    }
                    catch
                    {
                        //fallback, use the first
                        index = 0;
                    }
                }

                string[] validMethods = methods.Select(x => x.DeclaringType.Name + "." + x.Name).ToArray();
                int val = EditorGUILayout.Popup("Signal handler", index, validMethods);

                if (methods.Count > val && val >= 0)
                {
                    if (val != index)
                    {
                        Undo.RecordObject(sensorObj, "Set Callback");
                        MarkSceneDirty();
                        methodInfo = methods[val];
                    }

                    if (methods[val] != null)
                    {
                        sensorObj.signalDetectionHandlerMethod = methods[val].Name;
                        sensorObj.signalDetectionMonobehaviorHandler = methods[val].DeclaringType.Name;
                        sensorObj.ResolveCallbacks();
                    }
                }
            }
        }

        private void AddCustomDistanceGUI(SensorObject sensorObj)
        {
            methods = new List<MethodInfo>();
            sensorObj.resolveCallbackMethods(methods, typeof(Signal), typeof(Vector3));

            if (sensorObj.sensor != null)
            {
                int index = 0;
                sensorObj.ResolveCallbacks(); //otherwise the next method info won't be resolved
                MethodInfo methodInfo = sensorObj.sensor.delegateDistanceCalculation != null ? sensorObj.sensor.delegateDistanceCalculation.Method : null;

                if (methodInfo != null && methods != null)
                { 
                    try
                    { // resolve the index of the already selected method, if any
                        index = methods
                            .Select((v, i) => new { Method = v, Index = i })
                            .OrderBy(m => (m.Method == methodInfo && m.Method.DeclaringType == methodInfo.DeclaringType) ? 0 : 1)
                            .First().Index;
                    }
                    catch
                    {
                        //fallback, use the first
                        index = 0;
                    }
                }
                
                string[] validMethods = methods.Select(x => x.DeclaringType.Name + "." + x.Name).ToArray();
                int val = EditorGUILayout.Popup("Distance callback", index, validMethods);

                if (methods.Count > val && val >= 0)
                {
                    if (methods[val] != methodInfo)
                    {
                        Undo.RecordObject(sensorObj, "Set Callback");
                        MarkSceneDirty();
                        methodInfo = methods[val];
                    }

                    if (methods[val] != null)
                    {
                        sensorObj.customDistanceHandlerMethod = methods[val].Name;
                        sensorObj.customDistanceMonobehaviorHandler = methods[val].DeclaringType.Name;
                        sensorObj.ResolveCallbacks();
                    }
                }
            }
        }

        private void AddLineOfSightGUI(SensorObject sensorObj)
        {
            methods = new List<MethodInfo>();
            sensorObj.resolveCallbackMethods(methods, typeof(Signal), typeof(bool));

            if (sensorObj.sensor != null)
            {
                int index = 0;
                sensorObj.ResolveCallbacks(); //otherwise the next method info won't be resolved
                MethodInfo methodInfo = sensorObj.sensor.delegateLineOfSight != null ? sensorObj.sensor.delegateLineOfSight.Method : null;

                if (methodInfo != null && methods != null)
                {
                    try
                    { // resolve the index of the already selected method, if any
                        index = methods
                            .Select((v, i) => new { Method = v, Index = i })
                            .OrderBy(m => (m.Method == methodInfo && m.Method.DeclaringType == methodInfo.DeclaringType) ? 0 : 1)
                            .First().Index;
                    }
                    catch
                    {
                        //fallback, use the first
                        index = 0;
                    }
                }
                
                string[] validMethods = methods.Select(x => x.DeclaringType.Name + "." + x.Name).ToArray();
                int val = EditorGUILayout.Popup("Line Of Sight Calculation", index, validMethods);

                if (methods.Count > val && val >= 0)
                {
                    if (methods[val] != methodInfo)
                    { 
                        Undo.RecordObject(sensorObj, "Set Callback");
                        MarkSceneDirty();
                        methodInfo = methods[val];
                    }

                    if (methods[val] != null)
                    {
                        sensorObj.customLineOfSightHandlerMethod = methods[val].Name;
                        sensorObj.customLineOfSightMonobehaviorHandler = methods[val].DeclaringType.Name;
                        sensorObj.ResolveCallbacks();
                    }
                }
            }
        }

        void OnSceneGUI()
        {
            SensorObject sensorObj = (SensorObject)target;

            if (sensorObj == null || !sensorObj.enabled)
                return;

            if (sensorObj.sensor == null)
                sensorObj.sensor = new Sensor();

            if (!sensorObj.sensor.DrawCones)
                return;

            if (sensorObj.sensor.ViewCones == null)
                return;

            for (int i = 0; i < sensorObj.sensor.ViewCones.Count; i++)
            {
                ViewCone vc = sensorObj.sensor.ViewCones[i];
                if (vc.DrawCone)
                {
                    Handles.color = vc.SceneColor;
                    Handles.DrawSolidArc(sensorObj.transform.position, sensorObj.transform.up,
                            Quaternion.Euler(0, -vc.FoVAngle * 0.5f + vc.HorizontalOffset, 0) * sensorObj.transform.forward,
                            vc.FoVAngle, vc.Range);
                }
            }
        }

        private void MarkSceneDirty()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

    }
}