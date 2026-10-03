using System.Collections.Generic;
using NUnit.Framework;
using Townscape.State;

namespace Townscape.Tests.State
{
    public sealed class StoreTests
    {
        private static Store<TownState> CreateStore() =>
            new Store<TownState>(TownReducer.Reduce, TownState.CreateDefault(TimePreset.Day));

        [Test]
        public void Dispatch_UpdatesState()
        {
            var store = CreateStore();

            store.Dispatch(new SelectTimePreset(TimePreset.Night));

            Assert.That(store.State.TimeOfDay.Preset, Is.EqualTo(TimePreset.Night));
            Assert.That(store.State.TimeOfDay.TargetHour, Is.EqualTo(TimePresets.HourOf(TimePreset.Night)));
        }

        [Test]
        public void UnhandledAction_KeepsSameStateInstance()
        {
            var store = CreateStore();
            var before = store.State;

            store.Dispatch(new UnknownAction());

            Assert.That(store.State, Is.SameAs(before));
        }

        [Test]
        public void Subscribe_FiresImmediatelyWithCurrentSlice()
        {
            var store = CreateStore();
            var received = new List<TimeOfDayState>();

            store.Subscribe(s => s.TimeOfDay, received.Add);

            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(received[0], Is.EqualTo(store.State.TimeOfDay));
        }

        [Test]
        public void Subscribe_OnlyFiresWhenSelectedSliceChanges()
        {
            var store = CreateStore();
            var timeChanges = 0;
            var weatherChanges = 0;
            store.Subscribe(s => s.TimeOfDay, _ => timeChanges++);
            store.Subscribe(s => s.Weather, _ => weatherChanges++);

            store.Dispatch(new SetRainIntensity(0.1f));

            Assert.That(timeChanges, Is.EqualTo(1), "time slice did not change, so only the initial call");
            Assert.That(weatherChanges, Is.EqualTo(2));
        }

        [Test]
        public void Subscribe_DoesNotFireWhenValueIsEqual()
        {
            var store = CreateStore();
            var calls = 0;
            store.Subscribe(s => s.Weather.RainIntensity, _ => calls++);

            store.Dispatch(new SetRainIntensity(store.State.Weather.RainIntensity));

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_StopsNotifications()
        {
            var store = CreateStore();
            var calls = 0;
            var handle = store.Subscribe(s => s.Weather, _ => calls++);

            handle.Dispose();
            store.Dispatch(new SetWindStrength(0.9f));

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void DispatchFromSubscriber_IsQueuedAndProcessedInOrder()
        {
            var store = CreateStore();
            var processed = new List<IAction>();
            store.ActionProcessed += (action, _) => processed.Add(action);
            store.Subscribe(s => s.Weather.RainIntensity, rain =>
            {
                if (rain > 0.95f)
                {
                    store.Dispatch(new SetWindStrength(1f));
                }
            });

            var first = new SetRainIntensity(1f);
            var second = new SelectTimePreset(TimePreset.Dawn);
            store.Dispatch(first);
            store.Dispatch(second);

            Assert.That(processed, Has.Count.EqualTo(3));
            Assert.That(processed[0], Is.SameAs(first));
            Assert.That(processed[1], Is.InstanceOf<SetWindStrength>());
            Assert.That(processed[2], Is.SameAs(second));
            Assert.That(store.State.Weather.WindStrength, Is.EqualTo(1f));
        }

        [Test]
        public void UnsubscribingDuringNotification_IsSafe()
        {
            var store = CreateStore();
            System.IDisposable handle = null;
            var calls = 0;
            handle = store.Subscribe(s => s.Weather, _ =>
            {
                calls++;
                handle?.Dispose();
            });

            store.Dispatch(new SetWindStrength(0.2f));
            store.Dispatch(new SetWindStrength(0.3f));

            Assert.That(calls, Is.EqualTo(2), "initial call plus the first change only");
        }

        private sealed record UnknownAction : IAction;
    }
}
