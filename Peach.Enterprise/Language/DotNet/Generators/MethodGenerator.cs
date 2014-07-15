
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace Peach.Enterprise.Language.DotNet.Generators
{
	public class MethodGenerator : ITypeGenerator
	{
		IContext _context { set; get; }
		IGroup _group { set; get; }
		MethodInfo _methodInfo { set; get; }
		object[] _args { set; get; }
		IGenerator _ctorGenerator { set; get; }

		Dictionary<ParameterInfo, IGenerator> _parameterGenerators = new Dictionary<ParameterInfo, IGenerator>();

		int _position = 0;
		ParameterInfo[] _parameters = null;

		public MethodGenerator(IContext context, IGroup group, MethodInfo ctorInfo, IGenerator ctorGenerator)
		{
			_context = context;
			_group = group;
			_methodInfo = ctorInfo;
			_ctorGenerator = ctorGenerator;

			_parameters = ctorInfo.GetParameters();
			_args = new object[_parameters.Length];

			foreach (ParameterInfo param in _parameters)
				_parameterGenerators.Add(param, _context.GetTypeGenerator(group, param.ParameterType, new object[] { param }));
		}

		#region ITypeGenerator Members

		public static bool SupportedType(Type type)
		{
			if (type.IsSubclassOf(typeof(MethodInfo)))
				return true;

			return false;
		}

		public static ITypeGenerator CreateInstance(IContext context, IGroup group, Type type, object [] obj)
		{
			return new MethodGenerator(context, group, (MethodInfo)obj[0], (IGenerator) obj[1]);
		}

		#endregion

		#region IGenerator Members

		public object GetValue()
		{
			for (int i = 0; i < _parameters.Length; i++)
			{
				if (_parameterGenerators[_parameters[i]] == null)
					_args[i] = _parameterGenerators[_parameters[i]];
				else
					_args[i] = _parameterGenerators[_parameters[i]].GetValue();
			}

			try
			{
				return _methodInfo.Invoke(_ctorGenerator.GetValue(), _args);
			}
			catch
			{
				return null;
			}
		}

		public void Next()
		{
			try
			{
				if (_parameters.Length == 0)
					throw new GeneratorCompleted();

				_parameterGenerators[_parameters[_position]].Next();
			}
			catch (GeneratorCompleted)
			{
				_position++;
				if (_position >= _parameters.Length)
				{
					_position--;
					throw new GeneratorCompleted();
				}
			}
		}

		public void Reset()
		{
			_position = 0;
			foreach (IGenerator generator in _parameterGenerators.Values)
				generator.Reset();
		}

		#endregion
	}
}

// end
