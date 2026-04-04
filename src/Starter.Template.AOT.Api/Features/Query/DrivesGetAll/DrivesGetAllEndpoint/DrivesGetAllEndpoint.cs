using Microsoft.AspNetCore.Mvc;

namespace Starter.Template.AOT.Api.Features.Query.DrivesGetAll;

[ApiController]
[Route("drives")]
public class DrivesGetAllEndpoint(DrivesGetAllUseCase useCase, ILogger<DrivesGetAllEndpoint> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        logger.LogInformation("[DrivesGetAllEndpoint][GetAll] Receber requisição para listar todos os drives");

        var output = useCase.Execute();

        logger.LogInformation("[DrivesGetAllEndpoint][GetAll] Retornar {Count} drives", output.Drives.Count);

        return Ok(output);
    }
}
