
//
// Copyright (c) Deja vu Security
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using Godel.Core.OCL;

namespace Godel.Tests
{
	[TestFixture]
	class OclTests
	{
		[Test]
		public void Comments()
		{
			var script = @"
-- This is a test of comments
-- And more comments
context Meeting 
pre foo: self.grade.name = 'diploma'
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = 7;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Pre(self, null));
		}

		[Test]
		public void LogicalConditions()
		{
			var script = @"context Meeting 
pre foo: self.grade.name = 'diploma' and self.start != self.end and self.end > self.start
pre bar1: self.end < self.start or self.end > self.start
";
//pre bar2: self.end < self.start or self.end > self.start
//pre bar3: self.end < self.start or self.end > self.start
//pre bar4: self.end < self.start or self.end > self.start
//pre bar5: self.end < self.start or self.end > self.start
//pre bar6: self.end < self.start or self.end > self.start
//pre bar7: self.end < self.start or self.end > self.start
//pre bar8: self.end < self.start or self.end > self.start
//pre bar9: self.end < self.start or self.end > self.start
//pre bar10: self.end < self.start or self.end > self.start
//pre bar11: self.end < self.start or self.end > self.start
//pre bar12: self.end < self.start or self.end > self.start
//pre bar13: self.end < self.start or self.end > self.start
//pre bar14: self.end < self.start or self.end > self.start
//pre bar15: self.end < self.start or self.end > self.start
//pre bar16: self.end < self.start or self.end > self.start
//pre bar17: self.end < self.start or self.end > self.start
//pre bar18: self.end < self.start or self.end > self.start
//pre bar19: self.end < self.start or self.end > self.start
//pre bar20: self.end < self.start or self.end > self.start
//pre bar21: self.end < self.start or self.end > self.start
//pre bar22: self.end < self.start or self.end > self.start
//pre bar23: self.end < self.start or self.end > self.start

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = 7;

			var contexts = Ocl.ParseOcl(script);

			//var start = DateTime.Now;
			//for (int i = 0; i < 10000; i++)
			//    contexts[0].Pre(self, null);
			//var end = DateTime.Now;

			//var diff = (end - start).TotalSeconds;

			//Assert.Greater(0, diff);
			Assert.IsTrue(contexts[0].Pre(self, null));
		}

