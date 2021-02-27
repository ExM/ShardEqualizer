using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Accord.Math;

namespace ShardEqualizer.ShardSizeEqualizing
{
	public class LinearPolynomial2<T> where T : IEquatable<T>
	{
		public LinearPolynomial2()
		{
			LinearTerms = new Vector<T>();
		}

		public LinearPolynomial2(LinearPolynomial2<T> src)
		{
			LinearTerms = new Vector<T>(src.LinearTerms);
			Constant = src.Constant;
		}

		public LinearPolynomial2(Vector<T> linearTerms, double constant)
		{
			LinearTerms = linearTerms;
			Constant = constant;
		}

		public double Function(Vector<T> input)
		{
			return LinearTerms.Dot(input) + Constant;
		}

		public double this[T key]
		{
			get => LinearTerms[key];
			set => LinearTerms[key] = value;
		}

		public Vector<T> LinearTerms { get; private set; }

		public double Constant { get; set; }

		public static LinearPolynomial2<T> operator +(LinearPolynomial2<T> a, LinearPolynomial2<T> b)
		{
			var result = new LinearPolynomial2<T>(a);
			foreach (var p in b.LinearTerms)
				result[p.Key] += p.Value;
			result.Constant += b.Constant;
			return result;
		}

		public static LinearPolynomial2<T> operator *(double a, LinearPolynomial2<T> p)
		{
			var result = new LinearPolynomial2<T>() { Constant = a * p.Constant};
			foreach (var t in p.LinearTerms)
				result[t.Key] = a * t.Value;
			return result;
		}

		/*
		public QuadraticObjectiveFunction Square()
		{
			var q = new double[NumberOfVariables, NumberOfVariables];
			var d = new double[NumberOfVariables];

			for (var i = 0; i < NumberOfVariables; i++)
			{
				var ti = LinearTerms[i];
				q[i, i] = 2 * ti * ti;
				d[i] = 2 * ti * ConstantTerm;
				for (var j = i + 1; j < NumberOfVariables; j++)
				{
					var s = 2 * ti * LinearTerms[j];
					q[i, j] = s;
					q[j, i] = s;
				}
			}

			return new QuadraticObjectiveFunction(q, d)
			{
				ConstantTerm = ConstantTerm * ConstantTerm
			};
		}
		*/

		public static LinearPolynomial2<T> Substitute(LinearPolynomial2<T> source, T key, LinearPolynomial2<T> inline)
		{
			var c = source.LinearTerms[key];
			if (c == 0)
				return source;

			return source + new LinearPolynomial2<T>() {[key] = -c} + c * inline;
		}

		public void Substitute(T key, LinearPolynomial2<T> inline)
		{
			var c = LinearTerms[key];
			if (c == 0)
				return;

			LinearTerms[key] = 0;

			foreach (var p in inline.LinearTerms)
				this[p.Key] += c * p.Value;
			Constant += c * inline.Constant;
		}

		public LinearPolynomial2<T> Express(T key)
		{
			var result = new LinearPolynomial2<T>(this);
			var a = result.LinearTerms[key];
			if (a == 0)
				throw new Exception("variable not found");
			a = -1 / a;

			result[key] = 0;
			result.Constant *= a;
			result.LinearTerms.Multiply(a);
			return result;
		}
	}

	public class Vector<T>: IEnumerable<KeyValuePair<T, double>> where T : IEquatable<T>
	{
		private readonly Dictionary<T, double> _values = new Dictionary<T, double>();

		public Vector()
		{
		}

		private Vector(Dictionary<T, double> values)
		{
			_values = values;
		}

		public Vector(Vector<T> src)
		{
			_values = new Dictionary<T, double>(src._values);
		}

		public double Dot(Vector<T> input)
		{
			return _values.Keys.Union(input._values.Keys)
				.Sum(key => this[key] * input[key]);
		}

		public static Vector<T> Unit(IEnumerable<T> keys)
		{
			return new Vector<T>(keys.ToDictionary(_ => _, _ => 1d));
		}

		public double this[T key]
		{
			get => _values.TryGetValue(key, out var value) ? value : 0;
			set
			{
				if (value == 0)
					_values.Remove(key);
				else
					_values[key] = value;
			}
		}

		public static Vector<T> operator *(Vector<T> v, double a)
		{
			return Multiply(a, v);
		}

		public static Vector<T> operator *(double a,  Vector<T> v)
		{
			return Multiply(a, v);
		}

		public static Vector<T> Multiply(double a,  Vector<T> v)
		{
			var result = new Vector<T>();
			foreach (var p in v._values)
				result[p.Key] = a * p.Value;
			return result;
		}

		public static Vector<T> operator +(Vector<T> x, Vector<T> y)
		{
			var result = new Vector<T>(x);
			foreach (var p in y._values)
				result[p.Key] += p.Value;
			return result;
		}

		public void Multiply(double a)
		{
			foreach (var key in _values.Keys.ToList())
				_values[key] *= a;
		}

		public IEnumerator<KeyValuePair<T, double>> GetEnumerator() => _values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
