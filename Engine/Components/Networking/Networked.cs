using EnCS.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Components
{
	[Component]
	public ref partial struct Networked
	{
		public ref int idx;
	}
}