		[Test]
		public void LogicalParenConditions()
		{
			var script = @"context Meeting 
pre foo: (self.grade.name = 'diploma' and self.start != self.end) and self.end > self.start
pre bar: self.end < self.start or (self.end > self.start)
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = 7;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Pre(self, null));
		}

		[Test]
		public void Addition()
		{
			var script = @"context Meeting 
pre a: self.end = 6+1
post b: self.end = 8-1
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = 7;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
		}

		[Test]
		public void Multiplication()
		{
			var script = @"context Meeting 
pre a: self.end = 4*2
post b: self.end = 16/2
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = 8;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
		}

		[Test]
		public void Field()
		{
			var script = @"context Meeting 
pre a: self.fieldString = 'field'
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = 8;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Pre(self, null));
		}

		[Test]
		public void Properties()
		{
			var script = @"context Meeting 
pre a: self.grade.name = 'diploma'
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = 8;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Pre(self, null));
		}

		[Test]
		public void ItemAccess()
		{
			var script = @"context Meeting 
pre a: self.dict['test'] = 'godel'
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = 8;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Pre(self, null));
		}

		[Test]
		public void Method()
		{
			var script = @"context Meeting 
pre a: self.GetGrade().name = 'diploma'
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = 8;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Pre(self, null));
		}

		[Test]
		public void MultipleConstraints()
		{
			var script = @"context Meeting 
pre a: self.grade.name = 'diploma'
pre b: self.end > self.start
post c: self.end = self.start
inv d: self.end > self.start
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = 7;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsFalse(contexts[0].Post(self, null));
			Assert.IsTrue(contexts[0].Inv(self, null));

			script = @"context Meeting 
pre a: self.grade.name = 'diploma'
pre b: self.end < self.start
post c: self.end != self.start
inv d: self.end > self.start
inv e: self.end < self.start
";

			self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = 7;

			contexts = Ocl.ParseOcl(script);
			Assert.IsFalse(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
			Assert.IsFalse(contexts[0].Inv(self, null));
		}

		[Test]
		public void Pre()
		{
			var script = @"context Meeting 
pre a: self.grade.name = 'diploma'
pre b: self.start = 5
pre c: self.end = 7
post d: self@pre.grade.name = 'hello'
post e: self@pre.end > self.end
post f: self@pre.start > self.start
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = 7;

			var self_pre = new test();
			self_pre.grade = new Grade();
			self_pre.grade.name = "hello";
			self_pre.start = 6;
			self_pre.end = 8;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Pre(self, self_pre));
			Assert.IsTrue(contexts[0].Post(self, self_pre));
		}

		[Test]
		public void Abs()
		{
			var script = @"context Meeting 
inv a: self.end->abs() = 7
pre b: self.end->abs() = 7
post c: self.end->abs() = 7
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = -7;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Inv(self, null));
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
		}

		[Test]
		public void Size()
		{
			//            var script = @"context Meeting 
			//inv a: self.grade.name->size() = 7
			//pre b: self.grade.name->size() = 7
			//post c: self.grade.name->size() = 7
			//";
			var script = @"context Meeting 
inv a: self.name->size() = 5
pre b: self.name->size() = 5
post c: self.name->size() = 5
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = -7;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Inv(self, null));
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
		}

		[Test]
		public void Size2()
		{
			var script = @"context Meeting 
			inv a: self.grade.name->size() = 7
			pre b: self.grade.name->size() = 7
			post c: self.grade.name->size() = 7
			";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = -7;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Inv(self, null));
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
		}

		[Test]
		public void Concat()
		{
			var script = @"context Meeting 
inv a: self.name->concat('foo') = 'Godelfoo'
pre b: self.name->concat('foo') = 'Godelfoo'
post c: self.name->concat('foo') = 'Godelfoo'
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = -7;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Inv(self, null));
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
		}

		[Test]
		public void Substring()
		{
			var script = @"context Meeting 
inv a: self.name->substring(0, 2) = 'Go'
pre b: self.name->substring(0, 2) = 'Go'
post c: self.name->substring(0, 2) = 'Go'
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = -7;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Inv(self, null));
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
		}

		[Test]
		public void Floor()
		{
			var script = @"context Meeting 
inv a:  self.realnumber->floor() = 5
pre b:  self.realnumber->floor() = 5
post c: self.realnumber->floor() = 5
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = -7;
			self.realnumber = 5.5;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Inv(self, null));
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
		}

		[Test]
		public void Xor()
		{
			var script = @"context Meeting 
inv a:  (self.start xor 2) = 7
pre b:  (self.start xor 2) = 7
post c: (self.start xor 2) = 7
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = -7;
			self.realnumber = 5.5;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Inv(self, null));
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
		}

		[Test]
		public void IntPlusReal()
		{
			var script = @"context Meeting 
inv a:  (self.start + self.realnumber) = 10.5
pre b:  (self.start + self.realnumber) = 10.5
post c: (self.start + self.realnumber) = 10.5
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = -7;
			self.realnumber = 5.5;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Inv(self, null));
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
		}

		[Test]
		public void Unary()
		{
			var script = @"context Meeting 
inv a:  -self.start = -5
pre b:  -self.start = -5
post c: -self.start = -5
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = -7;
			self.realnumber = 5.5;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Inv(self, null));
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
		}

		[Test]
		public void IfThenElse()
		{
			var script = @"context Meeting 
inv a:  if self.start = 5 then self.end < 0 else self.end > 0 endif
pre b:  if self.start = 5 then self.end < 0 else self.end > 0 endif
post c: if self.start = 5 then self.end < 0 else self.end > 0 endif
";

			var self = new test();
			self.grade = new Grade();
			self.grade.name = "diploma";
			self.start = 5;
			self.end = -7;
			self.realnumber = 5.5;

			var contexts = Ocl.ParseOcl(script);
			Assert.IsTrue(contexts[0].Inv(self, null));
			Assert.IsTrue(contexts[0].Pre(self, null));
			Assert.IsTrue(contexts[0].Post(self, null));
		}

		// Advanced language feature that we have not yet implemented.
		// not sure this feature is actually needed.
//        [Test]
//        public void LetIn()
//        {
//            var script = @"context Meeting 
//inv a:  let newStart: Integer = self.start - self.end in newStart < 0
//pre b:  let newStart: Integer = self.start - self.end in newStart < 0
//post c: let newStart: Integer = self.start - self.end in newStart < 0
//";

//            var self = new test();
//            self.grade = new Grade();
//            self.grade.name = "diploma";
//            self.start = 5;
//            self.end = -7;
//            self.realnumber = 5.5;

//            var contexts = Ocl.ParseOcl(script);
//            Assert.IsTrue(contexts[0].Inv(self, null));
//            Assert.IsTrue(contexts[0].Pre(self, null));
//            Assert.IsTrue(contexts[0].Post(self, null));
//        }
	}
	
	public class test
	{
		public string fieldString = "field";
		public int fieldInteger = 10;

		public string name = "Godel";

		public int start { get; set; }
		public int end { get; set; }
		public Grade grade { get; set; }
		
		public int property { get; set; }
		public double realnumber { get; set; }

		public Dictionary<string, string> dict = new Dictionary<string, string>();

		public test()
		{
			dict["test"] = "godel";
		}

		public Grade GetGrade()
		{
			return grade;
		}

		public Grade GetGrade(test t)
		{
			return t.grade;
		}
	}

	public class Grade
	{
		public string name { get; set; }
	}

}
