using Godot;
using System;

namespace BrotatoMy.Test
{
    public partial class TestEntity : Node2D, IEntity, IPoolable
    {
        private static readonly Log _log = new Log("TestEntity");
        /// <summary>
        /// 实体局部事件总线
        /// </summary>
        public EventBus Events { get; } = new EventBus();
        // IEntity Implementation
        public Data Data { get; private set; } = new Data();
        public string EntityId { get; private set; } = string.Empty;

        public override void _Ready()
        {
            EntityId = GetInstanceId().ToString();
            _log.Debug("TestEntity Ready");
        }

        public override void _ExitTree()
        {
            Data.Clear();

            // 仅在已注册时才注销，避免未注册实体的警告
            // 对象池初始化时创建的实体不会被注册，因此不需要注销
            if (EntityManager.GetEntityById(EntityId) != null)
            {
                EntityManager.UnregisterEntity(this);
            }

            base._ExitTree();
        }

        // IPoolable Implementation
        public void OnPoolAcquire()
        {
            _log.Debug("Acquired from pool");
        }

        public void OnPoolRelease()
        {
            _log.Debug("Released to pool");
            Data.Clear();
        }

        public void OnPoolReset()
        {
            // Optional reset logic
        }
    }
}
