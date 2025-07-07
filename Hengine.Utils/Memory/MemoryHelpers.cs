using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hengine.Utils
{
	public unsafe struct MappedMemory<T> where T : unmanaged
	{
		public ref T Value
		{
			get
			{
				return ref data[0];
			}
		}

		T* data;

		public MappedMemory(T* data)
		{
			this.data = data;
		}
	}
}
