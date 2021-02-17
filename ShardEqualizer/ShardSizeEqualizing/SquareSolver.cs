using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Accord.Math;
using Accord.Math.Optimization;
using NLog;

namespace ShardEqualizer.ShardSizeEqualizing
{
	public class SquareSolver<T> where T: IEquatable<T>
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		private readonly IDictionary<T, Variable> _nameMap = new Dictionary<T, Variable>();

		private readonly List<PositiveConstraint> _positiveConstraints = new List<PositiveConstraint>();

		private readonly List<LinearPolynomial2<T>> _objectives = new List<LinearPolynomial2<T>>();

		public SquareSolver()
		{
		}

		public void InitVariable(T vName, double init)
		{
			_nameMap.Add(vName, new Variable(vName, init));
		}

		public void SetMax(T variable, double max)
		{
			SetPositiveConstraint(new LinearPolynomial2<T>() {[variable] = -1, Constant = max}, $"[{variable}] <= {max}");
		}

		public void SetMin(T variable, double min)
		{
			SetPositiveConstraint(new LinearPolynomial2<T>() {[variable] = 1, Constant = -min}, $"{min} <= [{variable}]");
		}

		public void SetPositiveConstraint(LinearPolynomial2<T> func, string desc)
		{
			_positiveConstraints.Add(new PositiveConstraint(func, desc));
		}

		public void SetEqualConstraint(Vector<T> linearTerms, double requiredValue)
		{
			var eq = new LinearPolynomial2<T>(linearTerms, -requiredValue);
			substituteClosedVariables(eq);

			if (!eq.LinearTerms.Any())
			{
				if (eq.Constant == 0)
					return;

				throw new Exception("constraint leads to no solution ");
			}

			var newVar = eq.LinearTerms.First().Key;
			var closeFunction = eq.Express(newVar);

			_nameMap[newVar].Function = closeFunction;

			//inline new closed variable into other closed variables
			foreach (var v in _nameMap.Values.Where(_ => _.Closed))
				v.Function.Substitute(newVar, closeFunction);
		}

		public void SetObjective(Vector<T> linearTerms, double objective)
		{
			_objectives.Add(new LinearPolynomial2<T>(linearTerms, -objective));
		}

		private void substituteClosedVariables(LinearPolynomial2<T> function)
		{
			var inlineVars = function.LinearTerms.Select(t => _nameMap[t.Key]).Where(_ => _.Closed).ToList();

			foreach (var inlineVar in inlineVars)
				function.Substitute(inlineVar.Name, inlineVar.Function);
		}

		public bool Find(CancellationToken token)
		{
			substituteConstraintFunctions();
			substituteObjectiveFunctions();

			var init = buildInit();
			checkConstraintsByInit(init);

			var indexMap = _nameMap.Values.Where(_ => _.Open).Select((v, i) => (v, i)).ToDictionary(_ => _.v.Name, _ => _.i);

			if (!indexMap.Any())
			{
				foreach (var v in _nameMap.Values)
					v.Solution = v.Function.Function(init);
				return true;
			}

			var totalVariables = indexMap.Count;

			var targetFunction = new QuadraticObjectiveFunction(
				new double[totalVariables, totalVariables],
				new double[totalVariables]);

			foreach (var func in _objectives)
				appendSquare(targetFunction, func, indexMap);

			var constraints = new List<LinearConstraint>();

			foreach (var c in _positiveConstraints)
				constraints.Add(positiveConstraint(c.Function, indexMap));

			var initArray = new double[indexMap.Count];

			foreach (var t in init)
				initArray[indexMap[t.Key]] = t.Value;

			_log.Trace("QuadraticTerms: {0}",  targetFunction.QuadraticTerms.ToCSharp());
			_log.Trace("LinearTerms: {0}",  targetFunction.LinearTerms.ToCSharp());
			_log.Trace("ConstantTerm: {0}",  targetFunction.ConstantTerm);

			var innerSolver = new BroydenFletcherGoldfarbShanno(indexMap.Count)
			{
				LineSearch = LineSearch.Default, //default BacktrackingArmijo algorithm has stationary starting values
				Corrections = 3,
				Epsilon = 1E-10,
				MaxIterations = 100000
			};

			var solver = new AugmentedLagrangian(innerSolver, targetFunction, constraints) {Token = token, Solution = initArray};

			var startValue = solver.Function(solver.Solution);
			_log.Trace($"Objective start value: {startValue}");
			_log.Trace($"Objective gradient: {solver.Gradient(solver.Solution).ToCSharp()}");

			var solveResult = solver.Minimize();

			_log.Trace("Objective end value: {0}", solver.Value);
			_log.Trace("Objective gradient on solution: {0}", solver.Gradient(solver.Solution).ToCSharp());
			_log.Trace("Solve status: {0}", solver.Status);

			if (!solveResult)
				return false;

			var solutionVector = new Vector<T>();

			foreach (var (key, value) in indexMap)
			{
				var x = solver.Solution[value];
				solutionVector[key] = x;
				_nameMap[key].Solution = x;
			}

			foreach (var v in _nameMap.Values.Where(_ => _.Closed))
				v.Solution = v.Function.Function(solutionVector);

			ActiveConstraints = _positiveConstraints
				.Where(c => c.Function.Function(solutionVector) <= 0.25)
				.Select(c => c.Description)
				.ToList();

			return true;
		}

