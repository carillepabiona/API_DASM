using API_DASM.Data;
using API_DASM.DTOs;
using API_DASM.Models;
using API_DASM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API_DASM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly ActivityLoggerService _logger;
        public CategoriesController(AppDbContext context, ActivityLoggerService logger)
        {
            _context = context;

            _logger = logger;
        }

        // =========================
        // GET ALL
        // =========================

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories =
                await _context.Categories
                    .Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Description
                    })
                    .ToListAsync();

            return Ok(categories);
        }

        // =========================
        // CREATE CATEGORY
        // =========================

        [HttpPost]
        public async Task<IActionResult> CreateCategory(
            CreateCategoryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest("Category name is required.");
                }

                var existing =
                    await _context.Categories
                        .FirstOrDefaultAsync(x =>
                            x.Name.ToLower() ==
                            request.Name.ToLower());

                if (existing != null)
                {
                    return BadRequest("Category already exists.");
                }

                var category = new Category
                {
                    Id = Guid.NewGuid(),

                    Name = request.Name,

                    Description = request.Description
                };

                _context.Categories.Add(category);

                await _context.SaveChangesAsync();

                await _logger.LogActivity(
                   request.CreatedBy,
                   "Create Category",
                   category.Name,
                   category.Id.ToString(),
                   $"Created category: {category.Name}");

                return Ok(new
                {
                    message = "Category created successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}