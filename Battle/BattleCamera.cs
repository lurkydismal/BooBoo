using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace BooBoo.Battle
{
    internal class BattleCamera
    {
        public static BattleCamera activeCamera { get; private set; }

        public Vector3 position = new Vector3(0.0f, 2.5f, 8.0f);
        public Vector3 target = new Vector3(0.0f, 2.5f, 0.0f);
        public float fov = 45.0f;
        public float zoom = 1.0f;
        public const float camMoveSpeed = 0.125f;

        public float autoZoom = 0.0f;
        public bool autoZoomReturning = false;
        public float autoZoomReturnStart;
        public int autoZoomTime = 0;
        public const int autoZoomHold = 90;

        public BattleActor player1, player2;
        public BattleActor targetActor;

        public CameraMode camMode { get; private set; } = CameraMode.Default;

        public BattleCamera()
        {
            activeCamera = this;
        }

        public void Update()
        {
            switch(camMode)
            {
                default:
                case CameraMode.Default:
                    Vector3 desiredPos = (player1.position + player2.position) / 2.0f;
                    desiredPos.Y = desiredPos.Y >= 1.0f ? desiredPos.Y + 1.5f : 2.5f;
                    desiredPos.Z = 0.0f;
                    float autoZoomAmount = (MathF.Abs(player1.GetDistanceFrom(player2).X) - 8.0f) / 1.5f;
                    if (autoZoomAmount < 0.0f)
                        autoZoomAmount = 0.0f;
                    if (autoZoomAmount >= autoZoom)
                    {
                        autoZoom = autoZoomAmount;
                        autoZoomReturning = false;
                        autoZoomTime = 0;
                    }
                    else if (autoZoomAmount < autoZoom)
                    {
                        if (!autoZoomReturning)
                        {
                            autoZoomTime++;
                            if (autoZoomTime >= autoZoomHold)
                            {
                                autoZoomReturning = true;
                                autoZoomReturnStart = autoZoom;
                            }
                        }
                        else
                        {
                            autoZoomTime--;
                            //i fucking hate visual studio this is the second time ive had to cast to floats and visual studio says it does nothing
                            autoZoom = autoZoomAmount + (autoZoomReturnStart - autoZoomAmount) * ((float)autoZoomTime / (float)autoZoomHold);
                            if (autoZoomTime <= 0)
                                autoZoomReturning = false;
                        }
                    }

                    if (MathF.Abs(desiredPos.X) >= BattleStage.stage.stageWidth - 6.0f)
                        desiredPos.X = (BattleStage.stage.stageWidth - 6.0f) * MathF.Sign(desiredPos.X);

                    Vector3 final = Vector3.Lerp(position, desiredPos, camMoveSpeed);
                    target = final;
                    final.Z = 8.0f + autoZoom;
                    position = final;
                    break;
            }
        }

        public void BlendToTarget(BattleActor target, int framesToBlend)
        {
            targetActor = target;
            camMode = CameraMode.TransToTarget;
        }

        public enum CameraMode
        {
            TransToDefault,
            Default,
            TransToTarget,
            Target,
            TargetDelay,
            TransToAnim,
            CamAnim,
            CamAnimTarget,
        }

        public static implicit operator Camera3D(BattleCamera camera) => new Camera3D(camera.position, camera.target, 
            Vector3.UnitY, camera.fov, CameraProjection.Perspective);
    }
}
