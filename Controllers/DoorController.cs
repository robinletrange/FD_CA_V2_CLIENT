namespace CLIENT.Controllers;

[ApiController]
[Route("doors")]
public class DoorController : ControllerBase
{
    private readonly IDoorRepository _repository;

    public DoorController(IDoorRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Door>>> GetAll()
    {
        return Ok(await _repository.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Door>> GetById(int id)
    {
        var door = await _repository.GetByIdAsync(id);

        if (door == null)
            return NotFound();

        return Ok(door);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, Door door)
    {
        if (id != door.Id)
            return BadRequest();

        Door? existingDoor = await _repository.GetByIdAsync(id);

        if (existingDoor == null)
            return NotFound();

        await _repository.UpdateAsync(door);

        return Ok(door);
    }
}