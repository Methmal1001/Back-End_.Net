using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain;
using NZWalks.API.Models.DTO;

namespace NZWalks.API.Controllers
{
    // https://localhost:1234/api/regions
    [Route("api/[controller]")]
    [ApiController]
    public class RegionsController : ControllerBase
    {
        private NZWalksDbContext dbContext;

        public RegionsController(NZWalksDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // GET ALL REGIONS
        // GET: https://localhost:7077/api/Regions
        [HttpGet]
        public IActionResult GetAll()
        {
            // Get Data From Database - Domain models
            var regionsDomain = dbContext.Regions.ToList();


            // Map Domain Models to DTOs
            var regionsDto = new List<RegionDto>();
            foreach (var regionDomain in regionsDomain)
            {
                regionsDto.Add(new RegionDto()
                {
                    Id = regionDomain.Id,
                    Code = regionDomain.Code,
                    Name = regionDomain.Name,
                    RegionImageUrl = regionDomain.RegionImageUrl
                });
            }


            // Return DTOs
            return Ok(regionsDto);
        }

        // GET SINGLE REGION (Get Region By ID)
        // GET: https://localhost:7077/api/Regions
        [HttpGet]
        [Route("{id:Guid}")]
        public IActionResult GetById([FromRoute] Guid id)
        {
            //var region = dbContext.Regions.Find(id);

            // Get Region Domain Model From Database

            var regionDomain = dbContext.Regions.FirstOrDefault(x => x.Id == id);

            if (regionDomain == null)
            {
                return NotFound(new ApiResponseFormat<string>
                {
                    ResultCode = StatusCodes.Status404NotFound,
                    ResultDesc = "Region Not Found",
                    Data = null
                });
            }

            // Map/Convert RegionDomain Model to Region DTO
            //
            var regionDto = new RegionDto
            {
                Id = regionDomain.Id,
                Code = regionDomain.Code,
                Name = regionDomain.Name,
                RegionImageUrl = regionDomain.RegionImageUrl
            };

            // Return DTO back to client
            return Ok(regionDto);
        }

        // POST To Create New Region
        // POST: https://localhost:7077/api/Regions
        [HttpPost]
        public IActionResult Create([FromBody] AddRegionRequestDto addRegionRequestDto)
        {
            // Map or convert DTO to domain model
            var regionDomainModel = new Region
            {
                Code = addRegionRequestDto.Code,
                Name = addRegionRequestDto.Name,
                RegionImageUrl = addRegionRequestDto.RegionImageUrl
            };

            // Use Domain Model to create Region
            dbContext.Regions.Add(regionDomainModel);
            dbContext.SaveChanges();

            //Map Domain model back to DTO
            var regionDto = new RegionDto
            {
                Id = regionDomainModel.Id,
                Code = regionDomainModel.Code,
                Name = regionDomainModel.Name,
                RegionImageUrl = regionDomainModel.RegionImageUrl
            };

            return CreatedAtAction(nameof(GetById), new { id = regionDomainModel.Id }, regionDto);
        }

    }
}
