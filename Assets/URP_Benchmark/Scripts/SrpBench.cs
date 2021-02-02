using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace HamCorGames.Benchmark
{
    public class SrpBench : MonoBehaviour
    {
        [Header("                                      ")]
        [Header("Watch 'CPU Rendering Time', compare it with other settings")]
        [Header("F8 to Show/Hide Stats Window")]
        [Header("Enable/Disable SRP Batcher from the RenderPipelineAsset")]
        [Header("Turn VSync OFF from the Game Window Resolution Settings")]
        [Header("Set the Resolution to a fixed value, like 1080p")]
        
        [SerializeField] private bool benchEnabled = true;
        private const float refreshRate = 1.0f;
        private int frameCount;
		private float accDeltaTime;
        private string statsLabel;
        private GUIStyle guiStyle;

        internal class RecorderEntry
        {
            public string name;
            public string oldName;
            public int callCount;
            public float accTime;
            public Recorder recorder;
        };

		enum SRPBMarkers
		{
			kStdRenderDraw,
			kStdShadowDraw,
			kSRPBRenderDraw,
			kSRPBShadowDraw,
			kRenderThreadIdle,
			kStdRenderApplyShader,
			kStdShadowApplyShader,
			kSRPBRenderApplyShader,
			kSRPBShadowApplyShader,
			kPrepareBatchRendererGroupNodes,
		};
		
        private RecorderEntry[] recordersList =
        {
			// Warning: Keep that list in the exact same order than SRPBMarkers enum
            new RecorderEntry() { name="RenderLoop.Draw" },
            new RecorderEntry() { name="Shadows.Draw" },
            new RecorderEntry() { name="SRPBatcher.Draw", oldName="RenderLoopNewBatcher.Draw" },
            new RecorderEntry() { name="SRPBatcherShadow.Draw", oldName="ShadowLoopNewBatcher.Draw" },
            new RecorderEntry() { name="RenderLoopDevice.Idle" },
            new RecorderEntry() { name="StdRender.ApplyShader" },
            new RecorderEntry() { name="StdShadow.ApplyShader" },
            new RecorderEntry() { name="SRPBRender.ApplyShader" },
            new RecorderEntry() { name="SRPBShadow.ApplyShader" },
            new RecorderEntry() { name="PrepareBatchRendererGroupNodes" },
        };

        private void Awake()
        {
            for (int i = 0; i < recordersList.Length; i++)
            {
                var sampler = Sampler.Get(recordersList[i].name);

                if (sampler.isValid)
                {
                    recordersList[i].recorder = sampler.GetRecorder();
                }
				else if ( recordersList[i].oldName != null )
				{
					sampler = Sampler.Get(recordersList[i].oldName);

					if (sampler.isValid)
						recordersList[i].recorder = sampler.GetRecorder();
				}
            }

            guiStyle =new GUIStyle();
            guiStyle.fontSize = 30;
            guiStyle.normal.textColor = Color.green;

            ResetStats();
        }

        private void RazCounters()
        {
            accDeltaTime = 0.0f;
            frameCount = 0;
            for (int i = 0; i < recordersList.Length; i++)
            {
                recordersList[i].accTime = 0.0f;
                recordersList[i].callCount = 0;
            }
        }

        private void ResetStats()
        {
			statsLabel = "Gathering data...";
            RazCounters();
        }
        
        private void ToggleStats()
        {
            benchEnabled = !benchEnabled;
            ResetStats();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                ToggleStats();
            }

			if (benchEnabled)
			{
				accDeltaTime += Time.unscaledDeltaTime;
				frameCount++;

				// get timing & update average accumulators
				for (int i = 0; i < recordersList.Length; i++)
				{
					if ( recordersList[i].recorder != null )
					{
						recordersList[i].accTime += recordersList[i].recorder.elapsedNanoseconds / 1000000.0f;      // acc time in ms
						recordersList[i].callCount += recordersList[i].recorder.sampleBlockCount;
					}
				}

				if (accDeltaTime >= refreshRate)
				{
					float ooFrameCount = 1.0f / (float)frameCount;

					float avgStdRender = recordersList[(int)SRPBMarkers.kStdRenderDraw].accTime * ooFrameCount;
					float avgStdShadow = recordersList[(int)SRPBMarkers.kStdShadowDraw].accTime * ooFrameCount;
					float avgSRPBRender = recordersList[(int)SRPBMarkers.kSRPBRenderDraw].accTime * ooFrameCount;
					float avgSRPBShadow = recordersList[(int)SRPBMarkers.kSRPBShadowDraw].accTime * ooFrameCount;
					float RTIdleTime = recordersList[(int)SRPBMarkers.kRenderThreadIdle].accTime * ooFrameCount;
					float avgPIRPrepareGroupNodes = recordersList[(int)SRPBMarkers.kPrepareBatchRendererGroupNodes].accTime * ooFrameCount;

					statsLabel = string.Format("Accumulated time for RenderLoop.Draw and ShadowLoop.Draw (all threads)\n{0:F2}ms CPU Rendering time ( incl {1:F2}ms RT idle )\n", avgStdRender + avgStdShadow + avgSRPBRender + avgSRPBShadow + avgPIRPrepareGroupNodes, RTIdleTime);
					
                    statsLabel += string.Format("  {0:F2}ms SRP Batcher code path\n", avgSRPBRender + avgSRPBShadow);
					statsLabel += string.Format("    {0:F2}ms All objects ( {1} ApplyShader calls )\n", avgSRPBRender, recordersList[(int)SRPBMarkers.kSRPBRenderApplyShader].callCount / frameCount);
					statsLabel += string.Format("    {0:F2}ms Shadows ( {1} ApplyShader calls )\n", avgSRPBShadow, recordersList[(int)SRPBMarkers.kSRPBShadowApplyShader].callCount / frameCount);
					
					statsLabel += string.Format("  {0:F2}ms Standard code path\n", avgStdRender + avgStdShadow);
					statsLabel += string.Format("    {0:F2}ms All objects ( {1} ApplyShader calls )\n", avgStdRender, recordersList[(int)SRPBMarkers.kStdRenderApplyShader].callCount / frameCount);
					statsLabel += string.Format("    {0:F2}ms Shadows ( {1} ApplyShader calls )\n", avgStdShadow, recordersList[(int)SRPBMarkers.kStdShadowApplyShader].callCount / frameCount);
					statsLabel += string.Format("  {0:F2}ms PIR Prepare Group Nodes ( {1} calls )\n", avgPIRPrepareGroupNodes, recordersList[(int)SRPBMarkers.kPrepareBatchRendererGroupNodes].callCount / frameCount);
					statsLabel += string.Format("Global Main Loop: {0:F2}ms ({1} FPS)\n", accDeltaTime * 1000.0f * ooFrameCount, (int)(((float)frameCount) / accDeltaTime));

					RazCounters();
				}
			}
        }

        private void OnGUI()
        {
            if (benchEnabled)
            {
                GUI.color = new Color(1, 1, 1, 1);

                float width = 1050;
                float height = 380;

                GUILayout.BeginArea(new Rect(32, 32, width, height), "SRP Benchmark", GUI.skin.window);

                GUILayout.Label(statsLabel, guiStyle);

                GUILayout.EndArea();
            }
        }
    }
}



    



