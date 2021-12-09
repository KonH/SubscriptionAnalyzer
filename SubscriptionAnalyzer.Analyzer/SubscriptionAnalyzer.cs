using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SubscriptionAnalyzer.Analyzer {
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class SubscriptionAnalyzer : DiagnosticAnalyzer {
		public static readonly DiagnosticDescriptor MissingUnsubscribeCallDiagnostic =
			new DiagnosticDescriptor(
				"SA1001",
				"Missing Unsubscribe call",
				"Subscribe call found, but no corresponding Unsubscribe call found",
				"Usage",
				DiagnosticSeverity.Warning,
				true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(new [] {
			MissingUnsubscribeCallDiagnostic,
		});

		public override void Initialize(AnalysisContext context) {
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterCompilationStartAction(Analyze);
		}

		void Analyze(CompilationStartAnalysisContext context) {
			var subs   = new ConcurrentBag<SubscriptionData>();
			var unSubs = new ConcurrentBag<SubscriptionData>();
			context.RegisterOperationAction(c => ValidateInvocation(c, subs, unSubs), OperationKind.Invocation);
			context.RegisterSemanticModelAction(c => FinishValidation(c, subs, unSubs));
		}

		void ValidateInvocation(OperationAnalysisContext context, ConcurrentBag<SubscriptionData> subs, ConcurrentBag<SubscriptionData> unSubs) {
			var invocation   = (IInvocationOperation)context.Operation;
			var methodSymbol = invocation.TargetMethod;
			var methodKind   = DetectTargetMethod(methodSymbol);
			if ( methodKind == MethodKind.Unspecified ) {
				return;
			}
			if ( !HasTargetAttribute(methodSymbol) ) {
				return;
			}
			var containingTypeSymbol = (ITypeSymbol)context.ContainingSymbol.ContainingType;
			var methodReference = invocation.Descendants()
				.OfType<IMethodReferenceOperation>()
				.FirstOrDefault();
			if ( methodReference == null ) {
				return;
			}
			var targetHandlerMethod = methodReference.Method;
			var handlerParameter    = targetHandlerMethod.Parameters.FirstOrDefault();
			if ( handlerParameter == null ) {
				return;
			}
			var typeName          = containingTypeSymbol.Name;
			var targetHandlerType = handlerParameter.Type;
			var handlerType       = targetHandlerType.Name;
			var handlerName       = targetHandlerMethod.Name;
			var targetSpan        = invocation.Syntax.Span;
			var data              = new SubscriptionData(typeName, handlerType, handlerName, targetSpan);
			switch ( methodKind ) {
				case MethodKind.Subscribe:
					subs.Add(data);
					break;
				case MethodKind.Unsubscribe:
					unSubs.Add(data);
					break;
			}
		}

		MethodKind DetectTargetMethod(IMethodSymbol methodSymbol) {
			var targetMethodName = methodSymbol.Name;
			switch ( targetMethodName ) {
				case "Subscribe":   return MethodKind.Subscribe;
				case "Unsubscribe": return MethodKind.Unsubscribe;
				default:            return MethodKind.Unspecified;
			}
		}

		bool HasTargetAttribute(IMethodSymbol methodSymbol) =>
			methodSymbol.GetAttributes()
				.Any(a => a.AttributeClass.Name == "TrackingSubscriptionAttribute");

		void FinishValidation(SemanticModelAnalysisContext context, ConcurrentBag<SubscriptionData> subs, ConcurrentBag<SubscriptionData> unSubs) {
			var subsPerType = subs
				.GroupBy(d => d.TypeName)
				.ToDictionary(p => p.Key, p => p.ToArray());
			var unSubsPerType = unSubs
				.GroupBy(d => d.TypeName)
				.ToDictionary(p => p.Key, p => p.ToArray());
			foreach ( var subPair in subsPerType ) {
				var subsForType = subPair.Value;
				foreach ( var subData in subsForType ) {
					if ( IsUnsubFound(subData, unSubsPerType) ) {
						continue;
					}
					var syntaxTree = context.SemanticModel.SyntaxTree;
					var span       = subData.TargetSpan;
					var location   = Location.Create(syntaxTree, span);
					var diagnostic = Diagnostic.Create(MissingUnsubscribeCallDiagnostic, location);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		bool IsUnsubFound(SubscriptionData subData, Dictionary<string, SubscriptionData[]> unSubsPerType) {
			if ( !unSubsPerType.TryGetValue(subData.TypeName, out var unSubsForType) ) {
				return false;
			}
			foreach ( var unSubData in unSubsForType ) {
				if ( unSubData.HandlerName != subData.HandlerName ) {
					continue;
				}
				if ( unSubData.HandlerType != subData.HandlerType ) {
					continue;
				}
				return true;
			}
			return false;
		}
	}
}