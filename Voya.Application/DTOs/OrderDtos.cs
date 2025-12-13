using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voya.Application.DTOs
{
	public class ReturnDecisionDto
	{
		public bool Approved { get; set; }
		public bool Restock { get; set; } // Should we put items back in inventory?
		public string AdminNote { get; set; } = string.Empty;
		public string? Note { get; set; }
	}
}
