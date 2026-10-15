using Claims.Application.Dto;
using Claims.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public sealed class ClaimsController(IClaimService claimService) : ControllerBase
    {
        /// <summary>
        /// Retrieves all claims.
        /// </summary>
        [HttpGet("get")]
        [ProducesResponseType(typeof(IReadOnlyList<ClaimDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ClaimDto>>> GetAsync(CancellationToken cancellationToken)
        {
            var claims = await claimService.GetAllAsync(cancellationToken);

            return Ok(claims);
        }

        /// <summary>
        /// Retrieves a claim by its identifier.
        /// </summary>
        [HttpGet("get/{id}")]
        [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClaimDto>> GetAsync(string id, CancellationToken cancellationToken)
        {
            var claim = await claimService.GetByIdAsync(id, cancellationToken);

            if (claim is null)
                return NotFound();

            return Ok(claim);
        }

        /// <summary>
        /// Creates a claim.
        /// </summary>
        [HttpPost("create")]
        [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<ClaimDto>> CreateAsync([FromBody] ClaimDto claim, CancellationToken cancellationToken)
        {
            var created = await claimService.CreateAsync(claim, cancellationToken);

            return Ok(created);
        }

        /// <summary>
        /// Deletes a claim by its identifier.
        /// </summary>
        [HttpDelete("delete/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
        {
            var deleted = await claimService.DeleteAsync(id, cancellationToken);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
