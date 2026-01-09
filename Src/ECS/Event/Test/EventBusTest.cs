using Godot;
using System;

namespace BrotatoMy.Test
{
    public partial class EventBusTest : Node
    {
        public override void _Ready()
        {
            var bus = new EventBus();
            int callCount = 0;

            // Test On
            bus.On<int>("TestEvent", (val) =>
            {
                GD.Print($"Received: {val}");
                callCount++;
            });

            // Test Emit
            bus.Emit("TestEvent", 100);

            // Test Once
            bus.Once("OnceEvent", () =>
            {
                GD.Print("Once triggered");
                callCount++;
            });

            bus.Emit("OnceEvent");
            bus.Emit("OnceEvent"); // Should not trigger

            // Test Priority
            bus.On("Order", () => GD.Print("Low Priority"), 0);
            bus.On("Order", () => GD.Print("High Priority"), 10);
            bus.Emit("Order");

            GD.Print($"Total Call Count: {callCount} (Expected 2)");
        }
    }
}
