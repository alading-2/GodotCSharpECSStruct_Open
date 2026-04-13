using Godot;
using System;

namespace Slime.Test
{
    public partial class TestEntity : Node, IEntity, IPoolable
    {
        private static readonly Log _log = new Log("TestEntity");
        /// <summary>
        /// 实体局部事件总线
        /// </summary>
        public EventBus Events { get; } = new EventBus();
        // IEntity Implementation
        public Data Data { get; private set; } = new Data();
        // EntityId 由 IEntity 默认实现（从 DataKey.Id 读取）

        public override void _Ready()
        {
            _log.Debug("TestEntity Ready");
        }

        public override void _ExitTree()
        {
            Data.Clear();

            // 仅在已注册时才注销，避免未注册实体的警告
            // 对象池初始化时创建的实体不会被注册，因此不需要注销
            var id = Data.Get<string>(DataKey.Id);
            if (!string.IsNullOrEmpty(id) && EntityManager.GetEntityById(id) != null)
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
