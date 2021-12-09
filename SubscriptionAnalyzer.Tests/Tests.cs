using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using AnalyzerVerifier = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<SubscriptionAnalyzer.Analyzer.SubscriptionAnalyzer>;

namespace SubscriptionAnalyzer.Tests {
	public sealed class Tests {
		string ExpectedValueId => Analyzer.SubscriptionAnalyzer.MissingUnsubscribeCallDiagnostic.Id;

		string ExpectedValueMessage => Analyzer.SubscriptionAnalyzer.MissingUnsubscribeCallDiagnostic.MessageFormat.ToString();

		[Test]
		[NonParallelizable]
		public async Task IsWarningFoundOnNoUnsubscribeCall() {
			var code = @"
using System;

public sealed class TrackingSubscriptionAttribute : Attribute {}

public static class EventManager {
	[TrackingSubscription]
	public static void Subscribe<T>(object target, Action<T> handler) {}
	
	[TrackingSubscription]
	public static void Unsubscribe<T>(Action<T> handler) {}
}

public sealed class Sample {
	public void Init() {
		// Warning required
		EventManager.Subscribe<object>(this, Handle);
	}

	public void Deinit() {}
	
	void Handle(object _) {}
}";
			var expected = new[] {
				DiagnosticResult.CompilerWarning(ExpectedValueId).WithLocation(17, 3).WithMessage(ExpectedValueMessage),
			};
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, expected);
		}



		[Test]
		[NonParallelizable]
		public async Task IsWarningFoundOnNotEnoughUnsubscribeCalls() {
			var code = @"
using System;

public sealed class TrackingSubscriptionAttribute : Attribute {}

public static class EventManager {
	[TrackingSubscription]
	public static void Subscribe<T>(object target, Action<T> handler) {}
	
	[TrackingSubscription]
	public static void Unsubscribe<T>(Action<T> handler) {}
}

public sealed class Sample {
	public void Init() {
		// Warning required
		EventManager.Subscribe<object>(this, Handle);
		EventManager.Subscribe<int>(this, Handle);
	}

	public void Deinit() {
		EventManager.Unsubscribe<object>(Handle);
	}
	
	void Handle(object _) {}
	void Handle(int _) {}
}";
			var expected = new[] {
				DiagnosticResult.CompilerWarning(ExpectedValueId).WithLocation(18, 3).WithMessage(ExpectedValueMessage),
			};
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, expected);
		}

		[Test]
		[NonParallelizable]
		public async Task IsWarningFoundOnNotMatchingHandlers() {
			var code = @"
using System;

public sealed class TrackingSubscriptionAttribute : Attribute {}

public static class EventManager {
	[TrackingSubscription]
	public static void Subscribe<T>(object target, Action<T> handler) {}
	
	[TrackingSubscription]
	public static void Unsubscribe<T>(Action<T> handler) {}
}

public sealed class Sample {
	public void Init() {
		// Warning required
		EventManager.Subscribe<object>(this, Handle1);
	}

	public void Deinit() {
		EventManager.Unsubscribe<object>(Handle2);
	}
	
	void Handle1(object _) {}
	void Handle2(object _) {}
}";
			var expected = new[] {
				DiagnosticResult.CompilerWarning(ExpectedValueId).WithLocation(17, 3).WithMessage(ExpectedValueMessage),
			};
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, expected);
		}

		[Test]
		[NonParallelizable]
		public async Task IsWarningNotFoundOnUnsubscribeCall() {
			var code = @"
using System;

public sealed class TrackingSubscriptionAttribute : Attribute {}

public static class EventManager {
	[TrackingSubscription]
	public static void Subscribe<T>(object target, Action<T> handler) {}
	
	[TrackingSubscription]
	public static void Unsubscribe<T>(Action<T> handler) {}
}

public sealed class Sample {
	public void Init() {
		// No warnings
		EventManager.Subscribe<object>(this, Handle);
	}

	public void Deinit() {
		EventManager.Unsubscribe<object>(Handle);
	}
	
	void Handle(object _) {}
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}

		[Test]
		[NonParallelizable]
		public async Task IsWarningNotFoundOnOutOfScopeNoUnsubscribeCall() {
			var code = @"
using System;

public static class EventManager {
	public static void Subscribe<T>(object target, Action<T> handler) {}
	public static void Unsubscribe<T>(Action<T> handler) {}
}

public sealed class Sample {
	public void Init() {
		// No warnings
		EventManager.Subscribe<object>(this, Handle);
	}

	public void Deinit() {}
	
	void Handle(object _) {}
}";
			await AnalyzerVerifier.VerifyAnalyzerAsync(code, Array.Empty<DiagnosticResult>());
		}
	}
}