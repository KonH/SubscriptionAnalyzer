using System;

namespace SubscriptionAnalyzer.Samples {
	public sealed class TrackingSubscriptionAttribute : Attribute {}

	public static class EventManager {
		[TrackingSubscription]
		public static void Subscribe<T>(object target, Action<T> handler) {}

		[TrackingSubscription]
		public static void Unsubscribe<T>(Action<T> handler) {}
	}

	public static class OutOfScopeEventManager {
		public static void Subscribe<T>(object target, Action<T> handler) {}
		public static void Unsubscribe<T>(Action<T> handler) {}
	}

	public sealed class ValidUsage {
		public void Init() {
			EventManager.Subscribe<object>(this, Handle);
		}

		public void Deinit() {
			EventManager.Unsubscribe<object>(Handle);
		}

		void Handle(object _) {}
	}

	public sealed class OutOfScopeUsage {
		public void Init() {
			OutOfScopeEventManager.Subscribe<object>(this, Handle);
		}

		public void Deinit() {}

		void Handle(object _) {}
	}

	public sealed class InvalidUsage1 {
		public void Init() {
			EventManager.Subscribe<object>(this, Handle);
		}

		public void Deinit() {}

		void Handle(object _) {}
	}

	public sealed class InvalidUsage2 {
		public void Init() {
			EventManager.Subscribe<object>(this, Handle);
			EventManager.Subscribe<int>(this, Handle);
		}

		public void Deinit() {
			EventManager.Unsubscribe<object>(Handle);
		}

		void Handle(object _) {}

		void Handle(int _) {}
	}

	public sealed class InvalidUsage3 {
		public void Init() {
			EventManager.Subscribe<object>(this, Handle1);
		}

		public void Deinit() {
			EventManager.Unsubscribe<object>(Handle2);
		}

		void Handle1(object _) {}

		void Handle2(object _) {}
	}
}