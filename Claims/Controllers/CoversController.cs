using Claims.Application.Dto;
using Claims.Application.Services;
using Claims.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CoversController(ICoverService coverService) : ControllerBase
    {
        /// <summary>
        /// Computes the premium for a cover based on its dates and type.
        /// </summary>
        [HttpPost("compute")]
        [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
        public ActionResult<decimal> ComputePremium(DateOnly startDate, DateOnly endDate, CoverType coverType)
        {
            var premium = coverService.ComputePremium(startDate, endDate, coverType);

            return Ok(premium);
        }

        /// <summary>
        /// Retrieves all covers.
        /// </summary>
        [HttpGet("get")]
        [ProducesResponseType(typeof(IReadOnlyList<CoverDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<CoverDto>>> GetAsync(CancellationToken cancellationToken)
        {
            var covers = await coverService.GetAllAsync(cancellationToken);

            return Ok(covers);
        }

        /// <summary>
        /// Retrieves a cover by its identifier.
        /// </summary>
        [HttpGet("get/{id}")]
        [ProducesResponseType(typeof(CoverDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CoverDto>> GetAsync(string id, CancellationToken cancellationToken)
        {
            var cover = await coverService.GetByIdAsync(id, cancellationToken);

            if (cover is null)
            {
                return NotFound();
            }

            return Ok(cover);
        }

        /// <summary>
        /// Creates a cover and calculates its premium.
        /// </summary>
        [HttpPost("create")]
        [ProducesResponseType(typeof(CoverDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<CoverDto>> CreateAsync([FromBody] CoverDto cover, CancellationToken cancellationToken)
        {
            var createdCover = await coverService.CreateAsync(cover, cancellationToken);

            return Ok(createdCover);
        }

        /// <summary>
        /// Deletes a cover by its identifier.
        /// </summary>
        [HttpDelete("delete/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
        {
            var deleted = await coverService.DeleteAsync(id, cancellationToken);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}