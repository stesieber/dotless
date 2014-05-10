using System;

namespace dotless.Core.Parser.Tree
{
    using System.Linq;
    using Infrastructure;
    using Infrastructure.Nodes;
    using Plugins;

    public class Call : Node
    {
        public string Name { get; set; }
        public NodeList<Node> Arguments { get; set; }

        public Call(string name, NodeList<Node> arguments)
        {
            Name = name;
            Arguments = arguments;
        }

        protected Call()
        {
        }

        public override Node Evaluate(Env env)
        {
            if (env == null)
            {
                throw new ArgumentNullException("env");
            }

			if (env.UseStrictMath)
			{
				Arguments.Accept(new SuppressOperationEvaluationVisitor());
			}

	        var args = Arguments.Select(a => a.Evaluate(env));

            var function = env.GetFunction(Name);
			
            if (function != null)
            {
                function.Name = Name;
                function.Location = Location;
                return function.Call(env, args).ReducedFrom<Node>(this);
            }

            env.Output.Push();
            
            env.Output
                .Append(Name)
                .Append("(")
                .AppendMany(args, env.Compress ? "," : ", ")
                .Append(")");

            var css = env.Output.Pop();

            return new TextNode(css.ToString()).ReducedFrom<Node>(this);
        }

        public override void Accept(IVisitor visitor)
        {
            Arguments = VisitAndReplace(Arguments, visitor);
        }

	    private class SuppressOperationEvaluationVisitor : IVisitor
	    {
		    public Node Visit(Node node)
		    {
			    Node result = node;

				Operation op = node as Operation;
				if (op != null)
				{
					return new OperationWithSuppressedEvaluation(op);
				}

				result.Accept(this);

				return result;
		    }
	    }

	    private class OperationWithSuppressedEvaluation : Node
	    {
		    private readonly Operation _wrappedOperation;

		    public OperationWithSuppressedEvaluation(Operation wrappedOperation)
		    {
			    _wrappedOperation = wrappedOperation;
		    }

		    public override Node Evaluate(Env env)
		    {
				//skipping real evaluation
			    return this;
		    }

		    public override void AppendCSS(Env env)
		    {
				_wrappedOperation.First.AppendCSS(env);
				env.Output.Append(" " + _wrappedOperation.Operator + " ");
				_wrappedOperation.Second.AppendCSS(env);
		    }

		    public override void Accept(IVisitor visitor)
		    {
			    _wrappedOperation.Accept(visitor);
		    }
	    }
    }
}