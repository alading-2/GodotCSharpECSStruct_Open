using Godot;
using System;

namespace Slime.Test
{
    public partial class TestComponent : Node, IComponent
    {
        private static readonly Log _log = new Log("TestComponent");

        private Data? _data;
        private IEntity? _entity;

        public bool IsRegistered { get; private set; } = false;

        public Data? GetData() => _data;
        public IEntity? GetEntity() => _entity;

        public void OnComponentRegistered(Node entity)
        {
            IsRegistered = true;
            if (entity is IEntity iEntity)
            {
                _data = iEntity.Data;
                _entity = iEntity;
                _log.Debug("Component registered to Entity");
            }
        }

        public void OnComponentUnregistered()
        {
            IsRegistered = false;
            _data = null;
            _entity = null;
            _log.Debug("Component unregistered");
        }

        public override void _Ready()
        {
            // Usually components don't log in Ready unless debug
        }
    }
}
