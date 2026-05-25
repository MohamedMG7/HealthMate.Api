using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.DTO.BodySiteDto;
using HealthMate.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthMate.Application.Manager.BodySiteManager
{

	public class BodySiteManager : IBodySiteManager
	{
		private readonly IGenericRepository<BodySite> _bodySiteRepo;
		public BodySiteManager(IGenericRepository<BodySite> bodySiteRepo)
		{
			_bodySiteRepo = bodySiteRepo;
		}

		public Task<List<BodySiteNameAndId>> getBodySiteNameAndId() {
			var result = _bodySiteRepo.GetAll().Select(x => new BodySiteNameAndId
			{
				BodySiteId = x.BodySite_Id,
				BodySiteName = x.DisplayName
			}).ToListAsync();

			return result;
		}

	}
}
