using System;
using System.Collections.Generic;
using System.Text;

namespace Shop.Domain
{
	public class Comment : Entity		
	{
		public Guid UserId { get; set; }
		public Guid ItemId { get; set; }
		public string Value { get; set; }
	}
}
