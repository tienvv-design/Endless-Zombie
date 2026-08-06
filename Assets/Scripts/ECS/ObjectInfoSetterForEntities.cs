using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ObjectInfoSetterForEntities : MonoBehaviour
{
    [SerializeField] private List<GameObject> _targetGameObjects;
    private List<Targetable> _targetables = new();

    private EntityManager _entityManager;
    private GameObjectInfo _gameObjectInfo;
    private Entity _objectInfoEntity;

    private void Awake()
    {
        foreach (var go in _targetGameObjects)
        {
            if (go.TryGetComponent<Targetable>(out var targetable))
            {
                _targetables.Add(targetable);
            }
        }
    }

    private void Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        _objectInfoEntity = _entityManager.CreateEntity();
        DynamicBuffer<GameObjectInfo> buffer = _entityManager.AddBuffer<GameObjectInfo>(_objectInfoEntity);

        foreach (var targetable in _targetables)
        {
            buffer.Add(new GameObjectInfo
            {
                ID = targetable.ID,
                ObjectType = targetable.GameObjectType,
                Position = float3.zero,
            });
        }
    }

    private void Update()
    {
        DynamicBuffer<GameObjectInfo> mobTargetBuffer = _entityManager.GetBuffer<GameObjectInfo>(_objectInfoEntity);

        for (int i = 0; i < mobTargetBuffer.Length; i++)
        {
            GameObjectInfo target = mobTargetBuffer[i];

            foreach (var targetable in _targetables)
            {
                if (target.ObjectType == targetable.GameObjectType)
                {
                    target.Position = targetable.transform.position;
                    mobTargetBuffer[i] = target;
                    break;
                }
            }
        }
        
    }
    
}

