using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vidka.Core
{
	public static class Utils
	{
		public static void AddUnique<T>(this List<T> list, T obj) {
			if (list.Contains(obj))
				return;
			list.Add(obj);
		}

		public static string StringJoin(this IEnumerable<string> list, string separator) {
			return string.Join(separator, list);
		}
		
	}
}
