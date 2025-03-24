using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TopicsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TopicsController> _logger;

        public TopicsController(ApplicationDbContext context, ILogger<TopicsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("add-topic")]
        public async Task<IActionResult> AddTopic([FromBody] TopicDto topicDto)
        {
            try
            {
                _logger.LogInformation("Получен запрос на добавление темы: {@TopicDto}", topicDto);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Некорректные данные темы: {@Errors}", ModelState.Values.SelectMany(v => v.Errors));
                    return BadRequest(ModelState);
                }

                // Поиск или создание раздела
                var section = await _context.Sections
                    .FirstOrDefaultAsync(s => s.SectionName == topicDto.SectionName);

                if (section == null)
                {
                    _logger.LogInformation("Создание нового раздела: {SectionName}", topicDto.SectionName);
                    section = new Section { SectionName = topicDto.SectionName };
                    _context.Sections.Add(section);
                    await _context.SaveChangesAsync();
                }

                // Преобразуем сложность из строки в число
                if (!int.TryParse(topicDto.Complexity, out int difficulty))
                {
                    _logger.LogWarning("Некорректное значение сложности: {Complexity}", topicDto.Complexity);
                    return BadRequest("Некорректное значение сложности");
                }

                var topic = new Topic
                {
                    TopicName = topicDto.TopicName,
                    Description = topicDto.Description,
                    Difficulty = difficulty,
                    TimeLimit = topicDto.ReadingTime,
                    AuthorName = topicDto.AuthorName,
                    AuthorEmail = topicDto.Email,
                    CreatedAt = DateTime.Parse(topicDto.CreationDate),
                    SectionId = section.Id
                };

                _logger.LogInformation("Попытка добавления темы в базу данных");
                _context.Topics.Add(topic);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Тема успешно добавлена в базу данных");

                return Ok(new { message = "Тема успешно добавлена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении темы");
                return StatusCode(500, new { message = "Ошибка при добавлении темы", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Topic>>> GetTopics()
        {
            try
            {
                _logger.LogInformation("Получен запрос на получение списка тем");
                
                // Получаем все разделы
                var sections = await _context.Sections.ToListAsync();
                _logger.LogInformation("Найдено разделов: {Count}", sections.Count);
                
                // Получаем все темы с включением разделов
                var topics = await _context.Topics
                    .Include(t => t.Section)
                    .Select(t => new
                    {
                        t.Id,
                        t.TopicName,
                        t.Description,
                        t.Difficulty,
                        t.TimeLimit,
                        t.AuthorName,
                        t.AuthorEmail,
                        t.CreatedAt,
                        t.SectionId,
                        Section = t.Section != null ? new { t.Section.SectionName } : null
                    })
                    .ToListAsync();
                    
                _logger.LogInformation("Успешно получено {Count} тем", topics.Count);
                
                // Логируем каждую тему
                foreach (var topic in topics)
                {
                    _logger.LogInformation("Тема: {TopicName}, Раздел: {SectionName}, ID раздела: {SectionId}", 
                        topic.TopicName, 
                        topic.Section?.SectionName ?? "Нет раздела", 
                        topic.SectionId);
                }
                
                return Ok(topics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка тем");
                return StatusCode(500, new { message = "Ошибка при получении списка тем", error = ex.Message });
            }
        }

        [HttpGet("section/{sectionName}")]
        public async Task<ActionResult<IEnumerable<Topic>>> GetTopicsBySection(string sectionName)
        {
            try
            {
                _logger.LogInformation("Получен запрос на получение тем для раздела: {SectionName}", sectionName);
                
                // Проверяем существование раздела
                var section = await _context.Sections
                    .FirstOrDefaultAsync(s => s.SectionName == sectionName);
                    
                if (section == null)
                {
                    _logger.LogWarning("Раздел {SectionName} не найден", sectionName);
                    return NotFound($"Раздел {sectionName} не найден");
                }

                _logger.LogInformation("Найден раздел с ID: {SectionId}", section.Id);
                
                // Получаем темы для раздела
                var topics = await _context.Topics
                    .Where(t => t.SectionId == section.Id)
                    .Select(t => new {
                        t.Id,
                        t.TopicName,
                        t.Description,
                        t.Difficulty,
                        t.TimeLimit,
                        t.AuthorName,
                        t.AuthorEmail,
                        t.CreatedAt,
                        t.SectionId
                    })
                    .ToListAsync();

                _logger.LogInformation("Успешно получено {Count} тем для раздела {SectionName}", topics.Count, sectionName);
                
                // Логируем каждую тему
                foreach (var topic in topics)
                {
                    _logger.LogInformation("Тема: {TopicName}, ID темы: {TopicId}, ID раздела: {SectionId}", 
                        topic.TopicName, 
                        topic.Id,
                        topic.SectionId);
                }
                
                return Ok(topics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении тем для раздела {SectionName}", sectionName);
                return StatusCode(500, new { message = "Ошибка при получении тем", error = ex.Message });
            }
        }

        [HttpGet("sections")]
        public async Task<IActionResult> GetSections()
        {
            try
            {
                _logger.LogInformation("Получение списка разделов");
                
                // Получаем разделы с количеством тем
                var sections = await _context.Sections
                    .Select(s => new
                    {
                        s.Id,
                        s.SectionName,
                        TopicCount = _context.Topics.Count(t => t.SectionId == s.Id)
                    })
                    .ToListAsync();
                    
                _logger.LogInformation($"Успешно получено {sections.Count} разделов");
                
                // Логируем каждый раздел
                foreach (var section in sections)
                {
                    _logger.LogInformation($"Раздел: {section.SectionName}, Количество тем: {section.TopicCount}");
                }
                
                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка разделов");
                return StatusCode(500, new { message = "Ошибка при получении списка разделов", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Topic>> GetTopic(int id)
        {
            try
            {
                _logger.LogInformation("Получен запрос на получение темы с ID: {Id}", id);
                
                // Проверяем существование темы
                var topic = await _context.Topics
                    .FirstOrDefaultAsync(t => t.Id == id);
                    
                if (topic == null)
                {
                    _logger.LogWarning("Тема с ID {Id} не найдена", id);
                    return NotFound($"Тема с ID {id} не найдена");
                }

                _logger.LogInformation("Найдена тема: {TopicName}, ID: {Id}, SectionId: {SectionId}", 
                    topic.TopicName, topic.Id, topic.SectionId);

                // Получаем информацию о разделе
                var section = await _context.Sections
                    .FirstOrDefaultAsync(s => s.Id == topic.SectionId);

                if (section == null)
                {
                    _logger.LogWarning("Раздел для темы {TopicName} не найден", topic.TopicName);
                    return StatusCode(500, "Раздел для темы не найден");
                }

                _logger.LogInformation("Найден раздел: {SectionName}", section.SectionName);

                // Создаем объект с данными темы и раздела
                var result = new
                {
                    topic.Id,
                    topic.TopicName,
                    topic.Description,
                    topic.Difficulty,
                    topic.TimeLimit,
                    topic.AuthorName,
                    topic.AuthorEmail,
                    topic.CreatedAt,
                    topic.SectionId,
                    Section = new { section.SectionName }
                };

                _logger.LogInformation("Успешно получена тема: {TopicName}", topic.TopicName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении темы с ID {Id}", id);
                return StatusCode(500, new { message = "Ошибка при получении темы", error = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Topic>>> SearchTopics(
            [FromQuery] string? query,
            [FromQuery] string? difficulty,
            [FromQuery] string? time)
        {
            try
            {
                _logger.LogInformation("Получен запрос на поиск тем. Параметры: Query={Query}, Difficulty={Difficulty}, Time={Time}", 
                    query, difficulty, time);

                // Начинаем с базового запроса
                var searchQuery = _context.Topics
                    .Include(t => t.Section)
                    .AsQueryable();

                // Применяем поиск по тексту, если запрос не пустой
                if (!string.IsNullOrWhiteSpace(query))
                {
                    searchQuery = searchQuery.Where(t =>
                        t.TopicName.Contains(query) ||
                        t.Description.Contains(query) ||
                        (t.Section != null && t.Section.SectionName.Contains(query)));
                }

                // Применяем фильтр по сложности, если указан
                if (!string.IsNullOrWhiteSpace(difficulty) && int.TryParse(difficulty, out int diffValue))
                {
                    searchQuery = searchQuery.Where(t => t.Difficulty == diffValue);
                }

                // Применяем фильтр по времени, если указан
                if (!string.IsNullOrWhiteSpace(time) && int.TryParse(time, out int timeValue))
                {
                    searchQuery = searchQuery.Where(t => t.TimeLimit <= timeValue);
                }

                // Получаем результаты
                var topics = await searchQuery
                    .Select(t => new
                    {
                        t.Id,
                        t.TopicName,
                        t.Description,
                        t.Difficulty,
                        t.TimeLimit,
                        t.AuthorName,
                        t.AuthorEmail,
                        t.CreatedAt,
                        t.SectionId,
                        Section = t.Section != null ? new { t.Section.SectionName } : null
                    })
                    .ToListAsync();

                _logger.LogInformation("Найдено тем: {Count}", topics.Count);

                // Логируем результаты поиска
                foreach (var topic in topics)
                {
                    _logger.LogInformation("Найдена тема: {TopicName}, Раздел: {SectionName}, Сложность: {Difficulty}, Время: {TimeLimit}",
                        topic.TopicName,
                        topic.Section?.SectionName ?? "Нет раздела",
                        topic.Difficulty,
                        topic.TimeLimit);
                }

                return Ok(topics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске тем");
                return StatusCode(500, new { message = "Ошибка при поиске тем", error = ex.Message });
            }
        }

        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<Topic>>> FilterTopics(
            [FromQuery] string? difficulty = null,
            [FromQuery] string? time = null)
        {
            try
            {
                _logger.LogInformation("Получен запрос на фильтрацию тем. Параметры: Difficulty={Difficulty}, Time={Time}", 
                    difficulty, time);

                // Начинаем с базового запроса
                var searchQuery = _context.Topics
                    .Include(t => t.Section)
                    .AsQueryable();

                // Применяем фильтр по сложности, если указан
                if (!string.IsNullOrWhiteSpace(difficulty) && int.TryParse(difficulty, out int diffValue))
                {
                    searchQuery = searchQuery.Where(t => t.Difficulty == diffValue);
                }

                // Применяем фильтр по времени, если указан
                if (!string.IsNullOrWhiteSpace(time) && int.TryParse(time, out int timeValue))
                {
                    searchQuery = searchQuery.Where(t => t.TimeLimit <= timeValue);
                }

                // Получаем результаты
                var topics = await searchQuery
                    .Select(t => new
                    {
                        t.Id,
                        t.TopicName,
                        t.Description,
                        t.Difficulty,
                        t.TimeLimit,
                        t.AuthorName,
                        t.AuthorEmail,
                        t.CreatedAt,
                        t.SectionId,
                        Section = t.Section != null ? new { t.Section.SectionName } : null
                    })
                    .ToListAsync();

                _logger.LogInformation("Найдено тем: {Count}", topics.Count);

                // Логируем результаты поиска
                foreach (var topic in topics)
                {
                    _logger.LogInformation("Найдена тема: {TopicName}, Раздел: {SectionName}, Сложность: {Difficulty}, Время: {TimeLimit}",
                        topic.TopicName,
                        topic.Section?.SectionName ?? "Нет раздела",
                        topic.Difficulty,
                        topic.TimeLimit);
                }

                return Ok(topics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при фильтрации тем");
                return StatusCode(500, new { message = "Ошибка при фильтрации тем", error = ex.Message });
            }
        }

        [HttpGet("section/{id}/topics")]
        public async Task<IActionResult> GetSectionTopics(int id)
        {
            try
            {
                _logger.LogInformation("Получен AJAX запрос на получение тем для раздела с ID: {Id}", id);
                
                var section = await _context.Sections
                    .FirstOrDefaultAsync(s => s.Id == id);
                    
                if (section == null)
                {
                    _logger.LogWarning("Раздел с ID {Id} не найден", id);
                    return NotFound(new { message = $"Раздел с ID {id} не найден" });
                }

                var topics = await _context.Topics
                    .Where(t => t.SectionId == section.Id)
                    .Include(t => t.Section)
                    .Select(t => new
                    {
                        t.Id,
                        t.TopicName,
                        t.Description,
                        t.Difficulty,
                        t.TimeLimit,
                        t.AuthorName,
                        t.AuthorEmail,
                        t.CreatedAt,
                        t.SectionId,
                        Section = new { t.Section!.SectionName }
                    })
                    .ToListAsync();

                _logger.LogInformation("Найдено {Count} тем для раздела {SectionName}", topics.Count, section.SectionName);
                
                return PartialView("_TopicsList", topics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении тем для раздела с ID {Id}", id);
                return StatusCode(500, new { message = "Ошибка при получении тем", error = ex.Message });
            }
        }

        [HttpGet("catalog")]
        public async Task<IActionResult> Catalog(
            [FromQuery] string? query = null,
            [FromQuery] string? difficulty = null,
            [FromQuery] string? time = null)
        {
            try
            {
                _logger.LogInformation("Получен запрос на отображение каталога. Параметры: Query={Query}, Difficulty={Difficulty}, Time={Time}", 
                    query, difficulty, time);
                
                // Получаем только разделы с количеством тем
                var sections = await _context.Sections
                    .Select(s => new
                    {
                        s.Id,
                        s.SectionName,
                        TopicCount = _context.Topics.Count(t => t.SectionId == s.Id)
                    })
                    .ToListAsync();
                    
                _logger.LogInformation("Успешно получено {Count} разделов", sections.Count);
                
                return View(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении каталога");
                return StatusCode(500, new { message = "Ошибка при получении каталога", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTopic(int id, [FromBody] Topic updatedTopic)
        {
            try
            {
                var existingTopic = await _context.Topics.FindAsync(id);
                if (existingTopic == null)
                {
                    return NotFound($"Тема с ID {id} не найдена");
                }

                // Обновляем свойства темы
                existingTopic.TopicName = updatedTopic.TopicName;
                existingTopic.Description = updatedTopic.Description;
                existingTopic.Difficulty = updatedTopic.Difficulty;
                existingTopic.TimeLimit = updatedTopic.TimeLimit;
                existingTopic.SectionId = updatedTopic.SectionId;

                // Сохраняем изменения
                await _context.SaveChangesAsync();

                // Логируем успешное обновление
                _logger.LogInformation($"Тема {id} успешно обновлена");

                return Ok(new { message = "Тема успешно обновлена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обновлении темы {id}");
                return StatusCode(500, "Произошла ошибка при обновлении темы");
            }
        }
    }

    public class TopicDto
    {
        public required string TopicName { get; set; }
        public required string SectionName { get; set; }
        public required string Description { get; set; }
        public required string AuthorName { get; set; }
        public required string Email { get; set; }
        public required string Complexity { get; set; }
        public required int ReadingTime { get; set; }
        public required string CreationDate { get; set; }
    }
} 