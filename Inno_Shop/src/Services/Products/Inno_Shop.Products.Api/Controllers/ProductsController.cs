using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Inno_Shop.Products.Application.Common;
using Inno_Shop.Products.Application.Features.Products.Commands;
using Inno_Shop.Products.Application.Features.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inno_Shop.Products.Api.Controllers
{
    /// <summary>
    /// Reads are public (requirement 2.2 only restricts create/update/delete); writes require a
    /// valid JWT and are limited to the owner of the product.
    /// </summary>
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProductsController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [ProducesResponseType<PagedResult<ProductDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] GetProductsQuery query, CancellationToken cancellationToken) =>
            Ok(await _mediator.Send(query, cancellationToken));

        [HttpGet("{id:guid}")]
        [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetProductByIdQuery(id), cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : ToProblem(result);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken cancellationToken)
        {
            var command = new CreateProductCommand(
                request.Name,
                request.Description,
                request.Price,
                request.IsAvailable,
                CurrentUserId);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return ToProblem(result);

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateProductCommand(
                id,
                request.Name,
                request.Description,
                request.Price,
                request.IsAvailable,
                CurrentUserId);

            var result = await _mediator.Send(command, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : ToProblem(result);
        }

        [HttpPatch("{id:guid}")]
        [Authorize]
        [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Patch(Guid id, PatchProductRequest request, CancellationToken cancellationToken)
        {
            var command = new PatchProductCommand(
                id,
                CurrentUserId,
                request.Name,
                request.Description,
                request.Price,
                request.IsAvailable);

            var result = await _mediator.Send(command, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : ToProblem(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DeleteProductCommand(id, CurrentUserId), cancellationToken);

            return result.IsSuccess ? NoContent() : ToProblem(result);
        }

        /// <summary>Owner id comes from the token, never from the request body.</summary>
        private string CurrentUserId =>
            User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Token does not contain user id.");

        private IActionResult ToProblem(Result result)
        {
            var (status, title) = result.Type switch
            {
                ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
                ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
                ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
                ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                _ => (StatusCodes.Status400BadRequest, "Bad Request"),
            };

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = string.Join(" ", result.Errors),
            };
            problem.Extensions["errors"] = result.Errors;

            return new ObjectResult(problem) { StatusCode = status };
        }
    }

    /// <summary>No <c>OwnerId</c> here on purpose: the owner always comes from the token.</summary>
    public record CreateProductRequest(
        string Name,
        string Description,
        decimal Price,
        bool IsAvailable = true);

    public record UpdateProductRequest(
        string Name,
        string Description,
        decimal Price,
        bool IsAvailable = true);

    /// <summary>Partial update: every field is optional, an omitted field stays unchanged.</summary>
    public record PatchProductRequest(
        string? Name = null,
        string? Description = null,
        decimal? Price = null,
        bool? IsAvailable = null);
}
