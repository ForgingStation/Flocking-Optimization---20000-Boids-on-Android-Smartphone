﻿using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

public class Boid_Movement : SystemBase
{
    public NativeMultiHashMap<int, Boid_ComponentData> cellVsEntityPositions;

    public static int GetUniqueKeyForPosition(float3 position, int cellSize)
    {
        return (int)((15 * math.floor(position.x / cellSize)) + (17 * math.floor(position.y / cellSize)) + (19 * math.floor(position.z / cellSize)));
    }

    protected override void OnCreate()
    {
        cellVsEntityPositions = new NativeMultiHashMap<int, Boid_ComponentData>(0, Allocator.Persistent);
    }

    protected override void OnUpdate()
    {
        EntityQuery eq = GetEntityQuery(typeof(Boid_ComponentData));
        cellVsEntityPositions.Clear();
        if (eq.CalculateEntityCount() > cellVsEntityPositions.Capacity)
        {
            cellVsEntityPositions.Capacity = eq.CalculateEntityCount();
        }

        NativeMultiHashMap<int, Boid_ComponentData>.ParallelWriter cellVsEntityPositionsParallel = cellVsEntityPositions.AsParallelWriter();
        Entities.ForEach((ref Boid_ComponentData bc, ref Translation trans) =>
        {
            Boid_ComponentData bcValues = new Boid_ComponentData();
            bcValues = bc;
            bcValues.currentPosition = trans.Value;
            cellVsEntityPositionsParallel.Add(GetUniqueKeyForPosition(trans.Value, bc.boidManagerReference.Value.blobManagerArray[0].cellSize), bcValues);
        }).ScheduleParallel();

        float deltaTime = Time.DeltaTime;
        NativeMultiHashMap<int, Boid_ComponentData> cellVsEntityPositionsForJob = cellVsEntityPositions;
        Entities.WithBurst().WithReadOnly(cellVsEntityPositionsForJob).ForEach((ref Boid_ComponentData bc, ref Translation trans, ref Rotation rot) =>
        {
            int key = GetUniqueKeyForPosition(trans.Value, bc.boidManagerReference.Value.blobManagerArray[0].cellSize);
            NativeMultiHashMapIterator<int> nmhKeyIterator;
            Boid_ComponentData neighbour;
            int total = 0;
            float3 separation = float3.zero;
            float3 alignment = float3.zero;
            float3 coheshion = float3.zero;
            float angle;

            if (cellVsEntityPositionsForJob.TryGetFirstValue(key, out neighbour, out nmhKeyIterator))
            {
                do
                {
                    if (!trans.Value.Equals(neighbour.currentPosition) 
                    && math.distance(trans.Value, neighbour.currentPosition) < bc.boidManagerReference.Value.blobManagerArray[0].perceptionRadius)
                    {
                        angle = math.acos(math.dot(bc.velocity, (neighbour.currentPosition - trans.Value)) / (math.length(bc.velocity) * math.length(neighbour.currentPosition - trans.Value)));
                        if (math.abs(angle) <= bc.boidManagerReference.Value.blobManagerArray[0].fieldOfView)
                        {
                            if (total >= bc.boidManagerReference.Value.blobManagerArray[0].maxPercived)
                            {
                                break;
                            }
                            float3 distanceFromTo = trans.Value - neighbour.currentPosition;
                            separation += (distanceFromTo / math.distance(trans.Value, neighbour.currentPosition));
                            coheshion += neighbour.currentPosition;
                            alignment += neighbour.velocity;
                            total++;
                            bc.debug = angle;
                        }
                    }
                } while (cellVsEntityPositionsForJob.TryGetNextValue(out neighbour, ref nmhKeyIterator));
                if (total > 0)
                {
                    coheshion = coheshion / total;
                    coheshion = coheshion - (trans.Value + bc.velocity);
                    coheshion = math.normalize(coheshion) * bc.boidManagerReference.Value.blobManagerArray[0].cohesionBias;

                    separation = separation / total;
                    separation = separation - bc.velocity;
                    separation = math.normalize(separation) * bc.boidManagerReference.Value.blobManagerArray[0].separationBias;

                    alignment = alignment / total;
                    alignment = alignment - bc.velocity;
                    alignment = math.normalize(alignment) * bc.boidManagerReference.Value.blobManagerArray[0].alignmentBias;

                }

                bc.acceleration += (coheshion + alignment + separation);
                rot.Value = math.slerp(rot.Value, quaternion.LookRotation(math.normalize(bc.velocity), math.up()), deltaTime * 10);
                bc.velocity = bc.velocity + bc.acceleration;
                bc.velocity = math.normalize(bc.velocity) * bc.speed;
                trans.Value = math.lerp(trans.Value, (trans.Value + bc.velocity), deltaTime * bc.boidManagerReference.Value.blobManagerArray[0].step);
                bc.acceleration = math.normalize(bc.target - trans.Value) * bc.boidManagerReference.Value.blobManagerArray[0].targetBias;
            }
        }).ScheduleParallel();
    }

    protected override void OnDestroy()
    {
        cellVsEntityPositions.Dispose();
    }
}