		public IList<string> ActiveConstraints { get; private set; } = new List<string>();

		private LinearConstraint positiveConstraint(LinearPolynomial2<T> func, Dictionary<T, int> indexMap)
		{
			var d = new double[indexMap.Count];

			foreach (var t in func.LinearTerms)
				d[indexMap[t.Key]] = t.Value;

			return new LinearConstraint(d)
			{
				ShouldBe = ConstraintType.GreaterThanOrEqualTo,
				Value = -func.Constant,
				Tolerance = 0.25,
			};
		}

		private void appendSquare(QuadraticObjectiveFunction objectiveFunction, LinearPolynomial2<T> func, Dictionary<T, int> indexMap)
		{
			objectiveFunction.ConstantTerm += func.Constant * func.Constant;
			var terms = func.LinearTerms.Select(t => (index: indexMap[t.Key], value: t.Value)).ToList();
			foreach (var (i, iV) in terms)
			{
				objectiveFunction.LinearTerms[i] += 2* iV * func.Constant;
				foreach (var (j, jV) in terms)
				{
					if(i == j)
						objectiveFunction.QuadraticTerms[i, j] += 2 * iV * jV;
					else
						objectiveFunction.QuadraticTerms[i, j] += 2 * iV * jV;
				}
			}
		}

		private void checkConstraintsByInit(Vector<T> init)
		{
			foreach (var c in _positiveConstraints)
			{
				if(c.Function.Function(init) < 0)
					throw new Exception("constraint fail");
			}
		}

		private Vector<T> buildInit()
		{
			var init = new Vector<T>();

			foreach (var v in _nameMap.Values.Where(_ => _.Open))
				init[v.Name] = v.Init;

			return init;
		}

		private void substituteObjectiveFunctions()
		{
			foreach (var func in _objectives)
				substituteClosedVariables(func);

			_objectives.RemoveAll(_ => !_.LinearTerms.Any());
		}

		private void substituteConstraintFunctions()
		{
			foreach (var c in _positiveConstraints)
			{
				substituteClosedVariables(c.Function);
				if (c.Function.LinearTerms.Any())
					continue;

				if (c.Function.Constant >= 0)
				{
					_log.Trace("constraint {0} not used", c.Description);
					continue;
				}

				throw new Exception($"constraint {c.Description} leads to no solution ");
			}

			_positiveConstraints.RemoveAll(_ => !_.Function.LinearTerms.Any());
		}

		public double GetSolution(T variable)
		{
			return _nameMap[variable].Solution;
		}

		private class Variable
		{
			public T Name { get; }

			public double Init { get; }

			public double Solution { get; set; }

			public bool Open => Function == null;

			public bool Closed => Function != null;

			public LinearPolynomial2<T> Function { get; set; }

			public Variable(T name, double init)
			{
				Init = init;
				Name = name;
			}
		}

		private class PositiveConstraint
		{
			public PositiveConstraint(LinearPolynomial2<T> function, string description)
			{
				Function = function;
				Description = description;
			}

			public LinearPolynomial2<T> Function { get; }
			public string Description { get; }
		}
	}
}
