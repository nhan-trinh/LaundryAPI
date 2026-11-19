using LaundryAPI.Dtos;
using LaundryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryAPI.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(LoginService loginService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await loginService.LoginAsync(request.Username, request.Password, cancellationToken);
        if (token is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid username or password.");

        return Ok(new LoginResponse(token));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentAccount>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentAccount> Me() =>
        Ok(new CurrentAccount(User.Identity!.Name!, User.FindFirst("role")!.Value));
}
