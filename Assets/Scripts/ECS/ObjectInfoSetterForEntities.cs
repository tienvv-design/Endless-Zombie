using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ObjectInfoSetterForEntities : MonoBehaviour
{
    [SerializeField] private List<GameObject> _targetGameObjects;
    private List<Targetable> _targetables = new();

    private World _world;
    private Entity _objectInfoEntity;

    private void Awake()
    {
        if (_targetGameObjects == null) return;
        foreach (var go in _targetGameObjects)
        {
            if (go != null && go.TryGetComponent<Targetable>(out var targetable))
            {
                _targetables.Add(targetable);
            }
        }
    }

    private void Start()
    {
        _world = World.DefaultGameObjectInjectionWorld;
        if (_world == null || !_world.IsCreated)
        {
            enabled = false;
            return;
        }

        EntityManager entityManager = _world.EntityManager;

        // This buffer is created from a MonoBehaviour rather than a SubScene, so it
        // must be cleaned explicitly between runs. Remove stale instances left by a
        // previous GameScene before creating the current bridge entity.
        EntityQuery staleBuffers = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<GameObjectInfo>());
        if (!staleBuffers.IsEmptyIgnoreFilter)
            entityManager.DestroyEntity(staleBuffers);
        staleBuffers.Dispose();

        _objectInfoEntity = entityManager.CreateEntity();
        DynamicBuffer<GameObjectInfo> buffer = entityManager.AddBuffer<GameObjectInfo>(_objectInfoEntity);

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

    private void OnDestroy()
    {
        if (_world != null && _world.IsCreated && _objectInfoEntity != Entity.Null)
        {
            EntityManager manager = _world.EntityManager;
            if (manager.Exists(_objectInfoEntity))
                manager.DestroyEntity(_objectInfoEntity);
        }
        _objectInfoEntity = Entity.Null;
        _world = null;
    }

    private void Update()
    {
        if (_world == null || !_world.IsCreated || _objectInfoEntity == Entity.Null)
            return;

        EntityManager manager = _world.EntityManager;
        if (!manager.Exists(_objectInfoEntity) || !manager.HasBuffer<GameObjectInfo>(_objectInfoEntity))
            return;

        DynamicBuffer<GameObjectInfo> mobTargetBuffer = manager.GetBuffer<GameObjectInfo>(_objectInfoEntity);

        for (int i = 0; i < mobTargetBuffer.Length; i++)
        {
            GameObjectInfo target = mobTargetBuffer[i];

            foreach (var targetable in _targetables)
            {
                if (targetable == null) continue;
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

