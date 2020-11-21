using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using System.Collections.Generic;
using UnityEngine.UI;

public class Boid_Spawner : MonoBehaviour
{
    public int boidsPerInterval;
    public int boidsToSpawn;
    public float interval;
    public float cohesionBias;
    public float separationBias;
    public float alignmentBias;
    public float targetBias;
    public float perceptionRadius;
    public float3 target;
    public Material material;
    public Material material2;
    public List<Entity> debugEntities;
    public Mesh mesh;
    public float maxSpeed;
    public float step;
    public int cellSize;
    public float fieldOfView;
    public int maxPercived;
    private EntityManager entitymanager;
    private Entity entity;
    private float elapsedTime;
    private int totalSpawnedBoids;
    private EntityArchetype ea;
    private float3 currentPosition;
    private BlobAssetReference<BoidManagerBLOB> boidManagerReference;
    private float deltaTime = 0.0f;

    private void Start()
    {
        //boidsToSpawn = 5000;
        GameObject.Find("Spawn").GetComponentInChildren<Text>().text = "Spawn";
        totalSpawnedBoids = 0;
        entitymanager = World.DefaultGameObjectInjectionWorld.EntityManager;
        currentPosition = this.transform.position;
        ea = entitymanager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(LocalToWorld),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(Boid_ComponentData)
            );
        debugEntities = new List<Entity>();

        BlobBuilder bb = new BlobBuilder(Unity.Collections.Allocator.Temp);
        boidManagerReference = new BlobAssetReference<BoidManagerBLOB>();
        ref BoidManagerBLOB bmb = ref bb.ConstructRoot<BoidManagerBLOB>();
        BlobBuilderArray<Boid_Manager> blobManagerArray = bb.Allocate(ref bmb.blobManagerArray, 9);
        blobManagerArray[0] = new Boid_Manager
        {
            cohesionBias = cohesionBias,
            separationBias = separationBias,
            alignmentBias = alignmentBias,
            targetBias = targetBias,
            perceptionRadius = perceptionRadius,
            step = step,
            cellSize = cellSize,
            fieldOfView = fieldOfView,
            maxPercived = maxPercived
        };
        boidManagerReference = bb.CreateBlobAssetReference<BoidManagerBLOB>(Unity.Collections.Allocator.Persistent);
        bb.Dispose();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        if (elapsedTime >= interval)
        {
            elapsedTime = 0;
            for (int i=0; i<=boidsPerInterval; i++)
            {
                if (totalSpawnedBoids == boidsToSpawn)
                {
                    break;
                }
                Entity e = entitymanager.CreateEntity(ea);

                entitymanager.AddComponentData(e, new Translation
                {
                    Value = currentPosition
                });
                entitymanager.AddComponentData(e, new Boid_ComponentData
                {
                    velocity = math.normalize(UnityEngine.Random.insideUnitSphere) * maxSpeed,
                    //perceptionRadius = perceptionRadius,
                    speed = maxSpeed,
                    //step = step,
                    //cohesionBias = cohesionBias,
                    //separationBias = separationBias,
                    //alignmentBias = alignmentBias,
                    target = target,
                    //targetBias = targetBias,
                    //cellSize = cellSize,
                    //fieldOfView = fieldOfView,
                    //maxPercived =maxPercived,
                    boidManagerReference = boidManagerReference
                });
                entitymanager.AddSharedComponentData(e, new RenderMesh
                {
                    mesh = mesh,
                    material = material,
                    castShadows = UnityEngine.Rendering.ShadowCastingMode.Off
                });
                totalSpawnedBoids++;
                //debugEntities.Add(e);
            }
        }

        //Debug
        /*foreach (Entity e in debugEntities)
        {
            RenderMesh r = entitymanager.GetSharedComponentData<RenderMesh>(e);
            Boid_ComponentData bc = entitymanager.GetComponentData<Boid_ComponentData>(e);
            if (bc.debug >0)
            {
                r.material = material2;
            }
            else
            {
                r.material = material;
            }
            entitymanager.SetSharedComponentData<RenderMesh>(e, r);
        }*/
    }

    private void OnGUI()
    {
        //float msec = deltaTime * 1000.0f;
        //float fps = 1.0f / deltaTime;
        //string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        //GUI.Box(new Rect(20, 20, 500, 40), fps + " fps");
        GUI.Box(new Rect(20, 20, 500, 40), "Spawned :" + totalSpawnedBoids);

        GUI.skin.box.fontSize = 25;
    }

    public void onButtonClick()
    {
        boidsToSpawn = boidsToSpawn + 5000;
    }
    private void OnDestroy()
    {
        boidManagerReference.Dispose();
    }
}
