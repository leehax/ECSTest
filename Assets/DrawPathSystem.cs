using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;


public class DrawPathSystem:SystemBase
{
    protected override void OnUpdate()
    {
         Entities.ForEach((Entity entity, in AgentTag agentTag) =>
         {
             var drawPathJob = new DrawPathJob
             {
                 Entity = entity,
                 PathPositionFromEntity = GetBufferFromEntity<PathPosition>(true),
             };
         }).Run();
    }


    private struct DrawPathJob : IJob
    {
       [ReadOnly] public BufferFromEntity<PathPosition> PathPositionFromEntity;
       public Entity Entity;

       public void Execute()
       {
           Debug.Log("Draw Path");
           var buffer = PathPositionFromEntity[Entity];
           for (int i = 0; i < buffer.Length; i++)
           {
               Debug.DrawLine(new Vector3(buffer[i].Value.x, 0.5f,buffer[i].Value.y ),new Vector3(buffer[i].Value.x + 0.5f, .5f,buffer[i].Value.y ),Color.blue,10f);
           }
       }
    }
}