﻿using BepuUtilities;
using DemoRenderer;
using BepuPhysics;
using BepuPhysics.Collidables;
using System.Numerics;
using Quaternion = BepuUtilities.Quaternion;
using System;
using BepuPhysics.CollisionDetection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using BepuPhysics.Constraints;
using DemoContentLoader;
using DemoUtilities;
using BepuUtilities.Memory;
using static BepuUtilities.GatherScatter;
using Demos.Demos.Character;

namespace Demos.Demos
{
    struct CharacterNarrowphaseCallbacks : INarrowPhaseCallbacks
    {
        public CharacterControllers Characters;

        public CharacterNarrowphaseCallbacks(CharacterControllers characters)
        {
            Characters = characters;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void GetMaterial(out PairMaterialProperties pairMaterial)
        {
            pairMaterial = new PairMaterialProperties { FrictionCoefficient = 1, MaximumRecoveryVelocity = 2, SpringSettings = new SpringSettings(30, 1) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, ConvexContactManifold* manifold, out PairMaterialProperties pairMaterial)
        {
            GetMaterial(out pairMaterial);
            Characters.TryReportContacts(pair, ref *manifold, workerIndex, ref pairMaterial);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, NonconvexContactManifold* manifold, out PairMaterialProperties pairMaterial)
        {
            GetMaterial(out pairMaterial);
            Characters.TryReportContacts(pair, ref *manifold, workerIndex, ref pairMaterial);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ConvexContactManifold* manifold)
        {
            return true;
        }

        public void Dispose()
        {
            Characters.Dispose();
        }

        public void Initialize(Simulation simulation)
        {
            Characters.Initialize(simulation);
        }
    }

    /// <summary>
    /// Shows how to use a simple character controller.
    /// </summary>
    public class CharacterDemo : Demo
    {
        public unsafe override void Initialize(ContentArchive content, Camera camera)
        {
            camera.Position = new Vector3(-20, 10, -20);
            camera.Yaw = MathHelper.Pi * 3f / 4;
            camera.Pitch = MathHelper.Pi * 0.05f;
            var masks = new BodyProperty<ulong>();
            var characters = new CharacterControllers(BufferPool, ThreadDispatcher);
            var timestepper = new PositionFirstTimestepper();
            Simulation = Simulation.Create(BufferPool, new CharacterNarrowphaseCallbacks(characters), new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0)), timestepper);
            timestepper.BodiesUpdated += characters.PrepareForContacts;
            timestepper.CollisionsDetected += characters.AnalyzeContacts;
            Simulation.Solver.Register<DynamicCharacterMotionConstraint>();
            Simulation.Solver.Register<StaticCharacterMotionConstraint>();

            ref var character = ref characters.AllocateCharacter(
                Simulation.Bodies.Add(
                    BodyDescription.CreateDynamic(
                        new Vector3(0, 2, 0), new BodyInertia { InverseMass = 1 },
                        new CollidableDescription(Simulation.Shapes.Add(new Capsule(0.5f, 1f)), 0.1f),
                        new BodyActivityDescription(-1))),
                out var characterIndex);

            character.CosMaximumSlope = .707f;
            character.LocalUp = Vector3.UnitY;
            character.MaximumHorizontalForce = 10;
            character.MaximumVerticalForce = 10000;
            character.MinimumSupportContinuationDepth = -0.1f;
            character.MinimumSupportDepth = -0.01f;
            character.TargetVelocity = new Vector2(0, 1f);
            character.ViewDirection = new Vector3(0, 0, -1);

            Simulation.Statics.Add(new StaticDescription(
                new Vector3(0, -0.5f, 0), Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1, 0, 1)), MathF.PI * 0.03f), new CollidableDescription(Simulation.Shapes.Add(new Box(300, 1, 300)), 0.1f)));
        }

    }
}

