
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Peach.Enterprise.Language.DotNet.Generators
{
	/// <summary>
	/// Object generator allows us to seed the ClassGenerator with an
	/// object instance that will be used to fuzz methods/parameters.
	/// </summary>
	public class ObjectGenerator: ClassGenerator
	{
		protected static object _target;

		protected ObjectGenerator(IContext context, IGroup group, Type type) : base(context, group, type)
		{
		}

		public object ObjectInstance
		{
			get { return _target; }
			set
			{
				if (_target != null && value.GetType() != _target.GetType())
					throw new ApplicationException("Invalid parameter to ObjectInstance.  Type change not allowed.");

				Reset();
				_target = value;
			}
		}

		#region ITypeGenerator Members

		public static new bool SupportedType(Type type)
		{
			// TODO: Make this correct!

			return false;

			//if (type.IsSubclassOf(typeof(MethodInfo)) ||
			//    type.IsSubclassOf(typeof(PropertyInfo)) ||
			//    type.IsSubclassOf(typeof(EventInfo)) ||
			//    type == typeof(Object))
			//    return false;

			//if (type.IsClass && !type.IsAbstract)
			//    return true;

			//return false;
		}

		public static new ITypeGenerator CreateInstance(IContext context, IGroup group, Type type, object[] obj)
		{
			return new ObjectGenerator(context, group, type);
		}

		#endregion
	}
}
