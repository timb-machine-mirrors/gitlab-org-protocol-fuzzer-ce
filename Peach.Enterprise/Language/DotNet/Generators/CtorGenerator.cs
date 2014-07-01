
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace Peach.Enterprise.Language.DotNet.Generators
{
	public class CtorGenerator : ITypeGenerator
	{
		IContext _context { set; get; }
		IGroup _group { set; get; }
		ConstructorInfo _ctorInfo { set; get; }
		object[] _args { set; get; }

		Dictionary<ParameterInfo, IGenerator> _parameterGenerators = new Dictionary<ParameterInfo, IGenerator>();

		int _position = 0;
		ParameterInfo[] _parameters = null;

		public CtorGenerator(IContext context, IGroup group, ConstructorInfo ctorInfo)
		{
			_context = context;
			_group = group;
			_ctorInfo = ctorInfo;

			_parameters = ctorInfo.GetParameters();
			_args = new object[_parameters.Length];

			foreach (ParameterInfo param in _parameters)
				_parameterGenerators.Add(param, _context.GetTypeGenerator(group, param.ParameterType, new object [] {param}));
		}

		#region ITypeGenerator Members

		public static bool SupportedType(Type type)
		{
			if (type.IsSubclassOf(typeof(ConstructorInfo)))
				return true;

			return false;
		}

		public static ITypeGenerator CreateInstance(IContext context, IGroup group, Type type, object [] obj)
		{
			return new CtorGenerator(context, group, (ConstructorInfo)obj[0]);
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

			return _ctorInfo.Invoke(_args);
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
