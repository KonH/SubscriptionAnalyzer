using Microsoft.CodeAnalysis.Text;

namespace SubscriptionAnalyzer.Analyzer {
	readonly struct SubscriptionData {
		public readonly string   TypeName;
		public readonly string   HandlerType;
		public readonly string   HandlerName;
		public readonly TextSpan TargetSpan;

		public SubscriptionData(string typeName, string handlerType, string handlerName, TextSpan targetSpan) {
			TypeName    = typeName;
			HandlerType = handlerType;
			HandlerName = handlerName;
			TargetSpan  = targetSpan;
		}
	}
}