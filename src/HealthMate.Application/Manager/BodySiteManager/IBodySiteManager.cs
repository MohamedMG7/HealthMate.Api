using HealthMate.Application.Clinical.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthMate.Application.Manager.BodySiteManager
{
	public interface IBodySiteManager
	{
		Task<List<BodySiteNameAndId>> getBodySiteNameAndId();
	}
}
